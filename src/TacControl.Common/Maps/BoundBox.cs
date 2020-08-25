using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapsui.Geometries;

namespace TacControl.Common.Maps
{
    public class BoundBox : Mapsui.Geometries.Geometry
    {
        public BoundBox(BoundingBox x)
        {
            BoundingBox = x;
        }

        public BoundBox(Point minPoint, Point maxPoint)
        {
            BoundingBox = new BoundingBox(minPoint, maxPoint);
        }

        public BoundBox(IEnumerable<float[]> points)
        {

            var firstPoint = new Point(points.First()[0], points.First()[1]);
            var boundingBox = new BoundingBox(firstPoint, firstPoint);
            foreach (var point in points.Skip(1))
            {
                var X = point[0];
                var Y = point[1];

                boundingBox.Min.X = X < boundingBox.Min.X ? X : boundingBox.Min.X;
                boundingBox.Min.Y = Y < boundingBox.Min.Y ? Y : boundingBox.Min.Y;
                boundingBox.Max.X = X > boundingBox.Max.X ? X : boundingBox.Max.X;
                boundingBox.Max.Y = Y > boundingBox.Max.Y ? Y : boundingBox.Max.Y;
            }

            BoundingBox = boundingBox;
        }


        

        public override bool IsEmpty()
        {
            return false;
        }

        public override bool Equals(Geometry geom)
        {
            var point = geom as BoundBox;
            if (point == null) return false;
            return BoundingBox.Equals(point.BoundingBox);
        }

        public override BoundingBox BoundingBox { get; }

        public override double Distance(Point point)
        {
            return BoundingBox.Distance(point);
        }

        public override bool Contains(Point point)
        {
            return BoundingBox.Contains(point);
        }
    }

}
