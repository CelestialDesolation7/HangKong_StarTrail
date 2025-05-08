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
        
        // 加载进度
        private int _currentProgress = 0;
        private int _targetProgress = 0;

        // 粒子系统
        private List<Particle> _particles = new List<Particle>();
        private const int MaxParticles = 100;

        // 3D动画控制
        private double _earthRotationSpeed = 0.2;
        private double _marsRotationSpeed = 0.15;
        private double _sunRotationSpeed = 0.05;
        private double _ringRotationSpeed = 0.1;

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
            InitializeComponent();
            
            // 初始化3D几何体
            InitializeGeometry();
            
            // 设置一个随机的太空引言
            QuoteText.Text = "\"" + _spaceQuotes[_random.Next(_spaceQuotes.Count)] + "\"";
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
            // 模拟各种加载阶段
            await SimulateLoadingPhase(0, 15, "初始化系统核心组件...", 1000);
            SystemInfoText.Text = "系统核心组件已加载\n准备加载资源文件...";
            
            await SimulateLoadingPhase(15, 35, "加载资源文件...", 1500);
            SystemInfoText.Text = "系统资源文件已加载\n准备加载物理引擎...";
            
            await SimulateLoadingPhase(35, 65, "初始化物理引擎...", 2000);
            SystemInfoText.Text = "物理引擎已加载\n准备载入宇宙数据...";
            ExplorationInfoText.Text = "物理参数校准中...\n重力场模拟引擎就绪";
            
            await SimulateLoadingPhase(65, 85, "载入宇宙数据...", 1500);
            SystemInfoText.Text = "宇宙数据已加载\n进行最终系统检查...";
            ExplorationInfoText.Text = "探测器校准完成\n坐标系统同步完成";
            
            await SimulateLoadingPhase(85, 100, "完成最终准备...", 1000);
            SystemInfoText.Text = "所有系统检查完毕\n准备就绪";
            ExplorationInfoText.Text = "所有系统正常运行\n等待用户指令";
            
            // 完成加载后更新状态
            LoadingStatusText.Text = "加载完成";
            StatusText.Text = "系统准备就绪，正在启动主界面...";
            
            // 等待一会儿后关闭启动屏幕，打开主界面
            await Task.Delay(1000);
            FinishLoading();
        }

        private async Task SimulateLoadingPhase(int startProgress, int endProgress, string message, int duration)
        {
            _targetProgress = endProgress;
            LoadingStatusText.Text = message;
            StatusText.Text = message;
            
            // 随机选择一个加载消息
            LoadingStatusText.Text = _loadingMessages[_random.Next(_loadingMessages.Count)];
            
            // 等待进度达到目标值
            await Task.Delay(duration);
        }

        private void FinishLoading()
        {
            // 关闭定时器
            _animationTimer.Stop();
            _particleTimer.Stop();
            _loadingTimer.Stop();
            
            // 关闭启动屏幕，显示主窗口
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void LoadingTimer_Tick(object? sender, EventArgs e)
        {
            // 更新加载进度显示
            if (_currentProgress < _targetProgress)
            {
                _currentProgress += 1;
                LoadingProgressBar.Value = _currentProgress;
                LoadingPercentText.Text = $"{_currentProgress}%";
            }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            // 更新3D模型旋转角度
            EarthRotation.Angle += _earthRotationSpeed;
            MarsRotation.Angle += _marsRotationSpeed;
            SunRotation.Angle += _sunRotationSpeed;
            RingRotation.Angle += _ringRotationSpeed;
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
            // 创建球体几何体用于地球模型
            SphereMesh.Positions = new Point3DCollection();
            SphereMesh.TriangleIndices = new Int32Collection();
            SphereMesh.TextureCoordinates = new PointCollection();
            
            CreateSphere(SphereMesh, 1, 32, 32);
            
            // 创建火星模型
            MarsMesh.Positions = new Point3DCollection();
            MarsMesh.TriangleIndices = new Int32Collection();
            MarsMesh.TextureCoordinates = new PointCollection();
            
            CreateSphere(MarsMesh, 1, 24, 24);
            
            // 创建环状几何体
            RingMesh.Positions = new Point3DCollection();
            RingMesh.TriangleIndices = new Int32Collection();
            RingMesh.TextureCoordinates = new PointCollection();
            
            CreateRing(RingMesh, 1.1, 1.5, 36);
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

        private void CreateRing(MeshGeometry3D mesh, double innerRadius, double outerRadius, int segments)
        {
            mesh.Positions.Clear();
            mesh.TriangleIndices.Clear();
            mesh.TextureCoordinates.Clear();
            
            // 创建顶点
            for (int i = 0; i <= segments; i++)
            {
                double theta = 2 * Math.PI * i / segments;
                double cosTheta = Math.Cos(theta);
                double sinTheta = Math.Sin(theta);
                
                double x1 = innerRadius * cosTheta;
                double z1 = innerRadius * sinTheta;
                double x2 = outerRadius * cosTheta;
                double z2 = outerRadius * sinTheta;
                
                mesh.Positions.Add(new Point3D(x1, 0, z1));
                mesh.Positions.Add(new Point3D(x2, 0, z2));
                
                double u = (double)i / segments;
                mesh.TextureCoordinates.Add(new Point(u, 0));
                mesh.TextureCoordinates.Add(new Point(u, 1));
            }
            
            // 创建三角形
            for (int i = 0; i < segments; i++)
            {
                int p1 = i * 2;
                int p2 = p1 + 1;
                int p3 = p1 + 2;
                int p4 = p1 + 3;
                
                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);
                
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p4);
                mesh.TriangleIndices.Add(p3);
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