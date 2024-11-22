using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using AOR.Model;
using AOR.View.Controls;
using System.Windows;
using System.Windows.Media.Imaging;

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
        public PieceBuffer PieceBuffer;
        public Algorithm Algorithm;

        public SongList SongList;

        public SheetWindow SheetWindow = null;
        
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

        private SongManager.PieceData _selectedPiece = null;

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
        
        private BitmapSource _currentSheet;
        public BitmapSource CurrentSheet
        {
            get => _currentSheet;
            set
            {
                _currentSheet = value;
                OnPropertyChanged();
            }
        }
        
        private BitmapSource _newSheet;
        public BitmapSource NewSheet
        {
            get => _newSheet;
            set
            {
                _newSheet = value;
                OnPropertyChanged();
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async void ProcessSelectedPiece()
        {
            if(SelectedPiece is null) return;
            PieceBuffer = new PieceBuffer(SelectedPiece.MidiFile);
            await PieceBuffer.LoadPdfPages(SelectedPiece.PdfDocument);
        }
    }
}