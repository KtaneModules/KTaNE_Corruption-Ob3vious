using System.Collections.Generic;

public sealed class CorruptionSettings
{
    public string SiteUrl = @"https://ktane.timwi.de/json/raw";

    public Dictionary<string, string> RememberedCompatibilities = new Dictionary<string, string>();

    public bool AutomaticUpdate = true;

    public int Version = 0;
}