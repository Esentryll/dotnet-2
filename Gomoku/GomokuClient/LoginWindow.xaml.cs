using System;
using System.Threading.Tasks;
using System.Windows;

using Gomoku;

namespace GomokuClient
{
    public partial class LoginWindow : Window
    {
        private readonly Client _client = new();
        public string Login { get; set; } = string.Empty;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void PlayClick(object sender, RoutedEventArgs e)
        {
            BusyIndicator.IsBusy = true;
            Login = LoginTextBox.Text;
            _client.LoginSubject.Subscribe(FindOpponent);
            Task sumTask = new(async () =>
            {
                await _client.LoginRequest(Login);
            });
            sumTask.Start();
        }

        public void FindOpponent(LoginReply loginReply)
        {
            _client.FindOpponentSubject.Subscribe(StartGame);
            Task sumTask = new(async () =>
            {
                await _client.FindOpponentRequest();
            });
            sumTask.Start();
        }

        public void StartGame(FindOpponentReply findOpponentReply)
        {
            _client.LoginSubject.OnCompleted();
            _client.FindOpponentSubject.OnCompleted();
            Dispatcher.InvokeAsync(() =>
            {
                Application.Current.MainWindow = new MainWindow(_client) { Owner = this };
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.Owner = null;
                Close();
            });
        }
    }
}