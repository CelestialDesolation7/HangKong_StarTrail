using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System;
using System.Collections.Generic;

namespace HangKong_StarTrail.Views;

public partial class MainWindow : Window
{
    // 3D交互变量
    private Point lastMousePos;
    private bool isMouseDown = false;
    private double rotationSpeed = 1.0;
    private double zoomSpeed = 0.2;
    private double minZoom = 2.0;
    private double maxZoom = 10.0;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // 加载后初始化3D场景
        this.Loaded += (s, e) => Initialize3DScene();
    }
    
    /// <summary>
    /// 初始化3D场景，创建球体网格
    /// </summary>
    private void Initialize3DScene()
    {
        // 创建太阳球体
        CreateSphere(sunMesh, 1.0, 32, 32);
        
        // 创建光晕球体（稍大一些）
        CreateSphere(glowMesh, 1.2, 32, 32);
        
        // 启动自动旋转动画
        StartAutoRotation();
    }
    
    /// <summary>
    /// 创建球体网格
    /// </summary>
    /// <param name="mesh">目标MeshGeometry3D对象</param>
    /// <param name="radius">球体半径</param>
    /// <param name="slices">水平切片数</param>
    /// <param name="stacks">垂直堆栈数</param>
    private void CreateSphere(MeshGeometry3D mesh, double radius, int slices, int stacks)
    {
        mesh.Positions.Clear();
        mesh.TriangleIndices.Clear();
        mesh.TextureCoordinates.Clear();
        
        // 生成顶点
        for (int stack = 0; stack <= stacks; stack++)
        {
            double phi = Math.PI * stack / stacks;
            double y = radius * Math.Cos(phi);
            double stackRadius = radius * Math.Sin(phi);
            
            for (int slice = 0; slice <= slices; slice++)
            {
                double theta = 2 * Math.PI * slice / slices;
                double x = stackRadius * Math.Cos(theta);
                double z = stackRadius * Math.Sin(theta);
                
                Vector3D normal = new Vector3D(x, y, z);
                normal.Normalize();
                
                mesh.Positions.Add(new Point3D(x, y, z));
                
                // 纹理坐标
                mesh.TextureCoordinates.Add(new Point(
                    (double)slice / slices,
                    (double)stack / stacks));
            }
        }
        
        // 生成三角形索引
        for (int stack = 0; stack < stacks; stack++)
        {
            int stackStart = stack * (slices + 1);
            int nextStackStart = (stack + 1) * (slices + 1);
            
            for (int slice = 0; slice < slices; slice++)
            {
                if (stack != 0)
                {
                    mesh.TriangleIndices.Add(stackStart + slice);
                    mesh.TriangleIndices.Add(stackStart + slice + 1);
                    mesh.TriangleIndices.Add(nextStackStart + slice);
                }
                
                if (stack != stacks - 1)
                {
                    mesh.TriangleIndices.Add(nextStackStart + slice);
                    mesh.TriangleIndices.Add(stackStart + slice + 1);
                    mesh.TriangleIndices.Add(nextStackStart + slice + 1);
                }
            }
        }
    }
    
    /// <summary>
    /// 启动自动旋转动画
    /// </summary>
    private void StartAutoRotation()
    {
        DoubleAnimation rotYAnimation = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(60),
            RepeatBehavior = RepeatBehavior.Forever
        };
        
        sunRotationY.BeginAnimation(AxisAngleRotation3D.AngleProperty, rotYAnimation);
    }
    
    #region 3D视图鼠标交互
    
    /// <summary>
    /// 处理鼠标按下事件
    /// </summary>
    private void Viewport3D_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            lastMousePos = e.GetPosition(mainViewport);
            isMouseDown = true;
            
            // 停止自动旋转
            sunRotationX.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);
            sunRotationY.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);
            
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// 处理鼠标移动事件
    /// </summary>
    private void Viewport3D_MouseMove(object sender, MouseEventArgs e)
    {
        if (isMouseDown)
        {
            Point currentPos = e.GetPosition(mainViewport);
            
            // 计算鼠标移动距离
            double deltaX = currentPos.X - lastMousePos.X;
            double deltaY = currentPos.Y - lastMousePos.Y;
            
            // 更新旋转角度
            sunRotationY.Angle = (sunRotationY.Angle + deltaX * rotationSpeed) % 360;
            sunRotationX.Angle = Math.Max(-80, Math.Min(80, sunRotationX.Angle - deltaY * rotationSpeed));
            
            lastMousePos = currentPos;
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// 处理鼠标释放事件
    /// </summary>
    private void Viewport3D_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            isMouseDown = false;
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// 处理鼠标滚轮事件，实现缩放
    /// </summary>
    private void Viewport3D_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 计算新的Z坐标
        double zDelta = -e.Delta * zoomSpeed / 120;
        double newZ = camera.Position.Z + zDelta;
        
        // 限制缩放范围
        newZ = Math.Max(minZoom, Math.Min(maxZoom, newZ));
        
        // 更新相机位置
        camera.Position = new Point3D(camera.Position.X, camera.Position.Y, newZ);
        
        e.Handled = true;
    }
    
    #endregion

    #region 窗口控制

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    #endregion

    #region 功能按钮事件

    private void StartExploration_Click(object sender, RoutedEventArgs e)
    {
        var gravitySimulationForm = new GravitySimulationForm();
        gravitySimulationForm.Closed += (s, args) => this.Show(); // 确保关闭时显示主窗口
        gravitySimulationForm.Show();
        this.Hide();
    }

    private void StartLearning_Click(object sender, RoutedEventArgs e)
    {
        var knowledgeBaseForm = new KnowledgeBaseForm();
        knowledgeBaseForm.Show();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void OpenStarTrail_Click(object sender, RoutedEventArgs e)
    {
        // 打开恒星轨迹界面
    }

    private void OpenGalaxy_Click(object sender, RoutedEventArgs e)
    {
        // 打开星系结构界面
    }

    private void OpenKnowledgeBase_Click(object sender, RoutedEventArgs e)
    {
        var knowledgeBaseForm = new KnowledgeBaseForm();
        knowledgeBaseForm.Show();
    }

    #endregion
}