using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BalahninObuv
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();

            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var context = BalahninObuvEntities.GetContext();
                var user = context.Users.FirstOrDefault(u => u.Login == login);

                if (user == null)
                {
                    MessageBox.Show("Пользователь с таким логином не найден.",
                                    "Ошибка авторизации",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Manager.CurrentUser = user;
                Manager.MainFrame.Navigate(new CatalogPage());

                while (Manager.MainFrame.CanGoBack)
                    Manager.MainFrame.RemoveBackEntry();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            Manager.CurrentUser = null;
            Manager.MainFrame.Navigate(new CatalogPage());

            while (Manager.MainFrame.CanGoBack)
                Manager.MainFrame.RemoveBackEntry();
        }
    }
}