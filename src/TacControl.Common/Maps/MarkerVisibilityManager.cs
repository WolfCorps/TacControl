using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TacControl.Common.Modules;

namespace TacControl.Common.Maps
{
    public interface IMarkerVisibilityManager
    {
        bool IsVisible(ActiveMarker marker);
        Action OnUpdated { get; set; }
    }


    public class MarkerVisibilityManager : IMarkerVisibilityManager
    {
        private bool[] _channelSolos = new bool[16];
        private bool[] _channelIgnore = new bool[16];
        private bool[] _channelEnabled = new bool[16];
        public Action OnUpdated { get; set; }

        public MarkerVisibilityManager()
        {
            for (int i = 0; i < _channelEnabled.Length; i++)
            {
                _channelEnabled[i] = true;
            }
        }

        public void SetChannelSolo(int channel, bool enabled)
        {
            _channelSolos[channel] = enabled;
            RefreshChannels();
        }

        public void SetChannelIgnore(int channel, bool enabled)
        {
            _channelIgnore[channel] = enabled;
            RefreshChannels();
        }

        private void RefreshChannels()
        {
            if (_channelSolos.Any(x => x)) { //Only solos

                for (int i = 0; i < _channelEnabled.Length; i++)
                {
                    _channelEnabled[i] = _channelSolos[i];
                }

                OnUpdated?.Invoke();
                return;
            }

            //All non ignore
            for (int i = 0; i < _channelEnabled.Length; i++)
            {
                _channelEnabled[i] = !_channelIgnore[i];
            }
            OnUpdated?.Invoke();
        }




        public bool IsVisible(ActiveMarker marker)
        {
            if (marker.channel == -1) return true;
            if (!_channelEnabled[marker.channel]) return false;



            return true;
        }

    }
}
