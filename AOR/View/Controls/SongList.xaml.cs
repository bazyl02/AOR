using System;
using System.Collections.Generic;
using System.Windows.Controls;
using AOR.Model;
using AOR.ModelView;

namespace AOR.View.Controls
{
    public partial class SongList : UserControl
    {
        public SongList()
        {
            InitializeComponent();
            Bindings.GetInstance().SongList = this;
        }

        public void UpdateSongList(List<SongManager.PieceData> newData)
        {
            List.ItemsSource = null;
            List.ItemsSource = newData;
        }

        private void List_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count == 0 ) return;
            SongManager.PieceData data = (SongManager.PieceData)e.AddedItems[0];
            Bindings.GetInstance().SelectedPiece = data;
        }
    }
}