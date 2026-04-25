using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Data container used to pass sprite Frame information between game systems.
internal sealed record SpriteFrame(Rectangle Source, int SourceWidth, int SourceHeight, int OffsetX, int OffsetY);

// Main runtime type for sprite Atlas, coordinating state and side effects for this feature.
internal sealed class SpriteAtlas
{
    public List<AtlasTexture>? Textures { get; set; }
}

// Main runtime type for atlas Texture, coordinating state and side effects for this feature.
internal sealed class AtlasTexture
{
    public List<AtlasFrame>? Frames { get; set; }
}

// Main runtime type for atlas Frame, coordinating state and side effects for this feature.
internal sealed class AtlasFrame
{
    public string? Filename { get; set; }

    public AtlasRect? Frame { get; set; }

    public AtlasSize? SourceSize { get; set; }

    public AtlasRect? SpriteSourceSize { get; set; }
}

// Main runtime type for atlas Rect, coordinating state and side effects for this feature.
internal sealed class AtlasRect
{
    public int X { get; set; }

    public int Y { get; set; }

    public int W { get; set; }

    public int H { get; set; }
}

// Main runtime type for atlas Size, coordinating state and side effects for this feature.
internal sealed class AtlasSize
{
    public int W { get; set; }

    public int H { get; set; }
}
