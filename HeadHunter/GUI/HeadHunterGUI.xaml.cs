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

namespace HeadHunter.GUI
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>
    public partial class HeadHunterGUI : UserControl
    {
        private HeadHunterPlugin Plugin { get; }

        public HeadHunterGUI()
        {
            InitializeComponent();
            SetupGUI();
        }

        public HeadHunterGUI(HeadHunterPlugin plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }

        private void SetupGUI()
        {
            HHGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
            HHGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
            HHGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
            HHGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
            HHGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)new GridLengthConverter().ConvertFromString("1*") });

            HHGrid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
            HHGrid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
            HHGrid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
            HHGrid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
            HHGrid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
            HHGrid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
            HHGrid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)new GridLengthConverter().ConvertFromString("1*") });
        }
    }
}
