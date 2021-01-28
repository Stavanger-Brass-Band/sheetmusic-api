using SheetMusic.Api.OData.Constants;

namespace SheetMusic.Api.OData.MVC
{
    public class ODataOrderByOption
    {
        public string Field { get; set; } = null!;

        public SortDirection Direction { get; set; }
    }
}
