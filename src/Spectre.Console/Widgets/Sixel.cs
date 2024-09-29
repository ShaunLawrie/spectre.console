namespace Spectre.Console;

/// <summary>
/// A renderable sixel.
/// </summary>
public sealed class Sixel : Renderable
{
    private readonly string _sixel;

    /// <summary>
    /// Gets an empty <see cref="Text"/> instance.
    /// </summary>
    public static Sixel Demo { get; } = new Sixel("#0;2;100;0;0#1;2;0;100;0#2;2;0;0;100#0!18F#1!18F$#1!18w#2!18w-#2!12~#0!12~#1!12~-", 38, 18, 4, 2);

    /// <summary>
    /// Gets the width of the sixel.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the sixel.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the width of the sixel in cells, this might be calculable but could be console host specific.
    /// </summary>
    public int CellWidth { get; }

    /// <summary>
    /// Gets the height of the sixel in cells, this might be calculable but could be console host specific.
    /// </summary>
    public int CellHeight { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sixel"/> class.
    /// </summary>
    /// <param name="content">The sixel data excluding enter and exit sixel mode codes.</param>
    /// <param name="width">The width of the sixel.</param>
    /// <param name="height">The height of the sixel.</param>
    /// <param name="cellWidth">The width of the sixel in cells.</param>
    /// <param name="cellHeight">The height of the sixel in cells.</param>
    public Sixel(string content, int width, int height, int cellWidth, int cellHeight)
    {
        _sixel = content;
        Width = width;
        Height = height;
        CellWidth = cellWidth;
        CellHeight = cellHeight;
    }

    /// <inheritdoc/>
    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        return new Measurement(CellWidth, maxWidth);
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Lol wtf, magic "2;1 is the default sixel ratio, required so that the width and height can
        // be set as parameter 3 & 4 for the DECGRA (") command. Without specifying the width and height
        // all terminal cells to the right and below the sixel will be wiped out which can mangle rendering.
        // When the sixel is finished being dumped in graphics mode the cursor needs to return to the
        // start position of the sixel so that the rest of the top line of the sixel can be rendered i.e the right border.
        var lines = new List<SegmentLine>();
        var firstLine = new List<Segment>()
        {
            new(AnsiSequences.EnterSixelMode()),
            Segment.SixelSegment($"\"2;1;{Width};{Height}{_sixel}", CellWidth),
            new(AnsiSequences.ExitSixelMode()),
            new(AnsiSequences.CUU(CellHeight - 1)),
        };

        lines.Add(new SegmentLine(firstLine));
        for (var i = 0; i < CellHeight - 1; i++)
        {
            lines.Add([Segment.Empty]);
        }

        return new SegmentLineEnumerator(lines);
    }
}