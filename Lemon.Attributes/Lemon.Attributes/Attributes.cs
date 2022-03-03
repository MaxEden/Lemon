using System;

namespace Lemon.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class WeaveMeAttribute : Attribute { }

    // [AttributeUsage(AttributeTargets.All)]
    // public sealed class GeneratedAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class LemonAttribute : Attribute
    {
        public readonly string Text;
        public LemonAttribute(string text)
        {
            Text = text;
        }
    }
}