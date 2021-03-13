using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PackageDependencyAnalysis.RemoteServices;

namespace PackageDependencyAnalysis.Tests.RemoteServices
{
    [Explicit]
    [TestFixture]
    public class NuGetSourcesTests
    {
        [TestCase(@"https://api.nuget.org/v3/index.json;C:\NuGet", "Newtonsoft.Json", true)]
        public void GetPackageVersionsTest(string sources, string packageName, bool includePreRelease)
        {
            var ngs = new NuGetSources(sources.Split(';'));
            var versions = ngs.GetPackageVersions(packageName, includePreRelease).Result;
        }

        [TestCase(@"https://api.nuget.org/v3/index.json;C:\NuGet", "Newtonsoft.Json", true)]
        [TestCase(@"https://api.nuget.org/v3/index.json;C:\NuGet", "ZEF", true)]
        [TestCase(@"C:\NuGet", "ZEF", true)]
        public void GetPackageMetaDataTest(string sources, string packageName, bool includePreRelease)
        {
            var ngs = new NuGetSources(sources.Split(';'));
            var versions = ngs.GetPackageMetaData(packageName, includePreRelease).Result;
        }
    }
}
