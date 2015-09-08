namespace CommonTools.WpfAnimatedGif.Decoding
{
    internal class GifTrailer : GifBlock
    {
        internal const int TrailerByte = 0x3B;

        private GifTrailer()
        {
        }

        internal override GifBlockKind Kind => GifBlockKind.Other;

        internal static GifTrailer ReadTrailer()
        {
            return new GifTrailer();
        }
    }
}
