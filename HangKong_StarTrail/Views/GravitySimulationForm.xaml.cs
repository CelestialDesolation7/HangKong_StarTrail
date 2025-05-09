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
using System.Windows.Shapes;
using HangKong_StarTrail.Models;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace HangKong_StarTrail.Views
{
    /// <summary>
    /// GravitySimulationForm.xaml 的交互逻辑
    /// </summary>
    public partial class GravitySimulationForm : Window
    {
        private PhysicsEngine _physicsEngine;
        private Renderer _renderer;
        private DispatcherTimer _simulationTimer;
        private double _timeStep = 43200; // 单位：秒，每帧约12小时物理时间
        private double _pixelToDistanceRatio = 23376e2; // 比例尺，每一个像素代表的物理距离
        private bool _showTrajectory = false;
        private Vector2D _canvasCenter;
        private bool _isSimulationRunning = true; // 默认开始仿真
        private double _timeScale = 1.0;
        private bool _isTimeReversed = false;
        private Vector2D _cameraOffset = Vector2D.ZeroVector;
        private double _zoomScale = 1.0;
        private bool _isAddingBody = false;
        private Body _focusedBody = null;
        private bool _isVelocitySettingMode = false;

        public GravitySimulationForm()
        {
            InitializeComponent();
            InitializeSimulation();
        }

        private void InitializeSimulation()
        {
            _physicsEngine = new PhysicsEngine();
            _renderer = new Renderer();
            
            // 初始化仿真计时器
            _simulationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // 约60FPS
            };
            _simulationTimer.Tick += SimulationTimer_Tick;
            _simulationTimer.Start();

            // 初始化画布事件
            animationCanva.PaintSurface += OnPaintSurface;

            // 初始化时间步长滑块
            SimulationTimeStepSlider.Minimum = 0.1;
            SimulationTimeStepSlider.Maximum = 100000.0;
            SimulationTimeStepSlider.Value = 1.0;
            SimulationTimeStepSlider.ValueChanged += SimulationTimeStepSlider_ValueChanged;

            // 初始化按钮状态
            UpdateButtonStates();

            // 加载默认地月系数据
            LoadDefaultEarthMoonSystem();
        }

        private void LoadDefaultEarthMoonSystem()
        {
            // 地球数据
            var earth = new Body(
                "地球",
                new Vector2D(0, 0),  // 位置在原点
                new Vector2D(0, 0),  // 初始速度
                5.972e24,            // 质量（kg）
                20,                  // 显示半径
                true,                // 中心天体
                System.Drawing.Color.Blue
            );

            // 月球数据
            var moon = new Body(
                "月球",
                new Vector2D(384400e3, 0),  // 地月距离（米）
                new Vector2D(0, 1022),      // 月球轨道速度（m/s）
                7.348e22,                   // 质量（kg）
                10,                         // 显示半径
                false,                      // 非中心天体
                System.Drawing.Color.Gray
            );

            _physicsEngine.AddBody(earth);
            _physicsEngine.AddBody(moon);

            // 设置合适的时间步长（约8小时每帧）
            _timeScale = 28800;  // 8小时 = 28800秒
            SimulationTimeStepSlider.Value = _timeScale;

            // 调整缩放比例以适应地月系统
            // 根据画布尺寸自适应调整比例尺
            double canvasWidth = animationCanva.ActualWidth;
            double canvasHeight = animationCanva.ActualHeight;
            double maxDimension = Math.Max(canvasWidth, canvasHeight);

            // 假设地月系统的最大范围为 800,000,000 米（地月距离的两倍）
            double systemMaxRange = 800000000;

            // 根据画布最大尺寸和系统最大范围计算比例尺
            _zoomScale = maxDimension / systemMaxRange;

            // 限制缩放范围
            // _zoomScale = Math.Max(0.00000001, Math.Min(1.0, _zoomScale));
        }

        private void UpdateButtonStates()
        {
            // 在仿真运行时禁用修改按钮
            AddBodyBtn.IsEnabled = !_isSimulationRunning;
            RemoveBodyBtn.IsEnabled = !_isSimulationRunning;
            TimeStepSettingBtn.IsEnabled = !_isSimulationRunning;
            ResetSimulationBtn.IsEnabled = !_isSimulationRunning;
            VelocitySettingModeBtn.IsEnabled = !_isSimulationRunning;

            // 更新开始/暂停按钮文本
            var startPauseBtn = StartPauseBtn.Content as StackPanel;
            if (startPauseBtn != null)
            {
                var icon = startPauseBtn.Children[0] as FontAwesome.WPF.ImageAwesome;
                var text = startPauseBtn.Children[1] as TextBlock;
                if (_isSimulationRunning)
                {
                    icon.Icon = FontAwesome.WPF.FontAwesomeIcon.Pause;
                    text.Text = "暂停仿真";
                }
                else
                {
                    icon.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
                    text.Text = "开始仿真";
                }
            }
        }

        private void StartPauseBtn_Click(object sender, RoutedEventArgs e)
        {
            _isSimulationRunning = !_isSimulationRunning;
            if (_isSimulationRunning)
            {
                _simulationTimer.Start();
            }
            else
            {
                _simulationTimer.Stop();
            }
            UpdateButtonStates();
        }

        private void VelocitySettingModeBtn_Click(object sender, RoutedEventArgs e)
        {
            _isVelocitySettingMode = !_isVelocitySettingMode;
            // TODO: 实现速度设置模式的具体功能
        }

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (_isSimulationRunning)
            {
                _physicsEngine.Update(_timeStep);
                UpdateDisplayPositions();
                animationCanva.InvalidateVisual();
            }
        }

        private void UpdateDisplayPositions()
        {
            _canvasCenter = new Vector2D(animationCanva.ActualWidth / 2, animationCanva.ActualHeight / 2);

            foreach (var body in _physicsEngine.Bodies)
            {
                if (body.IsCenter)
                {
                    body.DisplayPosition = _canvasCenter;
                    continue;
                }
                // 以中心天体为原点，将物理位置转换为画布位置
                body.DisplayPosition = (body.Position / _pixelToDistanceRatio) + _canvasCenter;
            }
        }

        private void UpdateUI()
        {
            // 更新天体数量显示
            var bodyCountText = this.FindName("BodyCountText") as TextBlock;
            if (bodyCountText != null)
            {
                bodyCountText.Text = $"天体总量 {_physicsEngine.Bodies.Count} 个";
            }

            // 更新其他UI元素
            // TODO: 实现其他UI更新
        }

        private void SimulationTimeStepSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _timeScale = e.NewValue;
        }

        private void AnimationCanva_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 根据滚轮方向调整缩放比例
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            _zoomScale *= zoomFactor;
            
            // 限制缩放范围
            _zoomScale = Math.Max(0.00000001, Math.Min(1.0, _zoomScale));
            
            // 强制重绘
            animationCanva.InvalidateVisual();
        }

        private Point? _lastMousePosition;
        private void AnimationCanva_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_focusedBody == null)
            {
                _lastMousePosition = e.GetPosition(animationCanva);
                animationCanva.CaptureMouse();
            }
        }

        private void AnimationCanva_MouseMove(object sender, MouseEventArgs e)
        {
            if (_lastMousePosition.HasValue && e.MiddleButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(animationCanva);
                var delta = currentPosition - _lastMousePosition.Value;
                
                // 更新相机偏移
                _cameraOffset = new Vector2D(
                    _cameraOffset.X + delta.X / _zoomScale,
                    _cameraOffset.Y + delta.Y / _zoomScale
                );
                
                _lastMousePosition = currentPosition;
            }
        }

        private void AnimationCanva_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAddingBody && !_isSimulationRunning)
            {
                try
                {
                    var position = e.GetPosition(animationCanva);
                    var worldPosition = new Vector2D(
                        (position.X - animationCanva.ActualWidth / 2) / _zoomScale + _cameraOffset.X,
                        (position.Y - animationCanva.ActualHeight / 2) / _zoomScale + _cameraOffset.Y
                    );

                    // 创建新天体
                    var newBody = new Body(
                        $"Body_{_physicsEngine.Bodies.Count + 1}",
                        worldPosition,
                        new Vector2D(0, 0), // 初始速度
                        1.0, // 默认质量
                        10, // 默认半径
                        false,
                        System.Drawing.Color.Red // 红色
                    );

                    _physicsEngine.AddBody(newBody);
                    _isAddingBody = false;
                    
                    // 强制重绘
                    animationCanva.InvalidateVisual();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"添加天体时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    _isAddingBody = false;
                }
            }
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            var info = e.Info;

            // 清空画布并设置黑色背景
            canvas.Clear(SKColors.Black);

            // 使用 Renderer 进行绘制
            _renderer.RenderBodies(_physicsEngine.Bodies, canvas);
            
            if (_showTrajectory)
            {
                _renderer.RenderTrajectory(_physicsEngine.Bodies, canvas);
            }
            
            _renderer.RenderText(_physicsEngine.Bodies, canvas);
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        #region 拖动窗口

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 以下这一行代码是为了将窗口的消息处理交给我们自定义的函数WindowProc
            IntPtr handle = (new WindowInteropHelper(this)).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }


        // 用于处理窗口消息的常量
        // 你不需要关心这些常量的具体含义
        private const int WM_NCHITTEST = 0x0084;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;


        // 以下这一函数接管了Windows系统的窗口消息处理，手动实现了窗口的拖动和缩放监控
        // 你不需要关心这个函数的实现细节
        // 所以请不要展开它的函数体
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                const int resizeBorderThickness = 16;

                int x = lParam.ToInt32() & 0xFFFF;
                int y = (lParam.ToInt32() >> 16) & 0xFFFF;

                Point pos = PointFromScreen(new Point(x, y));
                double width = ActualWidth;
                double height = ActualHeight;

                if (pos.X <= resizeBorderThickness && pos.Y <= resizeBorderThickness)
                {
                    handled = true;
                    return (IntPtr)HTTOPLEFT;
                }
                else if (pos.X >= width - resizeBorderThickness && pos.Y <= resizeBorderThickness)
                {
                    handled = true;
                    return (IntPtr)HTTOPRIGHT;
                }
                else if (pos.X <= resizeBorderThickness && pos.Y >= height - resizeBorderThickness)
                {
                    handled = true;
                    return (IntPtr)HTBOTTOMLEFT;
                }
                else if (pos.X >= width - resizeBorderThickness && pos.Y >= height - resizeBorderThickness)
                {
                    handled = true;
                    return (IntPtr)HTBOTTOMRIGHT;
                }
                else if (pos.Y <= resizeBorderThickness)
                {
                    handled = true;
                    return (IntPtr)HTTOP;
                }
                else if (pos.Y >= height - resizeBorderThickness)
                {
                    handled = true;
                    return (IntPtr)HTBOTTOM;
                }
                else if (pos.X <= resizeBorderThickness)
                {
                    handled = true;
                    return (IntPtr)HTLEFT;
                }
                else if (pos.X >= width - resizeBorderThickness)
                {
                    handled = true;
                    return (IntPtr)HTRIGHT;
                }

            }
            return IntPtr.Zero;
        }

        #endregion

        private void ExitSimulationBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TimeStepSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            /*
            // 创建时间步长设置窗口
            var timeStepWindow = new TimeStepSettingWindow();
            if (timeStepWindow.ShowDialog() == true)
            {
                // 更新滑块范围
                SimulationTimeStepSlider.Minimum = timeStepWindow.MinValue;
                SimulationTimeStepSlider.Maximum = timeStepWindow.MaxValue;
            }
            */
        }

        private void ResetSimulationBtn_Click(object sender, RoutedEventArgs e)
        {
            // 重置所有状态
            _physicsEngine.Bodies.Clear();
            _cameraOffset = Vector2D.ZeroVector;
            _zoomScale = 1.0;
            _focusedBody = null;
            _isTimeReversed = false;
            _showTrajectory = false;
            
            // 重置UI
            SimulationTimeStepSlider.Value = 1.0;
            VelocityIOTextBox.Text = "0";
            MassIOTextBox.Text = "1";
            PositionIOTextBox.Text = "0,0";
            FocusIOTextBox.SelectedIndex = -1;
        }

        private void FocusResetBtn_Click(object sender, RoutedEventArgs e)
        {
            _focusedBody = null;
            _cameraOffset = Vector2D.ZeroVector;
            _zoomScale = 1.0;
        }

        private void AddBodyBtn_Click(object sender, RoutedEventArgs e)
        {
            _isAddingBody = true;
            // 可以在这里添加视觉提示，表明正在添加天体
        }

        private void RemoveBodyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (FocusIOTextBox.SelectedItem != null)
            {
                var selectedBody = _physicsEngine.Bodies.FirstOrDefault(b => b.Name == FocusIOTextBox.SelectedItem.ToString());
                if (selectedBody != null)
                {
                    _physicsEngine.Bodies.Remove(selectedBody);
                    if (_focusedBody == selectedBody)
                    {
                        _focusedBody = null;
                    }
                }
            }
        }

        private void TimeReverseBtn_Click(object sender, RoutedEventArgs e)
        {
            _isTimeReversed = !_isTimeReversed;
        }

        private void TrajectoryModeBtn_Click(object sender, RoutedEventArgs e)
        {
            _showTrajectory = !_showTrajectory;
        }

        private void LoadPresetBtn_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现预设加载功能
        }

        private void ExportPresetBtn_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现预设导出功能
        }
    }
}
