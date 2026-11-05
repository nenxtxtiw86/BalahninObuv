using System.Windows.Controls;

namespace BalahninObuv
{
    internal static class Manager
    {
        public static Frame MainFrame { get; set; }
        public static Users CurrentUser { get; set; }

        public static bool IsAdmin => CurrentUser != null && CurrentUser.RoleId == 1;
        public static bool IsManager => CurrentUser != null && CurrentUser.RoleId == 2;
        public static bool IsAuthorizedUser => CurrentUser != null;
        public static bool IsGuest => CurrentUser == null;

        /// <summary>Менеджер или админ — видят список заказов</summary>
        public static bool CanSeeOrders => IsAdmin || IsManager;

        /// <summary>Менеджер или админ — могут создавать/удалять заказы</summary>
        public static bool CanManageOrders => IsAdmin || IsManager;

    }
}