using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapsui;
using Mapsui.Nts.Extensions;
using NetTopologySuite.Geometries;

namespace TacControl.Common.Maps
{
    public class BoundBox : NetTopologySuite.Geometries.LinearRing
    {
        //#TODO remove
        public BoundBox(Mapsui.MRect x) : base( x.Vertices.Select(x => x.ToCoordinate()).Append(x.BottomLeft.ToCoordinate()).ToArray())
        {
            //BoundingBox = x;
        }
     
        public BoundBox(MPoint minPoint, MPoint maxPoint) : base (new Mapsui.MRect(minPoint.X, minPoint.Y, maxPoint.X, maxPoint.Y).Vertices.Select(x => x.ToCoordinate()).Append(new Coordinate(minPoint.X, minPoint.Y)).ToArray())
        {
            //BoundingBox = new BoundingBox(minPoint, maxPoint);
        }

        public BoundBox(IEnumerable<float[]> points) : base (GenerateCoordinatesFromPolyline(points))
        {
            //BoundingBox = boundingBox;
        }

        private static Coordinate[] GenerateCoordinatesFromPolyline(IEnumerable<float[]> points)
        {
            var firstPoint = new Point(points.First()[0], points.First()[1]);
            var boundingBox = new MRect(firstPoint.X, firstPoint.Y, firstPoint.X, firstPoint.Y);
            foreach (var point in points.Skip(1))
            {
                var X = point[0];
                var Y = point[1];

                boundingBox.Min.X = X < boundingBox.Min.X ? X : boundingBox.Min.X;
                boundingBox.Min.Y = Y < boundingBox.Min.Y ? Y : boundingBox.Min.Y;
                boundingBox.Max.X = X > boundingBox.Max.X ? X : boundingBox.Max.X;
                boundingBox.Max.Y = Y > boundingBox.Max.Y ? Y : boundingBox.Max.Y;
            }

            return boundingBox.Vertices.Select(x => x.ToCoordinate()).Append(boundingBox.BottomLeft.ToCoordinate()).ToArray();
        }

        /*

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
        */
    }

}
