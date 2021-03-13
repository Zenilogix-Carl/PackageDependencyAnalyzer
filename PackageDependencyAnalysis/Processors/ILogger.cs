using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Processors
{
    public interface ILogger
    {
        void Error(IContext context, string message);
        void Warning(IContext context, string message);
        void Info(IContext context, string message);
        void WriteLine(string message);
    }
}
