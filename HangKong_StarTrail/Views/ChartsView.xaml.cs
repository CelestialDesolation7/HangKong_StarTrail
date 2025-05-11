using System;
using System.Collections.Generic;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using System.Collections.ObjectModel;
using LiveChartsCore.Defaults;
using System.Linq;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting;


namespace HangKong_StarTrail.Views
{
    public partial class ChartsView : Window
    {
        // 速度-时间曲线数据
        private Dictionary<string, ObservableCollection<ISeries>> velocitySeries = new();
        // 合力-时间曲线数据
        private Dictionary<string, ObservableCollection<ISeries>> forceSeries = new();
        // Δv统计数据
        private ObservableCollection<ISeries> deltaVSeries = new();
        private List<double> deltaVList = new();
        // 所有天体速度对比数据
        private ObservableCollection<ISeries> allVelocitySeries = new();

        public ChartsView()
        {
            InitializeComponent();
            InitCharts();
            DemoData(); // 加载演示数据
        }

        private void InitCharts()
        {
            var axisFont = SKTypeface.FromFamilyName("Microsoft YaHei");

            VelocityTimeChart.Series = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservablePoint> { Name = "Earth", Values = new ObservableCollection<ObservablePoint> { new ObservablePoint(0, 7.9), new ObservablePoint(1, 8.0), new ObservablePoint(2, 8.1) } },
                new LineSeries<ObservablePoint> { Name = "Mars", Values = new ObservableCollection<ObservablePoint> { new ObservablePoint(0, 5.0), new ObservablePoint(1, 5.2), new ObservablePoint(2, 5.3) } }
            };
            VelocityTimeChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Time",
                    TextSize = 18,
                    LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = axisFont }
                }
            };
            VelocityTimeChart.YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Velocity",
                    TextSize = 18,
                    LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = axisFont }
                }
            };
            // 合力-时间曲线图初始化
            ForceTimeChart.Series = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservablePoint> { Name = "Earth", Values = new ObservableCollection<ObservablePoint> { new ObservablePoint(0, 10), new ObservablePoint(1, 12), new ObservablePoint(2, 11) } },
                new LineSeries<ObservablePoint> { Name = "Mars", Values = new ObservableCollection<ObservablePoint> { new ObservablePoint(0, 7), new ObservablePoint(1, 8), new ObservablePoint(2, 7.5) } }
            };
            ForceTimeChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Time",
                    TextSize = 18,
                    LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = axisFont }
                }
            };
            ForceTimeChart.YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Force",
                    TextSize = 18,
                    LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = axisFont }
                }
            };
            // Δv统计图初始化
            DeltaVChart.Series = new ObservableCollection<ISeries>
            {
                new ColumnSeries<double> { Name = "Δv", Values = new double[] { 0.2, 0.5, 0.3 } }
            };
            DeltaVChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Maneuver Index",
                    TextSize = 18,
                    LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = axisFont }
                }
            };
            DeltaVChart.YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Δv",
                    TextSize = 18,
                    LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = axisFont }
                }
            };
            // 所有天体速度对比图初始化（竞速条形图风格）
            AllVelocityRaceChart.Series = new ObservableCollection<ISeries>
            {
                new RowSeries<double> { Name = "Verstapen", Values = new double[] { 9623 }, DataLabelsPaint = new SolidColorPaint(SKColors.White), DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End, Fill = new SolidColorPaint(SKColors.Red) },
                new RowSeries<double> { Name = "Sainz", Values = new double[] { 9486 }, DataLabelsPaint = new SolidColorPaint(SKColors.White), DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End, Fill = new SolidColorPaint(SKColors.SeaGreen) },
                new RowSeries<double> { Name = "Hamilton", Values = new double[] { 9366 }, DataLabelsPaint = new SolidColorPaint(SKColors.White), DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End, Fill = new SolidColorPaint(SKColors.DodgerBlue) },
                new RowSeries<double> { Name = "Tsunoda", Values = new double[] { 9352 }, DataLabelsPaint = new SolidColorPaint(SKColors.White), DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End, Fill = new SolidColorPaint(SKColors.Goldenrod) },
                new RowSeries<double> { Name = "Bottas", Values = new double[] { 9195 }, DataLabelsPaint = new SolidColorPaint(SKColors.White), DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End, Fill = new SolidColorPaint(SKColors.SteelBlue) },
                new RowSeries<double> { Name = "Riccardo", Values = new double[] { 9191 }, DataLabelsPaint = new SolidColorPaint(SKColors.White), DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End, Fill = new SolidColorPaint(SKColors.MediumTurquoise) }
            };
            AllVelocityRaceChart.XAxes = new Axis[]
            {
                new Axis { Name = "Racing bars", MinLimit = 8800, MaxLimit = 10000, TextSize = 16 }
            };
            AllVelocityRaceChart.YAxes = new Axis[]
            {
                new Axis { Labels = new[] { "Verstapen", "Sainz", "Hamilton", "Tsunoda", "Bottas", "Riccardo" }, TextSize = 16 }
            };
        }

        private void DemoData()
        {
            // 不再需要动态添加，所有数据已在InitCharts中直接设置
        }

        // 1. 更新速度-时间曲线图
        public void UpdateVelocityTime(string bodyName, double time, double velocity)
        {
            // 查找或新建该天体的曲线
            var series = GetOrCreateLineSeries(VelocityTimeChart, bodyName);
            if (series.Values is not ObservableCollection<ObservablePoint> values)
            {
                values = new ObservableCollection<ObservablePoint>();
                series.Values = values;
            }
            values.Add(new ObservablePoint(time, velocity));
        }

        // 2. 更新合力-时间曲线图
        public void UpdateForceTime(string bodyName, double time, double force)
        {
            var series = GetOrCreateLineSeries(ForceTimeChart, bodyName);
            if (series.Values is not ObservableCollection<ObservablePoint> values)
            {
                values = new ObservableCollection<ObservablePoint>();
                series.Values = values;
            }
            values.Add(new ObservablePoint(time, force));
        }

        // 3. 更新Δv统计图
        public void UpdateDeltaVCounts(int maneuverIndex, double deltaV)
        {
            if (!DeltaVChart.Series.Any()) return;
            if (deltaVList.Count <= maneuverIndex)
            {
                while (deltaVList.Count <= maneuverIndex) deltaVList.Add(0);
            }
            deltaVList[maneuverIndex] = deltaV;
        }

        // 4. 更新所有天体速度对比图
        public void UpdateAllVelocity(Dictionary<string, double> currentVelocities)
        {
            var series = new ObservableCollection<ISeries>();
            foreach (var kv in currentVelocities)
            {
                series.Add(new RowSeries<double> { Name = kv.Key, Values = new[] { kv.Value } });
            }
            AllVelocityRaceChart.Series = series;
        }

        // 工具：查找或新建LineSeries
        private LineSeries<ObservablePoint> GetOrCreateLineSeries(CartesianChart chart, string name)
        {
            foreach (var s in chart.Series)
            {
                if (s is LineSeries<ObservablePoint> ls && ls.Name == name)
                    return ls;
            }
            var newSeries = new LineSeries<ObservablePoint> { Name = name, Values = new ObservableCollection<ObservablePoint>() };
            (chart.Series as ObservableCollection<ISeries>)?.Add(newSeries);
            return newSeries;
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            // 这里可以写返回或关闭窗口的逻辑
            this.Close();
        }
    }
} 