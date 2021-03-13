using System.Collections.Generic;

namespace PackageDependencyAnalysis.Model
{
    public interface ISolution : IContext
    {
        void SetFile(string path);
        ICollection<IProject> Projects { get; }
        IDictionary<string, IProject> ProjectCache { get; }
        void Clear();
    }
}
