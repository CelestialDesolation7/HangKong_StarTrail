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
    public partial class GravitySimulationForm : Window
    {

        #region 成员变量
        // 仿真功能成员
        private Renderer _renderer; // 渲染器，包含物理引擎作为成员
        private DispatcherTimer _simulationTimer;   // 物理仿真计时器
        // 参数设置成员
        private double _timeStep; // 每帧物理仿真时间步长
        private bool _showTrajectory = false; // 是否显示轨迹
        private bool _isSimulationRunning = true; // 是否正在仿真
        private bool _isTimeReversed = false;   // 是否反向仿真
        private bool _isAddingBody = false;
        private bool _isVelocitySettingMode = false;
        // 画布相关后台成员
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
        // 调试信息成员
        private DebugForm.DebugSimulationForm _debugWindow; // 调试窗口
        private string[] _debugInfo = new string[10]; // 用于存储调试信息的字符串
        private long[] _universalCounter = new long[10]; // 用于存储全局计数器
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
                animationCanva.InvalidateVisual();
            };
        }

        private void InitializeSimulation()
        {
            _renderer = new Renderer(new PhysicsEngine());

            // 初始化仿真计时器
            _simulationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(8) // 约120FPS
            };
            _simulationTimer.Tick += SimulationTimer_Tick;
            _simulationTimer.Tick += (sender, e) =>
            {
                _universalCounter[6]++;
                _debugInfo[6] = ($"SimulationTimer_Tick次数: {_universalCounter[6]}");
            };
            _simulationTimer.Start();

            // 初始化画布事件
            animationCanva.PaintSurface += OnPaintSurface;
            animationCanva.MouseWheel += AnimationCanva_MouseWheel;
            animationCanva.MouseDown += AnimationCanva_MouseDown;
            animationCanva.MouseMove += AnimationCanva_MouseMove;
            animationCanva.MouseUp += AnimationCanva_MouseUp;

            // 初始化时间步长滑块
            SimulationTimeStepSlider.Minimum = 0.1;
            SimulationTimeStepSlider.Maximum = 100000.0;
            SimulationTimeStepSlider.Value = 1.0;
            SimulationTimeStepSlider.ValueChanged += SimulationTimeStepSlider_ValueChanged;

            // 初始化焦点下拉框
            FocusIOTextBox.SelectionChanged += FocusIOTextBox_SelectionChanged;

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
                false,                // 中心天体
                System.Drawing.Color.Blue
            );

            // 月球数据
            var moon = new Body(
                "月球",
                new Vector2D(384400e3 / 2, 0),  // 地月距离（米）
                new Vector2D(0, 1022),      // 月球轨道速度（m/s）
                7.348e22,                   // 质量（kg）
                10,                         // 显示半径
                false,                      // 非中心天体
                System.Drawing.Color.Gray
            );

            _renderer.AddBody(earth);
            _renderer.AddBody(moon);

            // 更新焦点下拉框
            UpdateFocusComboBox();

            // 设置合适的时间步长（约8小时每帧）
            _timeStep = 28800;  // 8小时 = 28800秒
            SimulationTimeStepSlider.Value = _timeStep;
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

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void SimulationTimeStepSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _timeStep = e.NewValue;
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

        #endregion



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


        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (_isSimulationRunning && !_isFrameRendering)
            {
                _isFrameRendering = true;
                _debugInfo[5] = "进入 SimulationTimer_Tick";

                try
                {
                    if (_isTimeReversed)
                    {
                        // 反向仿真
                        _renderer.UpdatePhysics(-_timeStep);
                    }
                    else
                    {
                        // 正向仿真
                        _renderer.UpdatePhysics(_timeStep);
                    }
                    UpdateDisplayPositions();
                    animationCanva.InvalidateVisual();
                }
                finally
                {
                    // 确保在任何情况下都会重置渲染标志
                    _isFrameRendering = false;
                    _debugInfo[5] = "完成 SimulationTimer_Tick";
                }
            }
        }

        private void UpdateDisplayPositions()
        {
            _debugInfo[5] = "进入 UpdateDisplayPositions";
            // 获取画布的实际像素尺寸
            var source = PresentationSource.FromVisual(animationCanva);
            var matrix = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
            double pixelWidth = animationCanva.ActualWidth * matrix.M11;
            double pixelHeight = animationCanva.ActualHeight * matrix.M22;
            _canvasCenter = new Vector2D(pixelWidth / 2, pixelHeight / 2);

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

            _universalCounter[1]++;
            _debugInfo[1] = ($"UpdateDisplayPositions函数被调用次数: {_universalCounter[1]}");
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
                var centerBody = _renderer._physicsEngine.Bodies.FirstOrDefault(b => b.IsCenter);
                if (centerBody == null) // 只在没有中心天体时允许拖动
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

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (_isFrameRendering || !_displayPositionUpdated)
            {
                _debugInfo[5] = "跳过 OnPaintSurface (正在渲染)";
                return;
            }

            _debugInfo[5] = "进入 OnPaintSurface";
            var canvas = e.Surface.Canvas;
            var info = e.Info;

            // 清空画布并设置黑色背景
            canvas.Clear(SKColors.Black);

            // 使用 Renderer 进行绘制
            
            
            _renderer.RenderBodies(canvas);
            _displayPositionUpdated = false;
            _universalCounter[0]++;
            _debugInfo[0] = ($"RenderBodies函数被调用次数: {_universalCounter[0]}");
            
            
            

            if (_showTrajectory)
            {
                _renderer.RenderTrajectory(canvas);
            }

            _renderer.RenderText(canvas);

            _debugInfo[5] = "完成 OnPaintSurface";
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
            //_renderer._physicsEngine.Bodies.Clear();
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


        #region 窗口关闭
        private void ExitSimulationBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        protected override void OnClosed(EventArgs e)
        {
            _debugWindow?.Close();
            base.OnClosed(e);
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
        #endregion


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
    }
}
