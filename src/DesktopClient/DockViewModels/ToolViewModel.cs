using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TacControl
{
    internal class ToolViewModel : PaneViewModel
    {

        public ToolViewModel(string name)
        {
            Name = name;
            Title = name;
        }

        public string Name { get; private set; }

        public bool IsVisible { get; set; } = true;
    }
}
