using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapsui;
using Mapsui.Rendering.Skia.SkiaWidgets;
using Mapsui.Styles;
using Mapsui.Widgets;
using SkiaSharp;

namespace TacControl.Common.Maps
{
    public class GridWidgetRenderer : ISkiaWidgetRenderer
    {

        static SKPaint paint10m = new SKPaint { Color = SKColors.Gray.WithAlpha(127) };
        static SKPaint paint100m = new SKPaint { Color = SKColors.Black };
        static SKPaint paint100mLight = new SKPaint { Color = SKColors.Black.WithAlpha(127) };
        static SKPaint paint1000m = new SKPaint { Color = SKColors.DarkRed, StrokeWidth = 2};
        static SKPaint paint1000mLight = new SKPaint { Color = SKColors.DarkRed.WithAlpha(127)};
        public const float lineOffset = 10.0f;

        private SKPaint[] paints = new SKPaint[] { paint10m, paint100m, paint1000m };
        // after 10m grid is hidden
        private SKPaint[] paintsL2 = new SKPaint[] { paint100mLight, paint1000m };
        // after 1km grid is hidden
        private SKPaint[] paintsL3 = new SKPaint[] {  paint1000mLight };

        static SKFont markerFont = new SKFont {Typeface = SKTypeface.FromFamilyName("Verdana"), Size = 16 }; 

        public void Draw(SKCanvas canvas, IReadOnlyViewport viewport, IWidget widget, float layerOpacity)
        {
            canvas.RotateDegrees((float)viewport.Rotation, 0.0f, 0.0f);


            var TL = viewport.ScreenToWorld(canvas.LocalClipBounds.Left, canvas.LocalClipBounds.Top);
            var BR = viewport.ScreenToWorld(canvas.LocalClipBounds.Right, canvas.LocalClipBounds.Bottom);

            var width = viewport.Extent.Width;
            var height = viewport.Extent.Height;

            var usedLineOffset = lineOffset;
            uint usedLineOffsetInt = (uint)usedLineOffset;

            IEnumerable<SKPaint> usedPaints = paints;

            if (viewport.Resolution > 0.25)
            {
                usedLineOffset *= 10; // hide 10m layer
                usedLineOffsetInt *= 10; // hide 10m layer
                usedPaints = paintsL2;
            }

            if (viewport.Resolution > 3)
            {
                usedLineOffset *= 10; // hide 100m layer
                usedLineOffsetInt *= 10; // hide 100m layer
                usedPaints = paintsL3;
            }

            int lineCount100M = (int)(100 / usedLineOffset);
            //How many lines are 1000m
            int lineCount1000M = (int)(1000 / usedLineOffset);

            if (lineCount100M == 0) lineCount100M = 9000; //will never be reached, if we only render 1k's


            double screenWidthPerLine = canvas.LocalClipBounds.Width / (width / usedLineOffset);
            double screenHeightPerLine = canvas.LocalClipBounds.Height / (height / usedLineOffset);

            //World coordinates of first lineOffset line
            var first100mW = (TL.X + (usedLineOffset - TL.X % usedLineOffset));
            var first100mH = (TL.Y + (usedLineOffset - TL.Y % usedLineOffset));

            //Screen offset of first lineOffset line
            double offsetCW = ((first100mW - TL.X)/ usedLineOffset) * screenWidthPerLine;
            double offsetCH = screenHeightPerLine + ((TL.Y - first100mH)/ usedLineOffset) * screenHeightPerLine;

            //offset of next 1k
            int KOffsetW = (int)((first100mW % 1000) / usedLineOffset);
            if (KOffsetW < 0)
                KOffsetW = 10 + KOffsetW;
            int KOffsetH = (int)((first100mH % 1000) / usedLineOffset) - 1;
            if (lineCount1000M > 1)
                KOffsetH = lineCount1000M - KOffsetH;

            for (double curX = offsetCW; curX < canvas.LocalClipBounds.Right; curX += screenWidthPerLine)
            {
                var worldPos = viewport.ScreenToWorld(curX, 0).X;
                var Xgrid10KM = (uint)(worldPos / 10000) % 10;
                var Xgrid1KM = (uint)(worldPos / 1000) % 10;
                var Xgrid100m = (uint)(worldPos / 100) % 10;
                //var Xgrid10m = (uint)(worldPos / 10) % 10;


                string gridPosString;
                if (usedLineOffsetInt == 1000)
                    gridPosString = $"{Xgrid10KM}{Xgrid1KM}";
                else if (usedLineOffsetInt == 100)
                    gridPosString = $"{Xgrid10KM}{Xgrid1KM}{Xgrid100m}";
                else
                    gridPosString = $"{Xgrid10KM}{Xgrid1KM}{Xgrid100m}{(uint)(worldPos / 10) % 10}";


                SKPaint paint;
                if (KOffsetW >= 1000 && KOffsetW % 1000 == 0)
                    paint = usedPaints.ElementAt(0);
                if (KOffsetW >= 100 && KOffsetW % 100 == 0)
                    paint = usedPaints.ElementAt(2);
                else if (KOffsetW >= 10 && KOffsetW % 10 == 0)
                    paint = usedPaints.ElementAt(1);
                else
                    paint = usedPaints.ElementAt(0);

                if (KOffsetW == lineCount1000M) KOffsetW = 0;

                canvas.DrawLine((float)curX, 0, (float)curX, canvas.LocalClipBounds.Height, paint);

               

                using (var gtext = SKTextBlob.Create(gridPosString, markerFont))
                    canvas.DrawText(gtext, (float)(curX + screenWidthPerLine / 2) - gtext.Bounds.MidX,
                        20+gtext.Bounds.MidY,
                        paint100m);

                KOffsetW++;
            }

            for (double curH = offsetCH; curH < canvas.LocalClipBounds.Bottom; curH += screenHeightPerLine)
            {
                var worldPos = viewport.ScreenToWorld(0, curH).Y;
                var Xgrid10KM = (uint)(worldPos / 10000) % 10;
                var Xgrid1KM = (uint)(worldPos / 1000) % 10;
                var Xgrid100m = (uint)(worldPos / 100) % 10;
                //var Xgrid10m = (uint)(worldPos / 10) % 10;

                string gridPosString;
                if (usedLineOffsetInt == 1000)
                    gridPosString = $"{Xgrid10KM}{Xgrid1KM}";
                else if (usedLineOffsetInt == 100)
                    gridPosString = $"{Xgrid10KM}{Xgrid1KM}{Xgrid100m}";
                else
                    gridPosString = $"{Xgrid10KM}{Xgrid1KM}{Xgrid100m}{(uint)(worldPos / 10) % 10}";

                SKPaint paint;
                if (KOffsetH >= 100 && KOffsetH % 100 == 0)
                {
                    paint = usedPaints.ElementAt(2);

                }
                else if (KOffsetH >= 10 && KOffsetH % 10 == 0)
                {
                    paint = usedPaints.ElementAt(1);
                }
                else
                {
                    paint = usedPaints.ElementAt(0);
                }

                


                if (KOffsetH == lineCount1000M) KOffsetH = 0;

                canvas.DrawLine(0, (float)curH, canvas.LocalClipBounds.Width, (float)curH, paint);

                using (var gtext = SKTextBlob.Create(gridPosString, markerFont))
                    canvas.DrawText(gtext, 0,
                        (float)(curH + screenWidthPerLine / 2) - gtext.Bounds.MidY,
                        paint100m);


                KOffsetH++;
            }







        }
    }
}
