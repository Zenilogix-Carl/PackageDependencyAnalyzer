using System;
using System.Collections.Generic;
using System.IO;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class PackageCacheContext
    {
        private readonly Func<string, bool> _frameworkFilter;
        public string SolutionPackagesFolder { get; }
        public string UserPackagesFolder { get; }

        public IEnumerable<PackageContext> Packages
        {
            get
            {
                foreach (var packageContext in EnumerateFolder(UserPackagesFolder))
                {
                    yield return packageContext;
                }

                foreach (var packageContext in EnumerateFolder(SolutionPackagesFolder))
                {
                    yield return packageContext;
                }
            }
        }

        public PackageCacheContext(string solutionFile, Func<string,bool> frameworkFilter=null)
        {
            _frameworkFilter = frameworkFilter;
            SolutionPackagesFolder = Path.Combine(Path.GetDirectoryName(solutionFile), "packages");
            UserPackagesFolder = Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), @".nuget\packages");
        }

        private IEnumerable<PackageContext> EnumerateFolder(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var enumerateFile in Directory.EnumerateFiles(path, "*.nupkg", SearchOption.AllDirectories))
                {
                    yield return PackageContext.Create(enumerateFile, _frameworkFilter);
                }
            }
        }
    }
}
