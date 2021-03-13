using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGetRepository = NuGet.Protocol.Core.Types.Repository;

namespace PackageDependencyAnalysis.RemoteServices
{
    /// <summary>
    /// Work with a nuget feed
    /// </summary>
    /// <remarks>
    /// See: https://docs.microsoft.com/en-us/nuget/reference/nuget-client-sdk
    /// </remarks>
    public class NuGetSource
    {
        public class PackageVersion
        {
            public NuGetSource Source { get; set; }
            public string Name { get; set; }
            public Version Version { get; set; }
            public bool IsPreRelease { get; set; }
            public string Release { get; set; }
        }

        public class PackageMetaData : PackageVersion
        {
            public string Description { get; set; }
            public DateTimeOffset? Published { get; set; }
        }

        private readonly SourceCacheContext _cache;
        private readonly SourceRepository _repository;
        private readonly ILogger _logger;

        /// <summary>
        /// NuGet feed URL
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Constructor (public feed)
        /// </summary>
        /// <param name="source">Feed URL</param>
        public NuGetSource(string source)
        {
            Source = source;
            _cache = new SourceCacheContext();
            _repository = NuGetRepository.Factory.GetCoreV3(source);
            _logger = NullLogger.Instance;
        }

        /// <summary>
        /// Constructor (authenticated feed)
        /// </summary>
        /// <param name="source">Feed URL</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        public NuGetSource(string source, string username, string password)
        {
            Source = source;
            _cache = new SourceCacheContext();
            var packageSource = new PackageSource(source)
            {
                Credentials = new PackageSourceCredential(
                    source: source,
                    username: username,
                    passwordText: password,
                    isPasswordClearText: true,
                    validAuthenticationTypesText: null)
            };
            _repository = NuGetRepository.Factory.GetCoreV3(packageSource);
            _logger = NullLogger.Instance;
        }

        /// <summary>
        /// Get versions of a package
        /// </summary>
        /// <param name="packageName">Package name</param>
        /// <param name="includePreRelease">True if pre-releases are to be included in results</param>
        /// <returns>Versions ordered by descending version number and release label (pre-release)</returns>
        public async Task<IEnumerable<PackageVersion>> GetPackageVersions(string packageName, bool includePreRelease)
        {
            var cancellationToken = CancellationToken.None;
            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
            var result = await resource.GetAllVersionsAsync(packageName, _cache, _logger, cancellationToken);
            return result.OrderByDescending(s => s.Version).ThenByDescending(s => s.Release).Where(s => includePreRelease || !s.IsPrerelease).Select(s => new PackageVersion
            {
                Source = this,
                Name = packageName,
                Version = s.Version,
                Release = s.Release,
                IsPreRelease = s.IsPrerelease
            });
        }

        /// <summary>
        /// Get metadata for a package
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="includePreRelease"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PackageMetaData>> GetPackageMetaData(string packageName, bool includePreRelease)
        {
            var cancellationToken = CancellationToken.None;
            var resource = await _repository.GetResourceAsync<PackageMetadataResource>();
            var result = await resource.GetMetadataAsync(packageName, includePreRelease, false, _cache, _logger, cancellationToken);
            return result.OrderByDescending(s => s.Identity.Version.Version).ThenByDescending(s => s.Identity.Version.Release).Select(s => new PackageMetaData
            {
                Source = this,
                Name = s.Identity.Id,
                Version = s.Identity.Version.Version,
                Release = s.Identity.Version.Release,
                IsPreRelease = s.Identity.Version.IsPrerelease,
                Description = s.Description,
                Published = s.Published
            });
        }

        /// <summary>
        /// Get a package
        /// </summary>
        /// <param name="packageName">Package ID (name)</param>
        /// <param name="version">Package version</param>
        /// <param name="releaseLabel">Release label</param>
        /// <returns>Stream containing package data</returns>
        public async Task<Stream> GetPackage(string packageName, Version version, string releaseLabel=null)
        {
            var cancellationToken = CancellationToken.None;
            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
            var packageStream = new MemoryStream();

            await resource.CopyNupkgToStreamAsync(
                packageName,
                new NuGetVersion(version, releaseLabel),
                packageStream,
                _cache,
                _logger,
                cancellationToken);

            packageStream.Position = 0;
            return packageStream;
        }

        /// <summary>
        /// Get a package
        /// </summary>
        /// <returns>Stream containing package data</returns>
        public async Task<Stream> GetPackage(PackageVersion package)
        {
            var cancellationToken = CancellationToken.None;
            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
            var packageStream = new MemoryStream();

            await resource.CopyNupkgToStreamAsync(
                package.Name,
                new NuGetVersion(package.Version, package.Release),
                packageStream,
                _cache,
                _logger,
                cancellationToken);

            return packageStream;
        }
    }
}
