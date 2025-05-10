using dnlib.DotNet;
using dnlib.IO;
using PatreonPatcher.Core.Logging;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PatreonPatcher.Core.Helpers;

internal static partial class Utils
{
    public static byte[]? GetCilBodyBytes(Stream assemblySource, MethodDef method, byte[]? buffer = null, IMAGE_COR_ILMETHOD? methodHeader = null)
    {
        if (!method.HasBody)
        {
            return null;
        }
        lock (assemblySource)
        {
            methodHeader ??= ReadMethodHeader(method, assemblySource);
            if (methodHeader == null)
            {
                return null;
            }
            bool isFatHeader = methodHeader.IsFatMethodBody();
            uint codeSize = methodHeader.MethodCodeSize();
            byte[] methodCilBody = buffer ?? new byte[codeSize];

            int headerSize = Marshal.SizeOf<IMAGE_COR_ILMETHOD>();
            int realHeaderSize = (methodHeader.Value.Fat_FlagsAndSize >> 12) * 4; // EMCA 335 II.25.4.3 - count of 4-byte integers
            if (isFatHeader && headerSize != realHeaderSize)
            {
                // this will never happen if the CLR is not modified
                Log.Warning("""
                    The readed method header size is not equal to the header size pointed by the metadata. This could be an update in the EMCA especification, bad image file or obsfucation.
                    Method: {0} in {1}
                    """, method.FullName, method.DeclaringType.DefinitionAssembly.FullName);
                Log.Debug("Header size: {0}, readed header size: {1}", headerSize, realHeaderSize);
                int offset = realHeaderSize - headerSize;
                Log.Debug("Skipping {0} bytes", offset);
                _ = assemblySource.Seek(offset, SeekOrigin.Current);
            }
            _ = assemblySource.Read(methodCilBody.AsSpan(0, (int)codeSize));
            return methodCilBody;
        }
    }

    public static IMAGE_COR_ILMETHOD? ReadMethodHeader(MethodDef method, Stream stream)
    {
        long bodyOffset = RVA2FileOffset(method);
        if (bodyOffset < 0)
        {
            Log.Warning("Invalid method RVA: {0} on {1}", method.RVA, method.FullName);
            return null;
        }
        _ = stream.Seek(bodyOffset, SeekOrigin.Begin);

        int ImageCorILMethodSize = Marshal.SizeOf<IMAGE_COR_ILMETHOD>();
        Span<byte> buffer = stackalloc byte[ImageCorILMethodSize];
        int headerType = stream.ReadByte();
        if (headerType == -1)
        {
            throw new EndOfStreamException("Failed to read method header");
        }
        buffer[0] = (byte)headerType;
        headerType &= 0b11; // mask the first 2 bits (header type values: EMCA 335 II.25.4.1)
        bool isFatMethodBody = headerType is 0x3;
        if (isFatMethodBody != method.Body.IsBigHeader)
        {
            throw new BadImageFormatException("Invalid header type");
        }
        if (isFatMethodBody)
        {
            int size = stream.Read(buffer[1..]) + 1;
            int expectedSize = (buffer[1] >> 4) * 4; // EMCA 335 II.25.4.3 - count of 4-byte integers
            if (size != expectedSize)
            {
                throw new BadImageFormatException("Invalid header size");
            }
        }

        IntPtr ptr = Marshal.AllocHGlobal(ImageCorILMethodSize);
        try
        {
            Span<byte> unmanagedBufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), ptr), ImageCorILMethodSize);
            buffer.CopyTo(unmanagedBufferSpan);
            return Marshal.PtrToStructure<IMAGE_COR_ILMETHOD>(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public static uint SwapEndianness(uint value)
    {
        return ((value & 0x000000FFU) << 24) | ((value & 0x0000FF00U) << 8) | ((value & 0x00FF0000U) >> 8) | ((value & 0xFF000000U) >> 24);
    }

    public static long RVA2FileOffset(MethodDef methodDef)
    {
        return methodDef.Module is not ModuleDefMD module ? -1 : (long)module.Metadata.PEImage.ToFileOffset(methodDef.RVA);
    }

    public static string GetLocalStorageDirectory(bool ensureExists = true)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string assemblyGuid = (assembly.GetCustomAttribute<GuidAttribute>()?.Value) ?? assembly.GetName().FullName;
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            assemblyGuid);
        if (ensureExists && !Directory.Exists(path))
        {
            _ = Directory.CreateDirectory(path);
        }
        return path;
    }

    public static AssemblyName GetAssemblyName(Stream peFileS)
    {
        long pos = peFileS.Position;
        try
        {
            using PEReader reader = new(peFileS, PEStreamOptions.LeaveOpen);
            MetadataReader metadata = reader.GetMetadataReader();
            return metadata.GetAssemblyDefinition().GetAssemblyName();
        }
        catch
        {
            throw;
        }
        finally
        {
            peFileS.Position = pos;
        }
    }

    public static MDToken? FindMethodRefToken(this AssemblyDef assemblyDef, string typeName, string methodName)
    {
        return assemblyDef.ManifestModule
            .GetMemberRefs()
            .Single(@ref => @ref.IsMethodRef &&
                @ref.DeclaringType.Name == typeName &&
                @ref.Name == methodName).MDToken;
    }

    public static MDToken? FindUserStringToken(this AssemblyDef assemblyDef, string value)
    {
        dnlib.DotNet.MD.USStream usStream = (assemblyDef.ManifestModule as ModuleDefMD)!
            .Metadata.USStream;

        DataReader usReader = usStream.CreateReader();
        usReader.Position++; // First byte is zero

        uint lenght = usStream.StreamLength;
        uint rawToken = 1; // The RID starts in 1
        while (lenght > 0)
        {
            uint pos = usReader.Position;
            string str = usReader.ReadUserStringHeap();
            if (str.Length > 0 && str == value)
            {
                rawToken = (0x70 << 24) | rawToken;
                return new MDToken(rawToken);
            }
            lenght -= usReader.Position - pos;
            rawToken++;
        }

        return null;
    }

    private static string ReadUserStringHeap(this ref DataReader reader)
    {
        // https://github.com/dotnet/runtime/blob/3b91ac601980f3cc35e1d8687e7235e874ffc8ea/src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/Internal/UserStringHeap.cs#L27
        int strLen = (int)(reader.ReadCompressedUInt32() & ~1);
        if (strLen < 1)
        {
            return string.Empty;
        }
        string str = reader.ReadString(strLen, Encoding.Unicode);
        byte terminator = reader.ReadByte();
        return terminator != GetStringTerminalByte(str) ? throw new Exception("Unexpected byte found in user string heap terminator") : str;

        // ECMA 335 - II.24.2.4
        static byte GetStringTerminalByte(string str)
        {
            foreach (char c in str)
            {
                if (c >> 8 != 0)
                {
                    return 0x01;
                }
                byte low = (byte)(c & 0xFF);
                if (low is (>= 0x01 and <= 0x08) or
                    (>= 0x0E and <= 0x1F) or
                    0x27 or 0x2D or 0x7F)
                {
                    return 0x01;
                }
            }
            return 0x00;
        }
    }
}