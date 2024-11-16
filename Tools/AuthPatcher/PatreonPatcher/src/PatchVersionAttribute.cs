using dnlib.DotNet;

namespace PatreonPatcher;

internal partial class PatchVersionAttribute : IPatchVersion
{
    public override Guid Id { get; }
    public override int Minor { get; }
    public override int Major { get; }
    public override int Patch { get; }

    private PatchVersionAttribute(Guid id, int major, int minor, int patch)
    {
        Id = id;
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public static IPatchVersion[]? GetPatchVersions(AssemblyDef assemblyDef)
    {
        return assemblyDef.CustomAttributes
            .Where(x => x.TypeFullName == $"{Constants.PatchAttributeNamespace}.{Constants.PatchAttributeTypeName}")
            .Select(x => new PatchVersionAttribute(
                Guid.Parse(x.ConstructorArguments[0].Value.ToString() ?? ""),
                (int)x.ConstructorArguments[1].Value,
                (int)x.ConstructorArguments[2].Value,
                (int)x.ConstructorArguments[3].Value))
            .ToArray();
    }

    public static IPatchVersion? GetPatchVersion(AssemblyDef assemblyDef, Guid id)
    {
        return GetPatchVersions(assemblyDef)?.FirstOrDefault(x => x.Id == id);
    }

    public static CustomAttribute Create(ModuleDef module, string patchId, int major, int minor, int patch)
    {
        var attributeRef = new TypeRefUser(module, Constants.PatchAttributeNamespace, Constants.PatchAttributeTypeName);
        var typeDef = attributeRef.Resolve();
        if (typeDef is null)
        {
            var builder = new Builder(module);
            typeDef = builder.CreateAttributeType();
        }
        var ctor = typeDef.FindConstructors().First();
        return Create(ctor, module, patchId, major, minor, patch);
    }

    private static CustomAttribute Create(ICustomAttributeType ctor, ModuleDef module, string patchId, int major, int minor, int patch)
    {
        return new CustomAttribute(ctor, new List<CAArgument>
        {
            new(module.CorLibTypes.String, patchId),
            new(module.CorLibTypes.Int32, major),
            new(module.CorLibTypes.Int32, minor),
            new(module.CorLibTypes.Int32, patch)
        });
    }
}


