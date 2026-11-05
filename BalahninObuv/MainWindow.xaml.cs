using System;
using System.Windows;
using System.Windows.Controls;

namespace BalahninObuv
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Manager.MainFrame = MainFrame;

            MainFrame.Navigated += (s, e) =>
            {
                UpdateHeader();
                UpdateBackButton();
            };

            MainFrame.Navigate(new LoginPage());
        }

        private void UpdateHeader()
        {
            if (Manager.IsAuthorizedUser)
            {
                string roleName = Manager.IsAdmin ? "Администратор"
                                : Manager.IsManager ? "Менеджер"
                                : "Пользователь";

                TbCurrentUser.Text = $"{Manager.CurrentUser.Surname} {Manager.CurrentUser.Name} ({roleName})";

                // Кнопку «Заказы» видят ТОЛЬКО менеджер и админ
                BtnOrders.Visibility = Manager.CanSeeOrders
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // ⚠️ Кнопку «Аналитика» видит ТОЛЬКО админ
                BtnAnalytics.Visibility = Manager.IsAdmin
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                BtnLogout.Content = "Выйти";
                BtnLogout.Visibility = Visibility.Visible;
            }
            else
            {
                TbCurrentUser.Text = "Гость";
                BtnOrders.Visibility = Visibility.Collapsed;
                BtnAnalytics.Visibility = Visibility.Collapsed;

                BtnLogout.Content = "Войти";
                BtnLogout.Visibility = Visibility.Visible;
            }
        }

        private void UpdateBackButton()
        {
            var current = MainFrame.Content;

            bool isRoot = current is LoginPage || current is CatalogPage;
            BtnBack.Visibility = isRoot
                ? Visibility.Collapsed
                : Visibility.Visible;

            if (current is LoginPage)
                BtnLogout.Visibility = Visibility.Collapsed;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
                MainFrame.GoBack();
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OrdersPage());
        }

        private void BtnAnalytics_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AnalyticsPage());
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Manager.CurrentUser = null;
            MainFrame.Navigate(new LoginPage());

            while (MainFrame.CanGoBack)
                MainFrame.RemoveBackEntry();

            UpdateHeader();
        }
    }
}