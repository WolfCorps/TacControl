using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkiaSharp;

namespace TacControl.Common.Maps.Util
{
    // https://github.com/mono/SkiaSharp/issues/692
    public static class SkiaTextBox
    {
        /// <summary>
        /// Draw the specified text in the region defined by x, y, width, height wrapping and breaking lines
        /// to fit in that region
        /// </summary>
        /// <returns>The draw.</returns>
        /// <param name="text">Text.</param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="canvas">Canvas.</param>
        /// <param name="paint">Paint.</param>
        public static void Draw(string text, SKPoint textOffset, SKCanvas canvas,
            SKPaint paint)
        {
            if (text == null)
            {
                return;
            }

            float textY = textOffset.Y;

            var lines = BreakLines(text, paint);

            var metrics = paint.FontMetrics;
            var lineHeight = metrics.Bottom - metrics.Top;

            //float textHeight = lines.Count * lineHeight - metrics.Leading;

            //if (ellipsize && lines.Count > height / lineHeight)
            //{
            //    var ellipsizedLine = $"{lines.FirstOrDefault()}...";
            //    canvas.DrawText(ellipsizedLine, (float) textX, (float) textY, paint);
            //}
            //else
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    canvas.DrawText(lines[i], textOffset.X, textY, paint);
                    textY += lineHeight;
                }
            }
        }

        static List<string> BreakLines(string text, SKPaint paint)
        {
            List<string> lines = new List<string>();

            string remainingText = text.Trim();

            do
            {
                int idx = LineBreak(remainingText, paint);
                if (idx == 0)
                {
                    lines.Add(remainingText);
                    break;
                }

                var lastLine = remainingText.Substring(0, idx).Trim();
                lines.Add(lastLine);
                remainingText = remainingText.Substring(idx).Trim();
            } while (!string.IsNullOrEmpty(remainingText));

            return lines;
        }

        static int LineBreak(string text, SKPaint paint)
        {
            int idx = 0, last = 0;
            //int lengthBreak = (int) paint.BreakText(text, (float) width);

            while (idx < text.Length)
            {
                int next = text.IndexOfAny(new char[] {' ', '\n'}, idx);
                if (next == -1)
                {
                    //if (idx == 0)
                    //{
                    //    // Word is too long, we will have to break it
                    //    return lengthBreak;
                    //}
                    //else
                    {
                        // Ellipsize if it's the last line
                        //if (lengthBreak == text.Length
                        //    // || text.IndexOfAny (new char [] { ' ', '\n' }, lengthBreak + 1) == -1
                        //   )
                        //{
                        //    return lengthBreak;
                        //}

                        // Split at the last word;
                        return last;
                    }
                }

                if (text[next] == '\n')
                {
                    return next;
                }

                //if (next > lengthBreak)
                //{
                //    return idx;
                //}

                last = next;
                idx = next + 1;
            }

            return last;
        }
    }
}
