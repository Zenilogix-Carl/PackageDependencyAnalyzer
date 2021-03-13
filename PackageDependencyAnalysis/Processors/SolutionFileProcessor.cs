using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Construction;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Processors
{
    public class SolutionFileProcessor
    {
        private readonly ISolution _solution;
        private readonly ProjectFileProcessor _projectFileProcessor;
        private readonly ILogger _logger;

        public SolutionFileProcessor(ISolution solution, ProjectFileProcessor projectFileProcessor, ILogger logger)
        {
            _solution = solution ?? throw new ArgumentNullException(nameof(solution));
            _projectFileProcessor = projectFileProcessor ?? throw new ArgumentNullException(nameof(projectFileProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (_solution.Projects == null)
                throw new ArgumentException("Projects collection must be initialized", nameof(solution));
        }

        public void LoadFromFile(string path)
        {
            var solutionFile = SolutionFile.Parse(path);

            _solution.SetFile(path);
            //_solution.Projects.Clear();

            foreach (var projectInSolution in solutionFile.ProjectsInOrder)
            {
                switch (projectInSolution.ProjectType)
                {
                    case SolutionProjectType.KnownToBeMSBuildFormat:
                        _projectFileProcessor.Get(Path.GetFullPath(projectInSolution.AbsolutePath));
                        break;
                    case SolutionProjectType.SolutionFolder:
                        break;
                    default:
                        _logger.Error(_solution, $"Solution {path} contains project {projectInSolution.ProjectName} of unsupported type: {projectInSolution.ProjectType}");
                        break;
                }
            }
        }
    }
}
