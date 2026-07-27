using System;

namespace SheetMusic.Api.Parts.Errors;

public class AliasAlreadyAddedError(string alias, string partName) : Exception($"Alias '{alias}' already exists for part '{partName}'")
{
}
