using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using PatreonPatcher.Core.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace PatreonPatcher.Core;

internal class Patcher
{
    private readonly Guid patchId = Guid.Parse("e8f238ce-a43c-4b61-86c5-0608a5f169fc");

    private readonly ModuleContext _context;
    private readonly string _assembliesPath;

    private bool _methodsLoaded = false;
    private MethodDef? _writeAuthFunction;
    private MethodDef? _invokeSuccessFunction;
    private MethodDef? _bypassAuthFunction;
    private MethodDef? _awakeFunction;

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

        LinkedList<Instruction> awakeIlCode = new(_awakeFunction!.Body.Instructions);
        LinkedListNode<Instruction>? ip = awakeIlCode.First;
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
                foreach (Instruction instr in instructions)
                {
                    _ = awakeIlCode.AddAfter(target, instr);
                    target = target.Next!;
                }
            }
        }
        _awakeFunction.Body.Instructions.Clear();
        foreach (Instruction instr in awakeIlCode)
        {
            _awakeFunction.Body.Instructions.Add(instr);
        }

        AssemblyDef assembly = _writeAuthFunction!.DeclaringType.DefinitionAssembly as AssemblyDef
            ?? throw new Exception("Failed to get auth assembly");

        CustomAttribute attb = PatchVersionAttribute.Create(assembly.ManifestModule, patchId.ToString(), 0, 0, 0);
        assembly.CustomAttributes.Add(attb);

        string assemblyName = assembly.Name + ".dll";

        string assemblyPath = GetAssemblyPath(assemblyName);
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

    private bool LoadMethods()
    {
        if (_methodsLoaded)
        {
            return true;
        }

        string[] modules = Directory.GetFiles(_assembliesPath, "*.dll");
        Logger.Info($"Found {modules.Length} assemblies in {_assembliesPath}");

        Stream? authAssemblyStream = null;
        MethodDef? invokeSuccessMethod = null;
        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = 2
        };
        _ = Parallel.ForEach(modules, options, (modulePath, loop) =>
        {
            Stream assembly = OpenAssembly(modulePath);

            int? rva = Utils.Pattern.FindMethodRVAMatchingPattern(assembly, Constants.Patterns.InvokeSuccessFunction);
            if (rva is null)
            {
                assembly.Dispose();
                return;
            }

            AssemblyDef assemblyDef = AssemblyDef.Load(assembly, _context);
            MethodDef methodDef = assemblyDef.ManifestModule
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
        TypeDef authType = invokeSuccessMethod.DeclaringType;
        AssemblyDef? authAssembly = authType.DefinitionAssembly as AssemblyDef;

        uint playerPrefsHasKeyToken = authAssembly!.FindMethodRefToken(
            Constants.UnityEngineTypes.PlayerPrefs,
            Constants.UnityEngineTypes.PlayerPrefs_HasKey)!.Value.Raw;
        Logger.Info($"Found PlayerPrefs.HasKey token: {playerPrefsHasKeyToken:X}");

        uint hwidStringToken = authAssembly!.FindUserStringToken("HWID")!.Value.Raw;
        Logger.Info($"Found HWID string token: {hwidStringToken:X}");

        PatternBuilder patternBuilder = new(Constants.Patterns.WriteAuthFunction);
        string writeAuthPattern = patternBuilder.Render(new Dictionary<string, object?>()
        {
            { "token", Utils.SwapEndianness(playerPrefsHasKeyToken) },
            { "string_token", Utils.SwapEndianness(hwidStringToken) }
        });
        MethodDef? writeAuthMethod = Utils.Pattern.FindTypeMethodMatchingPattern(authAssemblyStream, authType, writeAuthPattern);
        if (writeAuthMethod is null)
        {
            Logger.Error("Failed to find WriteAuthFunction");
            authAssemblyStream.Dispose();
            return false;
        }
        Logger.Info($"Found {writeAuthMethod.FullName} at {writeAuthMethod.RVA:X}");

        MethodDef? awakeMethod = authType.FindMethod(Constants.UnityEngineTypes.MonoBehaviour_Awake);
        if (awakeMethod is null)
        {
            Logger.Error("Failed to find Awake method");
            return false;
        }
        Logger.Info($"Found {awakeMethod.FullName} at {awakeMethod.RVA:X}");

        MethodDef[] bypassAuthFunctions = Utils.Pattern.FindTypeMethodsMatchingPattern(authAssemblyStream, authType, Constants.Patterns.BypassAuthFunction);
        if (bypassAuthFunctions.Length == 0)
        {
            Logger.Error("Failed to find BypassAuthFunction");
            authAssemblyStream.Dispose();
            return false;
        }

        // Dispose the stream after we're done with it
        authAssemblyStream.Dispose();

        MethodDef? bypassAuthMethod = null;
        foreach (MethodDef method in bypassAuthFunctions)
        {
            bool isCorrectMethod = method.Body.Instructions
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
        IPatchVersion? version = PatchVersionAttribute.GetPatchVersion(assembly, patchId);
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
        FileStream fs = new(assemblyName, FileMode.Open, FileAccess.Read, FileShare.Read);
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

        string assembliesDirectory = Path.Combine(gameBaseDirectory, exeName + Constants.Directories.AssembliesDirectory);
        if (!Directory.Exists(assembliesDirectory))
        {
            throw new DirectoryNotFoundException($"Assemblies directory not found at {assembliesDirectory}");
        }

        string unityEngineAssemblyPath = Path.Combine(assembliesDirectory, Constants.UnityEngineAssembly);
        if (!File.Exists(unityEngineAssemblyPath))
        {
            throw new FileNotFoundException($"UnityEngine assembly not found at {unityEngineAssemblyPath}");
        }

        LocalPathAssemblyResolver resolver = new(assembliesDirectory);
        ModuleContext moduleContext = new(resolver);

        return new Patcher(assembliesDirectory, moduleContext);
    }
}
