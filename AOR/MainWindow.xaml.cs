using AOR.ModelView;

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