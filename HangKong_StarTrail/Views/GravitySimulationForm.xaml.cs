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

namespace HangKong_StarTrail.Views
{
    public partial class GravitySimulationForm : Window
    {

        #region 成员变量
        #region 渲染相关控制器
        private Renderer _renderer; // 渲染器，包含物理引擎作为成员
        private DispatcherTimer _simulationTimer;   // 物理仿真计时器
        private DispatcherTimer _uiUpdateTimer;     // UI更新计时器
        private DispatcherTimer _renderTimer;       // 渲染计时器
        private Task? _physicsUpdateTask;          // 物理更新任务
        private CancellationTokenSource? _physicsUpdateCts; // 物理更新取消令牌
        private readonly object _physicsLock = new object(); // 物理更新锁
        private readonly object _renderLock = new object();  // 渲染锁
        private int _frameCount;                    // 帧计数器
        private DateTime _lastFrameTime;            // 上一帧时间
        #endregion

        #region 参数设置成员
        private double _timeStep; // 每帧物理仿真时间步长
        private double _baseTimeStep;
        private bool _showTrajectory = false; // 是否显示轨迹
        private bool _isSimulationRunning = false; // 是否正在仿真
        private bool _isTimeReversed = false;   // 是否反向仿真
        private bool _isAddingBody = false;
        private bool _isVelocitySettingMode = false;
        #endregion

        #region 画布相关参数成员
        private double _recommendedScale; // 推荐比例尺
        private double _zoomFactor = 1.0; // 缩放因子，初始为1
        private double _pixelToDistanceRatio; // 实际使用的比例尺 = 推荐比例尺 * 缩放因子
        private Vector2D _canvasCenter; // 画布中心点
        private Vector2D _cameraOffset = Vector2D.ZeroVector;   // 相机偏移量
        private double _zoomScale = 1.0;    // 缩放比例
        private bool _isFrameRendering = false; // 当前帧是否正在渲染
        private Body? _focusedBody = null;  // 当前聚焦的天体
        private Vector2D? _dragStartPosition; // 记录拖动开始时的物理位置
        private Vector2D? _lastMousePosition; // 记录上一帧的鼠标位置（屏幕坐标）
        private bool _isMiddleButtonPressed = false;    // 中键是否按下
        private bool _displayPositionUpdated = false; // 是否更新了显示位置
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
                _recommendedScale = CalculateRecommendedScale();
                _zoomFactor = 1.0;
                _pixelToDistanceRatio = _recommendedScale * _zoomFactor;
                _baseTimeStep = _timeStep = CalculateRecommendedTimeStep();
                animationCanva.InvalidateVisual();
                // 计算并设置推荐时间步长
            };
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
            _uiUpdateTimer = new DispatcherTimer();
            _uiUpdateTimer.Interval = TimeSpan.FromMilliseconds(100); // 100ms更新一次UI
            _uiUpdateTimer.Tick += (sender, e) =>
            {
                if (_focusedBody != null && _isSimulationRunning)
                {
                    UpdateFocusedBodyUI();
                }
                UpdateFrameRate();
            };

            // 初始化渲染计时器
            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(FRAME_TIME_MS);
            _renderTimer.Tick += (sender, e) =>
            {
                lock (_renderLock)
                {
                    if (!_isFrameRendering && _displayPositionUpdated)
                    {
                        animationCanva.InvalidateVisual();
                    }
                }
            };

            // 初始化帧率统计
            _frameCount = 0;
            _lastFrameTime = DateTime.Now;


            // 初始化时间步长滑块
            SimulationTimeStepSlider.Minimum = SLIDER_MIN_VALUE;
            SimulationTimeStepSlider.Maximum = SLIDER_MAX_VALUE;
            SimulationTimeStepSlider.Value = SLIDER_CENTER_VALUE;


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
                new Vector2D(384400e3 / 1.4, 0),  // 地月距离（米）
                new Vector2D(0, 1022),      // 月球轨道速度（m/s）
                7.348e22,                   // 质量（kg）
                10,                         // 显示半径
                false,                      // 非中心天体
                System.Drawing.Color.Gray
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
        private void UpdateButtonStates()
        {
            // 在仿真运行时禁用修改按钮
            AddBodyBtn.IsEnabled = !_isSimulationRunning;
            RemoveBodyBtn.IsEnabled = !_isSimulationRunning;
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
                _renderTimer.Start();
                _uiUpdateTimer.Start();
            }
            else
            {
                // 停止仿真
                _physicsUpdateCts?.Cancel();
                _simulationTimer.Stop();
                _renderTimer.Stop();
                _uiUpdateTimer.Stop();
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
                _debugInfo[1] = $"当前滑块值：{e.NewValue}";
            }
        }

        private void SimulationTimeStepSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _debugInfo[7] = "重置被执行。";
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

        private void animationCanva_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var source = PresentationSource.FromVisual(animationCanva);
            var matrix = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
            double pixelWidth = animationCanva.ActualWidth * matrix.M11;
            double pixelHeight = animationCanva.ActualHeight * matrix.M22;
            _canvasCenter = new Vector2D(pixelWidth / 2, pixelHeight / 2);
        }

        private void UpdateFocusedBodyUI()
        {
            if (_focusedBody != null)
            {
                // 更新位置
                PositionIOTextBox.Text = $"({FormatDistance(_focusedBody.Position.X)},{FormatDistance(_focusedBody.Position.Y)})";

                // 更新速度
                VelocityIOTextBox.Text = FormatVelocity(_focusedBody.Velocity.Length);

                // 更新质量
                MassIOTextBox.Text = FormatMass(_focusedBody.Mass);
            }
        }


        protected override void OnClosed(EventArgs e)
        {
            // 停止所有计时器和任务
            _physicsUpdateCts?.Cancel();
            _simulationTimer?.Stop();
            _renderTimer?.Stop();
            _uiUpdateTimer?.Stop();

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






        private async void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (!_isSimulationRunning) return;

            try
            {
                // 如果上一个物理更新任务还在运行，等待它完成
                if (_physicsUpdateTask != null && !_physicsUpdateTask.IsCompleted)
                {
                    await _physicsUpdateTask;
                }

                // 创建新的物理更新任务
                _physicsUpdateTask = Task.Run(() =>
                {
                    lock (_physicsLock)
                    {
                        if (_physicsUpdateCts?.Token.IsCancellationRequested == true)
                            return;

                        _renderer._physicsEngine.Update(_timeStep);
                        _displayPositionUpdated = false;
                        UpdateDisplayPositions();
                        _displayPositionUpdated = true;
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
                try
                {
                    var parts = PositionIOTextBox.Text.Trim('(', ')').Split(',');
                    if (parts.Length == 2)
                    {
                        double x = ParseDistance(parts[0]);
                        double y = ParseDistance(parts[1]);
                        _focusedBody.Position = new Vector2D(x, y);
                        UpdateDisplayPositions();
                        animationCanva.InvalidateVisual();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无效的位置格式：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void VelocityIOTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isSimulationRunning && _focusedBody != null)
            {
                try
                {
                    double velocity = ParseVelocity(VelocityIOTextBox.Text);
                    // 保持速度方向不变，只改变大小
                    double currentLength = _focusedBody.Velocity.Length;
                    if (currentLength > 0)
                    {
                        _focusedBody.Velocity = _focusedBody.Velocity * (velocity / currentLength);
                    }
                    else
                    {
                        _focusedBody.Velocity = new Vector2D(velocity, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无效的速度格式：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MassIOTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isSimulationRunning && _focusedBody != null)
            {
                try
                {
                    double mass = ParseMass(MassIOTextBox.Text);
                    _focusedBody.Mass = mass;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无效的质量格式：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateDisplayPositions()
        {
            _debugInfo[5] = "进入 UpdateDisplayPositions";
            // 获取画布的实际像素尺寸

            var centerBody = _renderer._physicsEngine.Bodies.FirstOrDefault(b => b.IsCenter);

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
                // 如果没有中心天体和焦点天体，所有天体都相对于画布中心计算，并考虑相机偏移
                foreach (var body in _renderer._physicsEngine.Bodies)
                {
                    var relativePosition = body.Position - _cameraOffset;
                    body.DisplayPosition = PhysicalToScreenPosition(relativePosition);
                }
            }

            _debugInfo[5] = "完成 UpdateDisplayPositions";
            _displayPositionUpdated = true;
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
                    _dragStartPosition = ScreenToPhysicalPosition(_lastMousePosition.Value);
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
                var currentPhysicalPos = ScreenToPhysicalPosition(currentMouseVector);
                var physicalDelta = currentPhysicalPos - _dragStartPosition.Value;

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
                        System.Drawing.Color.Red // 红色
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

        private void OnPaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            lock (_renderLock)
            {
                _isFrameRendering = true;
                try
                {
                    var canvas = e.Surface.Canvas;
                    canvas.Clear(SKColors.Black);

                    // 渲染天体
                    _renderer.RenderBodies(canvas);

                    // 渲染轨迹
                    if (_showTrajectory)
                    {
                        _renderer.RenderTrajectory(canvas);
                    }

                    _frameCount++;
                }
                finally
                {
                    _isFrameRendering = false;
                }
            }
        }






        #region 待实现功能
        private void UpdateUI()
        {
            // 更新天体数量显示
            var bodyCountText = this.FindName("BodyCountText") as TextBlock;
            if (bodyCountText != null)
            {
                bodyCountText.Text = $"天体总量 {_renderer._physicsEngine.Bodies.Count} 个";
            }

            // 更新其他UI元素
            // TODO: 实现其他UI更新
        }
        private void VelocitySettingModeBtn_Click(object sender, RoutedEventArgs e)
        {
            _isVelocitySettingMode = !_isVelocitySettingMode;
            // TODO: 实现速度设置模式的具体功能
        }

        private void TimeStepSettingBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ResetSimulationBtn_Click(object sender, RoutedEventArgs e)
        {
            // 重置所有状态
            _renderer._physicsEngine.Bodies.Clear();
            _cameraOffset = Vector2D.ZeroVector;
            _zoomScale = 1.0;
            _focusedBody = null;
            _isTimeReversed = false;
            _showTrajectory = false;

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
            _focusedBody = null;
            _cameraOffset = Vector2D.ZeroVector;
            _zoomScale = 1.0;
        }

        private void AddBodyBtn_Click(object sender, RoutedEventArgs e)
        {
            _isAddingBody = !_isAddingBody;

        }

        private void RemoveBodyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (FocusIOTextBox.SelectedItem != null)
            {
                var selectedBody = _renderer._physicsEngine.Bodies.FirstOrDefault(b => b.Name == FocusIOTextBox.SelectedItem.ToString());
                if (selectedBody != null)
                {
                    _renderer._physicsEngine.Bodies.Remove(selectedBody);
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

        #endregion


        #region 无副作用的计算函数
        // 将屏幕坐标转换为物理坐标
        private Vector2D ScreenToPhysicalPosition(Vector2D screenPos)
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

        // 格式化距离（米）
        private string FormatDistance(double meters)
        {
            if (Math.Abs(meters) >= 1e9)
                return $"{meters / 1e9:F2} × 10⁹ m";
            else if (Math.Abs(meters) >= 1e6)
                return $"{meters / 1e6:F2} × 10⁶ m";
            else if (Math.Abs(meters) >= 1e3)
                return $"{meters / 1e3:F2} × 10³ m";
            else
                return $"{meters:F2} m";
        }

        // 格式化速度（米/秒）
        private string FormatVelocity(double velocity)
        {
            if (Math.Abs(velocity) >= 1e6)
                return $"{velocity / 1e6:F2} × 10⁶ m/s";
            else if (Math.Abs(velocity) >= 1e3)
                return $"{velocity / 1e3:F2} × 10³ m/s";
            else
                return $"{velocity:F2} m/s";
        }

        // 格式化质量（千克）
        private string FormatMass(double mass)
        {
            if (Math.Abs(mass) >= 1e30)
                return $"{mass / 1e30:F2} × 10³⁰ kg";
            else if (Math.Abs(mass) >= 1e27)
                return $"{mass / 1e27:F2} × 10²⁷ kg";
            else if (Math.Abs(mass) >= 1e24)
                return $"{mass / 1e24:F2} × 10²⁴ kg";
            else if (Math.Abs(mass) >= 1e21)
                return $"{mass / 1e21:F2} × 10²¹ kg";
            else if (Math.Abs(mass) >= 1e18)
                return $"{mass / 1e18:F2} × 10¹⁸ kg";
            else
                return $"{mass:F2} kg";
        }

        // 解析带单位的距离字符串
        private double ParseDistance(string input)
        {
            input = input.Trim().ToLower();
            double value;
            if (input.EndsWith("m"))
            {
                input = input.Substring(0, input.Length - 1).Trim();
                if (double.TryParse(input, out value))
                    return value;
            }
            throw new FormatException("无效的距离格式");
        }

        // 解析带单位的速度字符串
        private double ParseVelocity(string input)
        {
            input = input.Trim().ToLower();
            double value;
            if (input.EndsWith("m/s"))
            {
                input = input.Substring(0, input.Length - 3).Trim();
                if (double.TryParse(input, out value))
                    return value;
            }
            throw new FormatException("无效的速度格式");
        }

        // 解析带单位的质量字符串
        private double ParseMass(string input)
        {
            input = input.Trim().ToLower();
            double value;
            if (input.EndsWith("kg"))
            {
                input = input.Substring(0, input.Length - 2).Trim();
                if (double.TryParse(input, out value))
                    return value;
            }
            throw new FormatException("无效的质量格式");
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

        private void UpdateFrameRate()
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastFrameTime).TotalSeconds;

            if (elapsed >= 1.0)
            {
                var fps = _frameCount / elapsed;
                FrameReportTextBlock.Text = $"FPS: {fps:F1}";
                _frameCount = 0;
                _lastFrameTime = now;
            }
        }

        #endregion

    }
}
