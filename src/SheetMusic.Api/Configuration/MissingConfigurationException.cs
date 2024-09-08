using System;

namespace SheetMusic.Api.Configuration;

public class MissingConfigurationException(string keyName) : Exception($"Missing configuration key {keyName}")
{
}
