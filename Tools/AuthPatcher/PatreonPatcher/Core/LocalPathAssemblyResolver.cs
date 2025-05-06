using dnlib.DotNet;
using PatreonPatcher.Core.Helpers;

namespace PatreonPatcher.Core;

internal class LocalPathAssemblyResolver : IAssemblyResolver
{
    private readonly string basePath;

    public LocalPathAssemblyResolver(string basePath)
    {
        this.basePath = basePath;
    }

    public AssemblyDef? Resolve(IAssembly assembly, ModuleDef sourceModule)
    {
        string path = Path.Combine(basePath, assembly.Name + ".dll");
        if (!File.Exists(path))
        {
            return null;
        }
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        System.Reflection.AssemblyName assemblyName = Utils.GetAssemblyName(stream);
        return assembly.Version == assemblyName.Version ? AssemblyDef.Load(stream, new ModuleContext(this)) : null;
    }
}

