using System;

namespace SheetMusic.Api.Errors;

public class MultipartFileError(string? message) : Exception(message)
{
}
