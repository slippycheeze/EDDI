using System.Collections.Generic;
using System.Windows;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;

namespace EddiSpeechResponder.AvalonEdit
{
    public class TextCompletionWindow : CompletionWindow
    {
        public TextCompletionWindow(TextArea textArea, object reflectionObject) : base(textArea)
        {
            // Hide the title bar and similar window stylings
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            BorderThickness = new Thickness(0);

            // Get the list to which completion data can be added
            IList<ICompletionData> data = this.CompletionList.CompletionData;

            foreach (var item in new TextCompletion(reflectionObject.GetType(), reflectionObject).Results)
            {
                data.Add(item);
            }

            if (data.Count > 0)
            {
                Show();
            }
        }
    }
}