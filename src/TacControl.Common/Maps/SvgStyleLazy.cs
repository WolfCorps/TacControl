using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Mapsui.Styles;

namespace TacControl.Common.Maps
{
    public class SvgStyleLazy : SvgStyle
    {
        public Helper.IMapLayerData data;
        private bool loading = false;
        public Action DoLoad;

        public override Svg.Skia.SKSvg GetImage()
        {
            if (loading)
            {
                if (image != null)
                    loading = false;
                else
                    return null;
            }
            if (image == null)
            {
                loading = true;
                Task.Run(DoLoad);
            }

            return image;
        }

    }
}
