using MacroSystem;
using MacroGuards;


namespace
MacroSln
{


/// <summary>
/// A Visual Studio solution-to-project configuration mapping entry
/// </summary>
///
/// <remarks>
/// Entries are in <c>.sln</c> files in blocks in the following format:
/// <code>
/// GlobalSection(ProjectConfigurationPlatforms) = postSolution
///     &lt;ProjectId&gt;.&lt;ProjectConfiguration&gt;.&lt;Property&gt; = &lt;SolutionConfiguration&gt;
///     ...
/// EndGlobalSection
/// </code>
/// </remarks>
/// 
public class
VisualStudioSolutionProjectConfiguration
{


public
VisualStudioSolutionProjectConfiguration(
    string projectId,
    string projectConfiguration,
    string property,
    string solutionConfiguration,
    int lineNumber)
{
    Guard.NotNull(projectId, nameof(projectId));
    Guard.NotNull(projectConfiguration, nameof(projectConfiguration));
    Guard.NotNull(property, nameof(property));
    Guard.NotNull(solutionConfiguration, nameof(solutionConfiguration));
    ProjectId = projectId;
    ProjectConfiguration = projectConfiguration;
    Property = property;
    SolutionConfiguration = solutionConfiguration;
    LineNumber = lineNumber;
}


public string
ProjectId { get; private set; }


public string
ProjectConfiguration { get; private set; }


public string
Property { get; private set; }


public string
SolutionConfiguration { get; private set; }


public int
LineNumber { get; private set; }


public static string
Format(
    string projectId,
    string projectConfiguration,
    string property,
    string solutionConfiguration)
{
    Guard.NotNull(projectId, nameof(projectId));
    Guard.NotNull(projectConfiguration, nameof(projectConfiguration));
    Guard.NotNull(property, nameof(property));
    Guard.NotNull(solutionConfiguration, nameof(solutionConfiguration));

    return StringExtensions.FormatInvariant(
        "{0}.{1}.{2} = {3}",
        projectId,
        projectConfiguration,
        property,
        solutionConfiguration);
}


public override string
ToString()
{
    return StringExtensions.FormatInvariant(
        "Line {0}: {1}",
        LineNumber + 1,
        Format(
            ProjectId,
            SolutionConfiguration,
            Property,
            ProjectConfiguration));
}


}
}
