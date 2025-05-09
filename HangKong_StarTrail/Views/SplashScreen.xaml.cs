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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.IO;
using System.Diagnostics;  // 添加用于日志记录
using Path = System.IO.Path; // 明确指定使用System.IO下的Path

namespace HangKong_StarTrail.Views
{
    /// <summary>
    /// SplashScreen.xaml 的交互逻辑
    /// </summary>
    public partial class AppSplashScreen : Window
    {
        private DispatcherTimer _loadingTimer = null!;
        private DispatcherTimer _animationTimer = null!;
        private DispatcherTimer _particleTimer = null!;
        private Random _random = new Random();
        private AxisAngleRotation3D? _sunRotationTransform;
        
        // 加载进度
        private int _currentProgress = 0;
        private int _targetProgress = 0;

        // 粒子系统
        private List<Particle> _particles = new List<Particle>();
        private const int MaxParticles = 100;

        // 3D动画控制
        private double _sunRotationSpeed = 0.3;

        // 加载状态标志
        private bool _isLoadingCompleted = false;
        private TextBlock _pressAnyKeyText = null!;

        // 加载文本列表
        private List<string> _loadingMessages = new List<string>
        {
            "正在连接星际数据库...",
            "读取恒星运行参数...",
            "量子引擎初始化中...",
            "校准空间坐标系统...",
            "加载宇宙地图数据...",
            "准备探索工具...",
            "进行最终引导校准...",
            "探测器准备就绪..."
        };

        // 经典引言
        private List<string> _spaceQuotes = new List<string>
        {
            "在浩瀚的宇宙中，我们只是一粒尘埃，但仰望星空的精神将引领我们触及无限。",
            "宇宙不仅比我们想象的更为奇妙，它可能比我们能够想象的还要更为奇妙。 ——J.B.S. 霍尔丹",
            "我们是宇宙认识自己的方式。 ——卡尔·萨根",
            "地球是我们的摇篮，但人类不会永远生活在摇篮里。 ——康斯坦丁·齐奥尔科夫斯基",
            "我们都是星尘。 ——卡尔·萨根"
        };

        public AppSplashScreen()
        {
            try
            {
                // 设置全局未处理异常处理
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    var ex = e.ExceptionObject as Exception;
                    Debug.WriteLine($"应用程序域未处理异常: {ex?.Message}");
                    MessageBox.Show($"发生未处理的异常: {ex?.Message}\n\n{ex?.StackTrace}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
                };
                
                Application.Current.DispatcherUnhandledException += (sender, e) =>
                {
                    Debug.WriteLine($"UI线程未处理异常: {e.Exception.Message}");
                    MessageBox.Show($"发生未处理的UI异常: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "UI错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true; // 标记为已处理，防止应用程序崩溃
                };
                
                // 确保日志输出重定向
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_log.txt");
                Debug.WriteLine($"应用程序日志将写入: {logPath}");
                
                // 初始化组件
                Debug.WriteLine("正在初始化组件...");
                InitializeComponent();
                Debug.WriteLine("组件初始化完成");
                
                // 初始化3D几何体
                Debug.WriteLine("正在初始化3D几何体...");
                InitializeGeometry();
                Debug.WriteLine("3D几何体初始化完成");
                
                // 设置一个随机的太空引言
                try
                {
                    if (QuoteText != null)
                    {
                        QuoteText.Text = "\"" + _spaceQuotes[_random.Next(_spaceQuotes.Count)] + "\"";
                        Debug.WriteLine("引言设置完成");
                    }
                    else
                    {
                        Debug.WriteLine("警告: QuoteText控件未找到");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"设置引言时出错: {ex.Message}");
                }

                // 添加键盘事件监听
                this.KeyDown += AppSplashScreen_KeyDown;
                this.MouseDown += AppSplashScreen_MouseDown;

                // 创建"按任意键继续"文本
                CreatePressAnyKeyText();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动屏幕构造函数中出现异常: {ex.Message}");
                MessageBox.Show($"启动屏幕初始化失败: {ex.Message}\n\n{ex.StackTrace}", "初始化错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreatePressAnyKeyText()
        {
            try
            {
                _pressAnyKeyText = new TextBlock
                {
                    Text = "加载中...",
                    FontSize = 18,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 20),
                    Opacity = 0 // 初始不可见
                };

                // 添加闪烁动画
                DoubleAnimation blinkAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0.4,
                    Duration = TimeSpan.FromSeconds(0.8),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                // 获取主Grid并添加文本
                if (this.Content is Grid mainGrid)
                {
                    Grid.SetRow(_pressAnyKeyText, mainGrid.RowDefinitions.Count - 1);
                    Grid.SetColumnSpan(_pressAnyKeyText, mainGrid.ColumnDefinitions.Count);
                    mainGrid.Children.Add(_pressAnyKeyText);
                    Debug.WriteLine("已添加按任意键继续提示文本");
                }
                else
                {
                    Debug.WriteLine("警告: 无法获取主Grid");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建按任意键提示文本时出错: {ex.Message}");
            }
        }

        private void AppSplashScreen_KeyDown(object sender, KeyEventArgs e)
        {
            // 如果加载完成且按任意键，则跳转到主界面
            if (_isLoadingCompleted)
            {
                Debug.WriteLine("检测到按键，跳转到主界面");
                e.Handled = true;
                FinishLoading();
            }
        }

        private void AppSplashScreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 如果加载完成且点击鼠标，则跳转到主界面
            if (_isLoadingCompleted)
            {
                Debug.WriteLine("检测到鼠标点击，跳转到主界面");
                e.Handled = true;
                FinishLoading();
            }
            else if (e.ButtonState == MouseButtonState.Pressed)
            {
                // 如果未加载完成，允许拖动窗口
                DragMove();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化粒子系统
            InitializeParticles();
            
            // 启动动画计时器
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();
            
            // 启动粒子计时器
            _particleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _particleTimer.Tick += ParticleTimer_Tick;
            _particleTimer.Start();
            
            // 启动加载进度计时器
            _loadingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _loadingTimer.Tick += LoadingTimer_Tick;
            _loadingTimer.Start();
            
            // 模拟异步加载进程
            SimulateLoading();
        }

        private async void SimulateLoading()
        {
            try
            {
                Debug.WriteLine("开始模拟加载过程...");
                
                // 优化加载阶段时间，提供更好的用户体验
                Debug.WriteLine("阶段1: 初始化系统核心组件...");
                await SimulateLoadingPhase(0, 15, "初始化系统核心组件...", 800);
                UpdateTextSafely(SystemInfoText, "系统核心组件已加载\n准备加载资源文件...");
                
                // 在阶段之间添加短暂延迟，使用户能看清每个阶段信息
                await Task.Delay(300);
                
                Debug.WriteLine("阶段2: 加载资源文件...");
                await SimulateLoadingPhase(15, 35, "加载资源文件...", 1000);
                UpdateTextSafely(SystemInfoText, "系统资源文件已加载\n准备加载物理引擎...");
                
                await Task.Delay(300);
                
                Debug.WriteLine("阶段3: 初始化物理引擎...");
                await SimulateLoadingPhase(35, 65, "初始化物理引擎...", 1200);
                UpdateTextSafely(SystemInfoText, "物理引擎已加载\n准备载入宇宙数据...");
                UpdateTextSafely(ExplorationInfoText, "物理参数校准中...\n重力场模拟引擎就绪");
                
                await Task.Delay(300);
                
                Debug.WriteLine("阶段4: 载入宇宙数据...");
                await SimulateLoadingPhase(65, 85, "载入宇宙数据...", 1000);
                UpdateTextSafely(SystemInfoText, "宇宙数据已加载\n进行最终系统检查...");
                UpdateTextSafely(ExplorationInfoText, "探测器校准完成\n坐标系统同步完成");
                
                await Task.Delay(300);
                
                Debug.WriteLine("阶段5: 完成最终准备...");
                await SimulateLoadingPhase(85, 100, "完成最终准备...", 800);
                UpdateTextSafely(SystemInfoText, "所有系统检查完毕\n准备就绪");
                UpdateTextSafely(ExplorationInfoText, "所有系统正常运行\n等待用户指令");
                
                // 完成加载后更新状态
                UpdateTextSafely(LoadingStatusText, "加载完成");
                UpdateTextSafely(StatusText, "系统准备就绪，点击任意键继续...");
                
                // 确保进度达到100%
                _currentProgress = 100;
                UpdateProgressSafely(100);

                // 显示"按任意键继续"提示
                ShowPressAnyKeyPrompt();
                
                // 标记加载已完成
                _isLoadingCompleted = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"模拟加载过程中出现异常: {ex.Message}");
                MessageBox.Show($"加载过程中出现错误: {ex.Message}\n\n{ex.StackTrace}", "加载错误", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 出错时也尝试完成加载
                try
                {
                    Debug.WriteLine("尝试强制完成加载过程...");
                    _isLoadingCompleted = true;
                    ShowPressAnyKeyPrompt();
                }
                catch (Exception innerEx)
                {
                    Debug.WriteLine($"强制完成加载失败: {innerEx.Message}");
                }
            }
        }

        private void ShowPressAnyKeyPrompt()
        {
            try
            {
                if (_pressAnyKeyText != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _pressAnyKeyText.Text = "按任意键继续...";
                        _pressAnyKeyText.Opacity = 1;

                        // 添加闪烁动画
                        DoubleAnimation blinkAnimation = new DoubleAnimation
                        {
                            From = 1,
                            To = 0.4,
                            Duration = TimeSpan.FromSeconds(0.8),
                            AutoReverse = true,
                            RepeatBehavior = RepeatBehavior.Forever
                        };

                        _pressAnyKeyText.BeginAnimation(TextBlock.OpacityProperty, blinkAnimation);
                        Debug.WriteLine("显示'按任意键继续'提示");
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"显示按任意键提示时出错: {ex.Message}");
            }
        }

        private async Task SimulateLoadingPhase(int startProgress, int endProgress, string message, int duration)
        {
            try
            {
                Debug.WriteLine($"开始加载阶段: {startProgress}% -> {endProgress}%, 消息: {message}");
                _targetProgress = endProgress;
                UpdateTextSafely(LoadingStatusText, message);
                UpdateTextSafely(StatusText, message);
                
                // 随机选择一个加载消息
                if (_loadingMessages.Count > 0)
                {
                    var randomMessage = _loadingMessages[_random.Next(_loadingMessages.Count)];
                    UpdateTextSafely(LoadingStatusText, randomMessage);
                    Debug.WriteLine($"随机加载消息: {randomMessage}");
                }
                
                // 逐步增加进度而不是等待计时器
                for (int i = startProgress + 1; i <= endProgress; i++)
                {
                    _currentProgress = i;
                    UpdateProgressSafely(i);
                    await Task.Delay(duration / (endProgress - startProgress + 1));
                }
                
                Debug.WriteLine($"阶段完成: 达到 {endProgress}%");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"模拟加载阶段中出现异常: {ex.Message}");
            }
        }

        private void FinishLoading()
        {
            try
            {
                // 关闭定时器
                if (_animationTimer != null && _animationTimer.IsEnabled)
                    _animationTimer.Stop();
                
                if (_particleTimer != null && _particleTimer.IsEnabled)
                    _particleTimer.Stop();
                
                if (_loadingTimer != null && _loadingTimer.IsEnabled)
                    _loadingTimer.Stop();
                
                Debug.WriteLine("所有定时器已停止");
                
                try
                {
                    // 关闭启动屏幕，显示主窗口
                    Debug.WriteLine("正在创建主窗口...");
                    
                    // 用分派器延迟执行主窗口创建和显示，确保UI线程不会过载
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            var mainWindow = new MainWindow();
                            Debug.WriteLine("主窗口已创建，准备显示");
                            
                            // 先显示主窗口，然后再关闭启动画面
                            mainWindow.Show();
                            Debug.WriteLine("主窗口已显示");
                            
                            // 延迟关闭启动窗口
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    Debug.WriteLine("正在关闭启动窗口");
                                    this.Close();
                                    Debug.WriteLine("启动窗口已关闭");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"关闭启动窗口时出错: {ex.Message}");
                                    MessageBox.Show($"关闭启动窗口时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }), DispatcherPriority.Background);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"创建或显示主窗口时出错: {ex.Message}");
                            MessageBox.Show($"创建或显示主窗口时出错: {ex.Message}\n\n详细信息: {ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }), DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"准备窗口转换时出错: {ex.Message}");
                    MessageBox.Show($"准备窗口转换时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FinishLoading方法中出现未处理的异常: {ex.Message}");
                MessageBox.Show($"应用加载过程中出现错误: {ex.Message}\n\n详细信息: {ex.StackTrace}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadingTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 更新加载进度显示
                if (_currentProgress < _targetProgress)
                {
                    _currentProgress += 1;
                    
                    if (LoadingProgressBar != null)
                    {
                        LoadingProgressBar.Value = _currentProgress;
                    }
                    
                    if (LoadingPercentText != null)
                    {
                        LoadingPercentText.Text = $"{_currentProgress}%";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新加载进度时出现异常: {ex.Message}");
            }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            // 更新3D模型旋转角度，添加脉动效果
            if (_sunRotationTransform != null)
            {
                _sunRotationTransform.Angle += _sunRotationSpeed;
                
                // 添加星体脉动效果
                ModelVisual3D? sunModel = this.FindName("SunModel") as ModelVisual3D;
                if (sunModel != null && sunModel.Content is GeometryModel3D sunGeometryModel)
                {
                    // 获取当前变换
                    Transform3DGroup? transformGroup = sunGeometryModel.Transform as Transform3DGroup;
                    if (transformGroup != null)
                    {
                        // 查找缩放变换
                        foreach (Transform3D transform in transformGroup.Children)
                        {
                            if (transform is ScaleTransform3D scaleTransform)
                            {
                                // 创建脉动效果 (0.95-1.05之间缓慢变化)
                                double pulseFactor = 1.0 + 0.05 * Math.Sin(_sunRotationTransform.Angle / 20.0);
                                scaleTransform.ScaleX = pulseFactor;
                                scaleTransform.ScaleY = pulseFactor;
                                scaleTransform.ScaleZ = pulseFactor;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ParticleTimer_Tick(object? sender, EventArgs e)
        {
            // 更新粒子位置
            UpdateParticles();
            
            // 创建新粒子
            if (_particles.Count < MaxParticles && _random.NextDouble() < 0.3)
            {
                AddParticle();
            }
        }

        private void InitializeGeometry()
        {
            // 尝试从 XAML 中获取 SunRotation
            _sunRotationTransform = this.FindName("SunRotation") as AxisAngleRotation3D;

            if (_sunRotationTransform == null)
            {
                System.Diagnostics.Debug.WriteLine("警告: 未找到 SunRotation 元素，创建新的旋转变换");
                _sunRotationTransform = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("成功找到 SunRotation 元素");
            }

            // 检查XAML中是否定义了SunMesh
            MeshGeometry3D? sunMesh = this.FindName("SunMesh") as MeshGeometry3D;
            if (sunMesh != null) 
            {
                System.Diagnostics.Debug.WriteLine("成功找到 SunMesh 元素，开始生成球体");
                try
                {
                    CreateSphere(sunMesh, 1.5, 48, 48); // 创建太阳，半径设为1.5，分段数增加到48x48
                    System.Diagnostics.Debug.WriteLine($"成功生成球体，顶点数: {sunMesh.Positions.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"生成球体时发生错误: {ex.Message}");
                    MessageBox.Show($"生成太阳球体时发生错误: {ex.Message}", "3D模型错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("警告: 未找到 SunMesh 元素");
                MessageBox.Show("无法找到SunMesh元素，太阳将不会显示", "3D模型错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 检查XAML中是否定义了SunModel及其材质
            ModelVisual3D? sunModel = this.FindName("SunModel") as ModelVisual3D;
            if (sunModel != null && sunModel.Content is GeometryModel3D sunGeometryModel)
            {
                System.Diagnostics.Debug.WriteLine("成功找到 SunModel 元素");
                
                // 确保材质是DiffuseMaterial并且可以设置ImageBrush
                DiffuseMaterial? diffuseMaterial = sunGeometryModel.Material as DiffuseMaterial;
                if (diffuseMaterial == null) // 如果材质不是DiffuseMaterial或者不存在，创建一个新的
                {
                    System.Diagnostics.Debug.WriteLine("SunModel没有DiffuseMaterial，创建新的");
                    diffuseMaterial = new DiffuseMaterial();
                    sunGeometryModel.Material = diffuseMaterial;
                }

                ImageBrush imageBrush = diffuseMaterial.Brush as ImageBrush;
                if (imageBrush == null) // 如果画刷不是ImageBrush或者不存在，创建一个新的
                {
                    System.Diagnostics.Debug.WriteLine("DiffuseMaterial没有ImageBrush，创建新的");
                    imageBrush = new ImageBrush();
                    diffuseMaterial.Brush = imageBrush;
                }

                try
                {
                    // 尝试几种不同的资源路径格式
                    try
                    {
                        // 首先尝试标准包路径格式
                        string resourcePath = "pack://application:,,,/StyleResources/Images/planets/sun_texture.jpg";
                        System.Diagnostics.Debug.WriteLine($"尝试加载纹理: {resourcePath}");
                        imageBrush.ImageSource = new BitmapImage(new Uri(resourcePath));
                        System.Diagnostics.Debug.WriteLine("成功加载太阳纹理");
                    }
                    catch (Exception ex1)
                    {
                        // 如果失败，尝试包含程序集名称的格式
                        System.Diagnostics.Debug.WriteLine($"第一种格式加载失败: {ex1.Message}，尝试第二种格式");
                        string resourcePath = "pack://application:,,,/HangKong_StarTrail;component/StyleResources/Images/planets/sun_texture.jpg";
                        System.Diagnostics.Debug.WriteLine($"尝试加载纹理: {resourcePath}");
                        imageBrush.ImageSource = new BitmapImage(new Uri(resourcePath));
                        System.Diagnostics.Debug.WriteLine("成功加载太阳纹理(第二种格式)");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载太阳纹理失败: {ex.Message}，使用备用颜色");
                    // 使用更明亮的橙色渐变，模拟太阳表面
                    RadialGradientBrush sunBrush = new RadialGradientBrush();
                    sunBrush.GradientOrigin = new Point(0.5, 0.5);
                    sunBrush.Center = new Point(0.5, 0.5);
                    sunBrush.RadiusX = 0.5;
                    sunBrush.RadiusY = 0.5;
                    sunBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.0));
                    sunBrush.GradientStops.Add(new GradientStop(Colors.Orange, 0.5));
                    sunBrush.GradientStops.Add(new GradientStop(Colors.OrangeRed, 1.0));
                    
                    diffuseMaterial.Brush = sunBrush;
                    
                    // 添加自发光效果，使太阳看起来更亮
                    MaterialGroup materialGroup = new MaterialGroup();
                    materialGroup.Children.Add(diffuseMaterial);
                    materialGroup.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(100, 255, 200, 50))));
                    sunGeometryModel.Material = materialGroup;
                    
                    MessageBox.Show($"无法加载太阳纹理，已使用备用颜色。\n错误详情: {ex.Message}", "纹理加载警告", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("警告: 未找到 SunModel 元素或其内容不是 GeometryModel3D");
                MessageBox.Show("无法找到SunModel元素或其内容不正确，太阳将不会显示", "3D模型错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CreateSphere(MeshGeometry3D mesh, double radius, int slices, int stacks)
        {
            mesh.Positions.Clear();
            mesh.TriangleIndices.Clear();
            mesh.TextureCoordinates.Clear();
            
            // 创建顶点
            for (int stack = 0; stack <= stacks; stack++)
            {
                double phi = Math.PI * stack / stacks;
                for (int slice = 0; slice <= slices; slice++)
                {
                    double theta = 2 * Math.PI * slice / slices;
                    
                    double x = radius * Math.Sin(phi) * Math.Cos(theta);
                    double y = radius * Math.Cos(phi);
                    double z = radius * Math.Sin(phi) * Math.Sin(theta);
                    
                    mesh.Positions.Add(new Point3D(x, y, z));
                    
                    double u = (double)slice / slices;
                    double v = (double)stack / stacks;
                    mesh.TextureCoordinates.Add(new Point(u, v));
                }
            }
            
            // 创建三角形
            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    int p1 = stack * (slices + 1) + slice;
                    int p2 = p1 + (slices + 1);
                    
                    mesh.TriangleIndices.Add(p1);
                    mesh.TriangleIndices.Add(p2);
                    mesh.TriangleIndices.Add(p1 + 1);
                    
                    mesh.TriangleIndices.Add(p1 + 1);
                    mesh.TriangleIndices.Add(p2);
                    mesh.TriangleIndices.Add(p2 + 1);
                }
            }
        }

        private void InitializeParticles()
        {
            // 初始创建一些粒子
            for (int i = 0; i < MaxParticles / 2; i++)
            {
                AddParticle();
            }
        }

        private void AddParticle()
        {
            // 创建新粒子
            double x = _random.NextDouble() * ParticleCanvas.ActualWidth;
            double y = _random.NextDouble() * ParticleCanvas.ActualHeight;
            double size = _random.NextDouble() * 2 + 0.5;
            
            byte alpha = (byte)(_random.Next(100, 200));
            Color color = Color.FromArgb(alpha, 255, 255, 255);
            
            Ellipse ellipse = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(color)
            };
            
            // 设置粒子位置
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);
            
            // 添加到粒子列表和Canvas中
            _particles.Add(new Particle 
            { 
                Element = ellipse, 
                VelocityX = (_random.NextDouble() - 0.5) * 0.5, 
                VelocityY = (_random.NextDouble() - 0.5) * 0.5, 
                Size = size,
                AlphaChange = _random.NextDouble() * 0.01 - 0.005
            });
            
            ParticleCanvas.Children.Add(ellipse);
        }

        private void UpdateParticles()
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle p = _particles[i];
                
                // 更新位置
                double x = Canvas.GetLeft(p.Element) + p.VelocityX;
                double y = Canvas.GetTop(p.Element) + p.VelocityY;
                
                // 更新透明度
                SolidColorBrush brush = p.Element.Fill as SolidColorBrush;
                if (brush != null)
                {
                    Color color = brush.Color;
                    double alpha = color.A / 255.0 + p.AlphaChange;
                    
                    if (alpha < 0.1 || alpha > 0.8)
                    {
                        p.AlphaChange = -p.AlphaChange;
                        alpha = Math.Clamp(alpha, 0.1, 0.8);
                    }
                    
                    color = Color.FromArgb((byte)(alpha * 255), color.R, color.G, color.B);
                    p.Element.Fill = new SolidColorBrush(color);
                }
                
                // 检查边界，如果粒子超出边界，则移除
                if (x < -10 || x > ParticleCanvas.ActualWidth + 10 || 
                    y < -10 || y > ParticleCanvas.ActualHeight + 10)
                {
                    ParticleCanvas.Children.Remove(p.Element);
                    _particles.RemoveAt(i);
                    continue;
                }
                
                Canvas.SetLeft(p.Element, x);
                Canvas.SetTop(p.Element, y);
            }
        }
        
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // 安全更新TextBlock文本的帮助方法
        private void UpdateTextSafely(TextBlock textBlock, string text)
        {
            try
            {
                if (textBlock != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            textBlock.Text = text;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"更新文本时出错: {ex.Message}");
                        }
                    }));
                }
                else
                {
                    Debug.WriteLine($"警告: 尝试更新不存在的TextBlock: {text}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新文本安全方法中出现异常: {ex.Message}");
            }
        }

        // 安全更新进度条的辅助方法
        private void UpdateProgressSafely(int progress)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (LoadingProgressBar != null)
                        {
                            LoadingProgressBar.Value = progress;
                            Debug.WriteLine($"进度条更新为: {progress}%");
                        }
                        
                        if (LoadingPercentText != null)
                        {
                            LoadingPercentText.Text = $"{progress}%";
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"更新进度条时出错: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"调用更新进度条方法时出错: {ex.Message}");
            }
        }

        // 粒子系统的粒子类
        public class Particle
        {
            public Ellipse Element { get; set; } = null!;
            public double VelocityX { get; set; }
            public double VelocityY { get; set; }
            public double Size { get; set; }
            public double AlphaChange { get; set; }
        }
    } 
} 