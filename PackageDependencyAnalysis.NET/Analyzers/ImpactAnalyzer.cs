using System.Collections.Generic;
using System.Linq;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Analyzers
{
    /// <summary>
    /// Analyzes package for impacted packages and projects
    /// </summary>
    public class ImpactAnalyzer
    {
        private readonly HashSet<object> _items = new HashSet<object>();
        public readonly List<object> List = new List<object>();

        private ImpactAnalyzer()
        {
        }

        public static IList<object> Analyze(IPackageVersion packageVersion)
        {
            var analyzer = new ImpactAnalyzer();

            analyzer.AnalyzePackage(packageVersion);

            return analyzer.List.Where(r => r is IPackageVersion).Union(analyzer.List.Where(r => r is IProject)).ToList();
        }

        private void AnalyzePackage(IPackageVersion packageVersion)
        {
            foreach (var referencingProject in packageVersion.ReferencingProjects)
            {
                AnalyzeProject(referencingProject);
            }

            foreach (var referencingPackage in packageVersion.ReferencingPackages)
            {
                AnalyzePackage(referencingPackage);
            }

            if (!_items.Contains(packageVersion))
            {
                _items.Add(packageVersion);
                List.Insert(0, packageVersion);
            }
        }

        private void AnalyzeProject(IProject project)
        {
            foreach (var referencingProject in project.Dependencies)
            {
                AnalyzeProject(referencingProject);
            }

            if (!_items.Contains(project))
            {
                _items.Add(project);
                List.Insert(0, project);
            }
        }
    }
}
