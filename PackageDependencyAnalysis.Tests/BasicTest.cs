using System;
using NUnit.Framework;
using PackageDependencyAnalysis.ContextObjects;

namespace PackageDependencyAnalysis.Tests
{
    [TestFixture]
    public class BasicTest
    {
        [TestCase(@"C:\Users\carlr\Documents\Projects\zenilogix-accounting\Source\Accounting.sln")]
        public void Test(string solutionFile)
        {
            var solution = SolutionContext.Create(solutionFile);

            foreach (var project in solution.Projects)
            {
                Console.WriteLine(project.FileName);
                Console.WriteLine("---------------------------------------------");
                Console.WriteLine("XAML.cs:");
                foreach (var fileName in project.GetSourceFiles(ProjectItemType.Compile, @"\.xaml\.cs$"))
                {
                    Console.WriteLine(fileName);
                }

                Console.WriteLine("Output paths:");
                foreach (var projectOutputPath in project.OutputPaths)
                {
                    Console.WriteLine(projectOutputPath);
                }

                Console.WriteLine("Package refs:");
                foreach (var packageReference in project.PackageReferences)
                {
                    Console.WriteLine($"{packageReference.Name}; Version={packageReference.Version}");
                    foreach (var assemblyReference in packageReference.AssemblyReferences)
                    {
                        Console.WriteLine($"    {assemblyReference.AssemblyName} {assemblyReference.AssemblyVersion}");
                    }
                }

                Console.WriteLine("Project refs:");
                foreach (var projectReference in project.ProjectReferences)
                {
                    Console.WriteLine($"Project Ref: {projectReference.Name} at {projectReference.FileName}");
                }

                if (project.HasPackagesConfig)
                {
                    Console.WriteLine("packages.config:");
                    var context = project.GetPackagesConfigContext();
                    foreach (var package in context.Packages)
                    {
                        Console.WriteLine($"{package.Id} {package.Version}");
                    }
                }
                else
                {
                    Console.WriteLine("(no packages.config)");
                }

                if (project.HasAppConfig)
                {
                    Console.WriteLine("app.config:");
                    var context = project.GetAppConfigContext();
                    foreach (var redirect in context.AssemblyRedirects)
                    {
                        Console.WriteLine($"{redirect.AssemblyName} {redirect.OldVersionFrom}-{redirect.OldVersionTo} => {redirect.NewVersion}");
                    }
                }
                else
                {
                    Console.WriteLine("(no app.config)");
                }

                Console.WriteLine("---------------------------------------------");
            }
        }
    }
}
