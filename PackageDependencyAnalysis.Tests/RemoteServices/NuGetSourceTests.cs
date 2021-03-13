using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using PackageDependencyAnalysis.RemoteServices;

namespace PackageDependencyAnalysis.Tests.RemoteServices
{
    [Explicit]
    [TestFixture]
    public class NuGetSourceTests
    {
        [TestCase("https://api.nuget.org/v3/index.json", "Newtonsoft.Json", true)]
        public void GetPackageVersionsTest(string source, string packageName, bool includePreRelease)
        {
            var ngs = new NuGetSource(source);
            var versions = ngs.GetPackageVersions(packageName, includePreRelease).Result;
        }

        [TestCase("https://api.nuget.org/v3/index.json", "Newtonsoft.Json", true)]
        [TestCase("https://api.nuget.org/v3/index.json", "ZEF", true)]
        [TestCase(@"C:\NuGet", "ZEF", true)]
        public void GetPackageMetaDataTest(string source, string packageName, bool includePreRelease)
        {
            var ngs = new NuGetSource(source);
            var versions = ngs.GetPackageMetaData(packageName, includePreRelease).Result;
        }

        [TestCase(@"C:\NuGet", "ZEF", "1.14.0.25284")]
        public void GetPackageTest(string source, string packageName, string version)
        {
            var ngs = new NuGetSource(source);
            using (var stream = ngs.GetPackage(packageName, Version.Parse(version)).Result)
            {
                var fileName = Path.GetTempFileName();
                using (var file = File.Create(fileName))
                {
                    stream.CopyTo(file);
                }

                using (var zip = ZipFile.OpenRead(fileName))
                {
                    foreach (var entry in zip.Entries)
                    {
                        Console.WriteLine(entry.Name);
                    }
                }

                File.Delete(fileName);
            }
        }
    }
}
