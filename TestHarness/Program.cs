using PackageDependencyAnalysis.Processors;
using TestHarness.Models;
using PackageCache = TestHarness.Models.PackageCache;
using PackageVersion = TestHarness.Models.PackageVersion;
using Project = TestHarness.Models.Project;

namespace TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var solution = new Solution();
                var packageCache = new PackageCache();
                var logger = new Logger();
                var packageCacheProcessor = new PackageCacheProcessor(name => new Package{Name = name}, file => new PackageVersion { File = file }, packageCache, logger);
                packageCacheProcessor.ProcessCaches(args[0]);
                var solutionFileProcessor = new SolutionFileProcessor(solution, packageCache, file => new Project { AbsolutePath = file }, name => new Package { Name = name }, packageCacheProcessor, logger);
                solutionFileProcessor.LoadFromFile(args[0]);
                solutionFileProcessor.ResolvePackageReferences();
            }
        }
    }
}
