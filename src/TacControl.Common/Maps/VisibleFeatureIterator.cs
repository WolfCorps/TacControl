using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using Mapsui.Extensions;

namespace TacControl.Common.Maps
{
    public static class VisibleFeatureIterator
    {
        public static void IterateLayers(Viewport viewport, IEnumerable<ILayer> layers, long iteration,
            Action<Viewport, ILayer, IStyle, IFeature, float, long> callback)
        {
            foreach (var layer in layers)
            {
                if (layer.Enabled == false) continue;
                if (layer.MinVisible > viewport.Resolution) continue;
                if (layer.MaxVisible < viewport.Resolution) continue;

                IterateLayer(viewport, layer, iteration, callback);
            }
        }

        private static void IterateLayer(Viewport viewport, ILayer layer, long iteration,
            Action<Viewport, ILayer, IStyle, IFeature, float, long> callback)
        {
            var features = layer.GetFeatures(viewport.ToExtent(), viewport.Resolution).ToList();

            var layerStyles = ToArray(layer);
            foreach (var layerStyle in layerStyles)
            {
                var style = layerStyle; // This is the default that could be overridden by an IThemeStyle

                foreach (var feature in features)
                {
                    if (layerStyle is IThemeStyle) style = (layerStyle as IThemeStyle).GetStyle(feature);
                    if (ShouldNotBeApplied(style, viewport)) continue;

                    if (style is StyleCollection styles) // The ThemeStyle can again return a StyleCollection
                    {
                        foreach (var s in styles.Styles)
                        {
                            if (ShouldNotBeApplied(s, viewport)) continue;
                            callback(viewport, layer, s, feature, (float)layer.Opacity, iteration);
                        }
                    }
                    else
                    {
                        callback(viewport, layer, style, feature, (float)layer.Opacity, iteration);
                    }
                }
            }

            foreach (var feature in features)
            {
                var featureStyles = feature.Styles ?? Enumerable.Empty<IStyle>(); // null check
                foreach (var featureStyle in featureStyles)
                {
                    if (ShouldNotBeApplied(featureStyle, viewport)) continue;

                    callback(viewport, layer, featureStyle, feature, (float)layer.Opacity, iteration);

                }
            }
        }

        private static bool ShouldNotBeApplied(IStyle style, Viewport viewport)
        {
            return style == null || !style.Enabled || style.MinVisible > viewport.Resolution || style.MaxVisible < viewport.Resolution;
        }

        private static IStyle[] ToArray(ILayer layer)
        {
            return (layer.Style as StyleCollection)?.Styles.ToArray() ?? new[] { layer.Style };
        }
    }
}
