namespace SheetMusic.Api.Controllers.InternalModels;

public class BoundingBox
{
    public BoundingBox(string commaSeparated)
    {
        var parts = commaSeparated.Split(",");
        X = int.Parse(parts[0]);
        Y = int.Parse(parts[1]);
        Width = int.Parse(parts[2]);
        Height = int.Parse(parts[3]);
    }

    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
