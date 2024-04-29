using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Bonsai;
using Bonsai.Design;
using System.Diagnostics;
using System.Drawing;
using System.Reactive;
using System.Text;

[assembly: TypeVisualizer(typeof(ObjectTextVisualizer), Target = typeof(object))]

namespace Bonsai.Design
{
    /// <summary>
    /// Provides a type visualizer for displaying any object type as text.
    /// </summary>
    public class ObjectTextVisualizer : BufferedVisualizer
    {
        const int AutoScaleHeight = 13;
        const float DefaultDpi = 96f;

        RichTextBox textBox;
        UserControl textPanel;
        Queue<string> buffer;
        int bufferSize;

        /// <inheritdoc/>
        protected override int TargetInterval => 1000 / 30;

        /// <inheritdoc/>
        protected override void ShowBuffer(IList<Timestamped<object>> values)
        {
            if (values.Count == 0)
                return;

            var sb = new StringBuilder();

            // Add new values to the buffer
            for (int i = Math.Max(0, values.Count - bufferSize); i < values.Count; i++)
            {
                var rawText = values[i].Value?.ToString() ?? string.Empty;
                sb.Clear();
                sb.EnsureCapacity(rawText.Length + rawText.Length / 16); // Arbitrary extra space for control characters

                foreach (char c in rawText)
                {
                    // To give point of reference we represent strings as they'd be represented in C# string literals
                    var replacement = c switch
                    {
                        '\\' => @"\\", // Backslash
                        '"' => "\\\"", // Double quote
                        '\n' => @"\n", // Line feed
                        '\r' => @"\r", // Carriage return
                        '\x0085' => @"\x0085", // Next line character (NEL)
                        '\x2028' => @"\x2028", // Unicode line separator
                        '\x2029' => @"\x2029", // Unicode paragraph separator
                        '\0' => @"\0", // Null
                        '\a' => @"\a", // Alert
                        '\b' => @"\b", // Backspace
                        '\f' => @"\f", // Form feed
                        '\t' => @"\t", // Tab
                        '\v' => @"\v", // Vertical tab
                        < ' ' => $@"\u{c:X4}", // Misc control characters
                        _ => null,
                    };

                    if (replacement is null)
                        sb.Append(c);
                    else
                        sb.Append(replacement);
                }

                if (buffer.Count >= bufferSize)
                    buffer.Dequeue();

                buffer.Enqueue(sb.ToString());
            }

            Debug.Assert(buffer.Count <= bufferSize);

            // Update the visual representation of the buffer
            sb.Clear();
            foreach (var line in buffer)
            {
                if (sb.Length > 0)
                    sb.Append(Environment.NewLine);
                sb.Append(line);
            }
            textBox.Text = sb.ToString();
            textPanel.Invalidate();
        }

        /// <inheritdoc/>
        public override void Show(object value)
            //TODO: This can still be called and must be implemented. Refactor to have a Show(object, Stringbuilder) overload and use that in both
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void Load(IServiceProvider provider)
        {
            buffer = new Queue<string>();
            textBox = new RichTextLabel { Dock = DockStyle.Fill };
            textBox.Multiline = true;
            textBox.WordWrap = false;
            textBox.ScrollBars = RichTextBoxScrollBars.Horizontal;
            textBox.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    buffer.Clear();
                    textBox.Text = string.Empty;
                    textPanel.Invalidate();
                }
            };

            textPanel = new UserControl();
            textPanel.SuspendLayout();
            textPanel.Dock = DockStyle.Fill;
            textPanel.MinimumSize = textPanel.Size = new Size(320, 2 * AutoScaleHeight);
            textPanel.AutoScaleDimensions = new SizeF(6F, AutoScaleHeight);
            textPanel.AutoScaleMode = AutoScaleMode.Font;
            textPanel.Paint += textPanel_Paint;
            textPanel.Controls.Add(textBox);
            textPanel.ResumeLayout(false);

            var visualizerService = (IDialogTypeVisualizerService)provider.GetService(typeof(IDialogTypeVisualizerService));
            if (visualizerService != null)
            {
                visualizerService.AddControl(textPanel);
            }
        }

        void textPanel_Paint(object sender, PaintEventArgs e)
        {
            var lineHeight = AutoScaleHeight * e.Graphics.DpiY / DefaultDpi;
            bufferSize = (int)((textBox.ClientSize.Height - 2) / lineHeight);
            var textSize = TextRenderer.MeasureText(textBox.Text, textBox.Font);
            if (textBox.ClientSize.Width < textSize.Width)
            {
                var offset = 2 * lineHeight + SystemInformation.HorizontalScrollBarHeight - textPanel.Height;
                if (offset > 0)
                {
                    textPanel.Parent.Height += (int)offset;
                }
            }
        }

        /// <inheritdoc/>
        public override void Unload()
        {
            bufferSize = 0;
            textBox.Dispose();
            textPanel.Dispose();
            textBox = null;
            textPanel = null;
            buffer = null;
        }
    }
}
