using dnlib.DotNet;
using System.Buffers;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace PatreonPatcher;

static partial class Utils
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
                header = ReadMethodHeader(method, assemblySource, out var isBigHeader)
                     ?? throw new Exception("Failed to read method header");
                codeSize = isBigHeader ? header.Fat_CodeSize : header.Tiny_Flags_CodeSize;
            }
            var pool = ArrayPool<byte>.Shared.Rent((int)codeSize);
            try
            {
                byte[]? methodCilBody = GetCilBodyBytes(assemblySource, method, pool) ?? throw new Exception("Failed to read method body");
                var offset = patternScanner.Find(methodCilBody);
                return offset >= 0;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pool);
            }
        }

        public static bool MethodMatchesPattern(PEReader reader, MethodDefinition methodDef, PatternScanner patternScanner)
        {
            var rva = methodDef.RelativeVirtualAddress;
            if (rva == 0)
            {
                return false;
            }
            var ilMethod = reader.GetMethodBody(rva);
            if (ilMethod.Size == 0)
            {
                return false;
            }
            var ilReader = ilMethod.GetILReader();
            var buffer = ArrayPool<byte>.Shared.Rent(ilMethod.Size);
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
            var scanResult = FindMethodsRVAMatchingPattern(assemblyStream, pattern);
            if (scanResult.Length == 0)
            {
                return null;
            }
            if (scanResult.Length > 1 && throwOnMultipleMatches)
            {
                throw new Exception($"Multiple methods found for pattern {pattern}");
            }
            return scanResult[0];
        }

        public static int[] FindMethodsRVAMatchingPattern(
            Stream assemblyStream,
            string pattern)
        {
            var scanner = new PatternScanner(pattern);
            var reader = new PEReader(assemblyStream);
            if (!reader.HasMetadata)
            {
                throw new Exception("No metadata found in assembly");
            }

            var metadata = reader.GetMetadataReader();
            var methods = metadata.MethodDefinitions;

            var scanResult = methods.AsParallel()
                .WithDegreeOfParallelism(4)
                .Select(metadata.GetMethodDefinition)
                .Where(methodDef => MethodMatchesPattern(reader, methodDef, scanner))
                .Select(methodDef => methodDef.RelativeVirtualAddress);

            return scanResult.ToArray();
        }

        public static MethodDef? FindTypeMethodMatchingPattern(Stream source, TypeDef typeDef, string pattern)
        {
            var methods = typeDef.Methods;
            var scanner = new PatternScanner(pattern);
            return methods.FirstOrDefault(method => MethodMatchesPattern(source, method, scanner));
        }

        public static MethodDef[] FindTypeMethodsMatchingPattern(Stream source, TypeDef typeDef, string pattern)
        {
            var methods = typeDef.Methods;
            var scanner = new PatternScanner(pattern);
            return methods.Where(method => MethodMatchesPattern(source, method, scanner))
                .ToArray();
        }
    }
}