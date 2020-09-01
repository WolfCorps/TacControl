using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TacControl
{
    class UserControlViewModel: ToolViewModel
    {
        public Type type;
        public UserControlViewModel(Type type)
            : base(type.FullName)
        {
            this.type = type;
            ContentId = type.FullName;
        }
    }
}
