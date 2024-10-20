using System;
using System.Collections.Generic;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Spectre.Console.ImageSharp.Sixel;
using Spectre.Console.Rendering;

namespace Spectre.Console;

/// <summary>
/// Represents a renderable image.
/// </summary>
public sealed class SixelImage : Renderable
{
    /// <summary>
    /// Gets the image width in terminal cells.
    /// </summary>
    public int Width => Image.Width;

    /// <summary>
    /// Gets the image height in terminal cells.
    /// </summary>
    public int Height => Image.Height;

    /// <summary>
    /// Gets or sets the render width of the canvas in terminal cells.
    /// </summary>
    public int? MaxWidth { get; set; }

    /// <summary>
    /// Gets the render width of the canvas. This is hard coded to 1 for sixel images.
    /// </summary>
    public int PixelWidth { get; } = 1;

    /// <summary>
    /// Gets or sets the <see cref="IResampler"/> that should
    /// be used when scaling the image. Defaults to bicubic sampling.
    /// </summary>
    public IResampler? Resampler { get; set; }

    internal SixLabors.ImageSharp.Image<Rgba32> Image { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SixelImage"/> class.
    /// </summary>
    /// <param name="filename">The image filename.</param>
    public SixelImage(string filename)
    {
        Image = SixLabors.ImageSharp.Image.Load<Rgba32>(filename);
    }

    /// <inheritdoc/>
    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        if (PixelWidth < 0)
        {
            throw new InvalidOperationException("Pixel width must be greater than zero.");
        }

        var width = MaxWidth ?? Width;
        if (maxWidth < width * PixelWidth)
        {
            return new Measurement(maxWidth, maxWidth);
        }

        return new Measurement(width * PixelWidth, width * PixelWidth);
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // The maxWidth parameter is in cells so this looks kind of whacky because we have an image
        // that is in pixels.
        var width = Width;
        if (MaxWidth != null && MaxWidth < maxWidth && MaxWidth < width)
        {
            width = MaxWidth.Value;
        }
        else if (maxWidth < width)
        {
            width = maxWidth;
        }

        // Calculate the height based on the aspect ratio and sixel cell size rendering
        var pixelWidth = width * Compatibility.GetCellSize().PixelWidth;
        var pixelHeight = (int)Math.Round((double)Image.Height / Image.Width * pixelWidth);
        var height = (int)Math.Ceiling((double)(pixelHeight / Compatibility.GetCellSize().PixelHeight));

        // Dump the sixel data as a control segment
        var sixelString = SixelParser.ImageToSixel(Image, width);
        var segments = new List<Segment>
        {
            Segment.Control(sixelString),
        };

        // Then dump a transparent renderable to take up the space the sixel is drawn in
        // so Spectre.Console can correctly measure and render the image and its container/surronding content.
        var canvas = new Canvas(width, height)
        {
            MaxWidth = width,
            PixelWidth = PixelWidth,
            Scale = false,
        };

        // TODO remove this, it's for drawing a red and transparent checkerboard pattern to debug sixel positioning
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SPECTRE_CONSOLE_DEBUG")))
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (y % 2 == 0)
                    {
                        if (x % 2 == 0)
                        {
                            canvas.SetPixel(x, y, new Color(255, 0, 0));
                        }
                    }
                    else if (x % 2 != 0)
                    {
                        canvas.SetPixel(x, y, new Color(255, 0, 0));
                    }
                }
            }
        }

        segments.AddRange(((IRenderable)canvas).Render(options, maxWidth));

        return segments;
    }
}