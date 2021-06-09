using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace CommentRemover
{
    public static class ProjectHelpers
    {
        public static bool IsKind(this Project project, string kindGuid)
        {
            return project.Kind.Equals(kindGuid, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<Project> GetChildProjects(Project parent)
        {
            try
            {
                if (!parent.IsKind(ProjectKinds.vsProjectKindSolutionFolder) && parent.Collection == null)  // Unloaded
                {
                    return Enumerable.Empty<Project>();
                }

                if (!string.IsNullOrEmpty(parent.FullName))
                {
                    return new[] { parent };
                }
            }
            catch (COMException)
            {
                return Enumerable.Empty<Project>();
            }

            return parent.ProjectItems
                    .Cast<ProjectItem>()
                    .Where(p => p.SubProject != null)
                    .SelectMany(p => GetChildProjects(p.SubProject));
        }

        public static IWpfTextView GetCurentTextView()
        {
            IComponentModel componentModel = GetComponentModel();
            if (componentModel == null)
            {
                return null;
            }

            IVsEditorAdaptersFactoryService editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            return editorAdapter.GetWpfTextView(GetCurrentNativeTextView());
        }

        public static IVsTextView GetCurrentNativeTextView()
        {
            var textManager = (IVsTextManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));
            Assumes.Present(textManager);

            ErrorHandler.ThrowOnFailure(textManager.GetActiveView(1, null, out IVsTextView activeView));
            return activeView;
        }

        public static IComponentModel GetComponentModel()
        {
            return (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
        }
    }
}
