using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TacControl
{
    class PaneViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public PaneViewModel()
        {
        }

        public string Title { get; set; }
        public ImageSource IconSource { get; protected set; }
        public string ContentId { get; set; }
        public bool IsSelected { get; set; }
        public bool IsActive { get; set; }
    }
}
