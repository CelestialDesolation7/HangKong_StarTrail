# 主界面 (MainWindow) 技术实现文档

## 1. 概述

主界面 (`MainWindow`) 是“星穹轨道”应用程序的核心交互中枢，用户通过此界面访问系统的各项主要功能，如星系探索、知识学习、AI助手等。本界面采用了现代化的UI设计，注重用户体验和功能的易用性。关键技术包括WPF的布局系统、样式与模板、数据绑定、命令、自定义控件以及C#的事件处理和面向对象设计原则。

## 2. 关键技术实现

### 2.1. 界面布局与自定义样式

- **现代化UI设计**:
    - **无边框窗口**: 通过设置 `WindowStyle="None"` 和 `AllowsTransparency="True"` 实现无边框效果，配合自定义标题栏，提升了应用的现代感。
    - **亚克力/模糊背景 (可选)**: 可以通过引入第三方库或自定义着色器效果实现类似Fluent Design的背景模糊效果，增强视觉层次感。
    - **星空主题**: 整体色调、图标和背景图均围绕星空和宇宙主题设计，与应用内容高度统一。

- **WPF布局系统**:
    - 主要使用 `Grid` 进行整体布局划分，确保界面的灵活性和响应式。
    - `StackPanel` 和 `DockPanel` 用于组织局部区域的元素，如功能按钮列表、标题栏元素等。
    - 利用 `Margin` 和 `Padding` 精确控制元素间距和内部空间。

- **样式 (Styles) 与模板 (ControlTemplates)**:
    - **统一样式**: 为常用控件（如 `Button`, `TextBlock`, `Border`）定义了全局或局部样式资源 (`ResourceDictionary`)，确保视觉风格的一致性。例如，按钮样式定义了其背景色、前景色、圆角、鼠标悬停效果、按下效果等。
    - **控件模板**: 对部分控件（如 `Button`, `Window` 的标题栏部分）进行了模板重定义，以实现完全自定义的外观和行为。例如，自定义标题栏的最小化、最大化/还原、关闭按钮是通过重写 `Button` 模板并绑定相应命令实现的。

    ```xml
    <!-- 在 App.xaml 或 MainWindow.Resources 中定义样式 -->
    <Style TargetType="Button" x:Key="NavigationButton">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="10,5"/>
        <Setter Property="Margin" Value="5"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}" 
                            CornerRadius="5" 
                            BorderBrush="{TemplateBinding BorderBrush}" 
                            BorderThickness="{TemplateBinding BorderThickness}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="#33FFFFFF"/> <!-- 半透明高亮 -->
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter Property="Background" Value="#66FFFFFF"/> <!-- 更亮的高亮 -->
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 自定义窗口标题栏按钮样式 -->
    <Style TargetType="Button" x:Key="TitleBarButton">
        <Setter Property="Width" Value="30"/>
        <Setter Property="Height" Value="30"/>
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="VerticalAlignment" Value="Center"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}">
                        <TextBlock Text="{TemplateBinding Content}" 
                                   FontFamily="Segoe MDL2 Assets" 
                                   FontSize="10"
                                   HorizontalAlignment="Center" 
                                   VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="#555555"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter Property="Background" Value="#777777"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    ```

### 2.2. 自定义标题栏与窗口控制

- **拖动窗口**: 在自定义标题栏的 `Border` 或 `Grid` 元素上附加 `MouseDown` 事件处理器，在处理器中调用 `this.DragMove()` 方法实现窗口拖动。
- **窗口控制按钮**: 
    - 最小化: `this.WindowState = WindowState.Minimized;`
    - 最大化/还原: `this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;`
    - 关闭: `Application.Current.Shutdown();` 或 `this.Close();`
    - 这些操作通常封装在 `ICommand` 实现中，并通过数据绑定到按钮的 `Command` 属性，或者直接在按钮的 `Click` 事件处理器中调用。

    ```csharp
    // 在 MainWindow.xaml.cs 中
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                // 检查是否在可拖动区域内，例如排除按钮区域
                if (e.OriginalSource is Border || e.OriginalSource is Grid || e.OriginalSource is TextBlock)
                {
                     this.DragMove();
                }
            }
        }
        catch (InvalidOperationException) 
        { 
            // DragMove 只能在主鼠标按钮按下时调用，这里捕获可能的异常
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"窗口拖动时出错: {ex.Message}");
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        // 可能需要更新最大化/还原按钮的图标
        // MaximizeRestoreButton.Content = (this.WindowState == WindowState.Maximized) ? "\uE923" : "\uE922"; // Segoe MDL2 Assets 图标
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
    ```

### 2.3. 功能模块导航

主界面通过一系列功能按钮导航到不同的视图或窗口。

- **事件驱动导航**: 每个功能按钮的 `Click` 事件处理器负责实例化并显示对应的窗口或用户控件。
    - `GravitySimulationForm`, `KnowledgeBaseForm`, `ChatWindow` 等都是独立的 `Window` 类。
    - 点击按钮时，创建相应窗口的实例并调用 `Show()` 或 `ShowDialog()`。
- **窗口管理**: 
    - `Show()`: 打开非模态窗口，用户可以与父窗口和其他窗口交互。
    - `ShowDialog()`: 打开模态窗口，用户必须先关闭此窗口才能与父窗口交互。
    - 通过订阅子窗口的 `Closed` 事件，可以在子窗口关闭时执行特定操作（如重新激活主窗口、刷新数据等）。
    - `this.Hide()` 和 `this.Show()` 可以用于在打开子窗口时隐藏主窗口，并在子窗口关闭后重新显示主窗口，以模拟单窗口应用的导航流。

    ```csharp
    private void StartExploration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var gravityForm = new GravitySimulationForm();
            gravityForm.Owner = this; // 设置所有者，确保窗口层级关系正确
            gravityForm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            // gravityForm.Closed += (s, args) => this.Activate(); // 如果主窗口未隐藏
            this.Hide(); // 隐藏主窗口
            gravityForm.ShowDialog(); // 以模态方式打开，等待其关闭
            this.Show(); // 探索窗口关闭后，重新显示主窗口
            this.Activate(); // 激活主窗口
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开太阳系探索界面时出错：{ex.Message}", "导航错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenAIChat_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ChatWindow chatWindow = new ChatWindow();
            chatWindow.Owner = this;
            // chatWindow.Show(); // 非模态打开，允许同时操作主窗口
            
            // 如果希望聊天窗口在主窗口之上，并且主窗口不可交互，则使用 ShowDialog()
            // 但通常聊天窗口是非模态的
             bool isChatWindowOpen = Application.Current.Windows.OfType<ChatWindow>().Any();
            if (!isChatWindowOpen)
            {
                chatWindow.Show();
            }
            else
            {
                Application.Current.Windows.OfType<ChatWindow>().First().Activate();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开星际智者界面时出错：{ex.Message}", "导航错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    ```

### 2.4. MVVM模式应用 (轻度或未来方向)

虽然当前 `MainWindow` 的代码示例更偏向事件驱动（Code-behind），但在更复杂的应用或后续重构中，可以引入MVVM模式来提升代码的可维护性和可测试性。

- **ViewModel (`MainWindowViewModel.cs`)**: 
    - 包含主界面的状态（如当前用户信息、应用程序设置等）。
    - 实现 `ICommand` 接口的属性，用于处理按钮点击等交互逻辑（如 `OpenExplorationCommand`, `OpenChatCommand`）。
    - 实现 `INotifyPropertyChanged` 接口，以便在属性更改时通知View更新。
- **View (`MainWindow.xaml`)**: 
    - 将控件的 `Command` 属性绑定到ViewModel中的命令属性。
    - 将需要显示的数据绑定到ViewModel中的属性。
    - `DataContext` 设置为 `MainWindowViewModel` 的实例。

    ```csharp
    // 示例: MainWindowViewModel.cs (简化版)
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ICommand OpenExplorationCommand { get; }
        public ICommand OpenChatCommand { get; }
        // ... 其他属性和命令

        public MainWindowViewModel()
        {
            OpenExplorationCommand = new RelayCommand(ExecuteOpenExploration, CanExecuteOpenExploration);
            OpenChatCommand = new RelayCommand(ExecuteOpenChat, CanExecuteOpenChat);
        }

        private void ExecuteOpenExploration(object parameter)
        {
            // 实际导航逻辑，与Click事件处理器类似，但可以从ViewModel调用服务
            var gravityForm = new GravitySimulationForm();
            gravityForm.Show(); 
        }
        private bool CanExecuteOpenExploration(object parameter) => true;

        private void ExecuteOpenChat(object parameter)
        {
            var chatWindow = new ChatWindow();
            chatWindow.Show();
        }
        private bool CanExecuteOpenChat(object parameter) => true;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // RelayCommand 简单实现 (通常放在单独的文件中)
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
    ```
    ```xml
    <!-- MainWindow.xaml 的 DataContext 设置 -->
    <!-- <Window.DataContext>
        <local:MainWindowViewModel/>
    </Window.DataContext> -->

    <!-- 按钮绑定到命令 -->
    <!-- <Button Content="开始探索" Command="{Binding OpenExplorationCommand}"/> -->
    ```

### 2.5. 用户活动记录 (可选)

- 在关键功能按钮的事件处理器中，可以添加记录用户活动的代码。这些信息可以用于分析用户行为或调试。

    ```csharp
    private void SaveActivityHistory(string activityName)
    {
        try
        {
            // 示例：简单地输出到调试控制台
            Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 用户活动: {activityName}");

            // 实际应用中可能写入日志文件或数据库
            // string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_activity_log.txt");
            // File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {activityName}\n");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"记录活动历史时出错: {ex.Message}");
        }
    }

    // 在按钮点击事件中调用:
    // SaveActivityHistory("打开星际智者对话界面");
    ```

## 3. C# 语言特性与 .NET 框架应用总结

- **事件驱动编程**: 大量使用控件的 `Click` 事件和窗口的生命周期事件 (`Loaded`, `Closed`)。
- **面向对象 (OOP)**: `MainWindow` 和其他功能窗口都是类的实例，封装了各自的逻辑和UI。
- **WPF核心**:
    - **布局系统 (`Grid`, `StackPanel`)**: 构建灵活的UI结构。
    - **样式与模板**: 实现高度自定义和统一的视觉外观。
    - **`Window` 类及其属性/方法**: 控制窗口行为 (`WindowState`, `DragMove`, `Show`, `Hide`, `Close`)。
    - **`Application.Current`**: 访问应用程序级资源和方法 (如 `Shutdown`, `Windows` 集合)。
- **LINQ (轻度使用)**: `Application.Current.Windows.OfType<ChatWindow>().Any()` 用于检查窗口是否已打开。
- **异常处理 (`try-catch`)**: 在事件处理器和关键逻辑中捕获并处理潜在异常，增强程序稳定性。
- **`System.Diagnostics.Debug`**: 输出调试信息。
- **文件I/O (`System.IO.File`, `System.IO.Path`)**: (可选) 用于日志记录。
- **`ICommand` 和数据绑定 (MVVM方向)**: 提供更结构化和可测试的代码组织方式。

## 4. 未来拓展与优化方向

- **全面MVVM化**: 将所有UI逻辑和状态迁移到ViewModel，实现前后端分离。
- **用户控件 (UserControls)**: 将可复用的UI部分（如自定义标题栏、功能卡片）封装成用户控件。
- **主题切换**: 实现动态切换应用程序主题（如亮色/暗色主题）的功能。
- **可停靠的面板/插件系统**: 允许用户自定义主界面布局，或通过插件扩展功能。
- **更精细的错误处理与用户反馈**: 提供更具体的错误信息和操作指引。

This concludes the technical implementation details for the MainWindow.
