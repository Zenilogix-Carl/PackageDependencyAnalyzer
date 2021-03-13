using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class PackagesConfigContext
    {
        private const string PackagesString = "packages";
        private const string PackageString = "package";

        private readonly XDocument _doc;

        public string FileName { get; }

        internal PackagesConfigContext(string fileName)
        {
            FileName = fileName;
            var text = File.ReadAllText(FileName);
            _doc = string.IsNullOrWhiteSpace(text) ? new XDocument(new XElement(PackagesString)) : XDocument.Parse(text, LoadOptions.PreserveWhitespace|LoadOptions.SetLineInfo);
        }

        public static PackagesConfigContext Create(string fileName)
        {
            return new PackagesConfigContext(fileName);
        }

        public IEnumerable<PackagesConfigPackageContext> Packages
        {
            get
            {
                foreach (var xElement in _doc.Root.Elements(PackageString))
                {
                    yield return new PackagesConfigPackageContext(this, xElement);
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
