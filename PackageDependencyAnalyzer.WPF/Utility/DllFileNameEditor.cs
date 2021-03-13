using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using Microsoft.Win32;
using PackageDependencyAnalyzer.ViewModel;

namespace PackageDependencyAnalyzer.Utility
{
    public class DllFileNameEditor : UITypeEditor // System.Windows.Forms.Design.FileNameEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        //protected override void InitializeDialog(System.Windows.Forms.OpenFileDialog openFileDialog)
        //{
        //    openFileDialog.Filter = "Assemblies (*.dll)|*.dll";
        //}

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (context?.Instance is EditPackageReferenceViewModel vm && value is string path)
            {
                if (!Path.IsPathRooted(path))
                {
                    path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(vm.Project.AbsolutePath), path));
                }

                var dlg = new OpenFileDialog
                {
                    Filter = "Assemblies (*.dll)|*.dll",
                    InitialDirectory = Path.GetDirectoryName(path),
                    FileName = path
                };

                if (dlg.ShowDialog() == true)
                {
                    path = dlg.FileName;
                    if (Path.IsPathRooted(path))
                    {
                        var projectFolder = Path.GetDirectoryName(vm.Project.AbsolutePath);
                        path = Utility.PathHelper.MakeRelativePath(projectFolder, path);
                    }

                    return path;
                }
            }

            return base.EditValue(context, provider, value);
        }
    }
}
