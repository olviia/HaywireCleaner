namespace Core.Input
{
    public static class GlyphInput
    {
        public static IInputGlyphProvider Glyphs { get; private set; }
        public static void Register(IInputGlyphProvider glyphs) => Glyphs = glyphs;
    }
}