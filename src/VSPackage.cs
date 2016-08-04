using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using task = System.Threading.Tasks.Task;

namespace CommentRemover
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidPackageString)]
    public sealed class CommentRemoverPackage : AsyncPackage
    {
        protected override async task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await Logger.InitializeAsync(this, Vsix.Name);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            RemoveAllCommentsCommand.Initialize(this);
            RemoveRegionsCommand.Initialize(this);
            RemoveXmlDocComments.Initialize(this);
            RemoveAllExceptXmlDocComments.Initialize(this);
            RemoveTasksCommand.Initialize(this);
            RemoveAllExceptTaskComments.Initialize(this);
        }
    }
}
