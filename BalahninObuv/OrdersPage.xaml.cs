using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BalahninObuv
{
    public partial class OrdersPage : Page
    {
        private List<OrderView> _allOrders = new List<OrderView>();

        public OrdersPage()
        {
            InitializeComponent();
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                var context = BalahninObuvEntities.GetContext();

                var orders = context.Orders
                    .Include("Users")
                    .Include("OrderItems")
                    .ToList();

                // Менеджер и админ видят ВСЕ заказы
                // (Обычный пользователь сюда попасть не может — у него нет кнопки)
                if (!Manager.IsAdmin && !Manager.IsManager)
                {
                    MessageBox.Show("У вас нет доступа к списку заказов.");
                    Manager.MainFrame.Navigate(new CatalogPage());
                    return;
                }

                _allOrders = orders.Select(o => new OrderView
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    ClientName = o.Users != null ? o.Users.FullName : "Неизвестно",
                    ItemsCount = o.OrderItems != null ? o.OrderItems.Count : 0,
                    TotalSum = o.OrderItems != null
                        ? o.OrderItems.Sum(i => i.OrderItemQuantity * i.PricePerUnit)
                        : 0m,

                    // Кнопка «Удалить» — для админа и менеджера
                    CanDelete = Manager.CanManageOrders,

                    // Кнопка «Редактировать» — ТОЛЬКО для админа
                }).OrderByDescending(o => o.OrderDate).ToList();

                // Кнопка «Добавить заказ» — для админа и менеджера
                BtnAddOrder.Visibility = Manager.CanManageOrders
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказов: " + ex.Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allOrders.AsEnumerable();

            string search = TBox_OrderSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(o =>
                    (o.ClientName ?? "").ToLower().Contains(search) ||
                    o.OrderNumber.ToString().Contains(search));
            }

            ListViewOrders.ItemsSource = filtered.ToList();
        }

        private void TBox_OrderSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditOrderPage(null));
        }

        private void BtnOpenOrder_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var order = btn?.DataContext as OrderView;
            if (order == null) return;

            // «Состав» — просмотр. Админ может редактировать, остальные только смотрят
            Manager.MainFrame.Navigate(new AddEditOrderPage(order.OrderId));
        }

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            // Проверка прав — только админ и менеджер
            if (!Manager.CanManageOrders)
            {
                MessageBox.Show("Недостаточно прав для удаления заказа.");
                return;
            }

            var btn = sender as Button;
            var order = btn?.DataContext as OrderView;
            if (order == null) return;

            var result = MessageBox.Show($"Удалить заказ №{order.OrderNumber}?",
                                         "Подтверждение",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var context = BalahninObuvEntities.GetContext();
                var dbOrder = context.Orders.Find(order.OrderId);
                if (dbOrder != null)
                {
                    var items = context.OrderItems.Where(i => i.OrderId == dbOrder.OrderId).ToList();
                    foreach (var item in items)
                        context.OrderItems.Remove(item);

                    context.Orders.Remove(dbOrder);
                    context.SaveChanges();
                }

                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message);
            }
        }
    }

    public class OrderView
    {
        public int OrderId { get; set; }
        public int OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string ClientName { get; set; }
        public int ItemsCount { get; set; }
        public decimal TotalSum { get; set; }
        public bool CanDelete { get; set; }

        public System.Windows.Visibility DeleteVisibility =>
            CanDelete ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    }
}