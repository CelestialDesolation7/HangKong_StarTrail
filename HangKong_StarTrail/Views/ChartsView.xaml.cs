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
using HangKong_StarTrail.Models;

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
        private Dictionary<string, double> _raceLabels = new();

        private GravitySimulationForm _gravityForm;
        private DispatcherTimer _timerVelocityForce;
        private DispatcherTimer _timerDeltaV;
        private DispatcherTimer _timerAllVelocity;

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

            // 1s定时器：竞速条形图
            _timerAllVelocity = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timerAllVelocity.Tick += TimerAllVelocity_Tick;
            _timerAllVelocity.Start();
        }

        private static readonly Dictionary<string, string> NameTranslation = new()
{
    { "太阳", "Sun" },
    { "水星", "Mercury" },
    { "金星", "Venus" },
    { "地球", "Earth" },
    { "月球", "Moon" },
    { "火星", "Mars" },
    { "木星", "Jupiter" },
    { "土星", "Saturn" },
    { "天王星", "Uranus" },
    { "海王星", "Neptune" },
    { "冥王星", "Pluto" },
    { "卡戎", "Charon" }
};


        private void TimerVelocityForce_Tick(object sender, EventArgs e)
        {
            if (_gravityForm == null || string.IsNullOrEmpty(_gravityForm.FocusedBodyName))
                return;
            string body = _gravityForm.FocusedBodyName;
            double time = DateTime.Now.TimeOfDay.TotalSeconds; // 或仿真时间
            double velocity = _gravityForm.CurrentVelocity;
            double force = _gravityForm.CurrentForce;

            UpdateVelocityTime(body, time, velocity);
            UpdateForceTime(body, time, force);


            List<Body> AllBody = _gravityForm.RendererInstance._physicsEngine.Bodies;
            foreach (var tem_body in AllBody)
            {
                // 读取每个 body 的属性，例如名称和速度
                string new_body = NameTranslation.TryGetValue(tem_body.Name, out var translated)
                                           ? translated
                                           : "Unknown";

                if (_raceLabels.ContainsKey(new_body))
                {
                    // 存在：更新
                    _raceLabels[new_body] = tem_body.Velocity.Length; // 或者更新具体值
                }
                else
                {
                    // 不存在：添加
                    _raceLabels.Add(new_body, tem_body.Velocity.Length);
                }
            }
        }

        private void TimerDeltaV_Tick(object sender, EventArgs e)
        {
            int maneuverIndex = _gravityForm.FrameCount; // 或用实际变轨计数
            double deltaV = _gravityForm.CurrentVelocity; // 示例，实际应为Δv
            UpdateDeltaVCounts(maneuverIndex, deltaV);
        }

        private void TimerAllVelocity_Tick(object sender, EventArgs e)
        {
            UpdateAllVelocity();
        }

        // ... existing code ...

        private void InitializeCharts()
        {
            // 速度-时间
            Series1 = Array.Empty<ISeries>();
            XAxes1 = new[] {
                new Axis {
                    Name = "Time",
                    MinLimit = 0,
                    MinStep = 500,
                    Labeler = value => $"{value:0}"
                }
            };
            YAxes1 = new[] {
                new Axis {
                    Name = "Velocity",
                    MinLimit = 0,
                    MaxLimit = _velocityYMax,
                    MinStep = 0.01,
                    Labeler = value => $"{value:0.00}"
                }
            };
            // 合力-时间
            Series2 = Array.Empty<ISeries>();
            XAxes2 = new[] {
                new Axis {
                    Name = "Time",
                    MinLimit = 0,
                    MinStep = 500,
                    Labeler = value => $"{value:0}"
                }
            };
            YAxes2 = new[] {
                new Axis {
                    Name = "Force",
                    MinLimit = 0,
                    MaxLimit = _forceYMax,
                    MinStep = 0.1,
                    Labeler = value => value.ToString("0.##E+0") // 科学计数法显示
                }
            };
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
                new ColumnSeries<double> { Name = "Race", Values = _raceValues }
            };
            XAxes4 = new[] { new Axis { Labels = _raceLabels.Keys.ToArray() } };
            YAxes4 = new[] { new Axis { Name = "Speed", MinLimit = 0, MaxLimit = 10 } };

        }

        // 1. 速度-时间曲线
        public void UpdateVelocityTime(string bodyName, double time, double velocity)
        {
            if (!_velocitySeries.ContainsKey(bodyName))
            {

                string new_bodyName = NameTranslation.TryGetValue(bodyName, out var translated)
                                           ? translated
                                           : "Unknown";

                var series = new LineSeries<ObservablePoint>
                {
                    Name = new_bodyName,
                    Values = new ObservableCollection<ObservablePoint>()
                };
                _velocitySeries[bodyName] = series;
                Series1 = _velocitySeries.Values.ToArray();
                OnPropertyChanged(nameof(Series1));
            }
            var values = (ObservableCollection<ObservablePoint>)_velocitySeries[bodyName].Values;
            values.Add(new ObservablePoint(time, velocity));

            // 保持最多 10 条数据
            while (values.Count > 10)
            {
                values.RemoveAt(0);
            }


            // X轴自适应


            if (time > _velocityXMax)
            {
                _velocityXMax = time * 1.1;
                // XAxes1[0].MaxLimit = _velocityXMax; // 由自适应逻辑替代
                // OnPropertyChanged(nameof(XAxes1));
            }
            // Y轴自适应
            if (velocity > _velocityYMax)
            {
                _velocityYMax = velocity * 1.1;
                YAxes1[0].MaxLimit = _velocityYMax;
                OnPropertyChanged(nameof(YAxes1));
            }
            // 横坐标自适应
            var allTimes = _velocitySeries.Values
                .SelectMany(series => ((ObservableCollection<ObservablePoint>)series.Values).Select(p => p.X))
                .ToList();
            if (allTimes.Count > 0)
            {
                double minTime = (double)allTimes.Min();
                double maxTime = (double)allTimes.Max();
                XAxes1[0].MinLimit = minTime;
                XAxes1[0].MaxLimit = maxTime;
                OnPropertyChanged(nameof(XAxes1));
            }
        }

        // 2. 合力-时间曲线
        public void UpdateForceTime(string bodyName, double time, double force)
        {
            if (!_forceSeries.ContainsKey(bodyName))
            {
                string new_bodyName = NameTranslation.TryGetValue(bodyName, out var translated)
                                           ? translated
                                           : "Unknown";
                var series = new LineSeries<ObservablePoint>
                {
                    Name = new_bodyName,
                    Values = new ObservableCollection<ObservablePoint>()
                };
                _forceSeries[bodyName] = series;
                Series2 = _forceSeries.Values.ToArray();
                OnPropertyChanged(nameof(Series2));
            }
            var values = (ObservableCollection<ObservablePoint>)_forceSeries[bodyName].Values;
            values.Add(new ObservablePoint(time, force));

            // 保持最多 10 条数据
            while (values.Count > 10)
            {
                values.RemoveAt(0);
            }

            // X轴自适应
            if (time > _forceXMax)
            {
                _forceXMax = time * 1.1;
                // XAxes2[0].MaxLimit = _forceXMax; // 由自适应逻辑替代
                // OnPropertyChanged(nameof(XAxes2));
            }
            // Y轴自适应
            if (force > _forceYMax)
            {
                _forceYMax = force * 1.1;
                YAxes2[0].MaxLimit = _forceYMax;
                OnPropertyChanged(nameof(YAxes2));
            }
            // 横坐标自适应
            var allTimes = _forceSeries.Values
                .SelectMany(series => ((ObservableCollection<ObservablePoint>)series.Values).Select(p => p.X))
                .ToList();
            if (allTimes.Count > 0)
            {
                double minTime = (double)allTimes.Min();
                double maxTime = (double)allTimes.Max();
                XAxes2[0].MinLimit = minTime;
                XAxes2[0].MaxLimit = maxTime;
                OnPropertyChanged(nameof(XAxes2));
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
        public void UpdateAllVelocity()
        {
            //_raceValues.Clear();
            // _raceLabels.Clear();
            //double max = 10;
            //foreach (var kv in currentVelocities)
            //{
            //    _raceLabels.Add(kv.Key);
            //    _raceValues.Add(kv.Value);
            //    if (kv.Value > max) max = kv.Value;
            //}
            if (_raceLabels == null || _raceLabels.Count == 0)
                return;

            // 清空旧数据
            _raceValues.Clear();

            // 动态计算最大值
            double max = _raceLabels.Values.Max();
            if (max <= 0) max = 10; // 默认最小范围

            // 填充新数据
            foreach (var key in _raceLabels.Keys)
            {
                _raceValues.Add(_raceLabels[key]);
            }

            XAxes4[0].Labels = _raceLabels.Keys.ToArray();
            XAxes4[0].MinStep = 1; // 防止自动跳过标签
            XAxes4[0].LabelsRotation = 90; // 或其他角度
            //XAxes4[0].TextSize = 10; // 设置字体大小为10

            YAxes4[0].MaxLimit = max * 1.1;


            // 复用已有的 Series 对象以提高性能
            var columnSeries = Series4.FirstOrDefault() as ColumnSeries<double>;
            if (columnSeries == null)
            {
                Series4 = new ISeries[]
                {
            new ColumnSeries<double>
            {
                Name = "Race",
                Values = _raceValues
            }
                };
            }
            else
            {
                columnSeries.Values = _raceValues;
            }

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