using System;
using Lemon.Tools;

namespace Lemon
{
    public class LemonWeaver
    {
        private readonly Processor      _processor;
        private readonly Action<string> _log;

        public LemonWeaver(Action<string> log)
        {
            this._log = log;
            _processor = new Processor(log);
        }

        public void Process(string[] directories, params IWeaver[] weavers)
        {
            using(new Measurer("Weaving", _log))
            {
                _processor.Search(directories);
                _processor.Process(weavers);
                _processor.WriteAssemblies();
            }
        }

        public void Restore(string[] directories)
        {
            using(new Measurer("Restore", _log))
            {
                _processor.Search(directories);
                _processor.RestoreDlls();
            }
        }
    }
}