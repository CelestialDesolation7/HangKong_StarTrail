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
using System.Windows.Controls.Primitives;
using System.Threading;
using System.Data.SQLite;
using System.IO;

namespace HangKong_StarTrail.Views
{
    public partial class GravitySimulationForm : Window
    {

        #region 成员变量
        #region 渲染相关控制器
        private Renderer _renderer; // 渲染器，包含物理引擎作为成员
        private DispatcherTimer _simulationTimer;   // 物理仿真计时器
        private DispatcherTimer _uiUpdateTimer;     // UI更新计时器
        private Task? _physicsUpdateTask;          // 物理更新任务
        private CancellationTokenSource? _physicsUpdateCts; // 物理更新取消令牌
        private readonly object _physicsLock = new object(); // 物理更新锁
        private readonly object _renderLock = new object();  // 渲染锁
        private int _frameCount;                    // 帧计数器
        private DateTime _lastFrameTime;            // 上一帧时间
        private bool _isPhysicsUpdatePending;      // 是否有待处理的物理更新
        private DateTime _lastPhysicsUpdate;        // 上次物理更新时间
        private const int MIN_PHYSICS_UPDATE_INTERVAL = 1; // 最小物理更新间隔(ms)
        private bool _isRenderingEnabled = true;    // 是否启用渲染
        #endregion

        #region 参数设置成员
        private double _timeStep; // 每帧物理仿真时间步长
        private double _baseTimeStep;
        private bool _isSimulationRunning = true; // 是否正在仿真
        private bool _isTimeReversed = false;   // 是否反向仿真
        private bool _isAddingBody = false;
        private bool _isVelocityVisualizeMode = false;
        #endregion

        #region 画布相关参数成员
        private double _recommendedScale; // 推荐比例尺
        private double _zoomFactor = 1.0; // 缩放因子，初始为1
        private double _pixelToDistanceRatio; // 实际使用的比例尺 = 推荐比例尺 * 缩放因子
        private Vector2D _canvasCenter; // 画布中心点
        private Vector2D _cameraOffset = Vector2D.ZeroVector;   // 相机偏移量
        private bool _isFrameRendering = false; // 当前帧是否正在渲染
        private Body? _focusedBody = null;  // 当前聚焦的天体
        private Vector2D? _dragStartPosition; // 记录拖动开始时的物理位置
        private Vector2D? _lastMousePosition; // 记录上一帧的鼠标位置（屏幕坐标）
        private bool _isMiddleButtonPressed;    // 中键是否按下
        #endregion

        #region 调试信息成员
        private DebugForm.DebugSimulationForm _debugWindow; // 调试窗口
        private string[] _debugInfo = new string[10]; // 用于存储调试信息的字符串
        private long[] _universalCounter = new long[10]; // 用于存储全局计数器
        #endregion

        #region 常量
        private const double DESIRED_PIXEL_MOVEMENT = 5.0; // 期望每帧移动的像素数
        private const double SLIDER_CENTER_VALUE = 1.0; // 滑块的中心值
        private const double SLIDER_MIN_VALUE = 0.0; // 滑块的最小值
        private const double SLIDER_MAX_VALUE = 2.0; // 滑块的最大值
        private const int TARGET_FPS = 120;         // 目标帧率
        private const int FRAME_TIME_MS = 1000 / TARGET_FPS; // 每帧预期时间(ms)
        #endregion
        #endregion


        #region 初始化函数
        public GravitySimulationForm()
        {
            InitializeComponent();
            InitializeSimulation();
            InitializeDebugWindow();

            // 等待控件完成布局后再计算推荐比例尺
            animationCanva.Loaded += (s, e) =>
            {
                _zoomFactor = 1.0;
                _pixelToDistanceRatio = _recommendedScale  = CalculateRecommendedScale();
                _baseTimeStep = _timeStep = CalculateRecommendedTimeStep();
                UpdateDisplayPositions();
                animationCanva.InvalidateVisual();
                // 计算并设置推荐时间步长
                _renderer.SetTimeStep(_timeStep);
                _renderer.SetPixelToDistanceRatio(_pixelToDistanceRatio);
            };

            var centerBody = _renderer._physicsEngine.Bodies.FirstOrDefault(b => b.IsCenter);
            if (centerBody != null)
            {
                FocusIOTextBox.SelectedItem = centerBody.Name;
                _focusedBody = centerBody;
            }
            else
            {
                // 如果没有中心天体，设置为自由模式
                FocusIOTextBox.SelectedItem = "自由模式";
                _focusedBody = null;
            }
        }

        private void InitializeSimulation()
        {
            // 初始化渲染器
            _renderer = new Renderer(new PhysicsEngine());

            // 初始化物理仿真计时器
            _simulationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(FRAME_TIME_MS)
            };
            _simulationTimer.Tick += SimulationTimer_Tick;

            // 初始化UI更新计时器
            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // 100ms更新一次UI
            };
            _uiUpdateTimer.Tick += UpdateAllUIMonitor;

            // 使用CompositionTarget.Rendering替代DispatcherTimer进行渲染
            CompositionTarget.Rendering += CompositionTarget_Rendering;


            // 初始化时间步长滑块
            SimulationTimeStepSlider.Minimum = SLIDER_MIN_VALUE;
            SimulationTimeStepSlider.Maximum = SLIDER_MAX_VALUE;
            SimulationTimeStepSlider.Value = SLIDER_CENTER_VALUE;

            // 初始化按钮状态
            UpdateButtonStates();

            // 加载默认地月系数据
            LoadDefaultEarthMoonSystem();

            // 初始化帧率统计
            _frameCount = 0;
            _lastFrameTime = DateTime.Now;
            _lastPhysicsUpdate = DateTime.Now;
            _isPhysicsUpdatePending = false;
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
                new SKColor(0, 0, 255) // 蓝色
            );

            // 月球数据
            var moon = new Body(
                "月球",
                new Vector2D(384400e3, 0),  // 地月距离（米）
                new Vector2D(0, 1022),      // 月球轨道速度（m/s）
                7.348e22,                   // 质量（kg）
                10,                         // 显示半径
                false,                      // 非中心天体
                new SKColor(128, 128, 128)  // 灰色
            );

            _renderer.AddBody(earth);
            _renderer.AddBody(moon);

            _focusedBody = earth;
            // 更新焦点下拉框
            UpdateFocusComboBox();
        }
        #endregion


        #region 调试函数

        private void InitializeDebugWindow()
        {
            _debugWindow = new DebugForm.DebugSimulationForm(UpdateDebugInfo);
            _debugWindow.Show();
        }

        private void UpdateDebugInfo(string currentText)
        {
            var centerBody = _renderer._physicsEngine.Bodies.FirstOrDefault(b => b.IsCenter);
            var debugInfo = new System.Text.StringBuilder();

            debugInfo.AppendLine($"时间步长: {_timeStep:F2} 秒");
            debugInfo.AppendLine($"基准步长: {_baseTimeStep:F2} 秒");
            debugInfo.AppendLine($"推荐比例尺: {_recommendedScale:F6}");
            debugInfo.AppendLine($"缩放因子: {_zoomFactor:F6}");
            debugInfo.AppendLine($"实际比例尺: {_pixelToDistanceRatio:F6}");
            debugInfo.AppendLine($"相机偏移(物理坐标): X={_cameraOffset.X:F2}, Y={_cameraOffset.Y:F2}");
            debugInfo.AppendLine($"画布中心: X={_canvasCenter.X:F2}, Y={_canvasCenter.Y:F2}");

            if (_dragStartPosition.HasValue)
            {
                debugInfo.AppendLine($"拖动起始位置(物理坐标): X={_dragStartPosition.Value.X:F2}, Y={_dragStartPosition.Value.Y:F2}");
            }

            if (centerBody != null)
            {
                debugInfo.AppendLine($"\n中心天体 ({centerBody.Name}):");
                debugInfo.AppendLine($"物理位置: X={centerBody.Position.X:F2}, Y={centerBody.Position.Y:F2}");
                debugInfo.AppendLine($"显示位置: X={centerBody.DisplayPosition.X:F2}, Y={centerBody.DisplayPosition.Y:F2}");
            }

            foreach (var body in _renderer._physicsEngine.Bodies.Where(b => !b.IsCenter))
            {
                debugInfo.AppendLine($"\n{body.Name}:");
                debugInfo.AppendLine($"物理位置: X={body.Position.X:F2}, Y={body.Position.Y:F2}");
                debugInfo.AppendLine($"显示位置: X={body.DisplayPosition.X:F2}, Y={body.DisplayPosition.Y:F2}");
                debugInfo.AppendLine($"速度: X={body.Velocity.X:F2}, Y={body.Velocity.Y:F2}");
            }

            foreach (var info in _debugInfo)
            {
                if (info == null) continue;
                debugInfo.AppendLine(info);
            }
            _debugWindow.DebugTextBox.Text = debugInfo.ToString();
        }

        #endregion


        #region 基本UI响应
        private void UpdateAllUIMonitor(object sender, EventArgs e)
        {
            // 帧数汇报
            var now = DateTime.Now;
            var elapsed = (now - _lastFrameTime).TotalSeconds;
            if (elapsed >= 1.0)
            {
                var fps = _frameCount / elapsed;
                FrameReportTextBlock.Text = $"{fps:F1}";
                _frameCount = 0;
                _lastFrameTime = now;
            }
            // 汇报观测对象属性
            if (_focusedBody != null && _isSimulationRunning)
            {
                if (_focusedBody != null)
                {
                    // 更新位置
                    PositionIOTextBox.Text = $"({FormatDistance(_focusedBody.Position.X)},{FormatDistance(_focusedBody.Position.Y)})";
                    // 更新速度（向量形式）
                    VelocityIOTextBox.Text = $"({FormatVelocity(_focusedBody.Velocity.X)},{FormatVelocity(_focusedBody.Velocity.Y)})";
                    // 更新质量
                    MassIOTextBox.Text = FormatMass(_focusedBody.Mass);
                }
            }
            else
            {
                PositionIOTextBox.Text = "N/A";
                VelocityIOTextBox.Text = "N/A";
                MassIOTextBox.Text = "N/A";
            }
            // 汇报渲染状态
            BodyCountReportLabel.Text = $" {_renderer._physicsEngine.Bodies.Count} ";
            if (_renderer._physicsEngine.timeElapsed <= 3600 * 24)
            {
                SimulationTimeReportLabel.Text = $" {_renderer._physicsEngine.timeElapsed:F2} ";
                SimulationTimeUnitLabel.Text = "秒";
            }
            else if (_renderer._physicsEngine.timeElapsed <= 3600 * 24 * 30)
            {
                SimulationTimeReportLabel.Text = $" {_renderer._physicsEngine.timeElapsed / (3600 * 24):F2} ";
                SimulationTimeUnitLabel.Text = "天";
            }
            else if (_renderer._physicsEngine.timeElapsed < 3600 * 24 * 30 * 12)
            {
                SimulationTimeReportLabel.Text = $" {_renderer._physicsEngine.timeElapsed / (3600 * 24 * 30):F2} ";
                SimulationTimeUnitLabel.Text = "月";
            }
            else
            {
                SimulationTimeReportLabel.Text = $" {_renderer._physicsEngine.timeElapsed / (3600 * 24 * 30 * 12):F2} ";
                SimulationTimeUnitLabel.Text = "年";
            }
            ScaleReportLabel.Text = NumberFormat(1 / _pixelToDistanceRatio) + " m";

        }

        private void UpdateButtonStates()
        {
            // 在仿真运行时禁用部分修改相关按钮

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

        private void ExitSimulationBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void StartPauseBtn_Click(object sender, RoutedEventArgs e)
        {
            _isSimulationRunning = !_isSimulationRunning;

            if (_isSimulationRunning)
            {
                // 启动仿真
                _physicsUpdateCts = new CancellationTokenSource();
                _simulationTimer.Start();
                _uiUpdateTimer.Start();
                _isRenderingEnabled = true;
            }
            else
            {
                // 停止仿真
                _physicsUpdateCts?.Cancel();
                _simulationTimer.Stop();
                _uiUpdateTimer.Stop();
                _isRenderingEnabled = false;
            }

            UpdateButtonStates();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // 确保鼠标左键按下时才能拖动窗口
                if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
                {
                    // 确保窗口处于正常状态
                    if (this.WindowState == WindowState.Normal || this.WindowState == WindowState.Maximized)
                    {
                        this.DragMove();
                    }
                    // 标记事件已处理，防止冒泡
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不影响程序运行
                Console.WriteLine($"窗口拖动时出错: {ex.Message}");
            }
        }

        private void SimulationTimeStepSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue != 0)
            {
                // 计算新的时间步长
                double ratio = Math.Pow(2, e.NewValue - SLIDER_CENTER_VALUE); // 使用更小的底数来降低灵敏度
                // 实时应用新的时间步长,但暂时不更新
                _timeStep = _baseTimeStep * ratio;
                _renderer.SetTimeStep(_timeStep);
                _debugInfo[1] = $"当前滑块值：{e.NewValue}";
            }
        }

        private void SimulationTimeStepSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _baseTimeStep = _timeStep;
            SimulationTimeStepSlider.Value = SLIDER_CENTER_VALUE;
        }

        private void UpdateFocusComboBox()
        {
            // 保存当前选中的项
            var currentSelection = FocusIOTextBox.SelectedItem?.ToString();
            // 清空下拉框
            FocusIOTextBox.Items.Clear();
            // 添加自由模式选项
            FocusIOTextBox.Items.Add("自由模式");
            // 添加所有天体
            foreach (var body in _renderer._physicsEngine.Bodies)
            {
                FocusIOTextBox.Items.Add(body.Name);
            }
            // 恢复选中项
            if (!string.IsNullOrEmpty(currentSelection))
            {
                FocusIOTextBox.SelectedItem = currentSelection;
            }
        }

        private void FocusIOTextBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = FocusIOTextBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedItem)) return;

            if (selectedItem == "自由模式")
            {
                // 如果当前有焦点天体，将其位置设为相机偏移
                if (_focusedBody != null)
                {
                    _cameraOffset = _focusedBody.Position;
                    _focusedBody = null;
                }
                // 清空UI数据显示
                PositionIOTextBox.Text = "N/A";
                VelocityIOTextBox.Text = "N/A";
                MassIOTextBox.Text = "N/A";
            }
            else
            {
                // 查找选中的天体
                _focusedBody = _renderer._physicsEngine.Bodies.FirstOrDefault(b => b.Name == selectedItem);
            }

            // 只在暂停模式下更新显示
            if (!_isSimulationRunning)
            {
                UpdateDisplayPositions();
                animationCanva.InvalidateVisual();
            }
        }

        private void AnimationCanva_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var source = PresentationSource.FromVisual(animationCanva);
            var matrix = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
            double pixelWidth = animationCanva.ActualWidth * matrix.M11;
            double pixelHeight = animationCanva.ActualHeight * matrix.M22;
            _canvasCenter = new Vector2D(pixelWidth / 2, pixelHeight / 2);
        }
        protected override void OnClosed(EventArgs e)
        {
            // 停止所有计时器和任务
            _physicsUpdateCts?.Cancel();
            _simulationTimer?.Stop();
            _uiUpdateTimer?.Stop();
            CompositionTarget.Rendering -= CompositionTarget_Rendering;

            // 等待物理更新任务完成
            if (_physicsUpdateTask != null && !_physicsUpdateTask.IsCompleted)
            {
                try
                {
                    _physicsUpdateTask.Wait(1000); // 等待最多1秒
                }
                catch (AggregateException)
                {
                    // 忽略取消异常
                }
            }

            _debugWindow?.Close();
            base.OnClosed(e);
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

        #endregion





        #region 画面渲染相关操作
        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (!_isSimulationRunning) return;

            var now = DateTime.Now;
            var timeSinceLastUpdate = (now - _lastPhysicsUpdate).TotalMilliseconds;

            // 如果距离上次物理更新时间太短，跳过这次更新
            if (timeSinceLastUpdate < MIN_PHYSICS_UPDATE_INTERVAL)
            {
                return;
            }

            // 如果上一个物理更新任务还在运行，标记为待处理并返回
            if (_physicsUpdateTask != null && !_physicsUpdateTask.IsCompleted)
            {
                _isPhysicsUpdatePending = true;
                return;
            }

            try
            {
                // 创建新的物理更新任务
                _physicsUpdateTask = Task.Run(() =>
                {
                    lock (_physicsLock)
                    {
                        if (_physicsUpdateCts?.Token.IsCancellationRequested == true)
                            return;

                        _renderer._physicsEngine.Update(_timeStep);
                        UpdateDisplayPositions();
                        _lastPhysicsUpdate = DateTime.Now;
                        _isPhysicsUpdatePending = false;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"物理更新出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PositionIOTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isSimulationRunning && _focusedBody != null)
            {
                var originalText = PositionIOTextBox.Text;
                try
                {
                    var parts = PositionIOTextBox.Text.Trim('(', ')').Split(',');
                    if (parts.Length == 2)
                    {
                        double x = ParseScientificNumber(parts[0].Substring(0, parts[0].Length - 2));
                        double y = ParseScientificNumber(parts[1].Substring(0, parts[1].Length - 2));
                        _focusedBody.Position = new Vector2D(x, y);
                        UpdateDisplayPositions();
                        animationCanva.InvalidateVisual();

                        // 更新显示
                        PositionIOTextBox.Text = $"({FormatDistance(x)},{FormatDistance(y)})";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无效的位置格式：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    // 恢复原始文本
                    PositionIOTextBox.Text = originalText;
                }
            }
        }

        private void VelocityIOTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isSimulationRunning && _focusedBody != null)
            {
                var originalText = VelocityIOTextBox.Text;
                _debugInfo[4] = $"原始文本：{originalText}";
                try
                {
                    var parts = VelocityIOTextBox.Text.Trim('(', ')').Split(',');
                    if (parts.Length == 2)
                    {
                        _debugInfo[3] = parts[0];
                        _debugInfo[2] = parts[1];
                        double vx = ParseScientificNumber(parts[0].Substring(0, parts[0].Length - 4));
                        double vy = ParseScientificNumber(parts[1].Substring(0, parts[1].Length - 4));
                        _focusedBody.Velocity = new Vector2D(vx, vy);

                        // 更新显示
                        VelocityIOTextBox.Text = $"({FormatVelocity(vx)},{FormatVelocity(vy)})";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无效的速度格式：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    // 恢复原始文本
                    VelocityIOTextBox.Text = originalText;
                }
            }
        }

        private void MassIOTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isSimulationRunning && _focusedBody != null)
            {
                var originalText = MassIOTextBox.Text;
                try
                {
                    double mass = ParseScientificNumber(MassIOTextBox.Text.Substring(0, MassIOTextBox.Text.Length - 3));
                    _focusedBody.Mass = mass;

                    // 更新显示
                    MassIOTextBox.Text = FormatMass(mass);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无效的质量格式：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    // 恢复原始文本
                    MassIOTextBox.Text = originalText;
                }
            }
        }

        private void UpdateDisplayPositions()
        {
            _debugInfo[5] = "进入 UpdateDisplayPositions";
            if (_focusedBody != null)
            {
                // 如果有焦点天体，将其置于画布中心
                _focusedBody.DisplayPosition = _canvasCenter;

                // 其他天体的位置相对于焦点天体计算
                foreach (var body in _renderer._physicsEngine.Bodies.Where(b => b != _focusedBody))
                {
                    var relativePosition = body.Position - _focusedBody.Position;
                    body.DisplayPosition = PhysicalToScreenPosition(relativePosition);
                }
            }
            else
            {
                // 如果没有焦点天体，所有天体都相对于画布中心计算，并考虑相机偏移
                foreach (var body in _renderer._physicsEngine.Bodies)
                {
                    var relativePosition = body.Position - _cameraOffset;
                    body.DisplayPosition = PhysicalToScreenPosition(relativePosition);
                }
            }
            _universalCounter[1]++;
            _debugInfo[1] = $"UpdateDisplayPositions调用次数{_universalCounter[1]}";
            _debugInfo[5] = "完成 UpdateDisplayPositions";
        }

        private void AnimationCanva_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _debugInfo[5] = "进入 AnimationCanva_MouseWheel";
            // 根据滚轮方向调整缩放因子
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            _zoomFactor *= zoomFactor;

            // 限制缩放因子范围
            _zoomFactor = Math.Max(0.1, Math.Min(10.0, _zoomFactor));

            // 更新实际使用的比例尺
            _pixelToDistanceRatio = _recommendedScale * _zoomFactor;
            _renderer.SetPixelToDistanceRatio(_pixelToDistanceRatio);

            // 只在暂停模式下更新显示
            if (!_isSimulationRunning)
            {
                UpdateDisplayPositions();
                animationCanva.InvalidateVisual();
            }
            _debugInfo[5] = "完成 AnimationCanva_MouseWheel";
        }

        private void AnimationCanva_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                if (_focusedBody == null) // 只在没有焦点天体时允许拖动
                {
                    _isMiddleButtonPressed = true;
                    var mousePos = e.GetPosition(animationCanva);
                    _lastMousePosition = new Vector2D(mousePos.X, mousePos.Y);

                    // 将鼠标位置转换为物理坐标
                    _dragStartPosition = ScreenToRelativePhysicalPosition(_lastMousePosition.Value);
                    animationCanva.CaptureMouse();
                }
            }
        }

        private void AnimationCanva_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMiddleButtonPressed && _lastMousePosition.HasValue && _dragStartPosition.HasValue)
            {
                _debugInfo[5] = "进入 AnimationCanva_MouseMove";
                var currentMousePos = e.GetPosition(animationCanva);
                var currentMouseVector = new Vector2D(currentMousePos.X, currentMousePos.Y);

                // 计算鼠标移动的物理位移
                var currentPhysicalPos = ScreenToRelativePhysicalPosition(currentMouseVector);
                var physicalDelta = currentPhysicalPos - _dragStartPosition.Value;
                var pixelDelta = currentMouseVector - _lastMousePosition.Value;
                _renderer.UpdateStarOffset((float)pixelDelta.X, (float)pixelDelta.Y);
                // 更新相机偏移
                _cameraOffset -= physicalDelta;

                // 更新拖动起始位置为当前位置
                _dragStartPosition = currentPhysicalPos;
                _lastMousePosition = currentMouseVector;

                // 只在暂停模式下更新显示
                if (!_isSimulationRunning)
                {
                    UpdateDisplayPositions();
                    animationCanva.InvalidateVisual();
                }
                _debugInfo[5] = "完成 AnimationCanva_MouseMove";
            }
        }

        private void AnimationCanva_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released)
            {
                _isMiddleButtonPressed = false;
                _lastMousePosition = null;
                _dragStartPosition = null;
                animationCanva.ReleaseMouseCapture();
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
                        (position.X - animationCanva.ActualWidth / 2) / _pixelToDistanceRatio + _cameraOffset.X,
                        (position.Y - animationCanva.ActualHeight / 2) / _pixelToDistanceRatio + _cameraOffset.Y
                    );

                    // 创建新天体
                    var newBody = new Body(
                        $"Body_{_renderer._physicsEngine.Bodies.Count + 1}",
                        worldPosition,
                        new Vector2D(0, 0), // 初始速度
                        1.0, // 默认质量
                        10, // 默认半径
                        false,
                        new SKColor(255, 0, 0) // 红色
                    );

                    _renderer.AddBody(newBody);
                    _isAddingBody = false;

                    // 强制重绘
                    animationCanva.InvalidateVisual();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"添加天体时发生错误：{ex.Message}", "错误",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    _isAddingBody = false;
                }
            }
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;

            // 渲染星空背景
            _renderer.RenderBackground(canvas);
            // 渲染天体
            _renderer.RenderBodies(canvas);
            // 渲染速度箭头
            if (_isVelocityVisualizeMode)
            {
                _renderer.RenderVelocityArrow(canvas);
            }
            _frameCount++;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (!_isRenderingEnabled) return;

            lock (_renderLock)
            {
                if (!_isFrameRendering)
                {
                    animationCanva.InvalidateVisual();
                }
            }
        }
        #endregion





        #region 待实现功能
        private void VelocityVisualizeModeBtn_Click(object sender, RoutedEventArgs e)
        {
            _isVelocityVisualizeMode = !_isVelocityVisualizeMode;
            // TODO: 实现速度设置模式的具体功能
        }

        private void ResetSimulationBtn_Click(object sender, RoutedEventArgs e)
        {
            // 重置所有状态
            _renderer._physicsEngine.Bodies.Clear();
            _cameraOffset = Vector2D.ZeroVector;
            _focusedBody = null;
            _isTimeReversed = false;

            // 重置UI
            SimulationTimeStepSlider.Value = SLIDER_CENTER_VALUE;
            VelocityIOTextBox.Text = "";
            MassIOTextBox.Text = "";
            PositionIOTextBox.Text = "";
            FocusIOTextBox.SelectedIndex = -1;

            LoadDefaultEarthMoonSystem();
        }

        private void FocusResetBtn_Click(object sender, RoutedEventArgs e)
        {
            var centerBody = _renderer._physicsEngine.Bodies.Where(b => b.IsCenter);
            if (centerBody.Any())
            {
                _cameraOffset = Vector2D.ZeroVector;
                _focusedBody = centerBody.First();
                FocusIOTextBox.SelectedItem = $"{centerBody.First().Name}";
            }
            else
            {
                // 如果没有中心天体，设置为自由模式
                FocusIOTextBox.SelectedItem = "自由模式";
                _cameraOffset = Vector2D.ZeroVector;
                _focusedBody = null;
            }
        }

        private void TimeReverseBtn_Click(object sender, RoutedEventArgs e)
        {
            _isTimeReversed = !_isTimeReversed;
        }


        private void ImportSceneButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSimulationRunning)
            {
                StartPauseBtn_Click(sender, e);
            }

            var sceneWindow = new SceneSelectionWindow();
            if (sceneWindow.ShowDialog() == true)
            {

                try
                {
                    string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\scene.db");
                    if (!File.Exists(dbPath))
                    {
                        MessageBox.Show("未找到预设场景数据库", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                    {
                        connection.Open();
                        // 获取场景ID
                        int sceneId;
                        using (var command = new SQLiteCommand("SELECT SceneId FROM Scenes WHERE SceneName = @SceneName", connection))
                        {
                            command.Parameters.AddWithValue("@SceneName", sceneWindow.SelectedSceneName);
                            sceneId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        // 获取场景中的所有天体
                        var bodies = new List<Body>();
                        using (var command = new SQLiteCommand(
                            "SELECT Name, PositionX, PositionY, VelocityX, VelocityY, Mass, IsCenter, " +
                            "ColorR, ColorG, ColorB, ColorA, DisplayRadius " +
                            "FROM Bodies WHERE SceneId = @SceneId", connection))
                        {
                            command.Parameters.AddWithValue("@SceneId", sceneId);
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var body = new Body(
                                        reader.GetString(0), // Name
                                        new Vector2D(reader.GetDouble(1), reader.GetDouble(2)), // Position
                                        new Vector2D(reader.GetDouble(3), reader.GetDouble(4)), // Velocity
                                        reader.GetDouble(5), // Mass
                                        Convert.ToInt32(reader.GetDouble(11)), // DisplayRadius
                                        reader.GetInt32(6) != 0, // IsCenter
                                        new SKColor(
                                            (byte)reader.GetInt32(7),  // R
                                            (byte)reader.GetInt32(8),  // G
                                            (byte)reader.GetInt32(9),  // B
                                            (byte)reader.GetInt32(10)  // A
                                        )
                                    );
                                    bodies.Add(body);
                                }
                            }
                        }

                        // 更新物理引擎中的天体
                        _renderer._physicsEngine.Bodies.Clear();
                        foreach (var body in bodies)
                        {
                            _renderer.AddBody(body);
                        }

                        _timeStep = CalculateRecommendedTimeStep();
                        _pixelToDistanceRatio = _recommendedScale = CalculateRecommendedScale();
                        
                        // 更新天体列表
                        UpdateFocusComboBox();
                        UpdateDisplayPositions();
                        // 重绘画布
                        animationCanva.InvalidateVisual();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入预设场景时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportSceneButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSimulationRunning)
            {
                StartPauseBtn_Click(sender, e);
            }

            var sceneName = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入场景名称：",
                "导出场景",
                "新场景");

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            try
            {
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\scene.db");
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("未找到预设场景数据库", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 插入场景
                            int sceneId;
                            using (var command = new SQLiteCommand(
                                "INSERT INTO Scenes (SceneName, CreateTime) VALUES (@SceneName, @CreateTime); " +
                                "SELECT last_insert_rowid();", connection))
                            {
                                command.Parameters.AddWithValue("@SceneName", sceneName);
                                command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                                sceneId = Convert.ToInt32(command.ExecuteScalar());
                            }

                            // 插入天体
                            foreach (var body in _renderer._physicsEngine.Bodies)
                            {
                                using (var command = new SQLiteCommand(
                                    "INSERT INTO Bodies (SceneId, Name, PositionX, PositionY, " +
                                    "VelocityX, VelocityY, Mass, IsCenter, " +
                                    "ColorR, ColorG, ColorB, ColorA, DisplayRadius) " +
                                    "VALUES (@SceneId, @Name, @PositionX, @PositionY, " +
                                    "@VelocityX, @VelocityY, @Mass, @IsCenter, " +
                                    "@ColorR, @ColorG, @ColorB, @ColorA, @DisplayRadius)", connection))
                                {
                                    command.Parameters.AddWithValue("@SceneId", sceneId);
                                    command.Parameters.AddWithValue("@Name", body.Name);
                                    command.Parameters.AddWithValue("@PositionX", body.Position.X);
                                    command.Parameters.AddWithValue("@PositionY", body.Position.Y);
                                    command.Parameters.AddWithValue("@VelocityX", body.Velocity.X);
                                    command.Parameters.AddWithValue("@VelocityY", body.Velocity.Y);
                                    command.Parameters.AddWithValue("@Mass", body.Mass);
                                    command.Parameters.AddWithValue("@IsCenter", body.IsCenter);
                                    command.Parameters.AddWithValue("@ColorR", body.DisplayColor.Red);
                                    command.Parameters.AddWithValue("@ColorG", body.DisplayColor.Green);
                                    command.Parameters.AddWithValue("@ColorB", body.DisplayColor.Blue);
                                    command.Parameters.AddWithValue("@ColorA", body.DisplayColor.Alpha);
                                    command.Parameters.AddWithValue("@DisplayRadius", body.RenderRadius);
                                    command.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            MessageBox.Show("场景导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出场景时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion


        #region 无副作用的计算函数
        // 将屏幕坐标转换为物理坐标
        private Vector2D ScreenToRelativePhysicalPosition(Vector2D screenPos)
        {
            // 将屏幕坐标转换为相对于画布中心的坐标
            var relativePos = screenPos - _canvasCenter;

            // 将相对坐标转换为物理坐标
            return relativePos / _pixelToDistanceRatio;
        }

        // 将物理坐标转换为屏幕坐标
        private Vector2D PhysicalToScreenPosition(Vector2D relativePhysicalPos)
        {
            // 将物理坐标转换为屏幕坐标
            return _canvasCenter + (relativePhysicalPos * _pixelToDistanceRatio);
        }

        // 通用数字格式化函数
        private string NumberFormat(double num, int digit = 2)
        {
            if (num == 0) return $"0×10⁰";

            // 计算数量级
            int exponent = (int)Math.Floor(Math.Log10(Math.Abs(num)));

            // 计算系数
            double coefficient = num / Math.Pow(10, exponent);

            // 如果系数大于10，调整数量级
            if (Math.Abs(coefficient) >= 10)
            {
                coefficient /= 10;
                exponent++;
            }

            // 格式化系数
            string formattedCoefficient = coefficient.ToString($"F{digit}");

            // 将指数转换为上标
            string superscript = exponent.ToString()
                .Replace("0", "⁰")
                .Replace("1", "¹")
                .Replace("2", "²")
                .Replace("3", "³")
                .Replace("4", "⁴")
                .Replace("5", "⁵")
                .Replace("6", "⁶")
                .Replace("7", "⁷")
                .Replace("8", "⁸")
                .Replace("9", "⁹")
                .Replace("-", "⁻");

            return $"{formattedCoefficient}×10{superscript}";
        }

        // 格式化距离（米）
        private string FormatDistance(double meters)
        {
            return $"{NumberFormat(meters)} m";
        }

        // 格式化速度（米/秒）
        private string FormatVelocity(double velocity)
        {
            return $"{NumberFormat(velocity)} m/s";
        }

        // 格式化质量（千克）
        private string FormatMass(double mass)
        {
            return $"{NumberFormat(mass)} kg";
        }

        // 解析科学计数法格式的数字
        private double ParseScientificNumber(string input)
        {
            input = input.Trim().ToLower();

            // 处理空输入
            if (string.IsNullOrEmpty(input)) return 0;

            // 处理普通数字
            if (double.TryParse(input, out double result))
                return result;

            // 将上标字符转换为普通数字
            string normalizedInput = input
                .Replace("⁰", "0")
                .Replace("¹", "1")
                .Replace("²", "2")
                .Replace("³", "3")
                .Replace("⁴", "4")
                .Replace("⁵", "5")
                .Replace("⁶", "6")
                .Replace("⁷", "7")
                .Replace("⁸", "8")
                .Replace("⁹", "9")
                .Replace("⁻", "-");

            // 处理科学计数法格式 (例如: 1.87×10^9 或 3.83×10⁸)
            var parts = normalizedInput.Split('×', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                // 获取系数部分
                if (!double.TryParse(parts[0], out double coefficient))
                    throw new FormatException("无效的系数格式");

                // 处理指数部分
                string expPart = parts[1];
                if (!expPart.StartsWith("10"))
                    throw new FormatException("无效的指数格式");

                // 获取指数值
                string exponentStr = expPart.Substring(2);
                if (!int.TryParse(exponentStr, out int exponent))
                    throw new FormatException("无效的指数值");

                return coefficient * Math.Pow(10, exponent);
            }

            throw new FormatException("无效的数字格式");
        }

        private double CalculateRecommendedTimeStep()
        {
            if (_renderer._physicsEngine.Bodies.Count == 0)
                return 1.0;

            // 找到最大速度的天体
            double maxVelocity = 0;
            foreach (var body in _renderer._physicsEngine.Bodies)
            {
                double velocity = body.Velocity.Length;
                maxVelocity = Math.Max(maxVelocity, velocity);
            }

            if (maxVelocity <= 0)
                return 1.0;

            // 计算推荐时间步长
            double recommendedTimeStep = DESIRED_PIXEL_MOVEMENT / (maxVelocity * _pixelToDistanceRatio);

            // 限制时间步长在合理范围内
            return Math.Max(0.1, Math.Min(100000.0, recommendedTimeStep));
        }

        private double CalculateRecommendedScale()
        {
            if (_renderer._physicsEngine.Bodies.Count < 2)
                return 1.0;

            // 找到最远距离的两个天体
            double maxDistance = 0;
            foreach (var body1 in _renderer._physicsEngine.Bodies)
            {
                foreach (var body2 in _renderer._physicsEngine.Bodies)
                {
                    if (body1 != body2)
                    {
                        double distance = (body1.Position - body2.Position).Length;
                        maxDistance = Math.Max(maxDistance, distance);
                    }
                }
            }

            // 获取画布的实际像素尺寸
            var source = PresentationSource.FromVisual(animationCanva);
            var matrix = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
            double pixelWidth = animationCanva.ActualWidth * matrix.M11;
            double pixelHeight = animationCanva.ActualHeight * matrix.M22;
            _canvasCenter.X = pixelWidth / 2;
            _canvasCenter.Y = pixelHeight / 2;
            double maxDimension = Math.Max(pixelWidth, pixelHeight);

            // 如果画布尺寸为0，返回一个默认值
            if (maxDimension <= 0)
            {
                return 1.0;
            }

            // 计算推荐比例尺，确保最远距离的天体在画布上可见
            double scale = maxDimension / (maxDistance * 3); // 1.2是边距系数
            return scale;
        }
        #endregion

    }
}
