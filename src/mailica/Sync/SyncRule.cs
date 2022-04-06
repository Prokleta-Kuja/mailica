using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace mailica.Sync;

[DebuggerDisplay("{_filter} -> {Destinations.Count}")]
public class SyncRule
{
    readonly Regex _filter;
    public List<SyncDestination> Destinations { get; }

    public SyncRule(string regexEscapedfilter, List<SyncDestination> destinations)
    {
        _filter = new Regex(regexEscapedfilter);
        Destinations = destinations;
    }
    public bool Matches(string input) => _filter.IsMatch(input);
}