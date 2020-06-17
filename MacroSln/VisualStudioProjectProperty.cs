using MacroGuards;
using MacroSystem;

namespace MacroSln
{

    /// <summary>
    /// A property entry in a PropertyGroup section in a .csproj file
    /// </summary>
    ///
    public class VisualStudioProjectProperty
    {

        internal VisualStudioProjectProperty(
            string name,
            string value,
            int lineNumber
        )
        {
            Guard.Required(name, nameof(name));
            Guard.NotNull(value, nameof(value));
            Name = name;
            Value = value;
            LineNumber = lineNumber;
        }


        public string Name
        {
            get;
        }


        public string Value
        {
            get;
        }


        public int LineNumber
        {
            get;
        }


        public static string Format(string name, string value)
        {
            Guard.NotNull(name, nameof(name));
            Guard.NotNull(value, nameof(value));
            return StringExtensions.FormatInvariant(
                "<{0}>{1}</{0}>",
                name,
                value);
        }


        public override string ToString()
        {
            return StringExtensions.FormatInvariant(
                "Line {0}: {1}",
                LineNumber + 1,
                Format(Name, Value));
        }

    }
}
