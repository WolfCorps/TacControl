using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using TacControl.Common.Modules;

namespace TacControl.Common.Maps
{
    public class GPSTrackerProvider : IProvider<IFeature>, IDisposable
    {
        public ILayer GpsTrackerLayer { get; private set; }
        private MRect _boundingBox;

        public string CRS { get; set; } = "";

        private Dictionary<string, IFeature> features = new Dictionary<string, IFeature>();


        public GPSTrackerProvider(ILayer gpsTrackerLayer, MRect boundingBox)
        {
            GpsTrackerLayer = gpsTrackerLayer;
            _boundingBox = boundingBox;
            //this._boundingBox = MemoryProvider.GetExtents(this.Features);
            GameState.Instance.gps.trackers.CollectionChanged += (a, e) => OnTrackersUpdated();

            GameState.Instance.gps.PropertyChanged += (a, e) =>
            {
                if (e.PropertyName == nameof(ModuleGPS.trackers) && GameState.Instance.gps.trackers != null)
                {
                    GameState.Instance.gps.trackers.CollectionChanged += (b, c) => OnTrackersUpdated();
                    OnTrackersUpdated();
                }

            };



            //#TODO Also need EH on gps itself, I think the collection is initialized with a new one, without EH
            OnTrackersUpdated();
        }

        void OnDataChanged()
        {
            GpsTrackerLayer.DataHasChanged();
            //GpsTrackerLayer.RefreshData(GetExtents(), 1, ChangeType.Discrete);
        }
        private void OnTrackersUpdated()
        {
            if (!GameState.Instance.marker.markerTypes.Any()) return; // not ready yet
            foreach (var keyValuePair in GameState.Instance.gps.trackers)
            {
                if (!features.ContainsKey(keyValuePair.Key))
                {
                    var feature = new GPSTrackerFeature(keyValuePair.Value);

                    features[keyValuePair.Key] = feature;

                    keyValuePair.Value.PropertyChanged += (a, e) =>
                    {
                        if (e.PropertyName == nameof(GPSTracker.displayName))
                        {
                            feature.SetDisplayName(keyValuePair.Value.displayName);
                            OnDataChanged();
                        }
                        else if (e.PropertyName == nameof(GPSTracker.pos))
                        {
                            feature.SetPosition(new MPoint(keyValuePair.Value.pos[0], keyValuePair.Value.pos[1]));
                            OnDataChanged();
                        }
                        else if (e.PropertyName == nameof(GPSTracker.vel))
                        {
                            feature.UpdateVelocity();
                            OnDataChanged();
                        }
                    };
                }
            }

            var toRemove = features.Where(x => !GameState.Instance.gps.trackers.ContainsKey(x.Key)).Select(x => x.Key)
                .ToList();

            toRemove.ForEach(x => features.Remove(x));

            OnDataChanged();
        }


        public static double SymbolSize { get; set; } = 64;
        //#TODO combine MapMarkerProvider and GPSTrackerProvider into a common baseclass that shares all common code
        public virtual IEnumerable<IFeature> GetFeatures(FetchInfo fetchInfo)
        {
            if (fetchInfo == null) throw new ArgumentNullException(nameof(fetchInfo));
            if (fetchInfo.Extent == null) throw new ArgumentNullException(nameof(fetchInfo.Extent));


            fetchInfo = new FetchInfo(fetchInfo);
            // Use a larger extent so that symbols partially outside of the extent are included
            var biggerBox = fetchInfo.Extent?.Grow(fetchInfo.Resolution * SymbolSize * 0.5);

            return features.Values.Where(f => f != null && ((f.Extent?.Intersects(biggerBox)) ?? false)).ToList();
        }

        public MRect? GetExtent()
        {
            return this._boundingBox;
        }


        //public virtual IEnumerable<IFeature> GetFeaturesInView(
        //  BoundingBox box,
        //  double resolution)
        //{
        //    if (box == null)
        //        throw new ArgumentNullException(nameof(box));
        //
        //    BoundingBox grownBox = box.Grow(resolution);
        //
        //    return features.Values.Where(f => f.Geometry != null && f.Geometry.BoundingBox.Intersects(grownBox)).ToList();
        //}
        //
        //public BoundingBox GetExtents()
        //{
        //    return this._boundingBox;
        //}
        //
        //private static BoundingBox GetExtents(IEnumerable<IFeature> features)
        //{
        //    BoundingBox boundingBox = (BoundingBox)null;
        //    foreach (IFeature feature in features)
        //    {
        //        if (!feature.Geometry.IsEmpty())
        //            boundingBox = boundingBox == null ? feature.Geometry.BoundingBox : boundingBox.Join(feature.Geometry.BoundingBox);
        //    }
        //    return boundingBox;
        //}

        public void Dispose()
        {

        }
    }
}
