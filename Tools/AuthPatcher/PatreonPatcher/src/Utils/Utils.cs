using dnlib.DotNet;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PatreonPatcher;

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
        uint lenght = usStream.StreamLength;

        uint rawToken = 0;
        while (lenght > 0)
        {
            var pos = usReader.Position;
            int strLen = usReader.Read7BitEncodedInt32();
            if (strLen < 0)
            {
                throw new Exception("No more bytes");
            }
            if (strLen > 0)
            {
                string str = usReader.ReadString(strLen - 1, Encoding.Unicode);
                if (str == value)
                {
                    rawToken = 0x70 << 24 | rawToken;
                    return new MDToken(rawToken);
                }
            }
            lenght -= (uint)(usReader.Position - pos);
            rawToken++;
        }

        return null;
    }

}