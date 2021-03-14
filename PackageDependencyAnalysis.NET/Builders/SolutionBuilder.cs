using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PackageDependencyAnalysis.ContextObjects;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Builders
{
    public class SolutionBuilder
    {
        private readonly ProjectBuilder _projectBuilder;

        public SolutionBuilder(ProjectBuilder projectBuilder)
        {
            _projectBuilder = projectBuilder ?? throw new ArgumentNullException(nameof(projectBuilder));
        }

        public async Task BuildSolution(ISolution solution, string solutionFileName)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            if (solutionFileName == null) throw new ArgumentNullException(nameof(solutionFileName));

            var context = new SolutionContext(solutionFileName);

            var tasks = new List<Task>();

            foreach (var projectContext in context.Projects)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var project = await _projectBuilder.CreateProject(projectContext);
                    solution.ProjectCache[projectContext.Name] = project;
                    Console.WriteLine($"SolutionBuilder: created project {project.Name}");
                }));
            }

            await Task.WhenAll(tasks);
        }
    }
}
