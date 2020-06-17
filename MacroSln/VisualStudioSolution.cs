using System;
using static System.FormattableString;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MacroSystem;
using MacroGuards;
using MacroCollections;
using MacroExceptions;
using MacroIO;

namespace MacroSln
{

    /// <summary>
    /// A Visual Studio solution (<c>.sln</c>) file
    /// </summary>
    ///
    /// <remarks>
    /// This class contains the entire contents of the file, and can interpret and modify some parts of it without
    /// disturbing the others.
    /// </remarks>
    ///
    public class VisualStudioSolution
    {

        /// <summary>
        /// Find and load the solution in a directory
        /// </summary>
        ///
        /// <returns>
        /// The solution
        /// - OR -
        /// <c>null</c> if the directory contained no solution
        /// </returns>
        ///
        /// <exception cref="UserException">
        /// The directory contained more than one solution
        /// </exception>
        ///
        public static VisualStudioSolution Find(string directoryPath)
        {
            Guard.NotNull(directoryPath, nameof(directoryPath));
            if (!Directory.Exists(directoryPath))
                throw new ArgumentException("Directory doesn't exist", "directoryPath");

            IList<string> slns = Directory.GetFiles(directoryPath, "*.sln");

            if (slns.Count > 1)
                throw new UserException(
                    StringExtensions.FormatInvariant(
                        "More than one .sln found in {0}",
                        directoryPath));

            if (slns.Count == 0)
                return null;

            return new VisualStudioSolution(slns[0]);
        }


        /// <summary>
        /// Load a solution from a <c>.sln</c> file
        /// </summary>
        ///
        /// <remarks>
        /// This constructor can throw IO-related exceptions, see <see cref="File.ReadLines(string)"/> for details.
        /// </remarks>
        ///
        public VisualStudioSolution(string path)
        {
            Guard.NotNull(path, nameof(path));

            Path = path;
            _lines = File.ReadLines(path).ToList();
            Load();
        }


        /// <summary>
        /// The location of the <c>.sln</c> file
        /// </summary>
        ///
        public string Path
        {
            get;
            private set;
        }


        /// <summary>
        /// Lines of text
        /// </summary>
        ///
        public IEnumerable<string> Lines
        {
            get { return _lines; }
        }

        IList<string> _lines;


        public int GlobalStartLineNumber
        {
            get; private set;
        }


        public int GlobalEndLineNumber
        {
            get; private set;
        }


        public int NestedProjectsStartLineNumber
        {
            get; private set;
        }


        public int NestedProjectsEndLineNumber
        {
            get; private set;
        }


        public int SolutionConfigurationsStartLineNumber
        {
            get; private set;
        }


        public int SolutionConfigurationsEndLineNumber
        {
            get; private set;
        }


        public int ProjectConfigurationsStartLineNumber
        {
            get; private set;
        }


        public int ProjectConfigurationsEndLineNumber
        {
            get; private set;
        }


        /// <summary>
        /// Project references
        /// </summary>
        ///
        public IEnumerable<VisualStudioSolutionProjectReference> ProjectReferences
        {
            get { return _projectReferences; }
        }

        IList<VisualStudioSolutionProjectReference> _projectReferences;


        /// <summary>
        /// Solution folders
        /// </summary>
        ///
        /// <remarks>
        /// Solution folders are implemented as a special kind of project reference
        /// </remarks>
        ///
        public IEnumerable<VisualStudioSolutionProjectReference> SolutionFolders
        {
            get { return ProjectReferences.Where(p => p.TypeId == VisualStudioProjectTypeIds.SolutionFolder); }
        }


        /// <summary>
        /// Nested project entries
        /// </summary>
        ///
        public IEnumerable<VisualStudioSolutionNestedProject> NestedProjects
        {
            get { return _nestedProjects; }
        }

        IList<VisualStudioSolutionNestedProject> _nestedProjects;


        /// <summary>
        /// Solution configurations
        /// </summary>
        ///
        public ISet<string> SolutionConfigurations
        {
            get { return _solutionConfigurations; }
        }

        ISet<string> _solutionConfigurations;


        /// <summary>
        /// Project configurations
        /// </summary>
        ///
        public IEnumerable<VisualStudioSolutionProjectConfiguration> ProjectConfigurations
        {
            get { return _projectConfigurations; }
        }

        IList<VisualStudioSolutionProjectConfiguration> _projectConfigurations;


        /// <summary>
        /// Get a project reference with a specified id
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// No project with specified <paramref name="id"/> in solution
        /// </exception>
        ///
        public VisualStudioSolutionProjectReference GetProjectReference(string id)
        {
            Guard.NotNull(id, nameof(id));

            var project = ProjectReferences.SingleOrDefault(p => p.Id == id);
            if (project == null)
                throw new ArgumentException(
                    StringExtensions.FormatInvariant("No project with Id {0} in solution", id),
                    "id");

            return project;
        }


        /// <summary>
        /// Add a project reference to the solution
        /// </summary>
        ///
        public VisualStudioSolutionProjectReference AddProjectReference(
            string typeId,
            string name,
            string location,
            string id)
        {
            Guard.NotNull(typeId, nameof(typeId));
            Guard.NotNull(name, nameof(name));
            Guard.NotNull(location, nameof(location));
            Guard.NotNull(id, nameof(id));

            _lines.Insert(
                GlobalStartLineNumber,
                VisualStudioSolutionProjectReference.FormatStart(typeId, name, location, id),
                VisualStudioSolutionProjectReference.FormatEnd());

            Load();

            return GetProjectReference(id);
        }


        /// <summary>
        /// Delete a project reference
        /// </summary>
        ///
        public void DeleteProjectReference(VisualStudioSolutionProjectReference projectReference)
        {
            Guard.NotNull(projectReference, nameof(projectReference));

            _lines.RemoveAt(projectReference.LineNumber, projectReference.LineCount);

            Load();
        }


        /// <summary>
        /// Delete a project reference and anything else relating to it
        /// </summary>
        ///
        public void DeleteProjectReferenceAndRelated(VisualStudioSolutionProjectReference projectReference)
        {
            Guard.NotNull(projectReference, nameof(projectReference));

            var projectId = projectReference.Id;

            //
            // Delete related NestedProjects entries
            //
            for (;;)
            {
                var nesting =
                    NestedProjects.FirstOrDefault(n => n.ParentProjectId == projectId || n.ChildProjectId == projectId);
                if (nesting == null) break;
                    
                DeleteNestedProject(nesting);
            }

            //
            // Delete related project configurations
            //
            for (;;)
            {
                var configuration = ProjectConfigurations.FirstOrDefault(c => c.ProjectId == projectId);
                if (configuration == null) break;

                DeleteProjectConfiguration(configuration);
            }

            //
            // Delete the project reference itself
            //
            DeleteProjectReference(GetProjectReference(projectId));
        }


        /// <summary>
        /// Add a nested projects section
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// The solution already contains a nested projects section
        /// </exception>
        ///
        public void AddNestedProjectsSection()
        {
            if (NestedProjectsStartLineNumber >= 0)
                throw new InvalidOperationException("Solution already contains a nested projects section");

            _lines.Insert(
                GlobalEndLineNumber,
                "\tGlobalSection(NestedProjects) = preSolution",
                "\tEndGlobalSection");

            Load();
        }


        /// <summary>
        /// Add a nested project entry
        /// </summary>
        ///
        public void AddNestedProject(string childProjectId, string parentProjectId)
        {
            Guard.NotNull(childProjectId, nameof(childProjectId));
            Guard.NotNull(parentProjectId, nameof(parentProjectId));

            if (NestedProjectsStartLineNumber < 0)
                AddNestedProjectsSection();

            _lines.Insert(
                NestedProjectsEndLineNumber,
                "\t\t" + VisualStudioSolutionNestedProject.Format(childProjectId, parentProjectId));

            Load();
        }


        /// <summary>
        /// Delete a nested project entry
        /// </summary>
        ///
        public void DeleteNestedProject(VisualStudioSolutionNestedProject nestedProject)
        {
            Guard.NotNull(nestedProject, nameof(nestedProject));

            _lines.RemoveAt(nestedProject.LineNumber);

            Load();
        }


        /// <summary>
        /// Add a solution folder to the solution
        /// </summary>
        ///
        public VisualStudioSolutionProjectReference AddSolutionFolder(string name)
        {
            return AddSolutionFolder(name, Guid.NewGuid().ToString("B").ToUpperInvariant());
        }


        /// <summary>
        /// Add a solution folder to the solution
        /// </summary>
        ///
        public VisualStudioSolutionProjectReference AddSolutionFolder(string name, string id)
        {
            Guard.Required(name, nameof(name));
            Guard.Required(id, nameof(id));
            if (SolutionFolders.Any(f => f.Id == id))
                throw new InvalidOperationException(Invariant($"Solution already contains a folder with id {id}"));
            return AddProjectReference(VisualStudioProjectTypeIds.SolutionFolder, name, name, id);
        }


        /// <summary>
        /// Completely delete a solution folder and all its contents
        /// </summary>
        ///
        /// <remarks>
        /// To delete just the solution folder project reference, use <see cref="DeleteProjectReference(string)"/>
        /// </remarks>
        ///
        public void DeleteSolutionFolder(VisualStudioSolutionProjectReference solutionFolder)
        {
            Guard.NotNull(solutionFolder, nameof(solutionFolder));

            if (solutionFolder.TypeId != VisualStudioProjectTypeIds.SolutionFolder)
                throw new ArgumentException("Not a solution folder", "solutionFolder");

            var solutionFolderId = solutionFolder.Id;

            DeleteSolutionFolderContents(solutionFolder);
            DeleteProjectReferenceAndRelated(GetProjectReference(solutionFolderId));
        }


        /// <summary>
        /// Completely delete the contents of a solution folder
        /// </summary>
        ///
        public void DeleteSolutionFolderContents(VisualStudioSolutionProjectReference solutionFolder)
        {
            Guard.NotNull(solutionFolder, nameof(solutionFolder));

            if (solutionFolder.TypeId != VisualStudioProjectTypeIds.SolutionFolder)
                throw new ArgumentException("Not a solution folder", "solutionFolder");

            var solutionFolderId = solutionFolder.Id;

            for(;;)
            {
                var childProject =
                    NestedProjects
                        .Where(np => np.ParentProjectId == solutionFolderId)
                        .Select(np => GetProjectReference(np.ChildProjectId))
                        .FirstOrDefault();
                if (childProject == null) break;
                
                if (childProject.TypeId == VisualStudioProjectTypeIds.SolutionFolder)
                {
                    DeleteSolutionFolder(childProject);
                }
                else
                {
                    DeleteProjectReferenceAndRelated(childProject);
                }
            }
        }


        /// <summary>
        /// Add a project configurations section
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// The solution already contains a project configurations section
        /// </exception>
        ///
        public void AddProjectConfigurationsSection()
        {
            if (ProjectConfigurationsStartLineNumber >= 0)
                throw new InvalidOperationException("Solution already contains a project configurations section");

            int lineNumber = GlobalEndLineNumber;
            if (SolutionConfigurationsEndLineNumber >= 0)
            {
                lineNumber = SolutionConfigurationsEndLineNumber + 1;
            }

            _lines.Insert(
                lineNumber,
                "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution",
                "\tEndGlobalSection");

            Load();
        }


        /// <summary>
        /// Add a project configuration entry
        /// </summary>
        ///
        public void AddProjectConfiguration(
            string projectId,
            string projectConfiguration,
            string property,
            string solutionConfiguration)
        {
            Guard.NotNull(projectId, nameof(projectId));
            Guard.NotNull(projectConfiguration, nameof(projectConfiguration));
            Guard.NotNull(property, nameof(property));
            Guard.NotNull(solutionConfiguration, nameof(solutionConfiguration));

            if (ProjectConfigurationsStartLineNumber < 0)
                AddProjectConfigurationsSection();

            int lineNumber = ProjectConfigurationsEndLineNumber;

            var next =
                ProjectConfigurations
                    .FirstOrDefault(c =>
                        c.ProjectId == projectId &&
                        string.CompareOrdinal(c.SolutionConfiguration, solutionConfiguration) > 0);
            if (next != null)
                lineNumber = next.LineNumber;

            var prev =
                ProjectConfigurations
                    .LastOrDefault(c => c.ProjectId == projectId && c.SolutionConfiguration == solutionConfiguration)
                ?? ProjectConfigurations
                    .LastOrDefault(c => c.ProjectId == projectId);
            if (prev != null)
                lineNumber = prev.LineNumber + 1;

            _lines.Insert(
                lineNumber,
                "\t\t" + VisualStudioSolutionProjectConfiguration.Format(
                    projectId,
                    projectConfiguration,
                    property,
                    solutionConfiguration));

            Load();
        }


        /// <summary>
        /// Delete a project configuration entry
        /// </summary>
        ///
        public void DeleteProjectConfiguration(VisualStudioSolutionProjectConfiguration configuration)
        {
            Guard.NotNull(configuration, nameof(configuration));

            _lines.RemoveAt(configuration.LineNumber);

            Load();
        }


        /// <summary>
        /// Interpret information in the solution file
        /// </summary>
        ///
        void Load()
        {
            GlobalStartLineNumber = -1;
            GlobalEndLineNumber = -1;
            NestedProjectsStartLineNumber = -1;
            NestedProjectsEndLineNumber = -1;
            SolutionConfigurationsStartLineNumber = -1;
            SolutionConfigurationsEndLineNumber = -1;
            ProjectConfigurationsStartLineNumber = -1;
            ProjectConfigurationsEndLineNumber = -1;
            _projectReferences = new List<VisualStudioSolutionProjectReference>();
            _nestedProjects = new List<VisualStudioSolutionNestedProject>();
            _solutionConfigurations = new HashSet<string>();
            _projectConfigurations = new List<VisualStudioSolutionProjectConfiguration>();

            int lineNumber = -1;
            int projectReferenceStartLineNumber = -1;
            string id = "";
            string typeId = "";
            string name = "";
            string location = "";
            Match match;
            foreach (var line in Lines)
            {
                lineNumber++;
                
                //
                // Ignore blank lines and comments
                //
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Trim().StartsWith("#", StringComparison.Ordinal)) continue;

                //
                // In a project reference block
                //
                if (projectReferenceStartLineNumber >= 0)
                {
                    if (line.Trim() == "EndProject")
                    {
                        _projectReferences.Add(
                            new VisualStudioSolutionProjectReference(
                                this,
                                id,
                                typeId,
                                name,
                                location,
                                projectReferenceStartLineNumber,
                                lineNumber - projectReferenceStartLineNumber + 1));
                        projectReferenceStartLineNumber = -1;
                        id = "";
                        typeId = "";
                        name = "";
                        location = "";
                    }
                    continue;
                }

                //
                // In nested projects block
                //
                if (NestedProjectsStartLineNumber >= 0 && NestedProjectsEndLineNumber < 0)
                {
                    if (line.Trim() == "EndGlobalSection")
                    {
                        NestedProjectsEndLineNumber = lineNumber;
                        continue;
                    }

                    match = Regex.Match(line, @"(\S+) = (\S+)");
                    if (!match.Success)
                        throw new TextFileParseException(
                            "Expected '{guid} = {guid}'",
                            lineNumber + 1,
                            line);

                    _nestedProjects.Add(
                        new VisualStudioSolutionNestedProject(
                            match.Groups[1].Value,
                            match.Groups[2].Value,
                            lineNumber));

                    continue;
                }

                //
                // In solution configurations block
                //
                if (SolutionConfigurationsStartLineNumber >= 0 && SolutionConfigurationsEndLineNumber < 0)
                {
                    if (line.Trim() == "EndGlobalSection")
                    {
                        SolutionConfigurationsEndLineNumber = lineNumber;
                        continue;
                    }

                    match = Regex.Match(line, @"^\s*([^=]+) = (.+)$");
                    if (!match.Success)
                        throw new TextFileParseException(
                            "Expected '{configuration} = {configuration}'",
                            lineNumber + 1,
                            line);

                    _solutionConfigurations.Add(match.Groups[1].Value.Trim());

                    continue;
                }

                //
                // In project configurations block
                //
                if (ProjectConfigurationsStartLineNumber >= 0 && ProjectConfigurationsEndLineNumber < 0)
                {
                    if (line.Trim() == "EndGlobalSection")
                    {
                        ProjectConfigurationsEndLineNumber = lineNumber;
                        continue;
                    }

                    match = Regex.Match(line, @"^\s*([^.]+)\.([^.]+)\.(.+) = (.+)$");
                    if (!match.Success)
                        throw new TextFileParseException(
                            "Expected '{guid}.{configuration}.{property} = {configuration}'",
                            lineNumber + 1,
                            line);

                    _projectConfigurations.Add(
                        new VisualStudioSolutionProjectConfiguration(
                            match.Groups[1].Value,
                            match.Groups[2].Value,
                            match.Groups[3].Value,
                            match.Groups[4].Value.Trim(),
                            lineNumber));

                    continue;
                }

                //
                // Starting a project reference
                //
                match = Regex.Match(line, @"Project\(""([^""]*)""\) = ""([^""]*)"", ""([^""]*)"", ""([^""]*)""");
                if (match.Success)
                {
                    if (projectReferenceStartLineNumber >= 0)
                        throw new TextFileParseException(
                            "Expected 'EndProject'",
                            lineNumber + 1,
                            line);

                    projectReferenceStartLineNumber = lineNumber;
                    typeId = match.Groups[1].Value;
                    name = match.Groups[2].Value;
                    location = match.Groups[3].Value;
                    id = match.Groups[4].Value;

                    continue;
                }

                //
                // Starting nested projects block
                //
                if (line.Trim() == "GlobalSection(NestedProjects) = preSolution")
                {
                    NestedProjectsStartLineNumber = lineNumber;
                    continue;
                }


                //
                // Starting solution configurations block
                //
                if (line.Trim() == "GlobalSection(SolutionConfigurationPlatforms) = preSolution")
                {
                    SolutionConfigurationsStartLineNumber = lineNumber;
                    continue;
                }


                //
                // Starting project configurations block
                //
                if (line.Trim() == "GlobalSection(ProjectConfigurationPlatforms) = postSolution")
                {
                    ProjectConfigurationsStartLineNumber = lineNumber;
                    continue;
                }

                //
                // Start of "Global" section
                //
                if (line.Trim() == "Global")
                {
                    GlobalStartLineNumber = lineNumber;
                    continue;
                }

                //
                // End of "Global" section
                //
                if (line.Trim() == "EndGlobal")
                {
                    GlobalEndLineNumber = lineNumber;
                    continue;
                }

                //
                // Nothing special
                //
            }

            if (GlobalStartLineNumber < 0)
                throw new TextFileParseException("No 'Global' section in file", 1, "");
            if (GlobalEndLineNumber < 0)
                throw new TextFileParseException("No 'EndGlobal' in file", 1, "");
            if (NestedProjectsStartLineNumber >= 0 && NestedProjectsEndLineNumber < 0)
                throw new TextFileParseException("No nested projects 'EndGlobalSection' in file", 1, "");
        }


        /// <summary>
        /// Save information to the <c>.sln</c> file
        /// </summary>
        ///
        public void Save()
        {
            //
            // The dotnet tool writes .sln files with Windows line endings and a UTF-8 bom
            //
            FileExtensions.RewriteAllLines(Path, Lines, LineEnding.CRLF, true);
        }

    }
}
