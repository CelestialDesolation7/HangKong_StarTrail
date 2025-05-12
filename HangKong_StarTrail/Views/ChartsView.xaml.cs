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

namespace HangKong_StarTrail.Views
{
    public partial class ChartsView : UserControl, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private readonly Random _random = new Random();
        private int _currentIndex = 0;
        private const int MaxPoints = 50;

        public ISeries[] Series1 { get; set; }
        public ISeries[] Series2 { get; set; }
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

        public ChartsView()
        {
            InitializeComponent();
            InitializeCharts();
            DataContext = this;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void InitializeCharts()
        {
            // 初始化位移图表
            Series1 = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Height",
                    Values = new ObservableCollection<double>(),
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                    GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                    GeometrySize = 8,
                    XToolTipLabelFormatter = (chartPoint) => $"Height: {chartPoint.Coordinate.PrimaryValue:F2} m",
                    EnableNullSplitting = false,
                    LineSmoothness = 0.5
                }
            };

            XAxes1 = new Axis[]
            {
                new Axis
                {
                    Name = "Time",
                    NamePaint = new SolidColorPaint(SKColors.Black),
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                    MinLimit = 0,
                    MaxLimit = MaxPoints
                }
            };

            YAxes1 = new Axis[]
            {
                new Axis
                {
                    Name = "Height (m)",
                    NamePaint = new SolidColorPaint(SKColors.Black),
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                    MinLimit = 0,
                    MaxLimit = 10000
                }
            };

            // 初始化速度图表
            Series2 = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Speed(m/s)",
                    Values = new ObservableCollection<double>(),
                    Stroke = new SolidColorPaint(SKColors.Orange, 2),
                    GeometryStroke = new SolidColorPaint(SKColors.Orange, 2),
                    GeometrySize = 8,
                    XToolTipLabelFormatter = (chartPoint) => $"Speed: {chartPoint.Coordinate.PrimaryValue:F2} m/s",
                    EnableNullSplitting = false,
                    LineSmoothness = 0.5
                }
            };

            XAxes2 = new Axis[]
            {
                new Axis
                {
                    Name = "Time",
                    NamePaint = new SolidColorPaint(SKColors.Black),
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                    MinLimit = 0,
                    MaxLimit = MaxPoints
                }
            };

            YAxes2 = new Axis[]
            {
                new Axis
                {
                    Name = "Speed (m/s)",
                    NamePaint = new SolidColorPaint(SKColors.Black),
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                    MinLimit = 0,
                    MaxLimit = 1000
                }
            };

            // Δv统计图
            Series3 = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "DeltaV",
                    Values = new ObservableCollection<double> { 0.2, 0.5, 0.3, 0.7, 0.4 },
                    Stroke = new SolidColorPaint(SKColors.MediumPurple, 2),
                    Fill = new SolidColorPaint(SKColors.MediumPurple.WithAlpha(128)),
                    MaxBarWidth = 40
                }
            };
            XAxes3 = new Axis[]
            {
                new Axis
                {
                    Name = "Maneuver",
                    NamePaint = new SolidColorPaint(SKColors.Black),
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                    Labels = new[] { "#1", "#2", "#3", "#4", "#5" }
                }
            };
            YAxes3 = new Axis[]
            {
                new Axis
                {
                    Name = "Δv (m/s)",
                    NamePaint = new SolidColorPaint(SKColors.Black),
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                    MinLimit = 0,
                    MaxLimit = 1
                }
            };

            // 所有天体速度对比图（Race Bar Chart）
            Series4 = new ISeries[]
            {
                new RowSeries<double>
                {
                    Name = "Celestial Bodies",
                    Values = new ObservableCollection<double> { 9623, 9486, 9366, 9352, 9195, 9191 },
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                    Fill = new SolidColorPaint(SKColors.MediumTurquoise),
                    MaxBarWidth = 40
                }
            };
            XAxes4 = new Axis[]
            {
                new Axis
                {
                    Name = "Speed (m/s)",
                    NamePaint = new SolidColorPaint(SKColors.Black),
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                    MinLimit = 8800,
                    MaxLimit = 10000
                }
            };
            YAxes4 = new Axis[]
            {
                new Axis
                {
                    Labels = new[] { "Verstappen", "Sainz", "Hamilton", "Tsunoda", "Bottas", "Riccardo" },
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    NamePaint = new SolidColorPaint(SKColors.Black)
                }
            };
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // 模拟数据更新
            var height = _random.NextDouble() * 10000;
            var speed = _random.NextDouble() * 1000;

            var heightSeries = (LineSeries<double>)Series1[0];
            var speedSeries = (LineSeries<double>)Series2[0];

            if (heightSeries?.Values == null || speedSeries?.Values == null) return;

            var heightValues = (ObservableCollection<double>)heightSeries.Values;
            var speedValues = (ObservableCollection<double>)speedSeries.Values;

            if (heightValues.Count >= MaxPoints)
            {
                heightValues.RemoveAt(0);
                speedValues.RemoveAt(0);
            }

            heightValues.Add(height);
            speedValues.Add(speed);

            _currentIndex++;

            // 更新第2个Δv条的值为0.8
            UpdateDeltaV(1, 0.8);
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

        public void UpdateDeltaV(int index, double value)
        {
            // 确保 Series3 已初始化
            if (Series3 == null || Series3.Length == 0) return;
            var columnSeries = Series3[0] as ColumnSeries<double>;
            if (columnSeries == null) return;

            var values = columnSeries.Values as ObservableCollection<double>;
            if (values == null) return;

            // 动态扩展长度
            while (values.Count <= index)
            {
                values.Add(0);
            }
            values[index] = value;
        }

        // 动态更新第四张图表数据示例
        public void UpdateRaceChart(double[] speeds)
        {
            if (Series4 == null || Series4.Length == 0) return;
            var rowSeries = Series4[0] as RowSeries<double>;
            if (rowSeries == null) return;
            var values = rowSeries.Values as ObservableCollection<double>;
            if (values == null) return;
            values.Clear();
            foreach (var s in speeds) values.Add(s);
        }

        public void UpdateRaceChartExample()
        {
            UpdateRaceChart(new double[] { 9700, 9500, 9400, 9300, 9200, 9100 });
        }
    }
}