using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Processors
{
    public class PackagesConfigFileProcessor
    {
        private const string PackagesString = "packages";
        private const string PackageString = "package";
        private const string IdString = "id";
        private const string VersionString = "version";

        public void HandlePackagesConfig(IProject project)
        {
            var localPath = Path.GetDirectoryName(project.AbsolutePath);
            var file = Path.Combine(localPath, "packages.config");

            if (File.Exists(file))
            {
                foreach (var packageReference in GetPackageReferences(file))
                {
                    if (project.PackageReferenceDictionary.TryGetValue(packageReference.Name, out var reference))
                    {
                        reference.PackagesConfigVersion = packageReference.Version;
                    }
                }
            }
        }

        public static string GetPackageReference(IProject project, PackageReference reference, out object document, out object referenceObject)
        {
            var localPath = Path.GetDirectoryName(project.AbsolutePath);
            var file = Path.Combine(localPath, "packages.config");

            if (File.Exists(file))
            {
                var doc = XDocument.Load(file);
                var ns = doc.Root.Name.Namespace;

                document = doc;
                referenceObject = doc.Root
                    .Elements(ns + PackageString)
                    .FirstOrDefault(e => e.Attribute(IdString).Value == reference.Package.Name);
                return referenceObject?.ToString();
            }

            document = null;
            referenceObject = null;
            return null;
        }

        public static string ReplacePackageReference(object document, object referenceObject, PackageReference newReference)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (referenceObject == null) throw new ArgumentNullException(nameof(referenceObject));
            if (!(document is XDocument doc)) throw new ArgumentException(nameof(document));
            if (!(referenceObject is XElement element)) throw new ArgumentException(nameof(referenceObject));

            var versionAttribute = element.Attribute(VersionString);

            var newVersion = new Version(newReference.Version.Major, newReference.Version.Minor,
                newReference.Version.Build);

            if (string.IsNullOrWhiteSpace(newReference.PreReleaseSuffix))
            {
                versionAttribute.Value = newVersion.ToString();
            }
            else
            {
                versionAttribute.Value = $"{newVersion}-{newReference.PreReleaseSuffix}";
            }

            return element.ToString();
        }

        public static void Save(IProject project, object document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!(document is XDocument doc)) throw new ArgumentException(nameof(document));
            var localPath = Path.GetDirectoryName(project.AbsolutePath);
            var file = Path.Combine(localPath, "packages.config");
            doc.Save(file);
        }

        private IEnumerable<(string Name, Version Version)> GetPackageReferences(string file)
        {
            var configDoc = XDocument.Load(file);
            foreach (var xElement in configDoc.Root.Elements(PackageString))
            {
                var id = xElement.Attribute(IdString).Value;
                var versionString = xElement.Attribute(VersionString).Value;

                if (Version.TryParse(versionString, out var version))
                {
                    yield return (Name: id, Version: version);
                }
            }
        }
    }
}
