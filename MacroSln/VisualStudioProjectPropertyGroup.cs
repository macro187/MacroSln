using System.Collections.Generic;
using System.Linq;
using MacroGuards;

namespace MacroSln
{

    /// <summary>
    /// A PropertyGroup section in a .csproj file
    /// </summary>
    ///
    public class VisualStudioProjectPropertyGroup : VisualStudioProjectGroup
    {

        public static string FormatBegin()
        {
            return FormatBegin(GroupName);
        }


        public static string FormatEnd()
        {
            return FormatEnd(GroupName);
        }


        const string GroupName = "PropertyGroup";


        internal VisualStudioProjectPropertyGroup(
            IEnumerable<VisualStudioProjectProperty> properties,
            int beginLineNumber,
            int endLineNumber
        )
            : base(GroupName, beginLineNumber, endLineNumber)
        {
            Guard.NotNull(properties, nameof(properties));
            Properties = properties.ToList();
        }


        public IReadOnlyList<VisualStudioProjectProperty> Properties
        {
            get;
        }

    }
}
