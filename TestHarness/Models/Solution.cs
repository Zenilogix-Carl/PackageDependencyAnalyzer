using System.Collections.Generic;
using System.Collections.ObjectModel;
using PackageDependencyAnalysis.Model;

namespace TestHarness.Models
{
    public class Solution : ISolution
    {
        public void SetFile(string path)
        {
            File = path;
        }

        public IDictionary<string, IProject> ProjectCache { get; } = new Dictionary<string, IProject>();
        public ICollection<IProject> Projects => ProjectCache.Values;
        public string File { get; private set; }

        public void Clear()
        {
            ProjectCache.Clear();
        }
    }
}
