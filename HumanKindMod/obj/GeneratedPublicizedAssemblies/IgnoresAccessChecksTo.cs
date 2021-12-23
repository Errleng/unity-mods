[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("Amplitude.Mercury.Firstpass")]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
        }
    }
}