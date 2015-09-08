namespace CommonTools.WpfAnimatedGif.Decoding
{
    internal struct GifColor
    {
        private readonly byte _r;
        private readonly byte _g;
        private readonly byte _b;

        internal GifColor(byte r, byte g, byte b)
        {
            _r = r;
            _g = g;
            _b = b;
        }

        public byte R => _r;
        public byte G => _g;
        public byte B => _b;

        public override string ToString()
        {
            return $"#{_r:x2}{_g:x2}{_b:x2}";
        }
    }
}
