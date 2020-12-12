1.5:
- Support dark mode
- Allow filtering of project items
- Fix #69: Implicit selected projects are not detected in netstandard format projects

1.4:
- Performance improvements.

1.3:
- Support VS2019

1.2:
- Ignore VS references for assemblies, only rely on the real references of the target assemblies.
- Detect late bound XAML references

1.1:
- use new version numbering scheme compliant with build agent auto-numbering

1.0.27.0:
- Fix second tier reference lookup.
- Show feature mappings.
- Make localization satellites optional.
- Fix #48: EnvDTE.Project.Dte may throw on imcomplete Project implementations.
- Fix #44: outdated document link

1.0.26.0:
- Fix #6: null reference exception on project with invalid project references

1.0.25.0:
- Fix #6: Another flavor of this issue occured.
- Fix #41: support WiX v4 templates.

1.0.24.0:
- Fix #42: EnvDTE.ProjectItem throws null ref exception when item has been removed

1.0.23.0:
- Update packages to align with other extensions (to avoid https://connect.microsoft.com/VisualStudio/feedback/details/2993889/)

1.0.22.0:
- Fix undefined behavior when not all projects have been built yet and project output is not available.

1.0.21.0:
- Added french translation
- Make second tier sattelite dlls optional.

1.0.20.0:
- #13, #21: Corectly handle output folder for non-default output locations, e.g. WebApi projects.
- #15, #26, #29: Include second tier references and their sattelite dlls.
- #17, #20: Improve readablity in dark theme.
- #19: Prefix names of directories if they match well known property names to avoid conflicts.
- #23: Generated ComponentGroupRef nodes sometimes do not appear.
- Added Chinese and German translation.

1.0.19.0:
- Fix #11: Handle deep directory structures
- Fix #12: Allow DirectoryNodes without name to support merge modules with the "MergeRedirectFolder"
- Format XML when adding new nodes.
 
- 1.0.18.0:
- Fix #10: VSIX installer problem with .net framework version

1.0.17.0:
- Fix #9: VS does not always reliably detect the installation of Wix

1.0.16.0:
- Support VS15 RC

1.0.15.0:
- Fix #6: Possible null ref exception when a project has no valid references

1.0.14.0
- Fix #3: No vertical scrollbar in directory pane.

1.0.13.0
- Fix http://waxsetupeditor.codeplex.com/workitem/4692

1.0.12.0
- Support VS15

1.0.11.0
- WI4692: Somtimes VS throws exceptions when enumerating project references.
- Exclude project references that are not marked as copy local

1.0.10.0
- WI4656: Visual Studio crashes on GetSubProjects
- WI4662: Path comparison should not be case sensitive

1.0.9.0
- WI4655: Format XML

1.0.8.0
- Fix missing XML-declaration element in .wxs file.
- New icon, internal fixes.

1.0.7.0
- WI4586: Suggestion for better file source determination. => Projects references are added and project reference variables used to locate the files.
- WI4599: Sub folders not copied => Wax now creates the ComponentGroupRef entries.

1.0.6.0
- Generate default defines for project output folders.
- Improve UX in project lists.

1.0.5.0
- WI1408: Fix missing scrollbar issue.
- Add documentation.

1.0.4.0
- Improve UI
- Unmapped items can be removed

1.0.3.0
- Id's may not contain characters except letters, digits, '.', '_'

1.0.2.0
- Deployment of symbols optional per project.
- Show unmapped files.
- Sort projects by folder/name.

1.0.1.0
- Make it compile and run with VS2010 too.