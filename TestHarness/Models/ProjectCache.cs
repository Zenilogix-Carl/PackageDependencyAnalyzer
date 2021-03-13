using System.Collections.Generic;
using PackageDependencyAnalysis.Model;

namespace TestHarness.Models
{
    public class ProjectCache : IProjectCache
    {
        public IDictionary<string, IProject> Projects { get; } = new Dictionary<string, IProject>();
    }
}
