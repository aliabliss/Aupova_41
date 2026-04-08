using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Aupova_41
{
    class Manager
    {
        public static Frame MainFrame { get; set; }
        public static class UserSession
        {
            public static User CurrentUser { get; set; }

            public static void SetUser(User user)
            {
                CurrentUser = user;
            }

            public static void ClearUser()
            {
                CurrentUser = null;
            }

            public static bool IsUserLoggedIn()
            {
                return CurrentUser != null && CurrentUser.UserID > 0;
            }
        }
    }
}
