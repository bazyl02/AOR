﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using AOR.Model;
using AOR.View.Controls;
using System.Drawing;
using System.Windows;

namespace AOR.ModelView
{
    public class Bindings : INotifyPropertyChanged
    {
        //Singleton setup
        private static Bindings _instance = null;

        private Bindings()
        {
            DeviceController = new DeviceController();
            SongManager = new SongManager();
            InputBuffer = new InputBuffer();
        }
        public static Bindings GetInstance()
        {
            return _instance ?? (_instance = new Bindings());
        }
        
        //Global controllers
        public DeviceController DeviceController;
        public SongManager SongManager;
        public InputBuffer InputBuffer;

        public SongList SongList;
        
        
        //Data stored for bindings
        private bool _fromFile = false;

        public bool FromFile
        {
            get => _fromFile;
            set
            {
                _fromFile = value;
                //OnPropertyChanged();
                FromFileVisible = _fromFile ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private Visibility _fromFileVisible = Visibility.Hidden;

        public Visibility FromFileVisible
        {
            get => _fromFileVisible;
            set
            {
                _fromFileVisible = value;
                OnPropertyChanged();
            }
        }

        private SongManager.PieceData _selectedPiece;

        public SongManager.PieceData SelectedPiece
        {
            get => _selectedPiece;
            set
            {
                _selectedPiece = value;
                OnPropertyChanged();
            }
        }
        
        private string _inputDeviceName;
        public string InputDeviceName
        {
            get => _inputDeviceName;
            set
            {
                _inputDeviceName = value;
                OnPropertyChanged();
                DeviceController.SetInputDevice(_inputDeviceName);
            }
        }
        
        private string _outputDeviceName;

        public string OutputDeviceName
        {
            get => _outputDeviceName;
            set
            {
                _outputDeviceName = value;
                OnPropertyChanged();
                DeviceController.SetOutputDevice(_outputDeviceName);
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}