using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TacControl.Common
{
    public static class EventSystem
    {
       // public static EventSystem Instance { get; set; } = new EventSystem();


        public delegate void CenterMapEvt(Mapsui.Geometries.Point position);

        // Define an Event based on the above Delegate
        public static event CenterMapEvt CenterMap;

        public static void CenterMapOn(Mapsui.Geometries.Point position)
        {
            CenterMap?.Invoke(position);
        }

    }
}
