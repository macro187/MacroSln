using System.IO;
using MacroSystem;
using MacroGuards;
using MacroIO;
using System.Linq;
using System.Collections.Generic;


namespace
MacroSln
{


/// <summary>
/// A Visual Studio solution reference to a project
/// </summary>
///
/// <remarks>
/// References are in <c>.sln</c> files in the following format:
/// <code>
/// Project("&lt;TypeId&gt;") = "&lt;Name&gt;", "&lt;Location&gt;", "&lt;Id&gt;"
/// ...
/// EndProject
/// </code>
/// </remarks>
/// 
public class
VisualStudioSolutionProjectReference
{


public
VisualStudioSolutionProjectReference(
    VisualStudioSolution solution,
    string id,
    string typeId,
    string name,
    string location,
    int lineNumber,
    int lineCount)
{
    Guard.NotNull(solution, nameof(solution));
    Guard.NotNull(id, nameof(id));
    Guard.NotNull(typeId, nameof(typeId));
    Guard.NotNull(name, nameof(name));
    Guard.NotNull(location, nameof(location));

    Solution = solution;
    Id = id;
    TypeId = typeId;
    Name = name;
    Location = location;
    LineNumber = lineNumber;
    LineCount = lineCount;
}


public
VisualStudioSolution Solution { get; private set; }


public string
Id { get; private set; }


public string
TypeId { get; private set; }


public string
Name { get; private set; }


public string
Location { get; private set; }


public int
LineNumber { get; private set; }


public int
LineCount { get; private set; }


/// <summary>
/// The full, absolute path to the referenced project file
/// </summary>
///
public string
AbsoluteLocation =>
    Path.GetFullPath(
        Path.Combine(
            Path.GetDirectoryName(Solution.Path),
            Location
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)));


/// <summary>
/// Whether the referenced project file is under the same directory subtree as the solution
/// </summary>
///
public bool
IsLocal =>
    PathExtensions.IsDescendantOf(AbsoluteLocation, Path.GetDirectoryName(Solution.Path));


/// <summary>
/// Specially escaped name/path of this project used by MSBuild in project-specific targets
/// </summary>
///
/// <remarks>
/// https://docs.microsoft.com/en-us/visualstudio/msbuild/how-to-build-specific-targets-in-solutions-by-using-msbuild-exe
/// </remarks>
///
public string
MSBuildTargetName =>
    string.Join(
        "\\",
        GetNestingPath()
            .Reverse()
            .Select(p => p.Name)
            .Select(name => EscapeMSBuildTargetNameComponent(name)));


/// <summary>
/// Load the project referred to by this reference
/// </summary>
///
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Design",
    "CA1024:UsePropertiesWhereAppropriate",
    Justification = "Significant operation involving reading from disk")]
public VisualStudioProject
GetProject()
{
    return new VisualStudioProject(AbsoluteLocation);
}


public static string
FormatStart(string typeId, string name, string location, string id)
{
    Guard.NotNull(typeId, nameof(typeId));
    Guard.NotNull(name, nameof(name));
    Guard.NotNull(location, nameof(location));
    Guard.NotNull(id, nameof(id));

    location = location.Replace('/', '\\');

    return StringExtensions.FormatInvariant(
        "Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"",
        typeId, name, location, id);
}


public static string
FormatEnd()
{
    return "EndProject";
}


public override string
ToString()
{
    return StringExtensions.FormatInvariant(
        "Line {0}:{1}: {2}",
        LineNumber + 1,
        LineCount,
        FormatStart(TypeId, Name, Location, Id));
}


IEnumerable<VisualStudioSolutionProjectReference>
GetNestingPath()
{
    for (var p = this; p != null; p = p.GetNestingParent()) yield return p;
}


VisualStudioSolutionProjectReference
GetNestingParent() =>
    Solution.NestedProjects
        .Where(p => p.ChildProjectId == Id)
        .SelectMany(np => Solution.ProjectReferences.Where(p => p.Id == np.ParentProjectId))
        .SingleOrDefault();

string
EscapeMSBuildTargetNameComponent(string component) =>
    component
        .Replace('%', '_')
        .Replace('$', '_')
        .Replace('@', '_')
        .Replace(';', '_')
        .Replace('.', '_')
        .Replace('(', '_')
        .Replace(')', '_')
        .Replace('\'', '_');

}
}
