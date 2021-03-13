using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class SolutionContext
    {
        private readonly SolutionFile _solutionFile;
        private Dictionary<string, ProjectContext> _projectDictionary;

        public string FileName { get; }
        public string Directory => Path.GetDirectoryName(FileName);

        public static SolutionContext Create(string fileName)
        {
            return new SolutionContext(fileName);
        }

        internal SolutionContext(string fileName)
        {
            FileName = fileName;
            _solutionFile = SolutionFile.Parse(fileName);
        }

        public ProjectContext GetProject(string path)
        {
            if (_projectDictionary == null)
            {
                _projectDictionary = Projects.ToDictionary(s => s.FileName);
            }

            return _projectDictionary.TryGetValue(path, out var project) ? project : null;
        }

        public IEnumerable<ProjectContext> Projects
        {
            get
            {
                if (_projectDictionary != null)
                {
                    foreach (var value in _projectDictionary.Values)
                    {
                        yield return value;
                    }
                }
                else
                {
                    foreach (var projectInSolution in _solutionFile.ProjectsInOrder)
                    {
                        switch (projectInSolution.ProjectType)
                        {
                            case SolutionProjectType.KnownToBeMSBuildFormat:
                                yield return ProjectContext.Create(this, Path.GetFullPath(Path.Combine(Directory, projectInSolution.AbsolutePath)));
                                break;
                        }
                    }
                }
            }
        }

        public IEnumerable<IProject> GetProjects(Func<ProjectContext, IProject> projectFactoryFunc) => Projects.Select(projectFactoryFunc);
    }
}