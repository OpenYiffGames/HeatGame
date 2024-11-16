using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace PatreonPatcher;

internal partial class PatchVersionAttribute
{
    class Builder
    {
        private readonly ModuleDef module;

        private TypeDefUser? patchedAttbType;
        private FieldDefUser? patchIdBkField;
        private FieldDefUser? majorVersionBkField;
        private FieldDefUser? minorVersionBkField;
        private FieldDefUser? patchVersionBkField;

        public Builder(ModuleDef module)
        {
            this.module = module;
        }

        public TypeDef CreateAttributeType()
        {
            BuildAttributeType();
            BuildProperties();
            BuildConstructor();
            return patchedAttbType ?? throw new Exception("patchedAttbType is null");
        }

        private void BuildAttributeType()
        {
            var attrbBaseType = module.CorLibTypes.GetTypeRef(nameof(System), nameof(Attribute));
            if (!module.GetTypeRefs()
                .Any(x => x.ReflectionFullName == attrbBaseType.ReflectionFullName))
            {
                module.Import(attrbBaseType);
            }

            patchedAttbType = new TypeDefUser(Constants.PatchAttributeNamespace, Constants.PatchAttributeTypeName, module.CorLibTypes.Object.TypeDefOrRef)
            {
                Attributes = TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoLayout |
                             TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Class,
                BaseType = attrbBaseType,
            };
            module.Types.Add(patchedAttbType);
        }

        private void BuildProperties()
        {
            if (patchedAttbType is null)
            {
                throw new InvalidOperationException("Attribute type is not created yet");
            }
            CreateProperty("PatchId", patchedAttbType, module.CorLibTypes.String, ref patchIdBkField);
            CreateProperty("Major", patchedAttbType, module.CorLibTypes.Int32, ref majorVersionBkField);
            CreateProperty("Minor", patchedAttbType, module.CorLibTypes.Int32, ref minorVersionBkField);
            CreateProperty("Patch", patchedAttbType, module.CorLibTypes.Int32, ref patchVersionBkField);
        }

        private void BuildConstructor()
        {
            if (patchedAttbType is null)
            {
                throw new InvalidOperationException("Attribute type is not created yet");
            }
            var attrbBaseType = module.CorLibTypes.GetTypeRef(nameof(System), nameof(Attribute));
            var attibuteCtor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), attrbBaseType);

            var ctorArgs = new[] { module.CorLibTypes.String, module.CorLibTypes.Int32, module.CorLibTypes.Int32, module.CorLibTypes.Int32 };
            var patchedAttbCtor = new MethodDefUser(".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, ctorArgs),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.ReuseSlot)
            {
                Body = new CilBody(true,
                [
                    OpCodes.Ldarg_0.ToInstruction(),
                    OpCodes.Call.ToInstruction(attibuteCtor),
                    OpCodes.Ldarg_0.ToInstruction(),
                    OpCodes.Ldarg_1.ToInstruction(),
                    OpCodes.Stfld.ToInstruction(patchIdBkField),
                    OpCodes.Ldarg_0.ToInstruction(),
                    OpCodes.Ldarg_2.ToInstruction(),
                    OpCodes.Stfld.ToInstruction(majorVersionBkField),
                    OpCodes.Ldarg_0.ToInstruction(),
                    OpCodes.Ldarg_3.ToInstruction(),
                    OpCodes.Stfld.ToInstruction(minorVersionBkField),
                    OpCodes.Ldarg_0.ToInstruction(),
                    OpCodes.Ldarg_S.ToInstruction(new Parameter(4)),
                    OpCodes.Stfld.ToInstruction(patchVersionBkField),
                    OpCodes.Ret.ToInstruction()
                ], [], [])
            };

            patchedAttbType.Methods.Add(patchedAttbCtor);
        }

        private static string GetBackingFieldName(string propertyName) => $"<{propertyName}>k__BackingField";

        private static FieldDefUser CreateBackingField(string name, TypeDefUser type, CorLibTypeSig fieldSiginature)
        {
            var field = new FieldDefUser(GetBackingFieldName(name), new FieldSig(fieldSiginature), FieldAttributes.Private | FieldAttributes.InitOnly);
            type.Fields.Add(field);
            return field;
        }

        private static void CreateProperty(string name, TypeDefUser type, CorLibTypeSig propertySignature, ref FieldDefUser? backingField)
        {
            backingField ??= CreateBackingField(name, type, propertySignature);
            var property = new PropertyDefUser(name, new PropertySig(true, propertySignature))
            {
                GetMethod = new MethodDefUser($"get_{name}", MethodSig.CreateInstance(propertySignature), MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.ReuseSlot)
                {
                    Body = new CilBody(true,
                    [
                        OpCodes.Ldarg_0.ToInstruction(),
                        OpCodes.Ldfld.ToInstruction(backingField),
                        OpCodes.Ret.ToInstruction()
                    ], [], [])
                }
            };

            type.Properties.Add(property);
            type.Methods.Add(property.GetMethod);
        }
    }
}