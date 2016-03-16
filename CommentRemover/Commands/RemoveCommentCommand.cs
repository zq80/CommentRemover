using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace CommentRemover
{
    internal sealed class RemoveCommentCommand
    {
        private readonly Package _package;

        private RemoveCommentCommand(Package package)
        {
            _package = package;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(PackageGuids.guidPackageCmdSet, PackageIds.RemoveComment);
                var menuItem = new MenuCommand(Execute, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static RemoveCommentCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new RemoveCommentCommand(package);
        }

        private void Execute(object sender, EventArgs e)
        {
            var view = ProjectHelpers.GetCurentTextView();
            var mappingSpans = GetClassificationSpans(view);

            if (!mappingSpans.Any())
                return;

            try
            {
                VSPackage.DTE.UndoContext.Open("Remove comments");
                DeleteFromBuffer(view, mappingSpans);
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

        private static void DeleteFromBuffer(IWpfTextView view, IEnumerable<IMappingSpan> mappingSpans)
        {
            var affectedLines = new List<int>();

            // Remove all comment spans
            using (var edit = view.TextBuffer.CreateEdit())
            {
                foreach (var mappingSpan in mappingSpans)
                {
                    var start = mappingSpan.Start.GetPoint(view.TextBuffer, PositionAffinity.Predecessor).Value;
                    var end = mappingSpan.End.GetPoint(view.TextBuffer, PositionAffinity.Predecessor).Value;

                    var span = new Span(start, end - start);
                    var line = view.TextBuffer.CurrentSnapshot.Lines.First(l => l.Extent.Contains(start));//  view.GetTextViewLineContainingBufferPosition(start);

                    var lineText = view.TextBuffer.CurrentSnapshot.GetText(line.Start, line.Length).Trim();
                    var mappingText = view.TextBuffer.CurrentSnapshot.GetText(span.Start, span.Length).Trim();

                    if (!affectedLines.Contains(line.LineNumber))
                        affectedLines.Add(line.LineNumber);

                    edit.Delete(span.Start, span.Length);
                }

                edit.Apply();
            }

            // Remove all remaining empty lines
            using (var edit = view.TextBuffer.CreateEdit())
            {
                foreach (var lineNumber in affectedLines)
                {
                    var line = view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber);
                    var text = line.GetText().Trim();

                    if (string.IsNullOrEmpty(text) || text == "<!--" || text == "-->" || text == "<%%>")
                        edit.Delete(line.Start, line.LengthIncludingLineBreak);
                }

                edit.Apply();
            }
        }

        private static IEnumerable<IMappingSpan> GetClassificationSpans(IWpfTextView view)
        {
            if (view == null)
                return Enumerable.Empty<IMappingSpan>();

            var componentModel = ProjectHelpers.GetComponentModel();
            var service = componentModel.GetService<IBufferTagAggregatorFactoryService>();
            var classifier = service.CreateTagAggregator<IClassificationTag>(view.TextBuffer);
            var snapshot = new SnapshotSpan(view.TextBuffer.CurrentSnapshot, 0, view.TextBuffer.CurrentSnapshot.Length);

            return from s in classifier.GetTags(snapshot).Reverse()
                   where s.Tag.ClassificationType.Classification.IndexOf("comment", StringComparison.OrdinalIgnoreCase) > -1
                   select s.Span;
        }
    }
}
