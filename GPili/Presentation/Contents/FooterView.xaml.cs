using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Dispatching;
using System;

namespace GPili.Presentation.Contents
{
    public partial class FooterView : ContentView, IDisposable
    {
        IDispatcherTimer? _timer;

        public FooterView()
        {
            InitializeComponent();
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (Parent != null)
                StartTimer();
            else
                StopTimer();
        }

        void StartTimer()
        {
            if (_timer is not null)
                return;

            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTick;
            _timer.Start();
        }

        void StopTimer()
        {
            if (_timer is null)
                return;

            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer = null;
        }

        void OnTick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            var isConnected = Connectivity.NetworkAccess == NetworkAccess.Internet;

            // Update UI elements from your XAML
            Date.Text = $"Date: {now:dd/MM/yyyy(ddd) hh:mm:ss tt}";
            PosName.Text = $"POS: {POSInfo.Terminal.PosName}" +
                                  (POSInfo.Terminal.IsTrainMode ? " (Training)" : "");
            NetworkStatus.Text = isConnected ? "Online" : "Offline";
            NetworkStatus.TextColor = isConnected ? Colors.Green : Colors.Red;
        }

        public void Dispose()
        {
            StopTimer();
        }
    }
}
