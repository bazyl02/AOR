using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using AOR.ModelView;
using AOR.Model;
using AOR.View.Controls;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR
{
    public partial class MainWindow
    {
        private Bindings _bindings;

        public static MainWindow Instance;
        
        
        public MainWindow()
        {
            InitializeComponent();
            _bindings = Bindings.GetInstance();
            DataContext = _bindings;
            Instance = this;
            
        }
    }
}