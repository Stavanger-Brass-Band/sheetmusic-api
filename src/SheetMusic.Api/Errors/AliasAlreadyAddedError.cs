using System;

namespace SheetMusic.Api.Errors
{
    public class AliasAlreadyAddedError : Exception
    {
        public AliasAlreadyAddedError(string alias, string partName) : base($"Alias '{alias}' already exists for part '{partName}'")
        {
        }
    }
}
