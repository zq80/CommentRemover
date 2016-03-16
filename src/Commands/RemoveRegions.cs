using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace CommentRemover
{
    internal sealed class RemoveRegionsCommand : BaseCommand<RemoveRegionsCommand>
    {
        private RemoveRegionsCommand(Package package)
        : base(package, PackageGuids.guidPackageCmdSet, PackageIds.RemoveRegions)
        { }

        public static void Initialize(Package package)
        {
            Instance = new RemoveRegionsCommand(package);
        }

        protected override void Execute(OleMenuCommand button)
        {
            var view = ProjectHelpers.GetCurentTextView();

            try
            {
                VSPackage.DTE.UndoContext.Open(button.Text);

                RemoveRegionsFromBuffer(view);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                VSPackage.DTE.UndoContext.Close();
            }
        }

        private void RemoveRegionsFromBuffer(IWpfTextView view)
        {
            using (var edit = view.TextBuffer.CreateEdit())
            {
                foreach (var line in view.TextBuffer.CurrentSnapshot.Lines.Reverse())
                {
                    if (line.Extent.IsEmpty)
                        continue;

                    string text = line.GetText()
                                      .TrimStart('/', '*')
                                      .Replace("<!--", string.Empty)
                                      .TrimStart()
                                      .ToLowerInvariant();

                    if (text.StartsWith("#region") || text.StartsWith("#endregion") || text.StartsWith("#end region"))
                    {
                        // Strip next line if empty
                        if (view.TextBuffer.CurrentSnapshot.LineCount > line.LineNumber + 1)
                        {
                            var next = view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(line.LineNumber + 1);

                            if (IsLineEmpty(next))
                                edit.Delete(next.Start, next.LengthIncludingLineBreak);
                        }

                        edit.Delete(line.Start, line.LengthIncludingLineBreak);
                    }
                }

                edit.Apply();
            }
        }
    }
}
