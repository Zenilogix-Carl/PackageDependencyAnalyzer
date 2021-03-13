using System;
using System.Text;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalysis.Processors;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class LoggerViewModel : ViewModelBase, ILogger
    {
        private readonly StringBuilder _sb = new StringBuilder(short.MaxValue, int.MaxValue);
        private int _counter;

        public string Messages
        {
            get
            {
                lock (_sb)
                {
                    return _sb.ToString();
                }
            }
        }

        public LoggerViewModel()
        {
            _sb.Clear();
        }

        public void Clear()
        {
            lock (_sb)
            {
                _sb.Clear();
                Update();
            }
        }

        public void Error(IContext context, string message)
        {
            WriteLine($"ERROR: {context.File}: {message}");
        }

        public void Warning(IContext context, string message)
        {
            WriteLine($"WARNING: {context.File}: {message}");
        }

        public void Info(IContext context, string message)
        {
            WriteLine($"Info: {context.File}: {message}");
        }

        public void WriteLine(string message)
        {
            lock (_sb)
            {
                _sb.AppendLine(message);
            }

            if (_counter-- <= 0)
            {
                _counter = 100;
                UpdateAsync();
            }
        }

        private void Update()
        {
            RaisePropertyChanged(nameof(Messages));
        }

        public void UpdateAsync()
        {
            Dispatcher.CurrentDispatcher.Invoke(Update);
        }
    }
}
