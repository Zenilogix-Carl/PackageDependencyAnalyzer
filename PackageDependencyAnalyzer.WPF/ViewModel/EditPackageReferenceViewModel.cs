using System;
using System.ComponentModel;
using GalaSoft.MvvmLight.Messaging;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalyzer.Messages;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class EditPackageReferenceViewModel
    {
        private readonly PackageReference _packageReference;

        [Browsable(false)]
        public IProject Project { get; }

        public string Version
        {
            get => _packageReference?.Version.ToString();
            set
            {
                _packageReference.Version = new Version(value);
                Messenger.Default.Send(new ProjectPackageReferenceModified());
            }
        }

        [Description("Contains a pre-release suffix if applicable; empty for normal release")]
        public string PreRelease
        {
            get => _packageReference?.PreReleaseSuffix;
            set
            {
                _packageReference.PreReleaseSuffix = value;
                Messenger.Default.Send(new ProjectPackageReferenceModified());
            }
        }

        [DisplayName("Binding Redirection Min Version")]
        public string BindingRedirectionFrom
        {
            get => _packageReference?.BindingRedirection?.OldVersionFrom?.ToString();
            set
            {
                if (_packageReference.BindingRedirection != null)
                {
                    _packageReference.BindingRedirection.OldVersionFrom = new Version(value);
                    Messenger.Default.Send(new BindingRedirectModified());
                }
            }
        }

        [DisplayName("Binding Redirection Max Version")]
        public string BindingRedirectionTo
        {
            get => _packageReference?.BindingRedirection?.OldVersionTo?.ToString();
            set
            {
                if (_packageReference.BindingRedirection != null)
                {
                    _packageReference.BindingRedirection.OldVersionTo = new Version(value);
                    Messenger.Default.Send(new BindingRedirectModified());
                }
            }
        }

        [DisplayName("Binding Redirection Target Version")]
        public string BindingRedirectionTarget
        {
            get => _packageReference?.BindingRedirection?.NewVersion?.ToString();
            set
            {
                if (_packageReference.BindingRedirection != null)
                {
                    _packageReference.BindingRedirection.NewVersion = new Version(value);
                    Messenger.Default.Send(new BindingRedirectModified());
                }
            }
        }

        //[Editor(typeof(DllFileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string HintPath
        {
            get => _packageReference?.HintPath;
            set
            {
                _packageReference.HintPath = string.IsNullOrWhiteSpace(value) ? null : value;
                Messenger.Default.Send(new ProjectPackageReferenceModified());
            }
        }

        public EditPackageReferenceViewModel(IProject project, PackageReference packageReference)
        {
            Project = project;
            _packageReference = packageReference;
        }
    }
}
