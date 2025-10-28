#undef DEBUG
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
        #region 渲染相关后台控制器
        private Renderer _renderer; // 渲染器，包含物理引擎作为成员
        private DispatcherTimer _simulationTimer;   // 物理仿真计时器
        private DispatcherTimer _uiUpdateTimer;     // UI更新计时器
        private Task? _physicsUpdateTask;          // 物理更新任务
        private CancellationTokenSource? _physicsUpdateCts; // 物理更新取消令牌
        private readonly object _physicsLock = new object(); // 物理更新锁
        private readonly object _renderLock = new object();  // 渲染锁
        private int _frameCount;                    // 帧计数器
        private DateTime _lastFrameTime;            // 上一帧时间
        private DateTime _lastPhysicsUpdate;        // 上次物理更新时间
        private bool _isRenderingEnabled = true;    // 是否启用渲染
        private bool _isFrameRendering = false; // 当前帧是否正在渲染
        #endregion

        #region 数据库相关成员
        private SQLiteConnection? _dbConnection;    // 数据库连接
        private int _currentSceneId = 1;           // 当前场景ID，默认为地月系统
        #endregion

        #region 仿真参数设置
        private double _timeStep; // 每帧物理仿真时间步长
        private double _recommendedTimeStep;   //推荐时间步长
        private bool _isSimulationRunning = true; // 是否正在仿真
        private bool _isTimeReversed = false;   // 是否反向仿真
        private bool _isVelocityVisualizeMode = false;
        private Body? _focusedBody = null;  // 当前聚焦的天体
        private Body? _centerBody = null;   // 当前场景下的中心天体
        #endregion

        #region 画布相关参数成员
        private double _recommendedScale; // 推荐比例尺
        private double _zoomFactor = 1.0; // 缩放因子，初始为1
        private double _pixelToDistanceRatio; // 实际使用的比例尺 = 推荐比例尺 * 缩放因子
        private Vector2D _canvasCenter; // 画布中心点
        private Vector2D? _lastPhysicalDistFromCanvasCenter; // 记录拖动开始时的物理位置
        private Vector2D? _lastMouseCanvasPos; // 记录上一帧的鼠标位置（屏幕坐标）
        private bool _isMiddleButtonPressed;    // 中键是否按下
        private Vector2D _cameraOffset = Vector2D.ZeroVector;   // 相机偏移量
        private bool _useVectorToDisplay = false; // 是否使用向量显示模式
        #endregion

#if DEBUG
        #region 调试信息成员
        private DebugForm.DebugSimulationForm _debugWindow; // 调试窗口
        private string[] _debugInfo = new string[10]; // 用于存储调试信息的字符串
        private long[] _universalCounter = new long[10]; // 用于存储全局计数器
        #endregion
#endif

        #region 常量
        private const double DESIRED_PIXEL_MOVEMENT = 5.0; // 期望每帧移动的像素数
        private const double SLIDER_CENTER_VALUE = 1.0; // 滑块的中心值
        private const double SLIDER_MIN_VALUE = 0.0; // 滑块的最小值
        private const double SLIDER_MAX_VALUE = 2.0; // 滑块的最大值
        private const int TARGET_FPS = 120;         // 目标帧率
        private const int FRAME_TIME_MS = 1000 / TARGET_FPS; // 每帧预期时间(ms)
        private const int MIN_PHYSICS_UPDATE_INTERVAL = 1; // 最小物理更新间隔(ms)
        // 窗口状态码
        private const int WM_NCHITTEST = 0x0084;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        #endregion

        public string FocusedBodyName => _focusedBody?.Name;

        // 假设你有速度、受力等数据结构
        public double CurrentVelocity { get; private set; }
        public double CurrentForce { get; private set; }
        public int FrameCount { get; private set; }


        /// <summary>
        /// 获取实例。
        /// </summary>
        public Renderer RendererInstance
        {
            get { return _renderer; }
        }


        #endregion


        #region 初始化函数
        public GravitySimulationForm()
        {
            InitializeComponent();
            InitializeSimulation();
#if DEBUG
            InitializeDebugWindow();
#endif
            InitializeStatisticsRecord();
            SetInitialFocusBody();

            // 等待控件完成布局后再计算推荐比例尺
            animationCanva.Loaded += (s, e) =>
            {
                InitializeSettingParameters();
                InitializeUIStatus();
                UpdateDisplayPositions();
                animationCanva.InvalidateVisual();
                // 设置同步
                _renderer.SetTimeStep(_timeStep);
                _renderer.SetPixelToDistanceRatio(_pixelToDistanceRatio);
            };


        }

        // 初始化所有参数设置
        public void InitializeSettingParameters()
        {
            // 渲染控制器参数成员
            _isFrameRendering = false;
            _isRenderingEnabled = true;
            _lastFrameTime = DateTime.Now;
            _lastPhysicsUpdate = DateTime.Now;
            // 画布相关参数成员
            _recommendedScale = CalculateRecommendedScale();
            _pixelToDistanceRatio = _recommendedScale;
            _zoomFactor = 1.0;
            _cameraOffset = Vector2D.ZeroVector;
            _lastPhysicalDistFromCanvasCenter = null;
            _lastMouseCanvasPos = null;
            _isMiddleButtonPressed = false;
            UpdateFocusComboBox();
            var centerBody = _renderer._physicsEngine.Bodies.FirstOrDefault(b => b.IsCenter);
            if (centerBody != null)
            {
                FocusIOComboBox.SelectedItem = centerBody.Name;
                _focusedBody = centerBody;
                _centerBody = centerBody;
            }
            else
            {
                // 如果没有中心天体，设置为自由模式
                FocusIOComboBox.SelectedItem = "自由模式";
                _focusedBody = null;
                _centerBody = null;
            }
            // 仿真相关参数成员
            _recommendedTimeStep = _timeStep = CalculateRecommendedTimeStep();
            _isSimulationRunning = false;
            _isTimeReversed = false;
            _isVelocityVisualizeMode = false;
        }

        // 初始化统计记录
        public void InitializeStatisticsRecord()
        {
            _frameCount = 0;
            _lastFrameTime = DateTime.Now;
            _lastPhysicsUpdate = DateTime.Now;
            _renderer._physicsEngine.timeElapsed = 0;
        }

        // 初始化UI状态
        public void InitializeUIStatus()
        {
            // 初始化帧率统计
            _frameCount = 0;
            _lastFrameTime = DateTime.Now;
            _lastPhysicsUpdate = DateTime.Now;

            // 初始化时间步长滑块
            SimulationTimeStepSlider.Minimum = SLIDER_MIN_VALUE;
            SimulationTimeStepSlider.Maximum = SLIDER_MAX_VALUE;
            SimulationTimeStepSlider.Value = SLIDER_CENTER_VALUE;

            // 初始化按钮状态
            UpdateButtonStates();
            UpdateAllUIMonitor(null, null);
        }

        // 仿真后台初始化，只会被调用一次
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
            CompositionTarget.Rendering += RenderCaller;

            // 加载默认地月系数据
            LoadDefaultEarthMoonSystem();
        }

        private void LoadDefaultEarthMoonSystem()
        {
            try
            {
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\scene.db");
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("未找到预设场景数据库", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 建立数据库连接
                _dbConnection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                _dbConnection.Open();

                // 加载地月系统场景
                LoadScene(_currentSceneId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载默认场景时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 加载指定场景ID的场景
        private void LoadScene(int sceneId)
        {
            if (_dbConnection == null) return;
            bool temp = _isVelocityVisualizeMode;
            try
            {
                // 获取场景中的所有天体
                var bodies = new List<Body>();
                using (var command = new SQLiteCommand(
                    "SELECT Name, PositionX, PositionY, VelocityX, VelocityY, Mass, IsCenter, " +
                    "ColorR, ColorG, ColorB, ColorA, PhysicalRadius " +
                    "FROM Bodies WHERE SceneId = @SceneId", _dbConnection))
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
                                reader.GetDouble(11), // PhysicalRadius
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

                var loadSceneNameCommand = new SQLiteCommand("SELECT SceneName FROM Scenes WHERE SceneId = @SceneId", _dbConnection);
                loadSceneNameCommand.Parameters.AddWithValue("@SceneId", sceneId);
                using (var reader = loadSceneNameCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        SceneNameReportLabel.Text = reader.GetString(0);
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

                // 更新焦点天体下拉框
                UpdateFocusComboBox();
                // 重绘画布
                animationCanva.InvalidateVisual();
                // 重置必要的数据
                // 更新UI
                InitializeSettingParameters();
                InitializeStatisticsRecord();
                InitializeUIStatus();
                _renderer.InitializeRenderer();
                _renderer.SetPixelToDistanceRatio(_pixelToDistanceRatio);
                _renderer.SetTimeStep(_timeStep);
                _isVelocityVisualizeMode = temp;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载场景时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
#endregion

#if DEBUG
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
            debugInfo.AppendLine($"基准步长: {_recommendedTimeStep:F2} 秒");
            debugInfo.AppendLine($"推荐比例尺: {_recommendedScale:F6}");
            debugInfo.AppendLine($"缩放因子: {_zoomFactor:F6}");
            debugInfo.AppendLine($"实际比例尺: {_pixelToDistanceRatio:F6}");
            debugInfo.AppendLine($"相机偏移(物理坐标): X={_cameraOffset.X:F2}, Y={_cameraOffset.Y:F2}");
            debugInfo.AppendLine($"画布中心: X={_canvasCenter.X:F2}, Y={_canvasCenter.Y:F2}");

            if (_lastPhysicalDistFromCanvasCenter.HasValue)
            {
                debugInfo.AppendLine($"拖动起始位置(物理坐标): X={_lastPhysicalDistFromCanvasCenter.Value.X:F2}, Y={_lastPhysicalDistFromCanvasCenter.Value.Y:F2}");
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
                debugInfo.AppendLine($"速度: {body.Velocity.Length}");
            }

            foreach (var info in _debugInfo)
            {
                if (info == null) continue;
                debugInfo.AppendLine(info);
            }
            _debugWindow.DebugTextBox.Text = debugInfo.ToString();
        }

        #endregion
#endif

        #region 基本UI响应
        // 数据显示模式切换按钮处理函数
        private void VelocityDispModeBtn_Click(object sender, RoutedEventArgs e)
        {
            _useVectorToDisplay = !_useVectorToDisplay;
            FocusDataReporter(sender, e);
            var velocityDispModeBtn = VelocityDispModeBtn.Content as StackPanel;
            var text = velocityDispModeBtn.Children[1] as TextBlock;
            text.Text = _useVectorToDisplay ? "标量模式" : "向量模式";
        }

        // 更新UI上所有的信息显示文本的行为集合
        private void UpdateAllUIMonitor(object? sender, EventArgs? e)
        {
            FPSReporter(sender, e);
            FocusDataReporter(sender, e);
            SimulationStaticsReporter(sender, e);
            if (_focusedBody != null)
            {
                CurrentVelocity = _focusedBody.Velocity.Length;
                CurrentForce = CalculateTotalForce(_focusedBody);
            }
            else
            {
                CurrentVelocity = 0;
                CurrentForce = 0;
            }
        }

        // 汇报帧率到UI的行为集合
        private void FPSReporter(object? sender, EventArgs? e)
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
        }

        // 汇报观测对象数据到UI的行为集合
        private void FocusDataReporter(object? sender, EventArgs? e)
        {
            // 汇报观测对象属性
            if (_focusedBody != null)
            {
                if (_focusedBody != null)
                {
                    // 更新位置
                    PositionIOTextBox.Text = $"({FormatDistance(_focusedBody.Position.X)},{FormatDistance(_focusedBody.Position.Y)})";
                    if (_useVectorToDisplay)
                    {// 更新速度（向量形式）
                        VelocityIOTextBox.Text = $"({FormatVelocity(_focusedBody.Velocity.X)},{FormatVelocity(_focusedBody.Velocity.Y)})";
                    }
                    else
                    {
                        VelocityIOTextBox.Text = $"{FormatVelocity(_focusedBody.Velocity.Length)}";
                    }
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
        }

        // 汇报渲染统计数据到UI的行为集合
        private void SimulationStaticsReporter(object? sender, EventArgs? e)
        {
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

        // 更新UI上所有的按钮状态（包括图标替换，启用/禁用等）
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

        // 关闭窗口按钮处理函数
        private void ExitSimulationBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 仿真开始/暂停按钮处理函数
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

        // 拖动窗口事件处理函数
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

        // 时间步长滑块值改变事件处理函数
        private void SimulationTimeStepSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue != 0)
            {
                // 计算新的时间步长
                double ratio = Math.Pow(2, e.NewValue - SLIDER_CENTER_VALUE); // 使用更小的底数来降低灵敏度
                // 实时应用新的时间步长,但暂时不更新
                _timeStep = _recommendedTimeStep * ratio;
                _renderer.SetTimeStep(_timeStep);
                if (!_isSimulationRunning)
                {
                    UpdateDisplayPositions();
                    animationCanva.InvalidateVisual();
                }
            }
        }

        // 时间步长滑块拖动完成事件处理函数
        private void SimulationTimeStepSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _recommendedTimeStep = _timeStep;
            SimulationTimeStepSlider.Value = SLIDER_CENTER_VALUE;
        }

        // 更新焦点下拉框的函数
        private void UpdateFocusComboBox()
        {
            // 保存当前选中的项
            var currentSelection = FocusIOComboBox.SelectedItem?.ToString();
            // 清空下拉框
            FocusIOComboBox.Items.Clear();
            // 添加自由模式选项
            FocusIOComboBox.Items.Add("自由模式");
            // 添加所有天体
            foreach (var body in _renderer._physicsEngine.Bodies)
            {
                FocusIOComboBox.Items.Add(body.Name);
            }
            // 恢复选中项
            if (!string.IsNullOrEmpty(currentSelection))
            {
                FocusIOComboBox.SelectedItem = currentSelection;
            }
        }

        // 焦点控制下拉框的选择改变事件处理函数
        private void FocusIOComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = FocusIOComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedItem)) return;

            if (selectedItem == "自由模式")
            {
                if (_focusedBody != null)
                {
                    _cameraOffset = _focusedBody.Position;
                    _focusedBody = null;
                }
                CurrentVelocity = 0;
                CurrentForce = 0;
                PositionIOTextBox.Text = "N/A";
                VelocityIOTextBox.Text = "N/A";
                MassIOTextBox.Text = "N/A";
            }
            else
            {
                _focusedBody = _renderer._physicsEngine.Bodies.FirstOrDefault(b => b.Name == selectedItem);
                if (_focusedBody != null)
                {
                    CurrentVelocity = _focusedBody.Velocity.Length;
                    CurrentForce = CalculateTotalForce(_focusedBody);
                }
                else
                {
                    CurrentVelocity = 0;
                    CurrentForce = 0;
                }
            }
            if (!_isSimulationRunning)
            {
                UpdateDisplayPositions();
                animationCanva.InvalidateVisual();
            }
        }

        // 画布缩放事件处理函数，更新画布像素尺寸
        private void AnimationCanva_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var source = PresentationSource.FromVisual(animationCanva);
            var matrix = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
            double pixelWidth = animationCanva.ActualWidth * matrix.M11;
            double pixelHeight = animationCanva.ActualHeight * matrix.M22;
            _canvasCenter = new Vector2D(pixelWidth / 2, pixelHeight / 2);
        }

        // 窗口关闭时的线程处理函数
        protected override void OnClosed(EventArgs e)
        {
            // 停止所有计时器和任务
            _physicsUpdateCts?.Cancel();
            _simulationTimer?.Stop();
            _uiUpdateTimer?.Stop();
            CompositionTarget.Rendering -= RenderCaller;

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
#if DEBUG
            _debugWindow?.Close();
#endif
            base.OnClosed(e);
        }

        // 位置输入输出框的键盘事件处理函数
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

        // 速度输入输出框的键盘事件处理函数
        private void VelocityIOTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isSimulationRunning && _focusedBody != null)
            {
                var originalText = VelocityIOTextBox.Text;
                try
                {
                    var parts = VelocityIOTextBox.Text.Trim('(', ')').Split(',');
                    if (parts.Length == 2)
                    {
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

        // 质量输入输出框的键盘事件处理函数
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

        // 速度可视化按钮的处理函数
        private void VelocityVisualizeModeBtn_Click(object sender, RoutedEventArgs e)
        {
            _isVelocityVisualizeMode = !_isVelocityVisualizeMode;
        }

        // 速度长度因子输入框的键盘事件处理函数
        private void VelocityLengthFactorIOTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _renderer._velocityLengthFactor = double.Parse(VelocityLengthFactorIOTextBox.Text);
            }
            // 更新此输入框的值
            VelocityLengthFactorIOTextBox.Text = _renderer._velocityLengthFactor.ToString();
        }

        // 最小显示半径输入框的键盘事件处理函数
        private void MinimumDisplayRadiusIOTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _renderer._minimumDisplayRadius = int.Parse(MinimumDisplayRadiusIOTextBox.Text);
            }
            // 更新此输入框的值
            MinimumDisplayRadiusIOTextBox.Text = _renderer._minimumDisplayRadius.ToString();
        }

        // 重置焦点按钮的处理函数
        private void FocusResetBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_centerBody != null)
            {
                _cameraOffset = Vector2D.ZeroVector;
                _focusedBody = _centerBody;
                FocusIOComboBox.SelectedItem = $"{_centerBody.Name}";
            }
            else
            {
                // 如果没有中心天体，设置为自由模式
                FocusIOComboBox.SelectedItem = "自由模式";
                _cameraOffset = Vector2D.ZeroVector;
                _focusedBody = null;
            }
        }

        // 导入场景按钮的处理函数
        private void ImportSceneButton_Click(object sender, RoutedEventArgs e)
        {
            // 如果仿真正在运行，则先暂停
            if (_isSimulationRunning)
            {
                StartPauseBtn_Click(sender, e);
            }

            var sceneWindow = new SceneSelectionWindow();
            if (sceneWindow.ShowDialog() == true)
            {
                try
                {
                    if (_dbConnection == null)
                    {
                        MessageBox.Show("数据库连接未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 获取场景ID
                    using (var command = new SQLiteCommand("SELECT SceneId FROM Scenes WHERE SceneName = @SceneName", _dbConnection))
                    {
                        command.Parameters.AddWithValue("@SceneName", sceneWindow.SelectedSceneName);
                        _currentSceneId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // 加载选中的场景
                    LoadScene(_currentSceneId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入预设场景时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 导出场景按钮的处理函数
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
                if (_dbConnection == null)
                {
                    MessageBox.Show("数据库连接未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var transaction = _dbConnection.BeginTransaction())
                {
                    try
                    {
                        // 插入场景
                        int sceneId;
                        using (var command = new SQLiteCommand(
                            "INSERT INTO Scenes (SceneName) VALUES (@SceneName); " +
                            "SELECT last_insert_rowid();", _dbConnection))
                        {
                            command.Parameters.AddWithValue("@SceneName", sceneName);
                            sceneId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        // 插入天体
                        foreach (var body in _renderer._physicsEngine.Bodies)
                        {
                            using (var command = new SQLiteCommand(
                                "INSERT INTO Bodies (SceneId, Name, PositionX, PositionY, " +
                                "VelocityX, VelocityY, Mass, IsCenter, " +
                                "ColorR, ColorG, ColorB, ColorA, PhysicalRadius) " +
                                "VALUES (@SceneId, @Name, @PositionX, @PositionY, " +
                                "@VelocityX, @VelocityY, @Mass, @IsCenter, " +
                                "@ColorR, @ColorG, @ColorB, @ColorA, @PhysicalRadius)", _dbConnection))
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
                                command.Parameters.AddWithValue("@PhysicalRadius", body.PhysicalRadius);
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
            catch (Exception ex)
            {
                MessageBox.Show($"导出场景时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 重置仿真按钮的处理函数
        private void ResetSimulationBtn_Click(object sender, RoutedEventArgs e)
        {
            // 如果仿真正在运行，则先暂停
            if (_isSimulationRunning)
            {
                StartPauseBtn_Click(sender, e);
            }

            // 重新加载当前场景
            LoadScene(_currentSceneId);
        }

        // 窗口处理函数注册器
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 以下这一行代码是为了将窗口的消息处理交给我们自定义的函数WindowProc
            IntPtr handle = (new WindowInteropHelper(this)).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }

        // 以下这一函数接管了Windows系统的窗口消息处理，手动实现了窗口的拖动和缩放监控
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



        #region 画面渲染相关操作
        // 物理更新线程的主要函数
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
            // 如果上一个物理更新任务还在运行，直接返回
            if (_physicsUpdateTask != null && !_physicsUpdateTask.IsCompleted)
            {
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
                        if (_isTimeReversed)
                        {
                            _renderer._physicsEngine.Update(-_timeStep);
                        }
                        else
                        {
                            _renderer._physicsEngine.Update(_timeStep);
                        }
                        UpdateDisplayPositions();
                        _lastPhysicsUpdate = DateTime.Now;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"物理更新出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 更新所有天体的显示位置
        private void UpdateDisplayPositions()
        {
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
        }

        // 滚轮缩放事件处理函数
        private void AnimationCanva_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 根据滚轮方向调整缩放因子
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            _zoomFactor *= zoomFactor;

            // 更新实际使用的比例尺
            _pixelToDistanceRatio = _recommendedScale * _zoomFactor;
            _renderer.SetPixelToDistanceRatio(_pixelToDistanceRatio);

            // 只在暂停模式下显式更新显示
            if (!_isSimulationRunning)
            {
                UpdateDisplayPositions();
                animationCanva.InvalidateVisual();
            }
        }

        // 自由模式移动事件入口函数
        private void AnimationCanva_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                // 只在没有焦点天体时允许拖动
                if (_focusedBody == null)
                {
                    _isMiddleButtonPressed = true;
                    var mousePos = e.GetPosition(animationCanva);
                    _lastMouseCanvasPos = new Vector2D(mousePos.X, mousePos.Y);

                    // 将鼠标位置转换为物理坐标
                    _lastPhysicalDistFromCanvasCenter = CaculatePhysicalDistFromCanvasCenter(_lastMouseCanvasPos.Value);
                    animationCanva.CaptureMouse();
                }
            }
        }

        // 自由模式移动事件拖动函数
        private void AnimationCanva_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMiddleButtonPressed && _lastMouseCanvasPos.HasValue && _lastPhysicalDistFromCanvasCenter.HasValue)
            {
                var newMouseCanvasPos = e.GetPosition(animationCanva);
                var newMouseCanvasPosVec2D = new Vector2D(newMouseCanvasPos.X, newMouseCanvasPos.Y);

                // 计算鼠标移动的物理位移
                var currentPhysicalDistFromCanvasCenter = CaculatePhysicalDistFromCanvasCenter(newMouseCanvasPosVec2D);
                var physicalDelta = currentPhysicalDistFromCanvasCenter - _lastPhysicalDistFromCanvasCenter.Value;
                var pixelDelta = newMouseCanvasPosVec2D - _lastMouseCanvasPos.Value;
                _renderer.UpdateStarOffset((float)pixelDelta.X, (float)pixelDelta.Y);
                // 更新相机偏移
                _cameraOffset -= physicalDelta;

                // 更新拖动起始位置为当前位置
                _lastPhysicalDistFromCanvasCenter = currentPhysicalDistFromCanvasCenter;
                _lastMouseCanvasPos = newMouseCanvasPosVec2D;

                // 只在暂停模式下更新显示
                if (!_isSimulationRunning)
                {
                    UpdateDisplayPositions();
                    animationCanva.InvalidateVisual();
                }
            }
        }

        // 自由模式移动事件退出函数
        private void AnimationCanva_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released)
            {
                _isMiddleButtonPressed = false;
                _lastMouseCanvasPos = null;
                _lastPhysicalDistFromCanvasCenter = null;
                animationCanva.ReleaseMouseCapture();
            }
        }

        // 画布绘制事件对应的处理委托
        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            RenderOneFrame(e.Surface.Canvas);
            _frameCount++;
        }

        // 调用渲染的委托函数
        private void RenderCaller(object sender, EventArgs e)
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

        // 渲染单帧的所有行为的集合
        private void RenderOneFrame(SKCanvas canvas)
        {
            // 渲染星空背景
            _renderer.RenderBackground(canvas);
            // 渲染天体
            _renderer.RenderBodies(canvas);
            // 渲染速度箭头
            if (_isVelocityVisualizeMode)
            {
                _renderer.RenderVelocityArrow(canvas);
            }
        }
        #endregion





        #region 待实现功能

        private void TimeReverseBtn_Click(object sender, RoutedEventArgs e)
        {
            _isTimeReversed = !_isTimeReversed;
        }

        private void ShowChartsViewButton_Click(object sender, RoutedEventArgs e)
        {
            var chartsView = new ChartsView(this);
            var window = new Window
            {
                Title = "仿真图表",
                Content = chartsView,
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            window.Show();
        }

        #endregion


        #region 无副作用的计算函数
        // 将屏幕坐标转换为物理坐标
        private Vector2D CaculatePhysicalDistFromCanvasCenter(Vector2D screenPos)
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

        // 计算推荐的时间步长
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

        // 计算推荐的缩放比例
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

        // 假设你有合力的计算方法
        private double CalculateTotalForce(Body body)
        {
            // 这里给出一个简单的合力计算示例（请根据你的物理引擎实际实现替换）
            if (body == null) return 0;
            var engine = _renderer._physicsEngine;
            double totalForce = 0;
            foreach (var other in engine.Bodies)
            {
                if (other == body) continue;
                var r = other.Position - body.Position;
                double distance = r.Length;
                if (distance == 0) continue;
                double force = 6.67430e-11 * body.Mass * other.Mass / (distance * distance);
                totalForce += force;
            }
            return totalForce;
        }
        #endregion
    }
}
