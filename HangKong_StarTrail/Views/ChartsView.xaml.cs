using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.VisualElements;
using SkiaSharp;
using System.Windows.Threading;
using System.Threading.Tasks;
using LiveChartsCore.Defaults;

namespace HangKong_StarTrail.Views
{
    /// <summary>
    /// <summary>
    /// ChartsView.xaml 的交互逻辑
    /// </summary>
    public partial class ChartsView : UserControl, INotifyPropertyChanged
    {
        // 速度-时间曲线图
        private Dictionary<string, LineSeries<ObservablePoint>> _velocitySeries = new();
        private double _velocityYMax = 10;
        private double _velocityXMax = 10;
        // 合力-时间曲线图
        private Dictionary<string, LineSeries<ObservablePoint>> _forceSeries = new();
        private double _forceYMax = 10;
        private double _forceXMax = 10;
        // Δv统计图
        private ObservableCollection<double> _deltaVValues = new();
        private double _deltaVMax = 1;
        // 所有天体速度对比图
        private ObservableCollection<double> _raceValues = new();
        private List<string> _raceLabels = new();

        private GravitySimulationForm _gravityForm;
        private DispatcherTimer _timerVelocityForce;
        private DispatcherTimer _timerDeltaV;

        public ISeries[] Series1 { get; set; } = Array.Empty<ISeries>();
        public ISeries[] Series2 { get; set; } = Array.Empty<ISeries>();
        public ISeries[] Series3 { get; set; }
        public ISeries[] Series4 { get; set; }
        public Axis[] XAxes1 { get; set; }
        public Axis[] YAxes1 { get; set; }
        public Axis[] XAxes2 { get; set; }
        public Axis[] YAxes2 { get; set; }
        public Axis[] XAxes3 { get; set; }
        public Axis[] YAxes3 { get; set; }
        public Axis[] XAxes4 { get; set; }
        public Axis[] YAxes4 { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ... existing code ...
// ... existing code ...
public ChartsView(GravitySimulationForm gravityForm)
{
    InitializeComponent();
    _gravityForm = gravityForm;
    InitializeCharts();
    DataContext = this;

    // 100ms定时器：速度-时间、受力-时间
    _timerVelocityForce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
    _timerVelocityForce.Tick += TimerVelocityForce_Tick;
    _timerVelocityForce.Start();

    // 1s定时器：Δv统计
    _timerDeltaV = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
    _timerDeltaV.Tick += TimerDeltaV_Tick;
    _timerDeltaV.Start();
}

private void TimerVelocityForce_Tick(object sender, EventArgs e)
{
    string body = _gravityForm.FocusedBodyName;
    double time = DateTime.Now.TimeOfDay.TotalSeconds; // 或仿真时间
    double velocity = _gravityForm.CurrentVelocity;
    double force = _gravityForm.CurrentForce;

    UpdateVelocityTime(body, time, velocity);
    UpdateForceTime(body, time, force);
}

private void TimerDeltaV_Tick(object sender, EventArgs e)
{
    int maneuverIndex = _gravityForm.FrameCount; // 或用实际变轨计数
    double deltaV = _gravityForm.CurrentVelocity; // 示例，实际应为Δv
    UpdateDeltaVCounts(maneuverIndex, deltaV);
}
// ... existing code ...

        private void InitializeCharts()
        {
            // 速度-时间
            Series1 = Array.Empty<ISeries>();
            XAxes1 = new[] { new Axis { Name = "Time", MinLimit = 0 } };
            YAxes1 = new[] { new Axis { Name = "Velocity", MinLimit = 0, MaxLimit = _velocityYMax } };
            // 合力-时间
            Series2 = Array.Empty<ISeries>();
            XAxes2 = new[] { new Axis { Name = "Time", MinLimit = 0 } };
            YAxes2 = new[] { new Axis { Name = "Force", MinLimit = 0, MaxLimit = _forceYMax } };
            // Δv统计
            Series3 = new ISeries[]
            {
                new ColumnSeries<double> { Name = "DeltaV", Values = _deltaVValues }
            };
            XAxes3 = new[] { new Axis { Name = "Maneuver" } };
            YAxes3 = new[] { new Axis { Name = "Δv", MinLimit = 0, MaxLimit = _deltaVMax } };
            // 竞速条形图
            Series4 = new ISeries[]
            {
                new RowSeries<double> { Name = "Race", Values = _raceValues }
            };
            XAxes4 = new[] { new Axis { Name = "Speed", MinLimit = 0, MaxLimit = 10 } };
            YAxes4 = new[] { new Axis { Labels = _raceLabels.ToArray() } };
        }

        // 1. 速度-时间曲线
        public void UpdateVelocityTime(string bodyName, double time, double velocity)
        {
            if (!_velocitySeries.ContainsKey(bodyName))
            {
                var series = new LineSeries<ObservablePoint>
                {
                    Name = bodyName,
                    Values = new ObservableCollection<ObservablePoint>()
                };
                _velocitySeries[bodyName] = series;
                Series1 = _velocitySeries.Values.ToArray();
                OnPropertyChanged(nameof(Series1));
            }
            var values = (ObservableCollection<ObservablePoint>)_velocitySeries[bodyName].Values;
            values.Add(new ObservablePoint(time, velocity));
            // X轴自适应
            if (time > _velocityXMax)
            {
                _velocityXMax = time * 1.1;
                XAxes1[0].MaxLimit = _velocityXMax;
                OnPropertyChanged(nameof(XAxes1));
            }
            // Y轴自适应
            if (velocity > _velocityYMax)
            {
                _velocityYMax = velocity * 1.1;
                YAxes1[0].MaxLimit = _velocityYMax;
                OnPropertyChanged(nameof(YAxes1));
            }
        }

        // 2. 合力-时间曲线
        public void UpdateForceTime(string bodyName, double time, double force)
        {
            if (!_forceSeries.ContainsKey(bodyName))
            {
                var series = new LineSeries<ObservablePoint>
                {
                    Name = bodyName,
                    Values = new ObservableCollection<ObservablePoint>()
                };
                _forceSeries[bodyName] = series;
                Series2 = _forceSeries.Values.ToArray();
                OnPropertyChanged(nameof(Series2));
            }
            var values = (ObservableCollection<ObservablePoint>)_forceSeries[bodyName].Values;
            values.Add(new ObservablePoint(time, force));
            // X轴自适应
            if (time > _forceXMax)
            {
                _forceXMax = time * 1.1;
                XAxes2[0].MaxLimit = _forceXMax;
                OnPropertyChanged(nameof(XAxes2));
            }
            // Y轴自适应
            if (force > _forceYMax)
            {
                _forceYMax = force * 1.1;
                YAxes2[0].MaxLimit = _forceYMax;
                OnPropertyChanged(nameof(YAxes2));
            }
        }

        // 3. Δv统计
        public void UpdateDeltaVCounts(int maneuverIndex, double deltaV)
        {
            while (_deltaVValues.Count <= maneuverIndex)
                _deltaVValues.Add(0);
            _deltaVValues[maneuverIndex] = deltaV;
            if (deltaV > _deltaVMax)
            {
                _deltaVMax = deltaV * 1.1;
                YAxes3[0].MaxLimit = _deltaVMax;
                OnPropertyChanged(nameof(YAxes3));
            }
        }

        // 4. 竞速条形图
        public void UpdateAllVelocity(Dictionary<string, double> currentVelocities)
        {
            _raceValues.Clear();
            _raceLabels.Clear();
            double max = 10;
            foreach (var kv in currentVelocities)
            {
                _raceLabels.Add(kv.Key);
                _raceValues.Add(kv.Value);
                if (kv.Value > max) max = kv.Value;
            }
            YAxes4[0].Labels = _raceLabels.ToArray();
            XAxes4[0].MaxLimit = max * 1.1;
            OnPropertyChanged(nameof(Series4));
            OnPropertyChanged(nameof(YAxes4));
            OnPropertyChanged(nameof(XAxes4));
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            // 关闭当前窗口（UserControl只能请求父窗口关闭）
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }
    }
}