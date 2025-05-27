using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace HangKong_StarTrail.Views.DebugForm
{
    public partial class DebugSimulationForm : Window
    {
        private DispatcherTimer _updateTimer;
        private Action<string> _updateCallback;

        public DebugSimulationForm(Action<string> updateCallback)
        {
            InitializeComponent();
            _updateCallback = updateCallback;

            // 初始化更新计时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // 每100ms更新一次
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            _updateCallback?.Invoke(DebugTextBox.Text);
        }

        protected override void OnClosed(EventArgs e)
        {
            _updateTimer.Stop();
            base.OnClosed(e);
        }
    }
}