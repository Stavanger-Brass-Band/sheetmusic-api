namespace SheetMusic.Api.Users.RequestModels;

public class ProfilePictureCropRequest
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Size { get; set; }
}