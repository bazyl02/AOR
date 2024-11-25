using System;
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (Bindings.GetInstance().SheetWindow != null)
            {
                Bindings.GetInstance().SheetWindow.Close();
                Bindings.GetInstance().SheetWindow = null;
            }
            Bindings.GetInstance()?.Report.Close();
        }
    }
}