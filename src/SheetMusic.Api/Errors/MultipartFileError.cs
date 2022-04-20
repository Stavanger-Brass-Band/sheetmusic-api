using System;

namespace SheetMusic.Api.Errors;

public class MultipartFileError : Exception
{
    public MultipartFileError(string? message) : base(message)
    {
    }
}
