using System.Windows;
using System.Windows.Controls;

namespace WindowsLayoutManager
{
    public partial class LayoutManagerWindow : Window
    {
        // Constructor
        public LayoutManagerWindow()
        {
            InitializeComponent();
            LayoutManager.layouts = LayoutManager.LoadLayoutsFromFile();
            RefreshLayoutList();
        }

        // ==================================================
        //                   Event Handlers                  
        // ==================================================

        // Deletes the selected layout from the list and saves changes
        private void DeleteLayout_Click(object sender, RoutedEventArgs e)
        {
            Layout selectedLayout = LayoutListView.SelectedItem as Layout;

            if (selectedLayout == null)
            {
                MessageBox.Show("No layout selected.");
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete '{selectedLayout.LayoutName}'?",
                                         "Confirm Delete", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                LayoutManager.layouts.Remove(selectedLayout);
                LayoutManager.SaveLayoutsToFile(LayoutManager.layouts);
                RefreshLayoutList();
            }
        }

        // Enables the Restore button only when a layout is selected in the list
        private void LayoutListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RestoreLayoutButton.IsEnabled = LayoutListView.SelectedItem != null;
        }

        // Restores the open windows to match the selected layout
        private void RestoreLayout_Click(object sender, RoutedEventArgs e)
        {
            Layout selectedLayout = LayoutListView.SelectedItem as Layout;
            LayoutManager.RestoreLayout(selectedLayout);
        }

        // Saves the current window layout with the specified name
        private void SaveLayout_Click(object sender, RoutedEventArgs e)
        {
            LayoutManager.SaveLayout(LayoutNameInput.Text);
            RefreshLayoutList();
        }

        // ==================================================
        //                Layout List Handling
        // ==================================================

        // Reloads the layout list to reflect any changes
        private void RefreshLayoutList()
        {
            LayoutListView.ItemsSource = null;
            LayoutListView.ItemsSource = LayoutManager.layouts;
        }
    }
}
