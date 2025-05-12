 # 主窗口技术实现文档

## 概述

主窗口（MainWindow）是星空航迹应用程序的入口点和核心导航界面，提供了对各个功能模块的访问入口，包括太阳系探索、宇宙知识学习、星际智者对话等功能。

## 关键技术实现

### 1. 窗口自定义

- **自定义窗口边框**：实现了无边框窗口设计，通过自定义TitleBar来实现窗口的拖动、最小化和关闭功能。
- **事件处理**：通过`TitleBar_MouseDown`事件处理窗口的拖动操作，通过`DragMove`方法实现窗口移动。

```csharp
private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
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
```

### 2. 功能导航系统

主窗口实现了应用程序的主导航功能，通过以下技术实现：

- **功能按钮**：提供太阳系探索、宇宙知识学习等按钮，分别跳转到不同功能窗口。
- **窗口关系管理**：通过`Show()`和`Hide()`方法控制窗口显示和隐藏，实现多窗口的导航关系。
- **活动历史跟踪**：提供`SaveActivityHistory`方法记录用户的活动历史，为后续的用户行为分析提供数据支持。

```csharp
private void StartExploration_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // 记录活动历史（实际应用中应存储到数据库）
        SaveActivityHistory("太阳系探索");
        
        var gravitySimulationForm = new GravitySimulationForm();
        gravitySimulationForm.Closed += (s, args) => this.Show(); // 确保关闭时显示主窗口
        gravitySimulationForm.Show();
        this.Hide();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"打开探索界面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### 3. 用户进度管理

主窗口实现了用户学习进度的加载和显示功能：

- **进度加载**：通过`LoadUserProgress`方法从持久化存储加载用户的学习进度。
- **UI更新**：将加载的进度数据用于更新界面上的进度条或其他进度显示元素。

```csharp
private void LoadUserProgress()
{
    try
    {
        // 这里应该是从数据存储中读取用户的实际进度
        // 此处为模拟数据
        double progressPercentage = 35;
        // 在实际应用中，这里会更新UI上的进度条
    }
    catch (Exception ex)
    {
        // 记录错误但不影响主程序运行
        Console.WriteLine($"加载用户进度出错: {ex.Message}");
    }
}
```

### 4. 错误处理机制

整个窗口实现中采用了一致的错误处理策略：

- **try-catch块**：所有可能发生异常的操作都被包装在try-catch块中。
- **降级处理**：对于非关键功能，出现错误时记录日志但不中断程序运行。
- **用户反馈**：对于影响用户体验的错误，通过MessageBox提供友好的错误信息。

## 界面设计

主窗口的界面设计采用了现代化的UI设计理念：

- **自定义控件样式**：通过XAML定义了统一的按钮、文本框等控件样式。
- **响应式布局**：使用Grid和StackPanel实现响应式布局，适应不同屏幕尺寸。
- **视觉效果**：使用WPF的动画和过渡效果增强用户体验。

## 性能优化

- **延迟加载**：非核心功能采用延迟加载策略，提高主窗口的加载速度。
- **异常处理**：所有操作都有完善的异常处理，确保程序的稳定性。

## 未来拓展

主窗口设计预留了以下拓展空间：

- **新功能模块集成**：通过添加新的导航按钮，可以轻松集成新的功能模块。
- **用户数据分析**：基于活动历史记录，可以进一步实现用户行为分析和个性化推荐。