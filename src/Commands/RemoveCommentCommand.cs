using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text.RegularExpressions;
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

            RemoveCommentSpansFromBuffer(view, mappingSpans, affectedLines);
            RemoveAffectedEmptyLines(view, affectedLines);
        }

        private static void RemoveCommentSpansFromBuffer(IWpfTextView view, IEnumerable<IMappingSpan> mappingSpans, IList<int> affectedLines)
        {
            using (var edit = view.TextBuffer.CreateEdit())
            {
                foreach (var mappingSpan in mappingSpans)
                {
                    var start = mappingSpan.Start.GetPoint(view.TextBuffer, PositionAffinity.Predecessor).Value;
                    var end = mappingSpan.End.GetPoint(view.TextBuffer, PositionAffinity.Successor).Value;

                    var span = new Span(start, end - start);
                    var lines = view.TextBuffer.CurrentSnapshot.Lines.Where(l => l.Extent.IntersectsWith(span));

                    foreach (var line in lines)
                    {
                        if (!affectedLines.Contains(line.LineNumber))
                            affectedLines.Add(line.LineNumber);
                    }

                    var mappingText = view.TextBuffer.CurrentSnapshot.GetText(span.Start, span.Length);
                    string empty = Regex.Replace(mappingText, "([\\S]+)", string.Empty);

                    edit.Replace(span.Start, span.Length, empty);
                }

                edit.Apply();
            }
        }

        private static void RemoveAffectedEmptyLines(IWpfTextView view, IList<int> affectedLines)
        {
            if (!affectedLines.Any())
                return;

            using (var edit = view.TextBuffer.CreateEdit())
            {
                foreach (var lineNumber in affectedLines)
                {
                    var line = view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber);

                    if (IsLineEmpty(line))
                    {
                        // Strip next line if empty
                        if (view.TextBuffer.CurrentSnapshot.LineCount > line.LineNumber + 1)
                        {
                            var next = view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber + 1);

                            if (IsLineEmpty(next))
                                edit.Delete(next.Start, next.LengthIncludingLineBreak);
                        }

                        edit.Delete(line.Start, line.LengthIncludingLineBreak);
                    }
                }

                edit.Apply();
            }
        }

        private static bool IsLineEmpty(ITextSnapshotLine line)
        {
            var text = line.GetText().Trim();

            return (string.IsNullOrWhiteSpace(text)
                   || text == "<!--"
                   || text == "-->"
                   || text == "<%%>"
                   || text == "<%"
                   || text == "%>"
                   || Regex.IsMatch(text, @"<!--(\s+)?-->"));
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
