using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TacControl.Common.Config.Section
{
    public class Map : ConfigSectionBase, INotifyPropertyChanged
    {

        /// <summary>
        /// Make all map markers that are not on currently selected channel be slightly transparent
        /// </summary>
        [ConfigEntry]
        public bool TransparentOffchannelMarkers { get; set; }
    }
}
