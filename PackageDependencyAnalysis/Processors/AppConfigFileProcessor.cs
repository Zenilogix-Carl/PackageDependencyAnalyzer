using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Processors
{
    public class AppConfigFileProcessor
    {
        private const string RunTimeString = "runtime";
        private const string AssemblyBindingString = "assemblyBinding";
        private const string DependentAssemblyString = "dependentAssembly";
        private const string AssemblyIdentityString = "assemblyIdentity";
        private const string NameString = "name";
        private const string BindingRedirectString = "bindingRedirect";
        private const string OldVersionString = "oldVersion";
        private const string NewVersionString = "newVersion";

        public void HandleAppConfig(IProject project)
        {
            var localPath = Path.GetDirectoryName(project.AbsolutePath);
            var file = Path.Combine(localPath, "app.config");

            if (File.Exists(file))
            {
                foreach (var bindingRedirection in GetBindingRedirects(file))
                {
                    if (project.PackageReferenceDictionary.TryGetValue(bindingRedirection.AssemblyName, out var reference))
                    {
                        reference.BindingRedirection = bindingRedirection;
                    }
                }
            }
        }

        public static string GetPackageReference(IProject project, PackageReference reference, out object document, out object referenceObject)
        {
            var localPath = Path.GetDirectoryName(project.AbsolutePath);
            var file = Path.Combine(localPath, "app.config");

            if (!File.Exists(file))
            {
                document = null;
                referenceObject = null;
                return null;
            }

            var doc = XDocument.Load(file);
            document = doc;
            referenceObject = null;

            var runtimeElement = doc.Root.Element(RunTimeString);
            foreach (var assemblyBindingElement in runtimeElement.Elements()
                .Where(e => e.Name.LocalName == AssemblyBindingString))
            {
                var ns = assemblyBindingElement.Name.Namespace;
                foreach (var dependentAssemblyElement in assemblyBindingElement.Elements(ns + DependentAssemblyString))
                {
                    var assemblyIdentityElement = dependentAssemblyElement.Element(ns + AssemblyIdentityString);
                    var assemblyName = assemblyIdentityElement?.Attribute(NameString)?.Value;

                    if (assemblyName == reference.Package.Name)
                    {
                        referenceObject = dependentAssemblyElement;
                        break;
                    }
                }
            }

            return referenceObject?.ToString();
        }

        public static string ReplacePackageReference(object document, object referenceObject, PackageReference newReference)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (referenceObject == null) throw new ArgumentNullException(nameof(referenceObject));
            if (!(document is XDocument doc)) throw new ArgumentException(nameof(document));
            if (!(referenceObject is XElement element)) throw new ArgumentException(nameof(referenceObject));

            var bindingRedirectElement = element.Element(element.Name.Namespace + BindingRedirectString);
            var rangeAttribute = bindingRedirectElement?.Attribute(OldVersionString);
            var newVersionAttribute = bindingRedirectElement?.Attribute(NewVersionString);
            rangeAttribute.Value = $"{newReference.BindingRedirection.OldVersionFrom}-{newReference.BindingRedirection.OldVersionTo}";
            newVersionAttribute.Value = newReference.BindingRedirection.NewVersion.ToString();
            return element.ToString();
        }

        public static void Save(IProject project, object document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!(document is XDocument doc)) throw new ArgumentException(nameof(document));
            var localPath = Path.GetDirectoryName(project.AbsolutePath);
            var file = Path.Combine(localPath, "app.config");
            doc.Save(file);
        }

        private IEnumerable<BindingRedirection> GetBindingRedirects(string file)
        {
            var configDoc = XDocument.Load(file);

            var runtimeElement = configDoc.Root.Element(RunTimeString);
            if (runtimeElement != null)
            {
                foreach (var assemblyBindingElement in runtimeElement.Elements().Where(e => e.Name.LocalName == AssemblyBindingString))
                {
                    var ns = assemblyBindingElement.Name.Namespace;
                    foreach (var dependentAssemblyElement in assemblyBindingElement.Elements(ns + DependentAssemblyString))
                    {
                        var assemblyIdentityElement = dependentAssemblyElement.Element(ns + AssemblyIdentityString);
                        var bindingRedirectElement = dependentAssemblyElement.Element(ns + BindingRedirectString);
                        var assemblyName = assemblyIdentityElement?.Attribute(NameString)?.Value;
                        var range = bindingRedirectElement?.Attribute(OldVersionString)?.Value;
                        var newVersionString = bindingRedirectElement?.Attribute(NewVersionString)?.Value;

                        if (range != null && newVersionString != null)
                        {
                            var rangeParts = range.Split('-');
                            if (Version.TryParse(rangeParts[0], out var fromVersion) && Version.TryParse(rangeParts[1], out var toVersion) && Version.TryParse(newVersionString, out var newVersion))
                            {
                                yield return new BindingRedirection
                                {
                                    AssemblyName = assemblyName,
                                    OldVersionFrom = fromVersion,
                                    OldVersionTo = toVersion,
                                    NewVersion = newVersion
                                };
                            }
                        }
                    }
                }
            }
        }
    }
}
