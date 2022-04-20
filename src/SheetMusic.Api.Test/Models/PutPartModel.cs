namespace SheetMusic.Api.Test.Models;

public class PutPartModel
{
    public string Name { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool? Indexable { get; set; }
}
