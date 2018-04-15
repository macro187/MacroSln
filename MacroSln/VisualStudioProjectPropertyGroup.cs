using System.Collections.Generic;
using System.Linq;
using MacroGuards;


namespace
MacroSln
{


/// <summary>
/// A PropertyGroup section in a .csproj file
/// </summary>
///
public class
VisualStudioProjectPropertyGroup
    : VisualStudioProjectGroup
{


internal
VisualStudioProjectPropertyGroup(
    IEnumerable<VisualStudioProjectProperty> properties,
    int beginLineNumber,
    int endLineNumber
)
    : base("PropertyGroup", beginLineNumber, endLineNumber)
{
    Guard.NotNull(properties, nameof(properties));
    Properties = properties.ToList();
}


public IReadOnlyList<VisualStudioProjectProperty>
Properties
{
    get;
}


}
}
