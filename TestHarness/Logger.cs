using System;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalysis.Processors;

namespace TestHarness
{
    public class Logger : ILogger
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void Error(IContext context, string message)
        {
            Console.WriteLine($"ERROR: ({context.File}) {message}");
        }

        public void Warning(IContext context, string message)
        {
            Console.WriteLine($"WARNING: ({context.File}) {message}");
        }

        public void Info(IContext context, string message)
        {
            Console.WriteLine($"Info: ({context.File}) {message}");
        }
    }
}
