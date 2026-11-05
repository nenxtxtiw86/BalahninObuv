using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BalahninObuv
{
    public partial class CatalogPage : Page
    {
        private List<ProductView> _allProducts = new List<ProductView>();
        private bool _isInitialized = false;

        public CatalogPage()
        {
            InitializeComponent(); 

            if (Manager.IsGuest)
            {
                FilterPanel.Visibility = Visibility.Collapsed;
            }

            LoadCategories();
            LoadProducts();
            _isInitialized = true;
        }

        private void LoadCategories()
        {
            try
            {
                var context = BalahninObuvEntities.GetContext();
                var cats = context.Category.OrderBy(c => c.Name).ToList();

                var items = new List<string> { "Все категории" };
                items.AddRange(cats.Select(c => c.Name));

                ComboCategory.ItemsSource = items;
                ComboCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки категорий: " + ex.Message);
            }
        }

        private void LoadProducts()
        {
            try
            {
                var context = BalahninObuvEntities.GetContext();

                var products = context.Products
                    .Include("Category")
                    .Include("SubCategory")
                    .Include("Manufacture")
                    .Include("Stock")
                    .Include("Stock.Sizes")
                    .ToList();

                _allProducts = products.Select(p => new ProductView
                {
                    ProductId = p.ProductId,
                    NameProduct = p.NameProduct,
                    Description = p.Description,
                    ProductPrice = p.ProductPrice,
                    ImagePath = GetImagePath(p.Image),

                    // Новое: состав
                    Compound = p.Compound,

                    // Новое: размерный ряд — все размеры, отсортированные
                    SizeRange = p.Stock != null && p.Stock.Any()
                        ? string.Join(", ", p.Stock
                            .Where(s => s.Sizes != null)
                            .Select(s => s.Sizes.Size)
                            .OrderBy(sz => sz))
                        : "—",

                    // Новое: доступные размеры — только там, где остаток > 0
                    AvailableSizes = p.Stock != null && p.Stock.Any(s => s.QuantityInStock > 0)
                        ? string.Join(", ", p.Stock
                            .Where(s => s.QuantityInStock > 0 && s.Sizes != null)
                            .Select(s => s.Sizes.Size)
                            .OrderBy(sz => sz))
                        : "нет в наличии",

                    CategoryName = p.Category != null ? p.Category.Name : null,
                    SubCategoryName = p.SubCategory != null ? p.SubCategory.Subcategory1 : null,
                    ManufactureName = p.Manufacture != null ? p.Manufacture.Manufacture1 : null,
                    TotalInStock = p.Stock != null ? p.Stock.Sum(s => s.QuantityInStock) : 0
                }).ToList();

                foreach (var item in _allProducts)
                {
                    item.RowBackground = item.TotalInStock <= 3
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8080"))
                        : Brushes.White;
                }

                ApplyFilter();
                ComboSort.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки каталога: " + ex.Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allProducts.AsEnumerable();

            string search = TBox_Search.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(p =>
                    (p.NameProduct ?? "").ToLower().Contains(search) ||
                    (p.ManufactureName ?? "").ToLower().Contains(search) ||
                    (p.CategoryName ?? "").ToLower().Contains(search) ||
                    (p.SubCategoryName ?? "").ToLower().Contains(search));
            }

            if (ComboCategory.SelectedIndex > 0)
            {
                string cat = ComboCategory.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(cat))
                    filtered = filtered.Where(p => p.CategoryName == cat);
            }
            
            switch (ComboSort.SelectedIndex)
            {
                case 1: filtered = filtered.OrderBy(p => p.ProductPrice); break;
                case 2: filtered = filtered.OrderByDescending(p => p.ProductPrice); break;
                case 3: filtered = filtered.OrderBy(p => p.TotalInStock); break;
                case 4: filtered = filtered.OrderByDescending(p => p.TotalInStock); break;
                case 5: filtered = filtered.OrderBy(p => p.NameProduct); break;
                case 6: filtered = filtered.OrderByDescending(p => p.NameProduct); break;
            }

            var result = filtered.ToList();
            ListViewProducts.ItemsSource = result;
            TbStatus.Text = $"Найдено: {result.Count}";
           
        }
        private string GetImagePath(string imageName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Если имя файла задано — ищем его
            if (!string.IsNullOrWhiteSpace(imageName))
            {
                // 1. bin\Debug\Товары\
                string full = System.IO.Path.Combine(baseDir, "res", imageName);
                if (System.IO.File.Exists(full)) return full;

                // 2. bin\Debug\
                full = System.IO.Path.Combine(baseDir, imageName);
                if (System.IO.File.Exists(full)) return full;

                // 3. ..\..\Товары\
                string projectPath = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(baseDir, @"..\..\res\", imageName));
                if (System.IO.File.Exists(projectPath)) return projectPath;
            }

            return GetDefaultImagePath(baseDir);
        }

        private string GetDefaultImagePath(string baseDir)
        {
            // Ищем picture.png в трёх местах
            string p1 = System.IO.Path.Combine(baseDir, "res", "picture.png");
            if (System.IO.File.Exists(p1)) return p1;

            string p2 = System.IO.Path.Combine(baseDir, "picture.png");
            if (System.IO.File.Exists(p2)) return p2;

            string p3 = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(baseDir, @"..\..\res\", "picture.png"));
            if (System.IO.File.Exists(p3)) return p3;

            return null;   // если даже заглушки нет — вернём null
        }

        private void TBox_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitialized) ApplyFilter();
        }

        private void ComboCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitialized) ApplyFilter();
        }

        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitialized) ApplyFilter();
        }
        private void BtnBuy_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.CurrentUser == null || Manager.CurrentUser.RoleId != 3)
            {
                MessageBox.Show("Оформление заказа через каталог доступно только авторизованному пользователю.\n" +
                                "Используйте раздел «Заказы».",
                                "Недоступно",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Открываем создание нового заказа
            Manager.MainFrame.Navigate(new AddEditOrderPage(null));
        }
    }

    public class ProductView
    {
        public int ProductId { get; set; }
        public string NameProduct { get; set; }
        public string Description { get; set; }
        public decimal ProductPrice { get; set; }
        public string ImagePath { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public string ManufactureName { get; set; }
        public int TotalInStock { get; set; }
        public string Compound { get; set; }
        public string SizeRange { get; set; }       
        public string AvailableSizes { get; set; }   
        public Brush RowBackground { get; set; }
        public System.Windows.Visibility BuyVisibility =>
            Manager.CurrentUser != null && Manager.CurrentUser.RoleId == 3
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;

    }
}