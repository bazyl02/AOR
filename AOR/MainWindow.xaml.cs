using AOR.ModelView;
using AOR.Model;

namespace AOR
{
    public partial class MainWindow
    {
        private DeviceController DeviceController;

        private Bindings _bindings;
        
        public MainWindow()
        {
            InitializeComponent();
            _bindings = Bindings.GetInstance();
            DataContext = _bindings;
            _bindings.DeviceController = new DeviceController();
        }
    }
}