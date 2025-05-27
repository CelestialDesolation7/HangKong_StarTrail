# 启动界面 (AppSplashScreen) 技术实现文档

## 1. 概述

启动界面 (`AppSplashScreen`) 是用户启动“星穹轨道”应用程序时首先看到的界面。它承载了初始化应用程序资源、展示品牌信息、提供加载进度反馈以及营造沉浸式宇宙探索氛围的重要功能。本界面重点运用了WPF的3D渲染、动画、异步编程以及C#的事件处理和多线程技术。

## 2. 关键技术实现

### 2.1. 3D星体和背景渲染

为了营造引人入胜的宇宙探索氛围，启动界面集成了3D太阳模型和动态粒子背景。

- **WPF 3D渲染 (`System.Windows.Media.Media3D`)**:
    - **`Viewport3D`**: 作为承载3D场景的容器。
    - **`PerspectiveCamera`**: 定义了观察场景的视角和投影方式。
    - **`PointLight` / `DirectionalLight`**: 用于照亮3D模型，增强真实感。
    - **`GeometryModel3D`**: 用于展示3D几何体，如太阳模型。太阳模型通过自定义的 `CreateSphere` 方法生成球体网格 (`MeshGeometry3D`)。
    - **`MaterialGroup` / `DiffuseMaterial`**: 定义了3D模型的表面材质和纹理。太阳表面使用了图片纹理。
    - **`Transform3DGroup` / `RotateTransform3D` / `AxisAngleRotation3D`**: 用于实现太阳的自转动画。`AxisAngleRotation3D` 的 `Angle` 属性通过 `DispatcherTimer` 定期更新，形成平滑的旋转效果。

    ```csharp
    // 初始化3D几何体和相机
    private void InitializeGeometry()
    {
        try
        {
            // 创建太阳几何体
            MeshGeometry3D sunMesh = new MeshGeometry3D();
            // CreateSphere 方法接受半径、细分级别等参数生成球体顶点和索引
            CreateSphere(sunMesh, 0.5, 32, 32); 
            SunModel.Geometry = sunMesh; // SunModel 是XAML中定义的 GeometryModel3D

            // 设置太阳材质，例如加载一张太阳表面纹理图
            ImageBrush sunTexture = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/StyleResources/Images/sun_texture.jpg")));
            DiffuseMaterial sunMaterial = new DiffuseMaterial(sunTexture);
            SunModel.Material = sunMaterial;

            // 设置太阳自转动画
            _sunRotationTransform = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0); // 绕Y轴旋转
            RotateTransform3D rotateTransform = new RotateTransform3D(_sunRotationTransform);
            SunModel.Transform = rotateTransform;

            // 初始化相机位置和朝向
            PerspectiveCamera mainCamera = new PerspectiveCamera();
            mainCamera.Position = new Point3D(0, 0, 3); // 相机位置
            mainCamera.LookDirection = new Vector3D(0, 0, -1); // 看向Z轴负方向
            mainCamera.FieldOfView = 60; // 视野角度
            MainViewport.Camera = mainCamera; // MainViewport 是XAML中定义的 Viewport3D

            // 添加光源
            PointLight pointLight = new PointLight(Colors.White, new Point3D(0, 2, 3));
            ModelVisual3D lightVisual = new ModelVisual3D();
            lightVisual.Content = pointLight;
            MainViewport.Children.Add(lightVisual);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"初始化3D几何体时出错: {ex.Message}");
            // 实际项目中应记录更详细的日志或用户提示
        }
    }

    // 创建球体网格的方法 (简化示例)
    private void CreateSphere(MeshGeometry3D mesh, double radius, int slices, int stacks)
    {
        // ... (省略详细的顶点和索引计算逻辑) ...
        // 循环计算球面上每个点的坐标 (Positions)
        // 定义三角形面片连接这些顶点 (TriangleIndices)
        // 计算法线向量 (Normals) 和纹理坐标 (TextureCoordinates)
    }
    ```

- **自定义粒子系统**:
    - 通过在 `Canvas` 上动态创建和管理一系列表示星星的 `Ellipse` 或自定义形状的 `Shape` 对象。
    - 每个粒子具有随机生成的位置、大小、透明度和运动速度。
    - 使用 `DispatcherTimer` 定期更新粒子的状态（如位置、透明度），实现动态闪烁和漂移效果。
    - 利用C#的 `Random` 类生成随机参数，增强粒子效果的自然感。

    ```csharp
    private List<Ellipse> _particles = new List<Ellipse>();
    private Random _random = new Random();
    private DispatcherTimer _particleTimer = new DispatcherTimer();

    private void InitializeParticleSystem()
    {
        _particleTimer.Interval = TimeSpan.FromMilliseconds(30); // 约33 FPS
        _particleTimer.Tick += ParticleTimer_Tick;
        
        for (int i = 0; i < 100; i++) // 创建100个粒子
        {
            Ellipse particle = new Ellipse
            {
                Width = _random.Next(1, 4), // 随机大小
                Height = _random.Next(1, 4),
                Fill = new SolidColorBrush(Color.FromArgb((byte)_random.Next(100, 200), 255, 255, 255)), // 半透明白色
                Opacity = _random.NextDouble() // 随机初始透明度
            };
            Canvas.SetLeft(particle, _random.NextDouble() * ParticleCanvas.ActualWidth); // ParticleCanvas 是XAML中的Canvas
            Canvas.SetTop(particle, _random.NextDouble() * ParticleCanvas.ActualHeight);
            _particles.Add(particle);
            ParticleCanvas.Children.Add(particle);
        }
        _particleTimer.Start();
    }

    private void ParticleTimer_Tick(object? sender, EventArgs e)
    {
        foreach (var particle in _particles)
        {
            // 更新粒子位置 (例如缓慢漂移)
            Canvas.SetLeft(particle, Canvas.GetLeft(particle) + (_random.NextDouble() - 0.5) * 0.5);
            Canvas.SetTop(particle, Canvas.GetTop(particle) + (_random.NextDouble() - 0.5) * 0.5);

            // 随机改变透明度 (闪烁效果)
            particle.Opacity = Math.Clamp(particle.Opacity + (_random.NextDouble() - 0.5) * 0.1, 0.2, 1.0);

            // 边界处理: 如果粒子移出画布，重新随机放置
            if (Canvas.GetLeft(particle) < 0 || Canvas.GetLeft(particle) > ParticleCanvas.ActualWidth ||
                Canvas.GetTop(particle) < 0 || Canvas.GetTop(particle) > ParticleCanvas.ActualHeight)
            {
                Canvas.SetLeft(particle, _random.NextDouble() * ParticleCanvas.ActualWidth);
                Canvas.SetTop(particle, _random.NextDouble() * ParticleCanvas.ActualHeight);
            }
        }
    }
    ```

### 2.2. 异步加载与进度反馈

为了避免长时间的UI卡顿，应用程序的初始化过程（如资源加载、配置读取）采用异步方式执行。

- **`async` 和 `await` 关键字**:
    - `Window_Loaded` 事件处理器被标记为 `async void`。
    - 耗时的初始化任务封装在 `SimulateLoading()` 方法中，该方法使用 `async Task`。
    - 在 `SimulateLoading()` 中，使用 `await Task.Delay()` 模拟实际的加载过程，并分阶段更新加载进度。在实际应用中，这里会替换为真正的异步I/O操作或计算任务。
    - 通过 `Task.Run()` 可以将CPU密集型任务放到线程池中执行，避免阻塞UI线程。

    ```csharp
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // ... 其他初始化 ...
        InitializeParticleSystem(); // 初始化粒子系统
        
        // 启动3D太阳旋转动画
        _animationTimer.Interval = TimeSpan.FromMilliseconds(16); // 约60 FPS
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationTimer.Start();

        Debug.WriteLine("启动异步加载流程...");
        await SimulateLoading(); // 异步执行加载过程
    }
    
    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (_sunRotationTransform != null)
        {
            // 更新太阳旋转角度
            _sunRotationTransform.Angle = (_sunRotationTransform.Angle + _sunRotationSpeed) % 360;
        }
    }

    private async Task SimulateLoading()
    {
        try
        {
            // 模拟分阶段加载
            await SimulateLoadingPhase(0, 25, "正在连接星际数据库...", 1000);
            await SimulateLoadingPhase(25, 50, "读取恒星运行参数...", 1500);
            await SimulateLoadingPhase(50, 75, "加载宇宙地图数据...", 2000);
            await SimulateLoadingPhase(75, 100, "探测器准备就绪...", 1000);

            _isLoadingCompleted = true;
            Debug.WriteLine("所有加载阶段完成。");
            ShowPressAnyKeyPrompt();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载过程中发生错误: {ex.Message}");
            // 向用户显示错误信息或记录日志
            LoadingStatusText.Text = "加载失败，请重试。"; 
        }
    }

    // 模拟单个加载阶段
    private async Task SimulateLoadingPhase(int startProgress, int endProgress, string message, int duration)
    {
        Debug.WriteLine($"开始加载阶段: {message}");
        UpdateTextSafely(LoadingStatusText, message); // LoadingStatusText 是XAML中的TextBlock
        
        int steps = 20; // 将一个阶段的进度更新分成更小的步数
        int stepDuration = duration / steps;

        for (int i = 0; i < steps; i++)
        {
            _targetProgress = startProgress + (int)(((double)(i + 1) / steps) * (endProgress - startProgress));
            await Task.Delay(stepDuration); // 模拟实际工作
            // 可以在这里执行实际的加载子任务
        }
        _targetProgress = endProgress; // 确保达到目标进度
        Debug.WriteLine($"完成加载阶段: {message}");
    }
    ```

- **进度条更新 (`ProgressBar`)**:
    - 使用 `DispatcherTimer` (`_loadingTimer`) 定期平滑地更新 `ProgressBar` 的值，使其从 `_currentProgress` 趋近 `_targetProgress`。这避免了进度条的跳跃式更新，提升了视觉体验。
    - `_targetProgress` 在每个异步加载阶段完成后被设定。
    - 进度文本和加载信息也通过 `UpdateTextSafely` 方法在UI线程上安全更新。

    ```csharp
    private DispatcherTimer _loadingTimer = new DispatcherTimer();
    private int _currentProgress = 0;
    private int _targetProgress = 0;
    private bool _isLoadingCompleted = false;

    public AppSplashScreen()
    {
        InitializeComponent();
        // ... 其他初始化 ...

        _loadingTimer.Interval = TimeSpan.FromMilliseconds(50); // 控制进度条更新频率
        _loadingTimer.Tick += LoadingTimer_Tick;
        _loadingTimer.Start();
    }

    private void LoadingTimer_Tick(object? sender, EventArgs e)
    {
        // 平滑更新当前进度到目标进度
        if (_currentProgress < _targetProgress)
        {
            _currentProgress += Math.Max(1, (_targetProgress - _currentProgress) / 5); // 每次前进目标差值的1/5或至少1
            _currentProgress = Math.Min(_currentProgress, _targetProgress); // 不超过目标值
        }
        else if (_currentProgress > _targetProgress) // 理论上不应发生，但作为保险
        {
            _currentProgress = _targetProgress;
        }
        
        UpdateProgressSafely(_currentProgress);

        if (_isLoadingCompleted && _currentProgress >= 100)
        {
            _loadingTimer.Stop();
            // 可以在这里触发 "加载完成" 的视觉提示，例如渐变消失进度条
        }
    }

    // 安全更新UI元素的方法，确保在UI线程执行
    private void UpdateTextSafely(TextBlock textBlock, string text)
    {
        if (textBlock.Dispatcher.CheckAccess())
        {
            textBlock.Text = text;
        }
        else
        {
            textBlock.Dispatcher.Invoke(() => textBlock.Text = text);
        }
    }

    private void UpdateProgressSafely(int progress)
    {
        if (LoadingProgressBar.Dispatcher.CheckAccess()) // LoadingProgressBar 是XAML中的ProgressBar
        {
            LoadingProgressBar.Value = progress;
            ProgressText.Text = $"{progress}%"; // ProgressText 是显示百分比的TextBlock
        }
        else
        {
            LoadingProgressBar.Dispatcher.Invoke(() => 
            {
                LoadingProgressBar.Value = progress;
                ProgressText.Text = $"{progress}%";
            });
        }
    }
    ```

### 2.3. 用户交互与界面过渡

- **"按任意键继续"提示**:
    - 当 `_isLoadingCompleted` 为 `true` 且进度达到100%后，通过 `ShowPressAnyKeyPrompt()` 方法显示提示文本。
    - 该文本 (`_pressAnyKeyText`) 是动态创建的 `TextBlock`，并添加到主 `Grid` 中。
    - 应用了 `DoubleAnimation` 实现文本的呼吸灯/闪烁效果，吸引用户注意。

    ```csharp
    private TextBlock _pressAnyKeyText = new TextBlock();

    private void CreatePressAnyKeyText()
    {
        _pressAnyKeyText = new TextBlock
        {
            Text = "加载中...", // 初始文本，会被 ShowPressAnyKeyPrompt 更新
            FontSize = 18,
            Foreground = new SolidColorBrush(Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 50), //调整位置
            Opacity = 0 // 初始不可见
        };

        // 添加到主Grid的合适位置
        if (this.Content is Grid mainGrid)
        {
            // 假设Grid有至少两行，提示放在最后一行
            if (mainGrid.RowDefinitions.Count > 1)
            {
                 Grid.SetRow(_pressAnyKeyText, mainGrid.RowDefinitions.Count - 1);
            }
            Grid.SetColumnSpan(_pressAnyKeyText, Math.Max(1,mainGrid.ColumnDefinitions.Count)); // 跨越所有列
            mainGrid.Children.Add(_pressAnyKeyText);
        }
    }
    
    private void ShowPressAnyKeyPrompt()
    {
        if (_pressAnyKeyText != null)
        {
            _pressAnyKeyText.Text = "按任意键或点击继续";
            // 创建闪烁动画
            DoubleAnimation blinkAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.4,
                Duration = TimeSpan.FromSeconds(0.8),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            _pressAnyKeyText.BeginAnimation(TextBlock.OpacityProperty, blinkAnimation);
            _pressAnyKeyText.Opacity = 1; // 设置为可见，动画会接管透明度
        }
    }
    ```

- **键盘与鼠标事件处理**:
    - 监听窗口的 `KeyDown` 和 `MouseDown` 事件。
    - 当加载完成后 (`_isLoadingCompleted == true`)，任何按键或鼠标点击都会触发 `FinishLoading()` 方法。
    - `FinishLoading()` 负责创建并显示主窗口 (`MainWindow`)，然后关闭启动界面。使用了 `async` 确保平滑过渡。

    ```csharp
    private void AppSplashScreen_KeyDown(object sender, KeyEventArgs e)
    {
        if (_isLoadingCompleted && _currentProgress >= 100)
        {
            Debug.WriteLine("检测到按键，跳转到主界面");
            e.Handled = true; // 阻止事件进一步传播
            FinishLoading();
        }
    }

    private void AppSplashScreen_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_isLoadingCompleted && _currentProgress >= 100)
        {
             Debug.WriteLine("检测到鼠标点击，跳转到主界面");
             e.Handled = true;
             FinishLoading();
        }
        else if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Left)
        {
            // 如果未加载完成，允许拖动窗口 (如果启动界面是无边框窗口)
            // DragMove(); // 仅当 WindowStyle="None" AllowTransparency="True" 时有效
        }
    }

    private async void FinishLoading()
    {
        // 确保只执行一次
        if (this.IsVisible == false) return; 

        _isLoadingCompleted = false; // 防止重复触发

        // 可选：添加一个淡出动画
        DoubleAnimation fadeOutAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromSeconds(0.5)
        };
        fadeOutAnimation.Completed += (s, _) =>
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        };
        this.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
        
        // 如果不需要淡出，直接切换：
        // MainWindow mainWindow = new MainWindow();
        // mainWindow.Show();
        // this.Close();
    }
    ```

### 2.4. 窗口自定义与异常处理

- **自定义窗口样式**:
    - 通常启动界面会设置为无边框窗口 (`WindowStyle="None"`, `AllowsTransparency="True"`) 以实现完全自定义的外观。
    - 如果是无边框窗口，需要自行实现拖动 (`DragMove()`) 和关闭按钮的逻辑。本示例的启动界面假设使用了标准窗口框体，但若要自定义，则需要在 `TitleBar_MouseDown` 或类似事件中调用 `DragMove()`。
- **全局异常处理**:
    - 在 `AppSplashScreen` 的构造函数或 `App.xaml.cs` 中设置全局异常处理器，捕获未处理的异常，记录日志并向用户显示友好的错误信息，防止程序崩溃。
    - `AppDomain.CurrentDomain.UnhandledException`：捕获所有线程的未处理异常。
    - `Application.Current.DispatcherUnhandledException`：捕获UI线程的未处理异常。

    ```csharp
    // 在 AppSplashScreen 构造函数中或 App.xaml.cs 的 OnStartup 中添加
    public AppSplashScreen()
    {
        InitializeComponent();
        // ...
        // 设置全局未处理异常处理
        AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
        Application.Current.DispatcherUnhandledException += UIDispatcherUnhandledExceptionHandler;
        // ...
    }

    private void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception;
        string errorMessage = $"应用程序域未处理异常: {(ex?.Message)} \nIsTerminating: {e.IsTerminating}";
        Debug.WriteLine(errorMessage);
        // 记录到文件或事件查看器
        LogException(ex, "GlobalUnhandledException");
        // 尝试优雅关闭或提示用户
        MessageBox.Show($"发生严重错误，应用程序即将关闭: {ex?.Message}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void UIDispatcherUnhandledExceptionHandler(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        string errorMessage = $"UI线程未处理异常: {e.Exception.Message}";
        Debug.WriteLine(errorMessage);
        LogException(e.Exception, "UIDispatcherUnhandledException");
        MessageBox.Show($"发生UI错误: {e.Exception.Message}", "UI错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true; // 标记为已处理，尝试防止应用程序崩溃 (某些情况下可能无效)
    }

    private void LogException(Exception ex, string context)
    {
        // 实现日志记录逻辑，例如写入到文件
        // string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
        // File.AppendAllText(logPath, $"[{DateTime.Now}] [{context}] {ex?.ToString()}\n\n");
    }
    ```

## 3. C# 语言特性与 .NET 框架应用总结

- **异步编程 (`async/await`)**: 显著提升了用户体验，通过将耗时操作异步化，避免了UI冻结。
- **`DispatcherTimer`**: 用于创建平滑的动画效果（太阳旋转、粒子系统、进度条更新）。
- **LINQ (轻度使用)**: 在更复杂的粒子管理或数据筛选场景中，LINQ可以简化集合操作。
- **事件与委托**: 大量用于响应用户输入 (`KeyDown`, `MouseDown`) 和定时器事件 (`Tick`)。
- **面向对象 (OOP)**: `AppSplashScreen` 本身是一个类，封装了启动界面的所有逻辑和状态。3D对象和粒子也可以被设计为独立的类以实现更好的管理。
- **WPF特性**:
    - **数据绑定**: 虽然在本界面中直接操作UI元素较多，但在复杂场景下，可以将加载状态、进度等通过ViewModel绑定到UI。
    - **XAML**: 定义了界面的基本结构和静态元素。
    - **`System.Windows.Media.Media3D`**: 核心的3D渲染能力。
    - **动画类**: `DoubleAnimation` 用于实现透明度动画。
- **异常处理**: 通过全局异常处理器增强了应用的健壮性。
- **`System.Diagnostics.Debug`**: 用于在开发阶段输出调试信息。
- **集合类**: `List<Ellipse>` 用于管理粒子。

## 4. 未来拓展与优化方向

- **更精细的粒子系统**: 引入粒子生命周期、更复杂的运动轨迹（如受引力影响）、粒子纹理等。
- **高级3D效果**: 使用着色器 (Shader Effects) 实现更炫酷的视觉效果。
- **MVVM模式应用**: 将启动逻辑（如加载任务列表、进度状态）封装到ViewModel中，使代码结构更清晰，更易于测试。
- **单元测试/集成测试**: 对加载逻辑和关键功能进行测试。
- **国际化与本地化**: 如果需要支持多语言，加载信息和提示文本需要进行本地化处理。

This concludes the technical implementation details for the AppSplashScreen.