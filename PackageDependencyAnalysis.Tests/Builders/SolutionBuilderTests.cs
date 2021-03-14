using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalysis.RemoteServices;
using PackageDependencyAnalysis.Repository;
using BuilderObjects = PackageDependencyAnalysis.Builders;

namespace PackageDependencyAnalysis.Tests.Builders
{
    [TestFixture]
    public class SolutionBuilderTests
    {
        class PackageCache : IPackageCache
        {
            public IDictionary<string, IPackage> PackagesDictionary { get; } = new ConcurrentDictionary<string, IPackage>();
            public ICollection<IPackage> Packages => PackagesDictionary.Values;
            public void Clear()
            {
                PackagesDictionary.Clear();
            }
        }

        class Package : IPackage
        {
            public string Name { get; set; }
            public ICollection<ReleaseVersion> Versions => VersionDictionary.Keys;
            public ICollection<IPackageVersion> PackageVersions => VersionDictionary.Values;
            public IDictionary<ReleaseVersion, IPackageVersion> VersionDictionary { get; } = new ConcurrentDictionary<ReleaseVersion, IPackageVersion>();
            public IDictionary<string, IProject> ReferencingProjects { get; } = new ConcurrentDictionary<string, IProject>();
            public IDictionary<string, IPackageVersion> ReferencingPackages { get; } = new ConcurrentDictionary<string, IPackageVersion>();
            public bool IsReferenced { get; set; }
            public bool HasVersions => VersionDictionary.Any();

            public override string ToString() => $"{Name} ({Versions.Count} versions)";
        }

        class PackageVersion : IPackageVersion
        {
            public string File { get; }
            public string Source { get; set; }
            public IPackage Package { get; set; }
            public ReleaseVersion Version { get; set; }
            public DateTime DateTime { get; set; }
            public string Description { get; set; }
            public bool IsPrerelease { get; set; }
            public string PreReleaseSuffix { get; set; }
            public ICollection<AssemblyInfo> Assemblies { get; }
            public PlatformDependencies Dependencies { get; }
            public ICollection<IProject> ReferencingProjects { get; }
            public ICollection<IProject> ConfigReferences { get; }
            public ICollection<IProject> BindingRedirectReferences { get; }
            public ICollection<IPackageVersion> ReferencingPackages { get; }
            public string NuSpec { get; set; }
            public string RepositoryUrl { get; set; }
            public bool IsReferenced { get; set; }

            public override string ToString() => $"{Package.Name} {Version}";
        }

        class Project : IProject
        {
            public string File { get; set; }
            public string Name { get; set; }
            public string AbsolutePath { get; set; }
            public string PackagesConfigPath { get; set; }
            public string AppConfigPath { get; set; }
            public Version Version { get; set; }
            public ICollection<IProject> Projects { get; } = new List<IProject>();
            public ICollection<IProject> Dependencies { get; } = new List<IProject>();
            public IDictionary<string, PackageReference> PackageReferenceDictionary { get; } = new ConcurrentDictionary<string, PackageReference>();
            public ICollection<PackageReference> PackageReferences => PackageReferenceDictionary.Values;
            public ICollection<BindingRedirection> BindingRedirections { get; } = new List<BindingRedirection>();

            public override string ToString() => $"{Name}";
        }

        class Solution : ISolution
        {
            public string File { get; private set; }
            public void SetFile(string path)
            {
                File = path;
            }

            public ICollection<IProject> Projects => ProjectCache.Values;
            public IDictionary<string, IProject> ProjectCache { get; } = new ConcurrentDictionary<string, IProject>();
            public void Clear()
            {
                ProjectCache.Clear();
            }
        }

        private readonly PackageCache _cache = new PackageCache();
        private readonly Solution _solution = new Solution();
        private PackageRepository _packageRepository;
        private BuilderObjects.PackageBuilder _packageBuilder;
        private BuilderObjects.PackageVersionBuilder _packageVersionBuilder;
        private BuilderObjects.ProjectBuilder _projectBuilder;
        private BuilderObjects.SolutionBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _packageRepository = new PackageRepository(SolutionFileName, Feeds);

            _packageBuilder = new BuilderObjects.PackageBuilder(_cache, name =>
            {
                Console.WriteLine($"Creating new package {name}");
                return new Package {Name = name};
            });
            _packageVersionBuilder = new BuilderObjects.PackageVersionBuilder((package, version) =>
            {
                Console.WriteLine($"Creating new package {package.Name} Version {version}");
                return new PackageVersion {Package = package, Version = version};
            }, _packageBuilder, _packageRepository);
            _projectBuilder = new BuilderObjects.ProjectBuilder(file =>
            {
                Console.WriteLine($"Creating new project {file}");
                return new Project {AbsolutePath = file};
            }, _packageVersionBuilder);
            _builder = new BuilderObjects.SolutionBuilder(_projectBuilder);
        }

        [Explicit]
        [TestCaseSource(nameof(TestSolutions))]
        public async Task BuildSolutionTest(string solutionFileName)
        {
            await _builder.BuildSolution(_solution, solutionFileName);
        }

        private static IEnumerable<TestCaseData> TestSolutions
        {
            get
            {
                yield return new TestCaseData(SolutionFileName);
            }
        }

        public static string SolutionFileName => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\..\PackageDependencyAnalyzer.sln"));

        public static string[] Feeds = {"https://api.nuget.org/v3/index.json" };
    }
}
