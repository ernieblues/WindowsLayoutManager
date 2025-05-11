using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WindowsLayoutManager
{
    public partial class LayoutManagerWindow : Window
    {
        // ==================================================
        //                    Declarations
        // ==================================================

        // Last clicked column header for sorting
        private GridViewColumnHeader lastHeaderClicked = null;

        // Sort direction (ascending or descending)
        private ListSortDirection lastDirection = ListSortDirection.Ascending;

        // ==================================================
        //                     Constructor                  
        // ==================================================
        public LayoutManagerWindow()
        {
            InitializeComponent();
            LayoutManager.layouts = LayoutManager.LoadLayoutsFromFile();
            RefreshLayoutList();
        }

        // ==================================================
        //                   Event Handlers                  
        // ==================================================

        // Sorts the list by the clicked column, toggling direction each time.
        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var header = sender as GridViewColumnHeader;
            if (header?.Tag == null)
                return;

            string propertyName = header.Tag.ToString();

            ListSortDirection direction;
            if (header == lastHeaderClicked)
            {
                direction = lastDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                direction = ListSortDirection.Ascending;
            }

            ICollectionView view = CollectionViewSource.GetDefaultView(LayoutListView.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(propertyName, direction));
            view.Refresh();

            lastHeaderClicked = header;
            lastDirection = direction;
        }

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

        // Deselects the ListView when clicking outside of it.
        private void LayoutNameInput_GotFocus(object sender, RoutedEventArgs e)
        {
            LayoutListView.SelectedItem = null;
        }

        // Clears the selected layout and disables related buttons when the header is clicked
        private void HeaderArea_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutListView.SelectedItem = null;
        }

        // Enables the Save Layout button only when the Layout Name Input is populated
        private void LayoutNameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveLayoutButton.IsEnabled = !string.IsNullOrWhiteSpace(LayoutNameInput.Text);
        }

        // Enables the Delete Layout and Restore Layout buttons only when a layout is selected in the list
        private void LayoutListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteLayoutButton.IsEnabled = LayoutListView.SelectedItem != null;
            RestoreLayoutButton.IsEnabled = LayoutListView.SelectedItem != null;

            if (LayoutListView.SelectedItem is Layout selectedLayout)
            {
                LayoutNameInput.Text = selectedLayout.LayoutName;
            }
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
