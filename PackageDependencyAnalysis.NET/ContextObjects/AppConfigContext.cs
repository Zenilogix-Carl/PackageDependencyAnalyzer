using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class AppConfigContext
    {
        private const string RunTimeString = "runtime";
        private const string AssemblyBindingString = "assemblyBinding";
        private const string DependentAssemblyString = "dependentAssembly";

        private readonly XDocument _doc;

        public string FileName { get; }

        internal AppConfigContext(string fileName)
        {
            FileName = fileName;
            _doc = XDocument.Load(FileName, LoadOptions.PreserveWhitespace|LoadOptions.SetLineInfo);
        }

        public static AppConfigContext Create(string fileName)
        {
            return new AppConfigContext(fileName);
        }

        public IEnumerable<AppConfigAssemblyRedirectContext> AssemblyRedirects
        {
            get
            {
                var runtimeElement = _doc.Root?.Element(RunTimeString);
                if (runtimeElement != null)
                {
                    foreach (var assemblyBindingElement in runtimeElement.Elements()
                        .Where(e => e.Name.LocalName == AssemblyBindingString))
                    {
                        var ns = assemblyBindingElement.Name.Namespace;
                        foreach (var dependentAssemblyElement in assemblyBindingElement.Elements(ns + DependentAssemblyString))
                        {
                            yield return new AppConfigAssemblyRedirectContext(this, ns, dependentAssemblyElement);
                        }
                    }
                }
            }
        }

        public void Save()
        {
            _doc.Save(FileName);
        }

        public override string ToString() => FileName;
    }
}
