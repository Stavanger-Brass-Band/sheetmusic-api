using System;

namespace SheetMusic.Api.Errors;

public class AliasAlreadyAddedError(string alias, string partName) : Exception($"Alias '{alias}' already exists for part '{partName}'")
{
}
