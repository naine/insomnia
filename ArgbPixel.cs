// Copyright (c) 2026 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

namespace Insomnia
{
    readonly struct ArgbPixel
    {
        private readonly uint value;

        public uint A
            => value >> 24;

        public byte R
            => (byte)(value >> 16);

        public byte G
            => (byte)(value >> 8);

        public byte B
            => (byte)value;

        public ArgbPixel(uint value)
            => this.value = value;

        public ArgbPixel(byte a, byte r, byte g, byte b)
            => value = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;

        public static implicit operator uint(ArgbPixel pixel)
            => pixel.value;
    }
}
