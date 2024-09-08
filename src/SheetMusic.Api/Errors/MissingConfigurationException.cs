using System;

namespace SheetMusic.Api.Errors;

public class MissingConfigurationException(string keyName) : Exception($"Missing configuration key {keyName}")
{
}
