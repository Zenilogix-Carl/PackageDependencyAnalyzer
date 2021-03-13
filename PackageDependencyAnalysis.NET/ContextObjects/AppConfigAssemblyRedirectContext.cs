using System;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class AppConfigAssemblyRedirectContext
    {
        private const string AssemblyIdentityString = "assemblyIdentity";
        private const string NameString = "name";
        private const string BindingRedirectString = "bindingRedirect";
        private const string OldVersionString = "oldVersion";
        private const string NewVersionString = "newVersion";

        private static readonly Regex VersionRangeRegex = new Regex(@"(?<VersionFrom>\d+(\.\d+){1,3})-(?<VersionTo>\d+(\.\d+){1,3})");
        private static readonly Regex VersionRegex = new Regex(@"(?<Version>\d+(\.\d+){1,3})");

        private readonly XAttribute _bindingRedirectOldAttribute;
        private readonly XAttribute _bindingRedirectNewAttribute;
        private Version _newVersion;
        private Version _oldVersionTo;
        private Version _oldVersionFrom;

        internal AppConfigContext Context { get; }
        internal XElement Element { get; }

        public string OriginalXml { get; }
        public int LineNumber { get; }
        public string CurrentXml => Element.ToString();


        public Version NewVersion
        {
            get => _newVersion;
            set
            {
                _newVersion = value;
                SetNewVersion();
            }
        }

        public Version OldVersionTo
        {
            get => _oldVersionTo;
            set
            {
                _oldVersionTo = value;
                SetOldVersion();
            }
        }

        public Version OldVersionFrom
        {
            get => _oldVersionFrom;
            set
            {
                _oldVersionFrom = value;
                 SetOldVersion();
            }
        }

        public string AssemblyName { get; }

        internal AppConfigAssemblyRedirectContext(AppConfigContext context, XNamespace ns, XElement dependentAssemblyElement)
        {
            Element = dependentAssemblyElement;
            OriginalXml = dependentAssemblyElement.ToString();
            LineNumber = ((IXmlLineInfo)dependentAssemblyElement).LineNumber;

            Context = context;
            var assemblyIdentityElement = dependentAssemblyElement.Element(ns + AssemblyIdentityString);
            var bindingRedirectElement = dependentAssemblyElement.Element(ns + BindingRedirectString);
            AssemblyName = assemblyIdentityElement?.Attribute(NameString)?.Value;
            _bindingRedirectOldAttribute = bindingRedirectElement?.Attribute(OldVersionString);
            var range = _bindingRedirectOldAttribute?.Value;
            _bindingRedirectNewAttribute = bindingRedirectElement?.Attribute(NewVersionString);
            var newVersionString = _bindingRedirectNewAttribute?.Value;

            if (range != null && newVersionString != null)
            {
                var oldMatch = VersionRangeRegex.Match(range);
                var newMatch = VersionRegex.Match(newVersionString);

                if (oldMatch.Success && newMatch.Success)
                {
                    _oldVersionFrom = new Version(oldMatch.Groups["VersionFrom"].Value);
                    _oldVersionTo = new Version(oldMatch.Groups["VersionTo"].Value);
                    _newVersion = new Version(newMatch.Groups["Version"].Value);
                }
            }
        }

        private void SetOldVersion()
        {
            _bindingRedirectOldAttribute.Value = $"{_oldVersionFrom}-{OldVersionTo}";
        }

        private void SetNewVersion()
        {
            _bindingRedirectNewAttribute.Value = _newVersion.ToString();
        }
    }
}
