using System;
using AvalonDock.Themes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TacControl.BigWidgets;
using TacControl.Common;

namespace TacControl
{
    internal class Workspace : ViewModelBase
    {
        private static Workspace _this = new Workspace();
        private Tuple<string, Theme> selectedTheme;

        protected Workspace()
        {
            this.Themes = new List<Tuple<string, Theme>>
            {
                new Tuple<string, Theme>(nameof(GenericTheme), new GenericTheme()),
                new Tuple<string, Theme>(nameof(Vs2013BlueTheme),new Vs2013BlueTheme()),
                new Tuple<string, Theme>(nameof(Vs2013DarkTheme),new Vs2013DarkTheme()),
                new Tuple<string, Theme>(nameof(Vs2013LightTheme),new Vs2013LightTheme()),
            };
            this.SelectedTheme = Themes.First();
        }


        public event EventHandler ActiveDocumentChanged;


        public static Workspace This => _this;

        public ObservableCollection<UserControlViewModel> Tools { get; } = new ObservableCollection<UserControlViewModel>();

        private RelayCommand _newTacMapCommand = null;

        private void OpenTacMap(object parameter)
        {
            Tools.Add(new UserControlViewModel(typeof(MapView)));
        }

        public ICommand NewTacMapCommand
        {
            get
            {
                if (_newTacMapCommand == null)
                {
                    _newTacMapCommand = new RelayCommand((p) => OpenTacMap(p), (p) => !string.IsNullOrEmpty(GameState.Instance.gameInfo.worldName));
                }

                return _newTacMapCommand;
            }
        }


        private RelayCommand _newTacRadioCommand = null;

        private void OpenTacRadio(object parameter)
        {
            Tools.Add(new UserControlViewModel(typeof(RadioWidget)));
        }

        public ICommand NewTacRadioCommand
        {
            get
            {
                if (_newTacRadioCommand == null)
                {
                    _newTacRadioCommand = new RelayCommand((p) => OpenTacRadio(p), (p) => true);
                }

                return _newTacRadioCommand;
            }
        }

        private RelayCommand _newTacVecCommand = null;

        private void OpenTacVec(object parameter)
        {
            Tools.Add(new UserControlViewModel(typeof(HelicopterControlButtons)));
        }

        public ICommand NewTacVecCommand
        {
            get
            {
                if (_newTacVecCommand == null)
                {
                    _newTacVecCommand = new RelayCommand((p) => OpenTacVec(p), (p) => true);
                }

                return _newTacVecCommand;
            }
        }


        private RelayCommand _newTacNoteCommand = null;

        private void OpenTacNote(object parameter)
        {
            Tools.Add(new UserControlViewModel(typeof(NotesList)));
        }

        public ICommand NewTacNoteCommand
        {
            get
            {
                if (_newTacNoteCommand == null)
                {
                    _newTacNoteCommand = new RelayCommand((p) => OpenTacNote(p), (p) => true);
                }

                return _newTacNoteCommand;
            }
        }



        private RelayCommand _newTacRadioPropertiesCommand = null;

        private void OpenTacRadioProperties(object parameter)
        {
            Tools.Add(new UserControlViewModel(typeof(RadioSettingsList)));
        }

        public ICommand NewTacRadioPropertiesCommand
        {
            get
            {
                if (_newTacRadioPropertiesCommand == null)
                {
                    _newTacRadioPropertiesCommand = new RelayCommand((p) => OpenTacRadioProperties(p), (p) => true);
                }

                return _newTacRadioPropertiesCommand;
            }
        }



        public List<Tuple<string, Theme>> Themes { get; set; }

        public Tuple<string, Theme> SelectedTheme
        {
            get { return selectedTheme; }
            set
            {
                selectedTheme = value;
                RaisePropertyChanged(nameof(SelectedTheme));
            }
        }


    }
}
