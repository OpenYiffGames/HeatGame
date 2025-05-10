using dnlib.DotNet;
using System.Buffers;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace PatreonPatcher.Core.Helpers;

internal static partial class Utils
{
    public static class Pattern
    {
        public static bool MethodMatchesPattern(Stream assemblySource, MethodDef method, PatternScanner patternScanner)
        {
            if (!method.HasBody)
            {
                return false;
            }

            IMAGE_COR_ILMETHOD header;
            uint codeSize;
            lock (assemblySource)
            {
                header = ReadMethodHeader(method, assemblySource)
                     ?? throw new Exception("Failed to read method header");
                codeSize = header.MethodCodeSize();
            }
            byte[] pool = ArrayPool<byte>.Shared.Rent((int)codeSize);
            try
            {
                byte[]? methodCilBody = GetCilBodyBytes(assemblySource, method, pool, header)
                    ?? throw new Exception("Failed to read method body");
                int offset = patternScanner.Find(methodCilBody);
                return offset >= 0;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pool);
            }
        }

        public static bool MethodMatchesPattern(PEReader reader, MethodDefinition methodDef, PatternScanner patternScanner)
        {
            int rva = methodDef.RelativeVirtualAddress;
            if (rva == 0)
            {
                return false;
            }
            MethodBodyBlock ilMethod = reader.GetMethodBody(rva);
            if (ilMethod.Size == 0)
            {
                return false;
            }
            BlobReader ilReader = ilMethod.GetILReader();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(ilMethod.Size);
            try
            {
                ilReader.ReadBytes(ilReader.RemainingBytes, buffer, 0);
                return patternScanner.Find(buffer) != -1;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static int? FindMethodRVAMatchingPattern(
           Stream assemblyStream,
           string pattern,
           bool throwOnMultipleMatches = true)
        {
            int[] scanResult = FindMethodsRVAMatchingPattern(assemblyStream, pattern);
            return scanResult.Length == 0
                ? null
                : throwOnMultipleMatches
                ? scanResult.Single()
                : scanResult.FirstOrDefault();
        }

        public static int[] FindMethodsRVAMatchingPattern(
            Stream assemblyStream,
            string pattern)
        {
            PatternScanner scanner = new(pattern);
            PEReader reader = new(assemblyStream);
            if (!reader.HasMetadata)
            {
                throw new Exception("No metadata found in assembly");
            }

            MetadataReader metadata = reader.GetMetadataReader();
            MethodDefinitionHandleCollection methods = metadata.MethodDefinitions;

            ParallelQuery<int> scanResult = methods.AsParallel()
                .WithDegreeOfParallelism(4)
                .Select(metadata.GetMethodDefinition)
                .Where(methodDef => MethodMatchesPattern(reader, methodDef, scanner))
                .Select(methodDef => methodDef.RelativeVirtualAddress);

            return scanResult.ToArray();
        }

        public static MethodDef? FindTypeMethodMatchingPattern(Stream source, TypeDef typeDef, string pattern)
        {
            IList<MethodDef> methods = typeDef.Methods;
            PatternScanner scanner = new(pattern);
            return methods.FirstOrDefault(method => MethodMatchesPattern(source, method, scanner));
        }

        public static MethodDef[] FindTypeMethodsMatchingPattern(Stream source, TypeDef typeDef, string pattern)
        {
            IList<MethodDef> methods = typeDef.Methods;
            PatternScanner scanner = new(pattern);
            return methods.Where(method => MethodMatchesPattern(source, method, scanner))
                .ToArray();
        }
    }
}