 # 重力模拟窗体技术实现文档

## 概述

重力模拟窗体（GravitySimulationForm）是星空航迹应用程序的核心功能模块之一，提供了太阳系天体运动的物理仿真功能。该窗体实现了基于牛顿万有引力定律的天体运动模拟，支持实时交互、缩放平移、参数调整等功能，为用户提供了探索太阳系天体运动规律的沉浸式体验。

## 关键技术实现

### 1. 物理引擎集成

重力模拟窗体通过整合自定义物理引擎实现天体运动模拟：

- **PhysicsEngine类**：负责基于牛顿万有引力定律计算天体间的引力并更新天体位置。
- **Body类**：表示天体，包含质量、位置、速度等物理属性。
- **Vector2D结构**：提供二维向量运算支持，用于位置和速度的表示与计算。

```csharp
// 物理引擎核心计算逻辑
private void UpdatePhysics(double timeStep, bool timeReversed)
{
    // 计算所有天体对彼此的引力
    foreach (var body1 in Bodies)
    {
        foreach (var body2 in Bodies)
        {
            if (body1 != body2)
            {
                // 计算引力方向和大小
                Vector2D direction = body2.Position - body1.Position;
                double distance = direction.Length();
                
                // 避免距离过近导致的数值不稳定
                if (distance < 1.0) distance = 1.0;
                
                // 牛顿万有引力计算
                double forceMagnitude = (G * body1.Mass * body2.Mass) / (distance * distance);
                Vector2D force = direction.Normalize() * forceMagnitude;
                
                // 应用力导致的加速度
                body1.Acceleration += force / body1.Mass;
            }
        }
    }
    
    // 更新天体位置和速度
    foreach (var body in Bodies)
    {
        // 时间可逆
        double effectiveTimeStep = timeReversed ? -timeStep : timeStep;
        
        // 更新位置和速度（半隐式欧拉积分法）
        body.Velocity += body.Acceleration * effectiveTimeStep;
        body.Position += body.Velocity * effectiveTimeStep;
        body.Acceleration = Vector2D.ZeroVector; // 重置加速度
    }
    
    // 更新模拟时间
    timeElapsed += timeReversed ? -timeStep : timeStep;
}
```

### 2. 多线程渲染架构

为了保证UI响应性和高帧率渲染，实现了多线程渲染架构：

- **UI线程**：负责用户交互和UI更新。
- **物理计算线程**：独立线程执行物理模拟计算，避免阻塞UI线程。
- **线程同步**：通过锁机制（_physicsLock和_renderLock）确保数据一致性。

```csharp
private void SimulationTimer_Tick(object sender, EventArgs e)
{
    if (_isSimulationRunning && !_isFrameRendering)
    {
        // 启动物理计算任务
        if (_physicsUpdateTask == null || _physicsUpdateTask.IsCompleted)
        {
            _physicsUpdateCts = new CancellationTokenSource();
            _physicsUpdateTask = Task.Run(() =>
            {
                try
                {
                    lock (_physicsLock)
                    {
                        // 执行物理计算
                        _renderer._physicsEngine.UpdatePhysics(_timeStep, _isTimeReversed);
                    }
                }
                catch (Exception ex)
                {
                    // 记录异常
                    Console.WriteLine($"物理更新错误: {ex.Message}");
                }
            }, _physicsUpdateCts.Token);
        }
        
        // 更新UI显示
        UpdateDisplayPositions();
    }
}
```

### 3. 高性能渲染

采用SkiaSharp图形库实现高性能渲染：

- **SKCanvas**：使用SkiaSharp提供的画布进行2D绘图。
- **双缓冲渲染**：避免屏幕闪烁，提供流畅的视觉体验。
- **渲染优化**：实现视口裁剪，只渲染可见区域内的天体。

```csharp
private void RenderOneFrame(SKCanvas canvas)
{
    try
    {
        lock (_renderLock)
        {
            // 清空画布
            canvas.Clear(SKColors.Black);
            
            // 获取当前视口信息
            double centerX = animationCanva.ActualWidth / 2 + _cameraOffset.X;
            double centerY = animationCanva.ActualHeight / 2 + _cameraOffset.Y;
            
            // 渲染所有天体
            foreach (var body in _renderer._physicsEngine.Bodies)
            {
                // 坐标转换：物理坐标系 -> 屏幕坐标系
                Vector2D screenPos = PhysicalToScreenPosition(body.Position);
                
                // 绘制天体
                float radius = (float)Math.Max(5, body.Radius * _pixelToDistanceRatio);
                var paint = new SKPaint
                {
                    Color = SKColor.Parse(body.Color),
                    IsAntialias = true
                };
                
                canvas.DrawCircle(
                    (float)screenPos.X,
                    (float)screenPos.Y,
                    radius,
                    paint
                );
                
                // 绘制天体名称
                // ...
                
                // 如果启用速度可视化，绘制速度矢量
                if (_isVelocityVisualizeMode)
                {
                    // ...
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"渲染错误: {ex.Message}");
    }
}
```

### 4. 相机控制系统

实现了灵活的相机控制系统，支持缩放、平移和聚焦功能：

- **缩放功能**：通过鼠标滚轮控制缩放比例。
- **平移功能**：通过鼠标中键拖动实现画布平移。
- **聚焦功能**：支持聚焦到特定天体，相机自动跟随天体运动。

```csharp
private void AnimationCanva_MouseWheel(object sender, MouseWheelEventArgs e)
{
    // 计算新的缩放因子
    double zoomDelta = e.Delta > 0 ? 1.1 : 0.9;
    _zoomFactor *= zoomDelta;
    
    // 限制缩放范围
    _zoomFactor = Math.Max(0.1, Math.Min(_zoomFactor, 100.0));
    
    // 更新实际比例尺
    _pixelToDistanceRatio = _recommendedScale * _zoomFactor;
    
    // 更新显示
    UpdateDisplayPositions();
    animationCanva.InvalidateVisual();
}
```

### 5. 时间控制系统

实现了灵活的时间控制系统，支持暂停、继续、加速和时间反转：

- **时间步长控制**：通过滑块调整物理模拟的时间步长。
- **暂停/继续**：控制物理模拟的暂停和继续。
- **时间反转**：支持时间倒流，观察天体逆向运动。

```csharp
private void SimulationTimeStepSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
{
    // 根据滑块值调整时间步长
    double sliderValue = SimulationTimeStepSlider.Value;
    double factor;
    
    if (sliderValue < SLIDER_CENTER_VALUE)
    {
        // 减速：0.1x - 1.0x
        factor = 0.1 + 0.9 * (sliderValue / SLIDER_CENTER_VALUE);
    }
    else
    {
        // 加速：1.0x - 10.0x
        factor = 1.0 + 9.0 * ((sliderValue - SLIDER_CENTER_VALUE) / SLIDER_CENTER_VALUE);
    }
    
    // 应用新的时间步长
    _timeStep = _recommendedTimeStep * factor;
    _renderer.SetTimeStep(_timeStep);
    
    // 更新UI显示
    TimeStepValueText.Text = $"时间步长: {_timeStep:F2}s";
}
```

### 6. 场景导入/导出

支持场景配置的保存和加载功能：

- **场景导出**：将当前天体系统配置导出为JSON文件。
- **场景导入**：从JSON文件加载天体系统配置。
- **数据库集成**：利用SQLite存储和管理场景配置。

```csharp
private void ExportSceneButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // 创建场景数据结构
        var sceneData = new SceneData
        {
            Bodies = _renderer._physicsEngine.Bodies.Select(b => new BodyData
            {
                Name = b.Name,
                Mass = b.Mass,
                Radius = b.Radius,
                Position = new Vector2DData { X = b.Position.X, Y = b.Position.Y },
                Velocity = new Vector2DData { X = b.Velocity.X, Y = b.Velocity.Y },
                Color = b.Color,
                IsCenter = b.IsCenter
            }).ToList(),
            TimeElapsed = _renderer._physicsEngine.timeElapsed
        };
        
        // 序列化为JSON
        string json = System.Text.Json.JsonSerializer.Serialize(sceneData, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        // 保存到文件
        string path = "Data/SavedScenes/scene.json";
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        System.IO.File.WriteAllText(path, json);
        
        MessageBox.Show("场景已成功导出", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"导出场景失败: {ex.Message}", "导出错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

## 界面设计

重力模拟窗体采用了以下界面设计策略：

- **动态画布**：中央区域是天体运动的动态画布，支持交互操作。
- **控制面板**：顶部和右侧区域提供各种控制按钮和参数调整功能。
- **信息显示**：底部区域显示当前模拟状态、帧率、物理参数等信息。
- **自适应布局**：界面元素会随窗口大小调整位置和大小。

## 性能优化

- **渲染优化**：
  - 双缓冲技术避免闪烁
  - 裁剪算法减少不必要的绘制
  - 动态调整渲染细节级别
- **计算优化**：
  - 多线程分离UI与物理计算
  - 自适应时间步长，平衡精度与性能
  - 优化引力计算，减少不必要的计算
- **内存管理**：
  - 对象池技术减少GC压力
  - 缓存临时计算结果避免重复计算

## 调试功能

重力模拟窗体实现了专门的调试功能，方便开发和测试：

- **调试窗口**：通过独立窗口显示详细的调试信息。
- **性能监控**：实时监控和显示帧率、物理更新时间等性能指标。
- **条件编译**：使用#if DEBUG指令隔离调试代码，避免影响发布版本性能。

## 未来拓展

重力模拟窗体设计预留了以下拓展空间：

- **更复杂的物理模型**：支持相对论效应、非点质量模型等高级物理特性。
- **碰撞检测**：实现天体碰撞与合并功能。
- **轨道预测**：添加轨道预测和可视化功能。
- **3D渲染**：扩展为3D天体运动模拟系统。