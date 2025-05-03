using Lemon.Tools.Weavers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lemon.Lemon.Weavers
{
    internal class WeaverProcessor
    {
        private List<object> weavers = new();
        private readonly Action<string> _log;

        public WeaverProcessor(Action<string> log)
        {
            _log = log;
        }
        //public void Process()
        //{
        //    _log("===WEAVING===");

        //    var values = Read();

        //    foreach (var weaver in weavers)
        //    {
        //        var weaverType = weaver.GetType();
        //        var weaverName = weaverType.Assembly.GetName().Name;
        //        var weaverMethod = weaverType.GetMethod(nameof(IWeaver.Weave));
        //        _log($"Weaving with {weaverName} --------------------------------------");
        //        weaverMethod.Invoke(weaver, new object[] { values, _log });
        //        _log($"Weaving with {weaverName} is done ------------------------------");
        //    }
        //}
    }
}
