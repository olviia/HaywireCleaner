using System;
using Core.Player;

namespace Core.Input
{
    /// <summary>
    /// this is a seam for getting what button letter/glyph corresponds with input/intent
    /// display projection
    /// </summary>
    public interface IInputGlyphProvider
    {
        Glyph GetGlyph(Intent intent);
        event Action DeviceChanged;
    }
}