using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Completions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatreonPatcher.Cli
{
    internal class DirectoryFilesCompletionSource : ICompletionSource
    {
        private readonly DirectoryInfo _directoryInfo;
        public string? FileFilter { get; set; } = null;
        public MatchType MatchType { get; set; } = MatchType.Win32;
        public MatchCasing MatchCasing { get; set; } = MatchCasing.CaseInsensitive;
        public Func<FileInfo, bool> MatchFilterPredicate { get; set; } = _ => true;
        
        private string AllFilesFilter => MatchType switch
        {
            MatchType.Win32 => "*.*",
            MatchType.Simple => "*",
            _ => "*.*"
        };

        public DirectoryFilesCompletionSource(DirectoryInfo directoryInfo)
        {
            _directoryInfo = directoryInfo;
        }

        public IEnumerable<CompletionItem> GetCompletions(CompletionContext context)
        {
            var possibleFileName = context.WordToComplete;

            var matchingFiles = _directoryInfo.GetFiles(FileFilter ?? AllFilesFilter, new EnumerationOptions()
            {
                MatchType = MatchType,
                MatchCasing = MatchCasing,
                MaxRecursionDepth = 1,
            })
                .Where(file => file.Name.StartsWith(possibleFileName, StringComparison.OrdinalIgnoreCase))
                .Where(MatchFilterPredicate)
                .Select(file => new CompletionItem(file.Name));

            return matchingFiles;
        }
    }
}
