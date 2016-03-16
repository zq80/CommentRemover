using System;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace CommentRemover
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidPackageString)]
    public sealed class VSPackage : Package
    {
        public static DTE2 DTE { get; private set; }

        protected override void Initialize()
        {
            DTE = (DTE2)GetService(typeof(DTE));

            Logger.Initialize(this, Vsix.Name);
            Telemetry.Initialize(this, Vsix.Version, "4f961700-5d74-4a99-b346-571e5c82cb9b");

            RemoveCommentCommand.Initialize(this);

            base.Initialize();
        }
    }
}
