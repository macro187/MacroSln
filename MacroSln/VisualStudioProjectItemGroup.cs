namespace
MacroSln
{


/// <summary>
/// An ItemGroup section in a .csproj file
/// </summary>
///
public class
VisualStudioProjectItemGroup
    : VisualStudioProjectGroup
{


internal
VisualStudioProjectItemGroup(
    int beginLineNumber,
    int endLineNumber
)
    : base("ItemGroup", beginLineNumber, endLineNumber)
{
}


}
}
