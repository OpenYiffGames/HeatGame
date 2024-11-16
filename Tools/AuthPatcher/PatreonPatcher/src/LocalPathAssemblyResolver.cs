using dnlib.DotNet;
using PatreonPatcher.Helpers;

namespace PatreonPatcher;

class LocalPathAssemblyResolver : IAssemblyResolver
{
    private readonly string basePath;

    public LocalPathAssemblyResolver(string basePath)
    {
        this.basePath = basePath;
    }

    public AssemblyDef? Resolve(IAssembly assembly, ModuleDef sourceModule)
    {
        var path = Path.Combine(basePath, assembly.Name + ".dll");
        if (!File.Exists(path))
        {
            return null;
        }
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var assemblyName = Utils.GetAssemblyName(stream);
        if (assembly.Version == assemblyName.Version)
        {
            return AssemblyDef.Load(stream, new ModuleContext(this));
        }
        return null;
    }
}

