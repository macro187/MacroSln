using MacroSystem;
using MacroGuards;


namespace
MacroSln
{


/// <summary>
/// A Group section in a .csproj file
/// </summary>
///
public abstract class
VisualStudioProjectGroup
{


internal
VisualStudioProjectGroup(
    string name,
    int beginLineNumber,
    int endLineNumber)
{
    Guard.Required(name, nameof(name));
    Name = name;
    BeginLineNumber = beginLineNumber;
    EndLineNumber = endLineNumber;
}


public string
Name { get; private set; }


public int
BeginLineNumber { get; private set; }


public int
EndLineNumber { get; private set; }


public static string
FormatBegin(string name)
{
    Guard.NotNull(name, nameof(name));
    return StringExtensions.FormatInvariant("<{0}>", name);
}


public static string
FormatEnd(string name)
{
    Guard.NotNull(name, nameof(name));
    return StringExtensions.FormatInvariant("</{0}>", name);
}


public override string
ToString()
{
    return StringExtensions.FormatInvariant(
        "Line {0}-{1}: {2}...{3}",
        BeginLineNumber + 1,
        EndLineNumber + 1,
        FormatBegin(Name),
        FormatEnd(Name));
}


}
}
