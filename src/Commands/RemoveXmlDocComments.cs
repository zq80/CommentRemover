using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace CommentRemover
{
    internal sealed class RemoveXmlDocComments : BaseCommand<RemoveXmlDocComments>
    {
        protected override void SetupCommands()
        {
            RegisterCommand(PackageGuids.guidPackageCmdSet, PackageIds.RemoveXmlDocComments);
        }

        protected override void Execute(OleMenuCommand button)
        {
            var view = ProjectHelpers.GetCurentTextView();
            var mappingSpans = GetClassificationSpans(view, "xml doc comment");

            if (!mappingSpans.Any())
                return;

            try
            {
                DTE.UndoContext.Open(button.Text);

                RemoveCommentsFromBuffer(view, mappingSpans);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                DTE.UndoContext.Close();
            }
        }

        private void RemoveCommentsFromBuffer(IWpfTextView view, IEnumerable<IMappingSpan> mappingSpans)
        {
            var affectedLines = new List<int>();

            foreach (var mappingSpan in mappingSpans)
            {
                var start = mappingSpan.Start.GetPoint(view.TextBuffer, PositionAffinity.Predecessor).Value;
                var end = mappingSpan.End.GetPoint(view.TextBuffer, PositionAffinity.Successor).Value;

                var span = new Span(start, end - start);
                var line = view.TextBuffer.CurrentSnapshot.Lines.First(l => l.Extent.IntersectsWith(span));

                if (!affectedLines.Contains(line.LineNumber))
                    affectedLines.Add(line.LineNumber);
            }

            using (var edit = view.TextBuffer.CreateEdit())
            {
                foreach (var lineNumber in affectedLines)
                {
                    var line = view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber);
                    edit.Delete(line.Start, line.LengthIncludingLineBreak);
                }

                edit.Apply();
            }
        }
    }
}
