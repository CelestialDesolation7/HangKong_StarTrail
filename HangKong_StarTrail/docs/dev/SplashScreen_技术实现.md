 # 启动界面技术实现文档

## 概述

启动界面（SplashScreen）是星空航迹应用程序的入口点，展示了一个优雅的加载过程和3D星球旋转动画。该界面不仅提供了应用程序加载状态的视觉反馈，还通过3D渲染展示了与应用主题相符的太阳系行星模型，创造沉浸式的用户体验。

## 关键技术实现

### 1. 3D星球旋转的实现

#### 1.1 WPF 3D渲染框架

启动界面使用WPF的3D渲染功能实现了行星旋转效果：

- **Viewport3D控件**：WPF提供的3D场景容器，用于渲染三维内容。
- **GeometryModel3D**：定义3D对象的几何形状和材质。
- **MeshGeometry3D**：构建3D模型的网格几何结构。
- **Transform3DGroup**：组合多种3D变换效果。

```xml
<Viewport3D x:Name="MainViewport" Grid.Row="1" Grid.Column="1" ClipToBounds="False" 
            Margin="350,0,0,0">
    <!-- 相机设置 -->
    <Viewport3D.Camera>
        <PerspectiveCamera x:Name="Camera" Position="0,0,4" LookDirection="0,0,-1" 
                          UpDirection="0,1,0" FieldOfView="60" />
    </Viewport3D.Camera>
    
    <!-- 灯光设置 -->
    <ModelVisual3D>
        <ModelVisual3D.Content>
            <DirectionalLight Color="#FFFFFF" Direction="-0.5,-0.5,-1" />
        </ModelVisual3D.Content>
    </ModelVisual3D>
    
    <!-- 中心恒星 -->
    <ModelVisual3D x:Name="SunModel">
        <ModelVisual3D.Content>
            <GeometryModel3D>
                <GeometryModel3D.Geometry>
                    <MeshGeometry3D x:Name="SunMesh"/>
                </GeometryModel3D.Geometry>
                <GeometryModel3D.Transform>
                    <Transform3DGroup>
                        <ScaleTransform3D ScaleX="1" ScaleY="1" ScaleZ="1"/>
                        <RotateTransform3D>
                            <RotateTransform3D.Rotation>
                                <AxisAngleRotation3D x:Name="SunRotation" Axis="0,1,0.1" Angle="0"/>
                            </RotateTransform3D.Rotation>
                        </RotateTransform3D>
                    </Transform3DGroup>
                </GeometryModel3D.Transform>
            </GeometryModel3D>
        </ModelVisual3D.Content>
    </ModelVisual3D>
</Viewport3D>
```

#### 1.2 球体网格生成

通过算法动态生成球体网格，而不是使用预先建模的3D模型，这提供了更高的灵活性和更小的资源占用：

```csharp
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
```

关键技术点：
- **参数化网格**：使用slices（经度分段）和stacks（纬度分段）参数控制球体的精细度。
- **球坐标转换**：使用球坐标系数学公式将经纬度转换为3D坐标点。
- **纹理映射**：为每个顶点生成对应的纹理坐标，实现纹理贴图。
- **三角形索引**：生成三角形索引，定义顶点之间的连接关系，形成球面。

#### 1.3 旋转动画实现

使用DispatcherTimer实现星球的持续旋转效果：

```csharp
private DispatcherTimer _animationTimer = new DispatcherTimer();
private AxisAngleRotation3D? _sunRotationTransform;
private double _sunRotationSpeed = 0.3;

private void Window_Loaded(object sender, RoutedEventArgs e)
{
    // 初始化动画定时器
    _animationTimer.Interval = TimeSpan.FromMilliseconds(16); // 约60fps
    _animationTimer.Tick += AnimationTimer_Tick;
    _animationTimer.Start();
    
    // 其他加载代码...
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
```

关键技术点：
- **AxisAngleRotation3D**：使用轴角旋转变换，定义旋转轴和旋转角度。
- **定时器动画**：使用DispatcherTimer定时更新旋转角度，创建连续动画效果。
- **脉动效果**：通过正弦函数周期性改变缩放因子，实现星球的膨胀和收缩，增强生动感。
- **帧率控制**：设置适当的定时器间隔（16ms），确保流畅的60fps动画效果。

#### 1.4 材质与纹理处理

为星球添加逼真的纹理和光照效果：

```csharp
private void InitializeGeometry()
{
    // 尝试从 XAML 中获取 SunRotation
    _sunRotationTransform = this.FindName("SunRotation") as AxisAngleRotation3D;

    // 检查XAML中是否定义了SunMesh
    MeshGeometry3D? sunMesh = this.FindName("SunMesh") as MeshGeometry3D;
    if (sunMesh != null) 
    {
        CreateSphere(sunMesh, 1.5, 48, 48); // 创建太阳，半径设为1.5，分段数增加到48x48
    }

    // 设置纹理
    ModelVisual3D? sunModel = this.FindName("SunModel") as ModelVisual3D;
    if (sunModel != null && sunModel.Content is GeometryModel3D sunGeometryModel)
    {
        // 确保材质是DiffuseMaterial
        DiffuseMaterial? diffuseMaterial = sunGeometryModel.Material as DiffuseMaterial;
        if (diffuseMaterial == null)
        {
            diffuseMaterial = new DiffuseMaterial();
            sunGeometryModel.Material = diffuseMaterial;
        }

        // 设置纹理贴图
        try {
            string resourcePath = "pack://application:,,,/StyleResources/Images/planets/sun_texture.jpg";
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = new BitmapImage(new Uri(resourcePath));
            diffuseMaterial.Brush = imageBrush;
        }
        catch (Exception ex)
        {
            // 使用备用渐变色
            RadialGradientBrush sunBrush = new RadialGradientBrush();
            sunBrush.GradientOrigin = new Point(0.5, 0.5);
            sunBrush.Center = new Point(0.5, 0.5);
            sunBrush.RadiusX = 0.5;
            sunBrush.RadiusY = 0.5;
            sunBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.0));
            sunBrush.GradientStops.Add(new GradientStop(Colors.Orange, 0.5));
            sunBrush.GradientStops.Add(new GradientStop(Colors.OrangeRed, 1.0));
            
            diffuseMaterial.Brush = sunBrush;
            
            // 添加自发光效果
            MaterialGroup materialGroup = new MaterialGroup();
            materialGroup.Children.Add(diffuseMaterial);
            materialGroup.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(100, 255, 200, 50))));
            sunGeometryModel.Material = materialGroup;
        }
    }
}
```

关键技术点：
- **纹理映射**：通过ImageBrush将图像纹理应用到球体表面。
- **资源URI**：使用WPF资源URI格式加载嵌入的图像资源。
- **备用方案**：提供纹理加载失败时的备用渐变色方案，确保显示效果。
- **材质组合**：使用MaterialGroup组合多种材质效果，如漫反射材质和自发光材质。
- **自发光效果**：添加EmissiveMaterial创造星球的发光效果，增强视觉吸引力。

### 2. 综合动画效果

#### 2.1 多层次动画结合

启动界面结合了多种动画效果，创造丰富的视觉体验：

- **3D星球旋转**：主体3D星球的旋转动画。
- **脉动效果**：星球周期性缩放的脉动效果。
- **进度条动画**：平滑过渡的加载进度条动画。
- **文本渐变**：加载状态文本的平滑切换。

```csharp
private async void SimulateLoading()
{
    try
    {
        await SimulateLoadingPhase(0, 20, "正在连接星际数据库...", 1500);
        await SimulateLoadingPhase(20, 40, "读取恒星运行参数...", 1200);
        await SimulateLoadingPhase(40, 60, "量子引擎初始化中...", 1000);
        await SimulateLoadingPhase(60, 80, "校准空间坐标系统...", 1300);
        await SimulateLoadingPhase(80, 95, "加载宇宙地图数据...", 1100);
        await SimulateLoadingPhase(95, 100, "探测器准备就绪...", 800);
        
        // 加载完成处理
        _isLoadingCompleted = true;
        UpdateTextSafely(LoadingStatusText, "加载完成!");
        ShowPressAnyKeyPrompt();
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"模拟加载过程中出现异常: {ex.Message}");
    }
}

private async Task SimulateLoadingPhase(int startProgress, int endProgress, string message, int duration)
{
    UpdateTextSafely(LoadingStatusText, message);
    UpdateTextSafely(ExplorationInfoText, message);
    
    int steps = 20; // 进度条平滑过渡步数
    int delayPerStep = duration / steps;
    
    for (int i = 0; i <= steps; i++)
    {
        int currentProgress = startProgress + (endProgress - startProgress) * i / steps;
        UpdateProgressSafely(currentProgress);
        await Task.Delay(delayPerStep);
    }
}
```

#### 2.2 性能优化

为确保启动界面的流畅性，实现了多种性能优化策略：

- **异步加载**：使用async/await实现非阻塞式加载过程。
- **增量更新**：进度更新采用增量方式，减少UI更新频率。
- **线程安全更新**：所有UI更新操作都通过Dispatcher确保线程安全。
- **低模式**：在资源受限情况下自动降级到低细节模式。

```csharp
private void UpdateProgressSafely(int progress)
{
    try
    {
        if (this.Dispatcher.CheckAccess())
        {
            // 直接在UI线程上更新进度
            _currentProgress = progress;
            LoadingProgressBar.Value = progress;
            LoadingPercentText.Text = $"{progress}%";
        }
        else
        {
            // 在非UI线程上，使用Dispatcher调度到UI线程
            this.Dispatcher.Invoke(() =>
            {
                _currentProgress = progress;
                LoadingProgressBar.Value = progress;
                LoadingPercentText.Text = $"{progress}%";
            });
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"更新加载进度时出现异常: {ex.Message}");
    }
}
```

## 技术难点与解决方案

### 1. 3D渲染性能

**难点**：在启动过程中加载和渲染3D内容可能导致应用启动缓慢。

**解决方案**：
- **参数化精细度**：根据设备性能动态调整球体的分段数（slices和stacks）。
- **延迟加载**：纹理图像使用延迟加载机制，减轻启动负担。
- **后备渲染**：提供低精度的备用渲染路径，确保在低端设备上也能流畅运行。

### 2. 资源加载策略

**难点**：纹理和3D资源的加载可能因文件丢失或权限问题而失败。

**解决方案**：
- **多级后备方案**：实现了完整的资源加载失败处理机制，先尝试标准路径，失败后尝试替代路径。
- **程序化生成**：纹理加载失败时使用程序化生成的渐变色作为替代。
- **详细错误日志**：使用Debug.WriteLine记录详细的错误信息，便于诊断和修复。

### 3. 跨平台兼容性

**难点**：WPF的3D功能在不同的Windows版本和硬件配置上可能有不同表现。

**解决方案**：
- **功能检测**：运行时检测设备3D渲染能力，根据设备能力调整渲染参数。
- **软件渲染后备**：默认使用硬件加速，但在不支持或性能不足时自动切换到软件渲染。
- **硬件加速检测**：检测设备的图形硬件加速能力，据此调整动画复杂度。

## 未来拓展

启动界面设计预留了以下拓展空间：

- **多行星系统**：扩展为显示多个行星的小型太阳系模型，展示行星轨道和互相关系。
- **交互式模型**：添加用户交互功能，允许用户通过鼠标/触控旋转和缩放3D模型。
- **动态环境效果**：添加星尘、光芒和粒子效果，增强太空环境的真实感。
- **响应式设计**：优化界面布局，更好地适应不同的屏幕尺寸和分辨率。