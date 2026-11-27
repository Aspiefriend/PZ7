using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using PZ6.Models;
using PZ6.Services;

namespace PZ6.Pages
{
    /// <summary>
    /// Логика взаимодействия для Autho.xaml
    /// </summary>
    public partial class Autho : Page
    {
        private int click;
        private DispatcherTimer timer;
        public Autho()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            if (Lock.isLocked)
            {
                StartTimer();
            }
            timer.Tick += HandlerTimer;
        }
        private void GenerateCaptcha()
        {
            tblCaptcha.Visibility = Visibility.Visible;
            tbCaptcha.Visibility = Visibility.Visible;

            string captchaText = CaptchaGenerator.GenerateCaptchaText(6);
            tblCaptcha.Text = captchaText;
            tblCaptcha.TextDecorations = TextDecorations.Strikethrough;
        }
        private void ClearInput()
        {
            txtPassword.Password = "";
        }
        private void btnEnterGuests_Click(object sender, RoutedEventArgs e)
        {
            ClearInput();
            tblCaptcha.Visibility = Visibility.Hidden;
            tbCaptcha.Visibility = Visibility.Hidden;
            NavigationService.Navigate(new Client(null,null));
        }
        private List<string> GetRoleNames(Accounts account)
        {

            using (var db = Helper.GetContext())
            {
                var roleNames = db.AccountRoles.Where(ar => ar.id_account == account.id_account).Join(db.Roles,
                    ar => ar.id_role,
                    r => r.id_role,
                    (ar, r) => r.role_name).ToList();
                return roleNames;
            }  


        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = txtLogin.Text.Trim();
                string password = txtPassword.Password.Trim();
                if (Lock.isLocked)
                {
                    return;

                }
                if (tblCaptcha.Text != tbCaptcha.Text && tblCaptcha.Visibility!=Visibility.Collapsed)
                {
                    MessageBox.Show("Вы неправильно ввели капчу");
                    FailHandler();
                }

                var account = CheckUserLogin(login);
                if (account == null)
                {
                    MessageBox.Show("Неверный логин или пароль");
                    AddToLoginHistory(login, false, "Аккаунта с таким логином не существует");
                    FailHandler();
                    return;

                }
                string hashPassword = Hash.HashPasswords(password);
                account = CheckUserPassword(login, hashPassword);

                if (account == null)
                {
                    MessageBox.Show("Неверный логин или пароль");
                    AddToLoginHistory(login, false, "Неверный пароль");
                    FailHandler();
                    return;

                }
                AddToLoginHistory(login, true, null);
                SuccessHandler(account);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка работы с базой данных {ex} ");


            }


        }
        
        
        public void AddToLoginHistory(string login,bool isSuccess, string reason = null)
        {

            using(var db= Helper.GetContext())
            {

                if(reason == null)
                {
                    LoginHistory newLog = new LoginHistory
                    {
                        UserLogin = login,
                        IsSuccess = isSuccess,
                        FailReason = reason,
                    };

                }

            }
            


        }
        public void StartTimer()
        {
            txtLogin.IsEnabled = false;
            txtPassword.IsEnabled = false;
            btnEnter.IsEnabled = false;
            tbCaptcha.Visibility = Visibility.Hidden;
            tblCaptcha.Visibility = Visibility.Hidden;
            int secondsToAdd;
            if (Lock.LockoutEndTime == null)
            {
                secondsToAdd = (int)Math.Pow(10, Lock.LockoutLevel);
                Lock.LockoutEndTime = DateTime.Now.AddSeconds(secondsToAdd);
                MessageBox.Show($"Вход заблокирован на {secondsToAdd} секунд ");

            }
            tbTime.Text = $"Блокировка до {Lock.LockoutEndTime}";
            timer.Start();


            
        }
        public void HandlerTimer(object sender, EventArgs e)
        {
            int seconds = Lock.GetSeconds();
            if (seconds == 0)
            {
                MessageBox.Show("Блокировка закончилась");
                EndTimer();

            }
            tbLockout.Text = $"Вход был заблокирован. Осталось {seconds} секунд";


        }
        public void EndTimer()
        {
            timer.Stop();
            txtLogin.IsEnabled = true;
            txtPassword.IsEnabled = true;
            btnEnter.IsEnabled = true;
            tbLockout.Visibility = Visibility.Collapsed;
            if (Lock.FailCount == 5)
            {
                Lock.Reset();
                MessageBox.Show("Счётчик неудачных попыток сбросился");
                tblCaptcha.Visibility = Visibility.Collapsed;
            }
            else
            {
                Lock.FailCount++;
                Lock.LockoutLevel++;
                
            }
            


        }
        public void FailHandler()
        {
            Lock.FailCount++;
            var count = Lock.FailCount;
            ClearInput();
            GenerateCaptcha();
            if(count < 3)
            {
                tbWarning.Visibility = Visibility.Visible;
                tbWarning.Text = $"У вас осталось {3 - count} попыток";
                return;
         

            }
            else
            {
                tbWarning.Visibility = Visibility.Collapsed;
                StartTimer();


            }
        }
        public void SuccessHandler(Accounts account)
        {
            var roleNames = GetRoleNames(account);
            if (roleNames.Count == 1)
            {
                LoadPage(account, roleNames[0]);
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Войти как сотрудник?", "Выбор роли", MessageBoxButton.YesNo);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        LoadPage(account, roleNames.Where(rn => rn == "Сотрудник").FirstOrDefault());
                        break;
                    case MessageBoxResult.No:
                        LoadPage(account, roleNames.Where(rn => rn == "Клиент").FirstOrDefault());
                        break;

                }
            }
        }
 

        public Accounts CheckUserPassword(string login,string password)
        {
            using (var db = Helper.GetContext())
            {
                var account = db.Accounts.Where(a => a.login == login && a.password_hash == password).FirstOrDefault();
                return account;
            }
        }
        public Accounts CheckUserLogin(string login)
        {
            using (var db = Helper.GetContext())
            {
                var account = db.Accounts.Where(a => a.login == login).FirstOrDefault();
                return account;

            }
        }

        
        private void LoadPage(Accounts account, string role)
        {
            ClearInput();
            Lock.Reset();
            tbWarning.Visibility = Visibility.Collapsed;
            tblCaptcha.Visibility = Visibility.Hidden;
            tbCaptcha.Visibility = Visibility.Hidden;
            NavigationService.Navigate(new Client(account, role));


        }
    }
}
