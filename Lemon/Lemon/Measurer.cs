using System;
using System.Diagnostics;

namespace Lemon
{
    public struct Measurer : IDisposable
    {
        private string                   _msg;
        private Action<string, TimeSpan> _log;
        private Stopwatch                _stopwatch;

        public Measurer(string msg, Action<string> log)
        {
            _msg = msg;
            _log = (msg, span) => log($"== finished {msg} in {span.TotalSeconds} sec");
            _stopwatch = Stopwatch.StartNew();
        }

        public Measurer(string msg, Action<string, TimeSpan> log)
        {
            _msg = msg;
            _log = log;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _log(_msg, _stopwatch.Elapsed);
            _msg = null;
            _log = null;
            _stopwatch = null;
        }
    }
}