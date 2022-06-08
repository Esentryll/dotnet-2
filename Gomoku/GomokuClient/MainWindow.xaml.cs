using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Gomoku;

namespace GomokuClient
{
    public partial class MainWindow : Window
    {
        readonly Button[,] _playground = new Button[15, 15];

        private readonly Client client;

        public MainWindow(Client client)
        {
            InitializeComponent();
            for (int i = 0; i < _playground.GetLength(0); ++i)
                for (int j = 0; j < _playground.GetLength(1); ++j)
                {
                    _playground[i, j] = new Button
                    {
                        Name = $"Button_{i}_{j}",
                        Background = Brushes.Transparent
                    };
                    Grid.SetColumn(_playground[i, j], i);
                    Grid.SetRow(_playground[i, j], j);
                    PlaygroundGrid.Children.Add(_playground[i, j]);
                }
            PlaygroundGrid.AddHandler(Button.ClickEvent, new RoutedEventHandler(PlaygroudClick));
            this.client = client;
            Player1.Content = client.Login;
            Player2.Content = client.OpponentLogin;
            SetActivePlayer();
            this.client.MakeTurnSubject.Subscribe(SetOpponentTurn);
            this.client.EndGameSubject.Subscribe(EndGame);
        }

        private void NewGameClick(object sender, RoutedEventArgs e)
        {
            BusyIndicator.IsBusy = true;
            ClearPlayground();
            client.FindOpponentSubject.OnCompleted();
            client.FindOpponentSubject = new();
            client.FindOpponentSubject.Subscribe(StartNewGame);
            Task sumTask = new(async () =>
            {
                await client.FindOpponentRequest();
            });
            sumTask.Start();
        }

        private void StartNewGame(FindOpponentReply findOpponentReply)
        {
            Dispatcher.InvokeAsync(() =>
            {
                BusyIndicator.IsBusy = false;
                NewGameButton.Visibility = Visibility.Hidden;
                Player2.Content = findOpponentReply.Login;
                SetActivePlayer();
            });
        }

        private void PlaygroudClick(object sender, RoutedEventArgs e)
        {
            Button cell = ((Button)e.OriginalSource);
            if (cell.Content is not null || !client.ActivePlayer) 
                return;
            if (client.IsFirstPlayer)
                cell.Content = "X";
            else
                cell.Content = "O";
            var pointString = cell.Name.Replace("Button_", "").Split("_");
            int x = int.Parse(pointString[0]);
            int y = int.Parse(pointString[1]);
            Task sumTask = new(async () =>
            {
                await client.MakeTurnRequest(new Gomoku.Point() { X = x, Y = y });
            });
            sumTask.Start();
        }

        private void SetOpponentTurn(MakeTurnReply makeTurnReply)
        {
            Dispatcher.InvokeAsync(() =>
            {
                SetActivePlayer();
            });

            if (makeTurnReply.YourTurn)
            {
                var point = makeTurnReply.Point;
                if (!client.IsFirstPlayer)
                {
                    Dispatcher.InvokeAsync(() => _playground[point.X, point.Y].Content = "X");
                }
                else
                    Dispatcher.InvokeAsync(() => _playground[point.X, point.Y].Content = "O");
            }
        }

        private void ClearPlayground()
        {
            for (int i = 0; i < _playground.GetLength(0); ++i)
                for (int j = 0; j < _playground.GetLength(1); ++j)
                {
                    _playground[i, j].Content = null;
                    _playground[i, j].Background = Brushes.Transparent;
                }
            ActivePlayerLabel.Content = null;
            Player2.Content = null;
        }

        private void SetActivePlayer()
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (client.ActivePlayer)
                {
                    ActivePlayerLabel.Content = $"Ходит {client.Login}";
                }
                else
                {
                    ActivePlayerLabel.Content = $"Ходит {client.OpponentLogin}";
                }
            });
        }

        private void EndGame(EndGameReply endGameReply)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (endGameReply.Status == OutcomeStatus.Victory)
                {
                    ActivePlayerLabel.Content = "Победа";
                }
                else if(endGameReply.Status == OutcomeStatus.Defeat)
                {
                    ActivePlayerLabel.Content = "Проигрыш";
                }
                else
                {
                    ActivePlayerLabel.Content = "Ничья";
                }
                var points = endGameReply.Points;
                foreach (var point in points)
                    _playground[point.X, point.Y].Background = Brushes.Red;
                client.ActivePlayer = false;
                NewGameButton.Visibility = Visibility.Visible;
            });
        }
    }
}
