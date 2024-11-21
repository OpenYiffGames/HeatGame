using dnlib.DotNet;
using dnlib.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PatreonPatcher.Helpers;

static partial class Utils
{
    public static byte[]? GetCilBodyBytes(Stream assemblySource, MethodDef method, byte[]? buffer = null)
    {
        if (!method.HasBody)
        {
            return null;
        }
        lock (assemblySource)
        {
            var header = ReadMethodHeader(method, assemblySource, out var isBigHeader);
            if (header == null)
            {
                return null;
            }
            uint codeSize = isBigHeader ? header.Value.Fat_CodeSize : header.Value.Tiny_Flags_CodeSize;
            byte[] methodCilBody = buffer ?? new byte[codeSize];
            assemblySource.Read(methodCilBody.AsSpan(0, (int)codeSize));
            return methodCilBody;
        }
    }

    public static IMAGE_COR_ILMETHOD? ReadMethodHeader(MethodDef method, Stream stream, out bool isBigHeader)
    {
        isBigHeader = method.Body.IsBigHeader;
        long bodyOffset = RVA2FileOffset(method);
        if (bodyOffset < 0)
        {
            return null;
        }
        stream.Seek(bodyOffset, SeekOrigin.Begin);

        int ImageCorILMethodSize = Marshal.SizeOf<IMAGE_COR_ILMETHOD>();
        Span<byte> buffer = stackalloc byte[ImageCorILMethodSize];
        if (!isBigHeader)
        {
            buffer = buffer[..1];
        }
        stream.Read(buffer);

        IntPtr ptr = Marshal.AllocHGlobal(ImageCorILMethodSize);
        try
        {
            var unmanagedBufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), ptr), ImageCorILMethodSize);
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
        return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 | (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
    }

    public static long RVA2FileOffset(MethodDef methodDef)
    {
        if (methodDef.Module is not ModuleDefMD module)
        {
            return -1;
        }
        return (long)module.Metadata.PEImage.ToFileOffset(methodDef.RVA);
    }

    public static string GetLocalStorageDirectory()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyGuid = (assembly.GetCustomAttribute<GuidAttribute>()?.Value) ?? assembly.GetName().FullName;
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            assemblyGuid);
    }

    public static AssemblyName GetAssemblyName(Stream peFileS)
    {
        long pos = peFileS.Position;
        try
        {
            using PEReader reader = new(peFileS, PEStreamOptions.LeaveOpen);
            var metadata = reader.GetMetadataReader();
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

    public static async Task<Stream> AsStreamAsync(this AssemblyDef assemblyDef, CancellationToken token = default)
    {
        var ms = new MemoryStream();
        try
        {
            await Task.Run(() => assemblyDef.Write(ms), token);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        catch
        {
            throw;
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
        var usStream = (assemblyDef.ManifestModule as ModuleDefMD)!
            .Metadata.USStream;

        var usReader = usStream.CreateReader();
        usReader.Position++; // First byte is zero
        
        uint lenght = usStream.StreamLength;
        uint rawToken = 1; // The RID starts in 1
        while (lenght > 0)
        {
            var pos = usReader.Position;
            string str = usReader.ReadUserStringHeap();
            if (str.Length > 0 && str == value)
            {
                rawToken = 0x70 << 24 | rawToken;
                return new MDToken(rawToken);
            }
            lenght -= (uint)(usReader.Position - pos);
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
        if (terminator != GetStringTerminalByte(str))
        {
            throw new Exception("Unexpected byte found in user string heap terminator");
        }
        return str;

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
                if ((low >= 0x01 && low <= 0x08) ||
                    (low >= 0x0E && low <= 0x1F) ||
                    low == 0x27 || low == 0x2D || low == 0x7F)
                {
                    return 0x01;
                }
            }
            return 0x00;
        }
    }
}