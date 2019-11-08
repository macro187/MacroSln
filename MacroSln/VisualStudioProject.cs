using System.Collections.Generic;
using System.IO;
using IOPath = System.IO.Path;
using System.Linq;
using System.Text.RegularExpressions;
using MacroGuards;
using MacroExceptions;
using MacroCollections;
using MacroIO;
using MacroSystem;
using System.Text;

namespace
MacroSln
{


/// <summary>
/// A 2017-era "SDK" style <c>.csproj</c> file
/// </summary>
///
/// <remarks>
/// This class contains the entire contents of the file, and can interpret and modify some parts of it without
/// disturbing the others.
/// </remarks>
///
public class
VisualStudioProject
{


/// <summary>
/// Create a new, empty <c>.csproj</c> file that uses the .NET Core SDK
/// </summary>
///
/// <param name="path">
/// The path to the <c>.csproj</c> file to create
/// </param>
///
/// <returns>
/// The newly-created <see cref="VisualStudioProject"/>
/// </returns>
///
/// <remarks>
/// If a file already exists at <paramref name="path"/>, it is completely replaced with no consideration given to its
/// existing encoding, line-ending, or BOM conventions.
///
/// This method can throw IO-related exceptions, see
/// <see cref="File.WriteAllLines(string, IEnumerable{string}, Encoding)"/> and
/// <see cref="File.ReadLines(string)"/> for details.
/// </remarks>
///
public static VisualStudioProject
Create(string path)
{
    return Create(path, VisualStudioProjectSdk.DotNetCore);
}


/// <summary>
/// Create a new, empty <c>.csproj</c> file
/// </summary>
///
/// <param name="path">
/// The path to the <c>.csproj</c> file to create
/// </param>
///
/// <param name="sdk">
/// The .Net Core SDK to be used by the project
/// </param>
///
/// <returns>
/// The newly-created <see cref="VisualStudioProject"/>
/// </returns>
///
/// <remarks>
/// If a file already exists at <paramref name="path"/>, it is completely replaced with no consideration given to its
/// existing encoding, line-ending, or BOM conventions.
///
/// This method can throw IO-related exceptions, see
/// <see cref="File.WriteAllLines(string, IEnumerable{string}, Encoding)"/> and
/// <see cref="File.ReadLines(string)"/> for details.
/// </remarks>
///
public static VisualStudioProject
Create(string path, VisualStudioProjectSdk sdk)
{
    Guard.NotNull(path, nameof(path));
    Guard.NotNull(sdk, nameof(sdk));

    File.WriteAllLines(
        path,
        new [] {
            $"<Project Sdk=\"{sdk.Id}\">",
            $"</Project>",
        },
        new UTF8Encoding(false));

    return new VisualStudioProject(path);
}


/// <summary>
/// Load a project from a <c>.csproj</c> file
/// </summary>
///
/// <remarks>
/// This constructor can throw IO-related exceptions, see <see cref="File.ReadLines(string)"/> for details.
/// </remarks>
///
public
VisualStudioProject(string path)
{
    Guard.NotNull(path, nameof(path));

    Path = path;
    Name = IOPath.GetFileNameWithoutExtension(path);
    _lines = File.ReadLines(path).ToList();
    Load();
}


/// <summary>
/// The location of the <c>.csproj</c> file
/// </summary>
///
public string
Path
{
    get;
}


/// <summary>
/// The name of the project according to the <c>.csproj</c> filename
/// </summary>
///
public string
Name
{
    get;
}


/// <summary>
/// Lines of text
/// </summary>
///
public IEnumerable<string>
Lines
{
    get { return _lines; }
}

IList<string>
_lines;


public IReadOnlyList<VisualStudioProjectGroup>
Groups
{
    get { return _groups; }
}

List<VisualStudioProjectGroup>
_groups;


/// <summary>
/// All framework monikers specifed in either the <c>TargetFramework</c> or <c>TargetFrameworks</c> properties
/// </summary>
///
public IEnumerable<string> AllTargetFrameworks =>
    new []{ GetProperty("TargetFramework") }
        .Concat(GetProperty("TargetFrameworks").Split(';'))
        .Select(s => s.Trim())
        .Where(s => s != "");


int
ProjectBeginLineNumber;


int
ProjectEndLineNumber;


/// <summary>
/// Get the value of a project property
/// </summary>
///
/// <returns>
/// The value of the project property, if present
/// - OR -
/// An empty string, if not present
/// </returns>
///
public string
GetProperty(string name)
{
    Guard.Required(name, nameof(name));
    return
        Groups
            .OfType<VisualStudioProjectPropertyGroup>()
            .SelectMany(g => g.Properties)
            .Where(p => p.Name == name)
            .Select(p => p.Value)
            .FirstOrDefault() ?? "";
}


/// <summary>
/// Set the value of a project property
/// </summary>
///
/// <remarks>
/// If the property is already present, its value is changed.  Otherwise, the property is added, along with a containing
/// PropertyGroup if necessary.
/// </remarks>
///
public void
SetProperty(string name, string value)
{
    Guard.Required(name, nameof(name));
    Guard.NotNull(value, nameof(value));

    if (!Groups.OfType<VisualStudioProjectPropertyGroup>().Any())
    {
        _lines.Insert(
            ProjectBeginLineNumber + 1,
            "",
            "  " + VisualStudioProjectPropertyGroup.FormatBegin(),
            "  " + VisualStudioProjectPropertyGroup.FormatEnd(),
            "");
        Load();
    }

    var insertAt = Groups.OfType<VisualStudioProjectPropertyGroup>().First().EndLineNumber;

    var existingProp =
        Groups
            .OfType<VisualStudioProjectPropertyGroup>()
            .SelectMany(g => g.Properties)
            .Where(p => p.Name == name)
            .FirstOrDefault();

    if (existingProp != null)
    {
        insertAt = existingProp.LineNumber;
        _lines.RemoveAt(insertAt);
    }

    _lines.Insert(
        insertAt,
        "    " + VisualStudioProjectProperty.Format(name, value));

    Load();
}


/// <summary>
/// Interpret information in the project file
/// </summary>
///
void
Load()
{
    _groups = new List<VisualStudioProjectGroup>();
    ProjectBeginLineNumber = -1;
    ProjectEndLineNumber = -1;

    int lineNumber = -1;
    Match match;
    int propertyGroupLineNumber = -1;
    List<VisualStudioProjectProperty> properties = null;
    int itemGroupLineNumber = -1;
    foreach (var line in Lines)
    {
        lineNumber++;
        
        //
        // Ignore blank lines and comments
        //
        if (string.IsNullOrWhiteSpace(line)) continue;

        if (propertyGroupLineNumber > -1)
        {

            //
            // </PropertyGroup>
            //
            match = Regex.Match(line, @"^\s*</PropertyGroup>\s*$");
            if (match.Success)
            {
                _groups.Add(
                    new VisualStudioProjectPropertyGroup(
                        properties,
                        propertyGroupLineNumber,
                        lineNumber));
                properties = null;
                propertyGroupLineNumber = -1;
                continue;
            }

            //
            // <Name>Value</Name>
            //
            match = Regex.Match(line, @"^\s*<([^/>]+)>(.*)</\1>\s*$");
            if (match.Success)
            {
                properties.Add(
                    new VisualStudioProjectProperty(
                        match.Groups[1].ToString(),
                        match.Groups[2].ToString(),
                        lineNumber));
                continue;
            }

            continue;
        }

        if (itemGroupLineNumber > -1)
        {

            //
            // </ItemGroup>
            //
            match = Regex.Match(line, @"^\s*</ItemGroup>\s*$");
            if (match.Success)
            {
                _groups.Add(
                    new VisualStudioProjectItemGroup(
                        itemGroupLineNumber,
                        lineNumber));
                itemGroupLineNumber = -1;
                continue;
            }

            continue;
        }

        //
        // <PropertyGroup>
        //
        match = Regex.Match(line, @"^\s*<PropertyGroup[> ].*$");
        if (match.Success)
        {
            propertyGroupLineNumber = lineNumber;
            properties = new List<VisualStudioProjectProperty>();
            continue;
        }

        //
        // <ItemGroup>
        //
        match = Regex.Match(line, @"^\s*<ItemGroup[> ].*$");
        if (match.Success)
        {
            itemGroupLineNumber = lineNumber;
            continue;
        }

        //
        // <Project>
        //
        match = Regex.Match(line, @"^\s*<Project Sdk=""Microsoft.NET.Sdk(?:\.[^.]+)*"">\s*$");
        if (match.Success)
        {
            if (ProjectBeginLineNumber > -1)
                throw new TextFileParseException(
                    "Multiple <Project> elements encountered",
                    Path,
                    lineNumber,
                    line);
            ProjectBeginLineNumber = lineNumber;
            continue;
        }

        //
        // </Project>
        //
        match = Regex.Match(line, @"^\s*</Project>\s*$");
        if (match.Success)
        {
            if (ProjectEndLineNumber > -1)
                throw new TextFileParseException(
                    "Multiple </Project> elements encountered",
                    Path,
                    lineNumber,
                    line);
            ProjectEndLineNumber = lineNumber;
            continue;
        }

        //
        // Nothing special
        //
    }

    if (ProjectBeginLineNumber == -1)
        throw new TextFileParseException(
            "No <Project> element in file",
            Path,
            lineNumber,
            "");

    if (ProjectEndLineNumber == -1)
        throw new TextFileParseException(
            "No </Project> element in file",
            Path,
            lineNumber,
            "");
}


/// <summary>
/// Save information to the <c>.csproj</c> file
/// </summary>
///
public void
Save()
{
    //
    // The dotnet tool writes .csproj files with Windows line endings and no UTF-8 BOM
    //
    FileExtensions.RewriteAllLines(Path, Lines, LineEnding.CRLF, false);
}


}
}
