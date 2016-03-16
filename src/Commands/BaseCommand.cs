using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace CommentRemover
{
    internal abstract class BaseCommand<T>
    {
        private readonly Package _package;

        protected BaseCommand(Package package, Guid commandSet, int commandId)
        {
            _package = package;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(commandSet, commandId);
                var menuItem = new OleMenuCommand(Callback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static T Instance { get; protected set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        private void Callback(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            Execute(button);
        }

        protected abstract void Execute(OleMenuCommand button);

        protected static IEnumerable<IMappingSpan> GetClassificationSpans(IWpfTextView view, string classificationName)
        {
            if (view == null)
                return Enumerable.Empty<IMappingSpan>();

            var componentModel = ProjectHelpers.GetComponentModel();
            var service = componentModel.GetService<IBufferTagAggregatorFactoryService>();
            var classifier = service.CreateTagAggregator<IClassificationTag>(view.TextBuffer);
            var snapshot = new SnapshotSpan(view.TextBuffer.CurrentSnapshot, 0, view.TextBuffer.CurrentSnapshot.Length);

            return from s in classifier.GetTags(snapshot).Reverse()
                   where s.Tag.ClassificationType.Classification.IndexOf(classificationName, StringComparison.OrdinalIgnoreCase) > -1
                   select s.Span;
        }
        protected static bool IsLineEmpty(ITextSnapshotLine line)
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

    }
}
