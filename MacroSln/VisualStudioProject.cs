using System.Collections.Generic;
using System.IO;
using IOPath = System.IO.Path;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MacroGuards;
using MacroExceptions;


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


int
ProjectBeginLineNumber;


int
ProjectEndLineNumber;


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
            match = Regex.Match(line, "^\\s*</PropertyGroup>\\s*$");
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
            match = Regex.Match(line, "^\\s*<([^/>]+)>(.*)</\\1>\\s*$");
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
            match = Regex.Match(line, "^\\s*</ItemGroup>\\s*$");
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
        match = Regex.Match(line, "^\\s*<PropertyGroup[> ].*$");
        if (match.Success)
        {
            propertyGroupLineNumber = lineNumber;
            properties = new List<VisualStudioProjectProperty>();
            continue;
        }

        //
        // <ItemGroup>
        //
        match = Regex.Match(line, "^\\s*<ItemGroup[> ].*$");
        if (match.Success)
        {
            itemGroupLineNumber = lineNumber;
            continue;
        }

        //
        // <Project>
        //
        match = Regex.Match(line, "^\\s*<Project Sdk=\"Microsoft.NET.Sdk\">\\s*$");
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
        match = Regex.Match(line, "^\\s*</Project>\\s*$");
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

    if (ProjectBeginLineNumber > -1)
        throw new TextFileParseException(
            "No <Project> element in file",
            Path,
            lineNumber,
            "");

    if (ProjectEndLineNumber > -1)
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
    // Visual Studio writes .csproj files in UTF-8 with BOM and Windows-style line endings
    //
    var encoding = new UTF8Encoding(true);
    var newline = "\r\n";

    using (var f = new StreamWriter(Path, false, encoding))
    {
        f.NewLine = newline;
        foreach (var line in Lines)
        {
            f.WriteLine(line);
        }
    }
}


}
}
