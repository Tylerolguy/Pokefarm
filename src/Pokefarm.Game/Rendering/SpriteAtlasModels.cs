using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

internal sealed record SpriteFrame(Rectangle Source, int SourceWidth, int SourceHeight, int OffsetX, int OffsetY);

internal sealed class SpriteAtlas
{
    public List<AtlasTexture>? Textures { get; set; }
}

internal sealed class AtlasTexture
{
    public List<AtlasFrame>? Frames { get; set; }
}

internal sealed class AtlasFrame
{
    public string? Filename { get; set; }

    public AtlasRect? Frame { get; set; }

    public AtlasSize? SourceSize { get; set; }

    public AtlasRect? SpriteSourceSize { get; set; }
}

internal sealed class AtlasRect
{
    public int X { get; set; }

    public int Y { get; set; }

    public int W { get; set; }

    public int H { get; set; }
}

internal sealed class AtlasSize
{
    public int W { get; set; }

    public int H { get; set; }
}
