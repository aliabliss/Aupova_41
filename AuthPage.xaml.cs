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

namespace Aupova_41
{
    /// <summary>
    /// Логика взаимодействия для AuthPage.xaml
    /// </summary>
    public partial class AuthPage : Page
    {
        private string currentCaptcha;
        private bool isBlocked = false;
        private int blockTimeRemaining = 0;
        private bool captchaRequired = false;
        public AuthPage()
        {
            InitializeComponent();
            CaptchaPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            User guestUser = new User
            {
                UserID = 0,
                UserLogin = "guest",
                UserPassword = "",
                UserName = "Гость",
                UserSurname = "",
                UserPatronymic = "",
                UserRole = 1
            };
            Manager.MainFrame.Navigate(new ProductPage(guestUser));
        }
        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isBlocked)
            {
                MessageBox.Show($"Система заблокирована. Осталось {blockTimeRemaining} секунд.");
                return;
            }

            
            string login = LoginTB.Text;
            string password = PassTB.Text;
            if(login == "" || password == "")
            {
                MessageBox.Show("Есть пустые поля");
                return;
            }
            if (captchaRequired && CaptchaPanel.Visibility == Visibility.Visible)
            {
                if (CaptchaInput.Text != currentCaptcha)
                {
                    // НЕВЕРНАЯ КАПЧА - БЛОКИРОВКА КНОПКИ "ВОЙТИ" НА 10 СЕКУНД
                    BlockSystem();
                    MessageBox.Show("Неверная капча! Кнопка 'Войти' заблокирована на 10 секунд.");
                    return;
                }
            }
            User user = aupova_41Entities.GetContext().User.ToList().Find(p => p.UserLogin == login && p.UserPassword == password);
            if (user != null)
            {
                Manager.MainFrame.Navigate(new ProductPage(user));
                LoginTB.Text = "";
                PassTB.Text = "";
                CaptchaPanel.Visibility = Visibility.Collapsed;
                captchaRequired = false;
            }
            else
            {
                if (!captchaRequired)
                {
                    captchaRequired = true;
                    GenerateCaptcha();
                    CaptchaPanel.Visibility = Visibility.Visible;
                    MessageBox.Show("Введены неверные данные. Tребуется ввод капчи.");
                }
                else
                {
                    // Капча верная, но логин/пароль неверные - просто сообщение
                    MessageBox.Show("Неверные данные. Попробуйте еще раз.");
                    GenerateCaptcha(); // Обновляем капчу
                }
            }
        }

        private void GenerateCaptcha()
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random rnd = new Random();
            currentCaptcha = "";

            for (int i = 0; i < 4; i++)
            {
                currentCaptcha += chars[rnd.Next(chars.Length)];
            }

            CaptchaText.Text = currentCaptcha;
            CaptchaInput.Text = "";
        }

        private async void BlockSystem()
        {
            isBlocked = true;
            blockTimeRemaining = 10;

            BtnLogin.IsEnabled = false;
            string originalText = BtnLogin.Content.ToString();

            // Запускаем таймер обратного отсчета
            for (int i = 9; i >= 0; i--)
            {
                BtnLogin.Content = $"Заблокировано ({blockTimeRemaining} сек)";
                await Task.Delay(1000);
                blockTimeRemaining--;
            }
            // Блокировка на 10 секунд
            

            isBlocked = false;
            BtnLogin.IsEnabled = true;
            BtnLogin.Content = originalText; // Возвращаем "Войти"
            BtnGuest.IsEnabled = true;
            GenerateCaptcha();
        }

        private void RefreshCaptchaBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isBlocked)
            {
                GenerateCaptcha();
            }
        }
    }
}
            
        
        



    

