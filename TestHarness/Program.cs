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
                var projectFileProcessor = new ProjectFileProcessor(file => new Project { AbsolutePath = file }, name => new Package{Name = name}, solution, packageCacheProcessor, new AppConfigFileProcessor(), new PackagesConfigFileProcessor(), packageCache, logger);
                new SolutionFileProcessor(solution, projectFileProcessor, logger).LoadFromFile(args[0]);
                projectFileProcessor.ResolvePackageReferences();
            }
        }
    }
}
