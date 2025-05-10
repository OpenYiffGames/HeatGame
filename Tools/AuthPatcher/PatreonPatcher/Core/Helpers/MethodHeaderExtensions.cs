using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatreonPatcher.Core.Helpers
{
    internal static class MethodHeaderExtensions
    {
        public static bool IsFatMethodBody(this IMAGE_COR_ILMETHOD? header)
        {
            if (!header.HasValue)
            {
                throw new ArgumentNullException(nameof(header));
            }
            return header.Value.IsFatMethodBody();
        }

        public static byte TinyCodeSize(this IMAGE_COR_ILMETHOD header)
        {
            if (header.IsFatMethodBody())
            {
                throw new InvalidOperationException("Header is not a tiny method body");
            }
            return (byte)(header.Tiny_FlagsAndCodeSize >> 2); // skip the first 2 bits (header type values: EMCA 335 II.25.4.1)
        }

        public static byte TinyCodeSize(this IMAGE_COR_ILMETHOD? header)
        {
            if (!header.HasValue)
            {
                throw new ArgumentNullException(nameof(header));
            }
            return header.Value.TinyCodeSize();
        }

        public static uint MethodCodeSize(this IMAGE_COR_ILMETHOD header)
        {
            if (header.IsFatMethodBody())
            {
                return header.Fat_CodeSize;
            }
            return header.TinyCodeSize();
        }

        public static uint MethodCodeSize(this IMAGE_COR_ILMETHOD? header)
        {
            if (!header.HasValue)
            {
                throw new ArgumentNullException(nameof(header));
            }
            return header.Value.MethodCodeSize();
        }

        public static bool IsFatMethodBody(this IMAGE_COR_ILMETHOD header)
        {
            int type = header.TinyFatFormat & 0b11; // mask the first 2 bits (header type values: EMCA 335 II.25.4.1)
            return type switch
            {
                0x3 => true,
                0x2 => false,
                _ => throw new BadImageFormatException("Invalid header type"),
            };
        }
    }
}
