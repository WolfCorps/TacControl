using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TacControl.Common;
using TacControl.Common.Modules;

namespace TacControl
{
    /// <summary>
    /// Interaction logic for NotesList.xaml
    /// </summary>
    public partial class NotesList : UserControl
    {
        public ModuleNote noteRef { get; } = GameState.Instance.note;

        public NotesList()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            noteRef.CreateNewNote();
        }
    }
}
