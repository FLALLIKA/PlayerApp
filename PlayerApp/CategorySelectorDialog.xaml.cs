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
using System.Windows.Shapes;

namespace PlayerApp
{
    /// <summary>
    /// Логика взаимодействия для CategorySelectorDialog.xaml
    /// </summary>
    public partial class CategorySelectorDialog : Window
    {
        public Category SelectedCategory { get; private set; }
        public bool AddNewCategorySelected { get; private set; }

        public CategorySelectorDialog(List<Category> categories)
        {
            InitializeComponent();
            listBoxCategories.ItemsSource = categories;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCategory = listBoxCategories.SelectedItem as Category;
            DialogResult = true;
        }

        private void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewCategorySelected = true;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
