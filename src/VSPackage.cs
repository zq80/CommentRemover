using System;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace CommentRemover
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidPackageString)]
    public sealed class CommentRemoverPackage : Package
    {
        public static DTE2 DTE { get; private set; }

        protected override void Initialize()
        {
            DTE = (DTE2)GetService(typeof(DTE));

            Logger.Initialize(this, Vsix.Name);

            RemoveAllCommentsCommand.Initialize(this);
            RemoveRegionsCommand.Initialize(this);
            RemoveXmlDocComments.Initialize(this);
            RemoveAllExceptXmlDocComments.Initialize(this);
            RemoveTasksCommand.Initialize(this);
            RemoveAllExceptTaskComments.Initialize(this);

            base.Initialize();
        }
    }
}
