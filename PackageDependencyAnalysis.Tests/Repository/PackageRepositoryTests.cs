using System;
using NUnit.Framework;
using PackageDependencyAnalysis.Repository;

namespace PackageDependencyAnalysis.Tests.Repository
{
    [TestFixture]
    public class PackageRepositoryTests
    {
        [TestCase(@"C:\nothing.sln", @"C:\NuGet", "ZEF", "1.9.0.29384", null)]
        public void FindTest(string solutionName, string source, string packageName, string version, string label)
        {
            var repo = new PackageRepository(solutionName, source.Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries));
            var result = repo.Find(packageName, Version.Parse(version), label).Result;
        }
    }
}
