using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using System.Diagnostics.CodeAnalysis;

namespace PatreonPatcher.src;

internal class Patcher
{
    private readonly Guid patchId = Guid.Parse("e8f238ce-a43c-4b61-86c5-0608a5f169fc");

    private readonly ModuleContext _context;
    private readonly string _assembliesPath;

    private bool _methodsLoaded = false;
    MethodDef? _writeAuthFunction;
    MethodDef? _invokeSuccessFunction;
    MethodDef? _bypassAuthFunction;
    MethodDef? _awakeFunction;

    private Patcher(string assembliesPath, ModuleContext moduleContext)
    {
        _context = moduleContext;
        _assembliesPath = assembliesPath;
    }

    [RequiresDynamicCode("Calls PatreonPatcher.src.Patcher.IsPatched()")]
    public async Task<bool> PatchAsync()
    {
        if (!LoadMethods())
        {
            Logger.Error("Failed to load methods.");
            return false;
        }

        if (IsPatched())
        {
            Logger.Info("Game already patched.");
            return true;
        }

        Instruction[] callBypassAuth =
        [
            OpCodes.Ldarg_0.ToInstruction(),
            OpCodes.Call.ToInstruction(_bypassAuthFunction),
            OpCodes.Ret.ToInstruction()
        ];

        var awakeIlCode = new LinkedList<Instruction>(_awakeFunction!.Body.Instructions);
        var ip = awakeIlCode.First;
        while (ip != null)
        {
            if (ip.Value.OpCode == OpCodes.Ret)
            {
                Logger.Info($"Patching ret instruction at {_awakeFunction.RVA:X}[{ip.Value.GetOffset():X}]");
                AddInstructionsAfter(ip.Previous!, callBypassAuth);
                awakeIlCode.Remove(ip);
                break;
            }
            ip = ip.Next;
            void AddInstructionsAfter(LinkedListNode<Instruction> target, Instruction[] instructions)
            {
                foreach (var instr in instructions)
                {
                    awakeIlCode.AddAfter(target, instr);
                    target = target.Next!;
                }
            }
        }
        _awakeFunction.Body.Instructions.Clear();
        foreach (var instr in awakeIlCode)
        {
            _awakeFunction.Body.Instructions.Add(instr);
        }

        var assembly = _writeAuthFunction!.DeclaringType.DefinitionAssembly as AssemblyDef
            ?? throw new Exception("Failed to get auth assembly");

        var attb = PatchVersionAttribute.Create(assembly.ManifestModule, patchId.ToString(), 0, 0, 0);
        assembly.CustomAttributes.Add(attb);

        var assemblyName = assembly.Name + ".dll";

        var assemblyPath = GetAssemblyPath(assemblyName);
        File.Move(assemblyPath, assemblyPath + ".bak");
        try
        {
            Logger.Info($"Writing patched assembly to {assemblyPath}");
            await Task.Run(() => assembly.Write(assemblyPath));
        }
        catch (Exception)
        {
            Logger.Error("Failed to write patched assembly. Restoring backup.");
            File.Move(assemblyPath + ".bak", assemblyPath);
            throw;
        }
        return true;
    }

    private  bool LoadMethods()
    {
        if (_methodsLoaded)
        {
            return true;
        }

        var dlls= Directory.GetFiles(_assembliesPath, "*.dll");
        var assemblies = dlls.Select(OpenAssembly).ToList();
        Logger.Info($"Found {assemblies.Count} assemblies in {_assembliesPath}");

        Stream? authAssemblyStream = null;
        MethodDef? invokeSuccessMethod = null;
        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 2
        };
        Parallel.ForEach(assemblies, options, (assembly, loop) =>
        {
            int? rva = Utils.Pattern.FindMethodRVAMatchingPattern(assembly, Constants.Patterns.InvokeSuccessFunction);
            if (rva is null)
            {
                return;
            }

            var assemblyDef = AssemblyDef.Load(assembly, _context);
            var methodDef = assemblyDef.ManifestModule
                .GetTypes()
                .SelectMany(x => x.Methods)
                .Single(x => x.RVA == (RVA)rva.Value);

            lock (options)
            {
                if (invokeSuccessMethod is null)
                {
                    invokeSuccessMethod = methodDef;
                    authAssemblyStream = assembly;
                    loop.Break();
                    Logger.Info($"Found {invokeSuccessMethod.FullName} at {invokeSuccessMethod.RVA:X}");
                }
                else
                {
                    throw new Exception("Multiple methods found for InvokeSuccessFunction");
                }
            }
        });

        if (invokeSuccessMethod is null || authAssemblyStream is null)
        {
            Logger.Error("Failed to find InvokeSuccessFunction");
            return false;
        }
        var authType = invokeSuccessMethod.DeclaringType;
        var authAssembly = authType.DefinitionAssembly as AssemblyDef;

        uint playerPrefsHasKeyToken = Utils.FindMethodRefToken(
            authAssembly!,
            Constants.UnityEngineTypes.PlayerPrefs,
            Constants.UnityEngineTypes.PlayerPrefs_HasKey)!.Value.Raw;
        Logger.Info($"Found PlayerPrefs.HasKey token: {playerPrefsHasKeyToken:X}");

        var hwidStringToken = Utils.FindUserStringToken(authAssembly!, "HWID")!.Value.Raw;
        Logger.Info($"Found HWID string token: {hwidStringToken:X}");

        var patternBuilder = new PatternBuilder(Constants.Patterns.WriteAuthFunction);
        string writeAuthPattern = patternBuilder.Render(new Dictionary<string, object?>()
        {
            { "token", Utils.SwapEndianness(playerPrefsHasKeyToken) },
            { "string_token", Utils.SwapEndianness(hwidStringToken) }
        });
        var writeAuthMethod = Utils.Pattern.FindTypeMethodMatchingPattern(authAssemblyStream, authType, writeAuthPattern);
        if (writeAuthMethod is null)
        {
            Logger.Error("Failed to find WriteAuthFunction");
            return false;
        }
        Logger.Info($"Found {writeAuthMethod.FullName} at {writeAuthMethod.RVA:X}");

        var awakeMethod = authType.FindMethod(Constants.UnityEngineTypes.MonoBehaviour_Awake);
        if (awakeMethod is null)
        {
            Logger.Error("Failed to find Awake method");
            return false;
        }
        Logger.Info($"Found {awakeMethod.FullName} at {awakeMethod.RVA:X}");

        var bypassAuthFunctions = Utils.Pattern.FindTypeMethodsMatchingPattern(authAssemblyStream, authType, Constants.Patterns.BypassAuthFunction);
        if (bypassAuthFunctions.Length == 0)
        {
            Logger.Error("Failed to find BypassAuthFunction");
            return false;
        }

        MethodDef? bypassAuthMethod = null;
        foreach (var method in bypassAuthFunctions)
        {
            var isCorrectMethod = method.Body.Instructions
                .Where(x => x.OpCode == OpCodes.Call &&
                    x.Operand is IMethodDefOrRef)
                .Select(x => (x.Operand as IMethodDefOrRef).ResolveMethodDef())
                .Any(def => def.RVA == writeAuthMethod.RVA 
                    || def.RVA == invokeSuccessMethod.RVA);

            if (isCorrectMethod)
            {
                bypassAuthMethod = method;
                Logger.Info($"Found {bypassAuthMethod.FullName} at {bypassAuthMethod.RVA:X}");
                break;
            }
        }

        if (bypassAuthMethod is null)
        {
            Logger.Error("Failed to find BypassAuthFunction");
            return false;
        }

        _writeAuthFunction = writeAuthMethod;
        _invokeSuccessFunction = invokeSuccessMethod;
        _bypassAuthFunction = bypassAuthMethod;
        _awakeFunction = awakeMethod;
        _methodsLoaded = true;
        return true;
    }

    [RequiresDynamicCode("Calls PatreonPatcher.src.Patcher.LoadMethods()")]
    private bool IsPatched()
    {
        if (!_methodsLoaded && !LoadMethods())
        {
            Logger.Error("Failed to load methods.");
            return false;
        }

        if (_writeAuthFunction!.DeclaringType.DefinitionAssembly is not AssemblyDef assembly)
        {
            Logger.Error("Failed to get AssemblyDef from the auth assembly");
            return false;
        }
        var version = PatchVersionAttribute.GetPatchVersion(assembly, patchId);
        return version != null;
    }

    private string GetAssemblyPath(string assemblyName)
    {
        return Path.Combine(_assembliesPath, assemblyName);
    }

    private Stream OpenAssembly(string assemblyName)
    {
        if (!Path.IsPathFullyQualified(assemblyName))
        {
            assemblyName = GetAssemblyPath(assemblyName);
        }
        var fs = new FileStream(assemblyName, FileMode.Open, FileAccess.Read, FileShare.Read);
        return fs;
    }

    public static Patcher Create(string gameExecutable)
    {
        if (!File.Exists(gameExecutable))
        {
            throw new FileNotFoundException($"Game executable not found at {gameExecutable}");
        }

        string gameBaseDirectory = Path.GetDirectoryName(gameExecutable)!;
        string exeName = Path.GetFileNameWithoutExtension(gameExecutable);

        var assembliesDirectory = Path.Combine(gameBaseDirectory, exeName + Constants.Directories.AssembliesDirectory);
        if (!Directory.Exists(assembliesDirectory))
        {
            throw new DirectoryNotFoundException($"Assemblies directory not found at {assembliesDirectory}");
        }

        var unityEngineAssemblyPath = Path.Combine(assembliesDirectory, Constants.UnityEngineAssembly);
        if (!File.Exists(unityEngineAssemblyPath))
        {
            throw new FileNotFoundException($"UnityEngine assembly not found at {unityEngineAssemblyPath}");
        }

        var resolver = new LocalPathAssemblyResolver(assembliesDirectory);
        var moduleContext = new ModuleContext(resolver);

        return new Patcher(assembliesDirectory, moduleContext);
    }
}
