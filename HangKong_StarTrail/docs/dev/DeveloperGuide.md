# 星穹轨道 —— 开发者指南

## 版本历史
| 版本号 | 日期 | 描述 |
| --- | --- | --- |
| v1.0 | 2025-04-27 | 初始版本 |

## 目录
1. [开发环境搭建](#开发环境搭建)
2. [项目结构介绍](#项目结构介绍)
3. [开发工作流程](#开发工作流程)
4. [核心API文档](#核心API文档)
5. [最佳实践](#最佳实践)
6. [常见问题与解决方案](#常见问题与解决方案)
7. [贡献指南](#贡献指南)

## 开发环境搭建

### 必备工具

- **IDE**：Visual Studio 2022（推荐企业版或专业版）
- **开发框架**：
  - .NET 8.0 SDK
  - WPF（Windows Presentation Foundation）
- **版本控制**：
  - Git 2.35.0 或更高版本
  - 可选：GitHub Desktop 或 GitKraken
- **设计工具**：
  - Blend for Visual Studio
  - 可选：Figma 或 Adobe XD 用于UI设计
- **其他工具**：
  - PowerShell 7.0 或更高版本
  - NuGet Package Manager
  - .NET CLI

### 环境配置步骤

1. **安装 Visual Studio 2022**
   - 安装时选择".NET 桌面开发"工作负载
   - 确保包含WPF组件

2. **安装必要的SDK和工具**
   ```powershell
   # 安装 .NET 8.0 SDK
   winget install Microsoft.DotNet.SDK.8
   
   # 安装 Git
   winget install Git.Git
   
   # 安装 PowerShell 7
   winget install Microsoft.PowerShell
   ```

3. **克隆代码仓库**
   ```powershell
   # 克隆主仓库
   git clone https://github.com/yourusername/HangKong_StarTrail.git
   
   # 进入项目目录
   cd HangKong_StarTrail
   
   # 安装Git LFS（用于大文件存储，如3D模型和纹理）
   git lfs install
   ```

4. **恢复NuGet包**
   ```powershell
   # 使用 .NET CLI
   dotnet restore
   
   # 或使用 NuGet CLI
   nuget restore HangKong_StarTrail.sln
   ```

5. **配置开发设置**
   - 复制 `settings.example.json` 到 `settings.json` 并根据本地环境调整
   - 配置本地数据库连接（如使用SQLite）
   - 设置API密钥（如使用外部天文数据API）

## 项目结构介绍

"星穹轨道"项目采用模块化架构，基于MVVM设计模式。项目结构如下：

### 解决方案组成

```
HangKong_StarTrail/
├── HangKong_StarTrail (主项目)
│   ├── App.xaml                  # 应用入口
│   ├── MainWindow.xaml           # 主窗口
│   ├── AssemblyInfo.cs           # 程序集信息
│   └── HangKong_StarTrail.csproj # 项目文件
├── HangKong_StarTrail.Core (核心库)
│   ├── Events/                   # 事件定义
│   ├── Services/                 # 核心服务接口
│   └── Helpers/                  # 通用工具类
├── HangKong_StarTrail.Simulation (模拟模块)
│   ├── Models/                   # 物理模型
│   ├── Engines/                  # 模拟引擎
│   └── Calculators/              # 轨道计算器
├── HangKong_StarTrail.Visualization (可视化模块)
│   ├── Renderers/                # 渲染器
│   ├── Materials/                # 材质定义
│   └── Cameras/                  # 相机控制
├── HangKong_StarTrail.Data (数据模块)
│   ├── Repositories/             # 数据访问层
│   ├── Models/                   # 数据模型
│   └── Services/                 # 数据服务
├── HangKong_StarTrail.Education (教育模块)
│   ├── Tutorials/                # 教程系统
│   ├── Quiz/                     # 测验系统
│   └── Progress/                 # 学习进度
├── HangKong_StarTrail.UI (UI组件)
│   ├── Controls/                 # 自定义控件
│   ├── Behaviors/                # 交互行为
│   └── Resources/                # UI资源
├── HangKong_StarTrail.Tests (测试项目)
└── docs/ (文档目录)
```

### 关键模块详解

#### Core 模块
提供整个应用程序的基础设施，包括：
- 依赖注入容器
- 事件聚合器
- 日志系统
- 通用工具类和扩展方法
- 全局常量和配置

#### Simulation 模块
负责天体运动的物理模拟：
- 基于牛顿力学的天体运动计算
- 开普勒轨道元素处理
- 时间系统管理
- 天体物理特性模型

#### Visualization 模块
处理3D渲染和视觉呈现：
- 封装HelixToolkit进行3D场景渲染
- 天体材质和纹理管理
- 相机系统和视角控制
- 特效系统（如光晕、尾迹）

#### Data 模块
管理应用程序的数据存取：
- 天文数据本地存储
- 外部API数据获取
- 用户数据管理
- 缓存策略

#### Education 模块
实现学习功能：
- 交互式教程生成
- 知识测验系统
- 学习进度跟踪
- 内容推荐算法

#### UI 模块
提供自定义UI组件：
- 控制面板组件
- 信息显示组件
- 自定义图表和可视化控件
- 样式和主题

## 开发工作流程

### 分支管理

项目采用GitHub Flow的简化版：

1. **主分支**：`main` 分支始终包含稳定可发布的代码
2. **特性分支**：从 `main` 分支创建特性分支进行开发
3. **命名规范**：
   - 特性分支：`feature/{特性名称}`
   - 错误修复：`bugfix/{错误描述}`
   - 发布准备：`release/v{版本号}`

### 开发流程

1. **任务分配**
   - 在GitHub Issues或团队任务管理系统中认领任务
   - 明确任务要求和完成标准

2. **本地开发**
   ```powershell
   # 确保main分支是最新的
   git checkout main
   git pull
   
   # 创建特性分支
   git checkout -b feature/orbit-visualization
   
   # 进行开发...
   
   # 提交更改
   git add .
   git commit -m "feat: 实现行星轨道可视化"
   
   # 推送到远程仓库
   git push -u origin feature/orbit-visualization
   ```

3. **代码审查**
   - 在GitHub上创建Pull Request
   - 分配至少一名团队成员进行代码审查
   - 解决审查中提出的问题
   - 确保CI测试通过

4. **合并到主分支**
   - 代码审查通过后，合并到main分支
   - 使用Squash合并保持提交历史整洁
   - 删除已合并的特性分支

### 发布流程

1. **版本规划**
   - 确定版本包含的功能和修复
   - 更新版本号（遵循语义化版本规范）

2. **创建发布分支**
   ```powershell
   git checkout main
   git checkout -b release/v1.0.0
   ```

3. **准备发布**
   - 更新版本信息
   - 生成发布说明
   - 执行最终测试

4. **创建发布标签**
   ```powershell
   git tag -a v1.0.0 -m "版本1.0.0发布"
   git push origin v1.0.0
   ```

5. **构建发布包**
   ```powershell
   # 发布生成
   dotnet publish -c Release -r win-x64 --self-contained true
   
   # 创建安装包
   # 使用WiX Toolset或NSIS创建MSI/EXE安装程序
   ```

## 核心API文档

### Simulation 模块核心API

#### OrbitCalculator 类

计算天体轨道位置和参数。

```csharp
/// <summary>
/// 轨道计算器，用于计算天体在轨道上的位置
/// </summary>
public class OrbitCalculator
{
    /// <summary>
    /// 使用开普勒轨道元素计算天体在特定时间的位置
    /// </summary>
    /// <param name="orbitalElements">轨道根数</param>
    /// <param name="time">时间（儒略日）</param>
    /// <returns>三维空间中的位置向量</returns>
    public Vector3 CalculatePosition(OrbitalElements orbitalElements, double time);
    
    /// <summary>
    /// 计算天体在特定时间的速度向量
    /// </summary>
    /// <param name="orbitalElements">轨道根数</param>
    /// <param name="time">时间（儒略日）</param>
    /// <returns>三维空间中的速度向量</returns>
    public Vector3 CalculateVelocity(OrbitalElements orbitalElements, double time);
    
    // 其他方法...
}
```

#### TimeController 类

控制模拟时间流速和时间点。

```csharp
/// <summary>
/// 时间控制器，管理模拟时间
/// </summary>
public class TimeController
{
    /// <summary>
    /// 当前模拟时间（儒略日）
    /// </summary>
    public double CurrentTime { get; }
    
    /// <summary>
    /// 时间流速倍率
    /// </summary>
    public double TimeScale { get; set; }
    
    /// <summary>
    /// 启动时间流动
    /// </summary>
    public void Start();
    
    /// <summary>
    /// 暂停时间流动
    /// </summary>
    public void Pause();
    
    /// <summary>
    /// 跳转到指定时间点
    /// </summary>
    /// <param name="julianDate">目标儒略日</param>
    public void JumpTo(double julianDate);
    
    // 其他方法...
}
```

### Visualization 模块核心API

#### SceneManager 类

管理3D渲染场景。

```csharp
/// <summary>
/// 场景管理器，处理3D场景的创建和管理
/// </summary>
public class SceneManager
{
    /// <summary>
    /// 添加天体到场景
    /// </summary>
    /// <param name="celestialBody">天体对象</param>
    /// <returns>场景中的天体ID</returns>
    public Guid AddCelestialBody(CelestialBody celestialBody);
    
    /// <summary>
    /// 更新天体位置
    /// </summary>
    /// <param name="bodyId">天体ID</param>
    /// <param name="position">新位置</param>
    public void UpdatePosition(Guid bodyId, Vector3 position);
    
    /// <summary>
    /// 创建轨道线
    /// </summary>
    /// <param name="orbitalElements">轨道根数</param>
    /// <param name="color">轨道颜色</param>
    /// <returns>轨道线ID</returns>
    public Guid CreateOrbitLine(OrbitalElements orbitalElements, Color color);
    
    // 其他方法...
}
```

#### CameraController 类

控制3D场景相机。

```csharp
/// <summary>
/// 相机控制器，管理视角和相机位置
/// </summary>
public class CameraController
{
    /// <summary>
    /// 设置相机位置
    /// </summary>
    /// <param name="position">位置向量</param>
    public void SetPosition(Vector3 position);
    
    /// <summary>
    /// 设置相机朝向
    /// </summary>
    /// <param name="target">目标点</param>
    public void LookAt(Vector3 target);
    
    /// <summary>
    /// 聚焦到指定天体
    /// </summary>
    /// <param name="bodyId">天体ID</param>
    /// <param name="distance">相机到天体的距离</param>
    public void FocusOn(Guid bodyId, double distance);
    
    // 其他方法...
}
```

### Data 模块核心API

#### CelestialDataService 类

提供天体数据访问服务。

```csharp
/// <summary>
/// 天体数据服务，提供天体信息访问
/// </summary>
public class CelestialDataService
{
    /// <summary>
    /// 获取天体信息
    /// </summary>
    /// <param name="name">天体名称</param>
    /// <returns>天体信息对象</returns>
    public Task<CelestialBody> GetByNameAsync(string name);
    
    /// <summary>
    /// 按类型获取天体列表
    /// </summary>
    /// <param name="type">天体类型</param>
    /// <returns>天体列表</returns>
    public Task<IEnumerable<CelestialBody>> GetByTypeAsync(CelestialBodyType type);
    
    /// <summary>
    /// 搜索天体
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="limit">最大结果数</param>
    /// <returns>匹配的天体列表</returns>
    public Task<IEnumerable<CelestialBody>> SearchAsync(string query, int limit = 10);
    
    // 其他方法...
}
```

## 最佳实践

### 编码最佳实践

#### 性能优化

1. **3D渲染优化**
   - 使用LOD（细节级别）技术减少远距离天体的渲染复杂度
   - 实现视锥体剔除，只渲染视野内的对象
   - 使用实例化渲染技术处理大量相似对象（如恒星）

   ```csharp
   // 实例化渲染示例
   public void RenderStarField(IEnumerable<StarData> stars)
   {
       var instanceParams = stars.Select(s => new InstanceParameter
       {
           Position = s.Position,
           Scale = new Vector3(s.Size),
           Color = ColorFromTemperature(s.Temperature)
       }).ToArray();
       
       _starInstanceRenderer.Update(instanceParams);
   }
   ```

2. **数据处理优化**
   - 实现分层缓存策略，减少数据库访问
   - 使用后台线程进行计算密集型任务
   - 避免在UI线程进行耗时操作

   ```csharp
   // 后台计算示例
   public async Task<IEnumerable<Vector3>> CalculateTrajectoryAsync(
       OrbitalElements elements, 
       TimeSpan duration, 
       int points)
   {
       return await Task.Run(() => 
       {
           var result = new Vector3[points];
           // 计算轨迹点...
           return result;
       });
   }
   ```

#### 代码质量

1. **单一职责原则**
   - 每个类只负责一个功能领域
   - 将大类拆分为小类和辅助类

2. **依赖注入**
   - 使用依赖注入容器管理服务实例
   - 面向接口编程，使用构造函数注入

   ```csharp
   // 依赖注入示例
   public class OrbitVisualizer
   {
       private readonly IOrbitCalculator _calculator;
       private readonly ISceneManager _sceneManager;
       
       public OrbitVisualizer(
           IOrbitCalculator calculator,
           ISceneManager sceneManager)
       {
           _calculator = calculator;
           _sceneManager = sceneManager;
       }
       
       // 实现...
   }
   ```

3. **异常处理**
   - 使用特定的异常类型
   - 在合适的抽象层处理异常
   - 记录详细的错误信息

   ```csharp
   try
   {
       var celestialData = await _dataService.GetByNameAsync(name);
       return celestialData;
   }
   catch (DataNotFoundException ex)
   {
       _logger.LogWarning(ex, "未找到天体数据: {Name}", name);
       return null;
   }
   catch (Exception ex)
   {
       _logger.LogError(ex, "获取天体数据时发生错误: {Name}", name);
       throw new CelestialDataAccessException($"访问天体数据失败: {name}", ex);
   }
   ```

### MVVM模式最佳实践

1. **ViewModels设计**
   - 实现INotifyPropertyChanged接口
   - 使用命令模式处理用户操作
   - 保持ViewModels独立于View

   ```csharp
   public class CelestialBodyViewModel : ViewModelBase
   {
       private string _name;
       public string Name
       {
           get => _name;
           set => SetProperty(ref _name, value);
       }
       
       private ICommand _focusCommand;
       public ICommand FocusCommand => _focusCommand ??= new RelayCommand(ExecuteFocus);
       
       private void ExecuteFocus()
       {
           // 实现聚焦逻辑...
       }
   }
   ```

2. **数据绑定**
   - 使用绑定而非直接操作UI元素
   - 对于频繁更新的属性使用高性能绑定技术
   - 使用值转换器处理格式转换

   ```xml
   <TextBlock Text="{Binding Temperature, StringFormat='{0:N1} K'}" />
   <Ellipse Width="{Binding Diameter, Converter={StaticResource SizeConverter}}" />
   ```

3. **用户控件复用**
   - 创建可重用的自定义控件
   - 使用附加属性和行为扩展现有控件

   ```csharp
   public static class CameraControlBehavior
   {
       public static readonly DependencyProperty EnableRotationProperty =
           DependencyProperty.RegisterAttached(
               "EnableRotation",
               typeof(bool),
               typeof(CameraControlBehavior),
               new PropertyMetadata(false, OnEnableRotationChanged));
               
       // 实现...
   }
   ```

## 常见问题与解决方案

### 1. 内存泄漏问题

**问题**：长时间运行应用程序后内存使用持续增长。

**解决方案**：
- 使用弱引用处理事件订阅
- 在不需要时显式释放大型资源
- 使用内存分析工具识别泄漏点

```csharp
// 使用弱事件模式示例
public class EventManager
{
    private readonly WeakEventManager<TimeChangedEventArgs> _timeChangedWeakEventManager =
        new WeakEventManager<TimeChangedEventArgs>();
        
    public event EventHandler<TimeChangedEventArgs> TimeChanged
    {
        add => _timeChangedWeakEventManager.AddHandler(value);
        remove => _timeChangedWeakEventManager.RemoveHandler(value);
    }
    
    protected void OnTimeChanged(TimeChangedEventArgs e)
    {
        _timeChangedWeakEventManager.RaiseEvent(this, e);
    }
}
```

### 2. 3D渲染性能问题

**问题**：在显示大量天体时帧率下降严重。

**解决方案**：
- 实现视距裁剪，只渲染可见范围内的天体
- 为远距离天体使用简化模型
- 使用GPU实例化渲染
- 优化光照和阴影计算

```csharp
// 视距裁剪示例
public IEnumerable<CelestialBody> GetVisibleBodies(Camera camera, double maxDistance)
{
    var cameraPosition = camera.Position;
    foreach (var body in _allBodies)
    {
        var distance = Vector3.Distance(cameraPosition, body.Position);
        if (distance <= maxDistance)
        {
            yield return body;
        }
    }
}
```

### 3. 多线程同步问题

**问题**：在后台线程更新数据时发生UI冻结或异常。

**解决方案**：
- 使用Dispatcher将后台操作结果同步到UI线程
- 采用TaskScheduler.FromCurrentSynchronizationContext()
- 对共享数据使用适当的同步机制

```csharp
// UI线程同步示例
private async Task UpdateOrbitDataAsync()
{
    try
    {
        // 在后台线程计算
        var positions = await Task.Run(() => _orbitCalculator.CalculatePositions());
        
        // 在UI线程更新
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            UpdateOrbitVisuals(positions);
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "轨道数据计算失败");
    }
}
```

## 贡献指南

### 提交代码

1. **编码前**
   - 确认你的开发环境满足要求
   - 检查Issues，避免重复工作
   - 与团队讨论实现方案

2. **编码规范**
   - 遵循[编码规范文档](../Development_Guidelines.md)中的约定
   - 使用一致的命名和格式风格
   - 添加适当的注释和文档

3. **单元测试**
   - 为新功能编写单元测试
   - 确保测试覆盖核心功能和边缘情况
   - 运行所有测试确保没有破坏现有功能

4. **提交PR**
   - 提供清晰的描述说明变更内容
   - 关联相关的Issue
   - 回应代码审查中的反馈

### 问题报告

如发现bug或有功能建议，请提交Issue，包含以下信息：

- 问题简短描述
- 重现步骤（如适用）
- 预期行为与实际行为
- 环境信息（操作系统、.NET版本等）
- 相关日志或截图

### 文档贡献

欢迎改进文档：

- 修正文档中的错误
- 添加代码示例和使用场景
- 完善API文档和开发指南

提交文档更改时，请确保：

- 遵循Markdown格式规范
- 更新目录和索引（如需要）
- 检查链接有效性

---

感谢您对"星穹轨道"项目的关注和贡献！如有任何疑问，请联系开发团队。 