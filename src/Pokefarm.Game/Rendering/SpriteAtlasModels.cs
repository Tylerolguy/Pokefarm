using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

/// <summary>
/// Executes the Sprite Frame operation.
/// </summary>
internal sealed record SpriteFrame(Rectangle Source, int SourceWidth, int SourceHeight, int OffsetX, int OffsetY);

/// <summary>
/// Represents the SpriteAtlas.
/// </summary>
internal sealed class SpriteAtlas
{
    public List<AtlasTexture>? Textures { get; set; }
}

/// <summary>
/// Represents the AtlasTexture.
/// </summary>
internal sealed class AtlasTexture
{
    public List<AtlasFrame>? Frames { get; set; }
}

/// <summary>
/// Represents the AtlasFrame.
/// </summary>
internal sealed class AtlasFrame
{
    public string? Filename { get; set; }

    public AtlasRect? Frame { get; set; }

    public AtlasSize? SourceSize { get; set; }

    public AtlasRect? SpriteSourceSize { get; set; }
}

/// <summary>
/// Represents the AtlasRect.
/// </summary>
internal sealed class AtlasRect
{
    public int X { get; set; }

    public int Y { get; set; }

    public int W { get; set; }

    public int H { get; set; }
}

/// <summary>
/// Represents the AtlasSize.
/// </summary>
internal sealed class AtlasSize
{
    public int W { get; set; }

    public int H { get; set; }
}
