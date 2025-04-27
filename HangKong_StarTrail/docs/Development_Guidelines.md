# 星穹轨道 —— 开发规范文档

## 版本历史
| 版本号 | 日期 | 描述 |
| --- | --- | --- |
| v1.0 | 2025-04-27 | 初始版本 |

## 目录
1. [编码规范](#编码规范)
2. [项目结构](#项目结构)
3. [版本控制](#版本控制)
4. [文档规范](#文档规范)
5. [测试规范](#测试规范)
6. [性能优化](#性能优化)
7. [协作流程](#协作流程)

## 编码规范

### C# 编码风格

#### 命名约定
- **命名空间**：使用 `HangKong_StarTrail.{模块名}` 格式
- **类名**：使用 PascalCase，如 `CelestialBody`
- **接口**：以 "I" 开头，如 `IDataService`
- **私有字段**：使用 camelCase 并以下划线开头，如 `_orbitCalculator`
- **属性和方法**：使用 PascalCase，如 `CalculateOrbit()`
- **参数和局部变量**：使用 camelCase，如 `celestialBody`
- **常量**：使用全大写加下划线分隔，如 `MAX_ORBIT_COUNT`
- **枚举**：使用 PascalCase，值也使用 PascalCase

#### 代码格式
- 使用4个空格作为缩进（不使用制表符）
- 大括号放在新行
- 每行代码不超过120个字符
- 使用空行分隔逻辑相关的代码块
- 在操作符两侧添加空格
- 在逗号、分号后添加空格

```csharp
// 正确的格式示例
public class OrbitCalculator
{
    private readonly double _gravitationalConstant;
    
    public OrbitCalculator(double gravitationalConstant)
    {
        _gravitationalConstant = gravitationalConstant;
    }
    
    public Vector3 CalculatePosition(CelestialBody body, double time)
    {
        // 实现逻辑
        return new Vector3();
    }
}
```

#### 代码组织
- 使用 `#region` 组织代码区域，但不过度使用
- 类成员顺序：常量、字段、构造函数、属性、方法
- 访问修饰符顺序：public, internal, protected, private

### XAML 编码风格

#### 命名约定
- **控件名称**：使用 PascalCase，并以控件类型结尾，如 `PlanetInfoPanel`
- **事件处理程序**：使用 `On{控件名称}_{事件名称}`，如 `OnPlanetInfoPanel_Click`
- **样式资源**：使用 `{控件类型}{用途}Style`，如 `ButtonPrimaryStyle`

#### 标记格式
- 缩进使用2个空格
- 每个属性占一行（当属性少于3个时可以放在同一行）
- 关闭标签使用自闭合形式（当没有子元素时）
- 资源字典中的键使用驼峰式命名

```xml
<!-- 正确的XAML格式示例 -->
<Grid>
  <Button 
    x:Name="ExploreButton"
    Content="探索"
    Style="{StaticResource ButtonPrimaryStyle}"
    Click="OnExploreButton_Click" />
    
  <TextBlock Text="恒星信息" Margin="5,0,0,0" />
</Grid>
```

## 项目结构

### 解决方案结构

```
HangKong_StarTrail/
├── HangKong_StarTrail (主项目)
│   ├── App.xaml
│   ├── MainWindow.xaml
│   ├── AssemblyInfo.cs
│   └── HangKong_StarTrail.csproj
├── HangKong_StarTrail.Core (核心库)
├── HangKong_StarTrail.Simulation (模拟模块)
├── HangKong_StarTrail.Visualization (可视化模块)
├── HangKong_StarTrail.Data (数据模块)
├── HangKong_StarTrail.Education (教育模块)
├── HangKong_StarTrail.UI (UI组件)
├── HangKong_StarTrail.Tests (测试项目)
└── docs/ (文档目录)
```

### 项目内部结构

每个项目内部应遵循以下结构：

```
Project/
├── Models/ (数据模型)
├── ViewModels/ (视图模型)
├── Views/ (视图组件)
├── Services/ (服务接口和实现)
├── Helpers/ (辅助类和扩展方法)
├── Resources/ (资源文件)
│   ├── Images/
│   ├── Styles/
│   └── DataTemplates/
└── Constants/ (常量定义)
```

## 版本控制

### Git 工作流

我们采用 Feature Branch Workflow：

1. `main` 分支始终保持可发布状态
2. 所有开发在特性分支进行（从 `main` 分支创建）
3. 完成开发后，通过 Pull Request 合并回 `main` 分支
4. 使用 `release/` 前缀的分支准备发布版本
5. 使用 `hotfix/` 前缀的分支修复生产环境问题

### 分支命名

- 特性分支：`feature/{特性描述}`，例如 `feature/orbit-visualization`
- 修复分支：`bugfix/{问题描述}`，例如 `bugfix/crash-on-startup`
- 发布分支：`release/v{版本号}`，例如 `release/v1.0.0`
- 热修复分支：`hotfix/v{版本号}`，例如 `hotfix/v1.0.1`

### 提交信息

提交信息应遵循以下格式：

```
{类型}: {简短描述}

{详细描述（可选）}

{关联的任务ID（可选）}
```

类型包括：
- `feat`：新功能
- `fix`：错误修复
- `docs`：文档变更
- `style`：代码格式变更（不影响代码功能）
- `refactor`：重构（不是修复错误也不是添加功能）
- `perf`：性能优化
- `test`：添加或修改测试
- `chore`：构建过程或辅助工具变更

例如：
```
feat: 添加行星轨道动画效果

实现了基于开普勒定律的行星轨道运动动画，包括:
- 椭圆轨道计算
- 基于时间的位置插值
- 轨迹渲染优化

关联任务: #42
```

## 文档规范

### 代码注释

#### 方法和类注释
使用XML文档注释格式：

```csharp
/// <summary>
/// 计算天体在特定时间点的轨道位置
/// </summary>
/// <param name="body">天体对象</param>
/// <param name="time">时间点（以儒略日表示）</param>
/// <returns>三维空间中的位置向量</returns>
/// <exception cref="ArgumentNullException">当body为null时抛出</exception>
public Vector3 CalculatePosition(CelestialBody body, double time)
{
    // 实现...
}
```

#### 内部注释
对于复杂算法或不明显的逻辑，添加内部注释说明：

```csharp
// 使用开普勒方程计算偏近点角
// 注意：这里使用牛顿-拉夫森迭代法求解
double CalculateEccentricAnomaly(double meanAnomaly, double eccentricity)
{
    // 实现...
}
```

### API文档
- 所有公共API必须有XML文档注释
- 在生成时启用XML文档生成
- 使用DocFX生成HTML格式的API文档

### 用户文档
- 用户手册放置在 `docs/user/` 目录
- 开发者指南放置在 `docs/dev/` 目录
- 文档使用Markdown格式编写
- 图表使用Mermaid或PlantUML生成

## 测试规范

### 单元测试

- 使用 xUnit 作为测试框架
- 每个项目对应一个测试项目，如 `HangKong_StarTrail.Core.Tests`
- 测试类名为被测试类名加 `Tests` 后缀
- 测试方法名为 `{被测试方法名}_{测试场景}_{预期结果}`

```csharp
public class OrbitCalculatorTests
{
    [Fact]
    public void CalculatePosition_CircularOrbit_ReturnsCorrectPosition()
    {
        // Arrange
        var calculator = new OrbitCalculator(6.67430e-11);
        var body = new CelestialBody { /* ... */ };
        
        // Act
        var position = calculator.CalculatePosition(body, 2459000.5);
        
        // Assert
        Assert.Equal(expectedX, position.X, precision);
        Assert.Equal(expectedY, position.Y, precision);
        Assert.Equal(expectedZ, position.Z, precision);
    }
}
```

### 集成测试
- 放置在 `HangKong_StarTrail.IntegrationTests` 项目中
- 测试跨组件交互和外部依赖

### UI测试
- 放置在 `HangKong_StarTrail.UITests` 项目中
- 使用UI自动化框架（如WinAppDriver）
- 测试关键用户流程和场景

### 测试覆盖率
- 目标代码覆盖率：核心模块>80%，其他模块>70%
- 使用 Coverlet 收集覆盖率数据
- 在CI管道中生成覆盖率报告

## 性能优化

### 性能目标
- 启动时间：<5秒（普通硬件配置）
- 帧率：>30 FPS（3D场景渲染）
- 内存使用：<1GB（正常使用）
- 响应时间：<300ms（用户操作）

### 优化技术

#### 3D渲染优化
- 使用LOD（细节级别）技术
- 实现视锥体剔除
- 优化纹理加载和管理
- 使用实例化渲染大量相似对象

#### 数据处理优化
- 实现数据缓存机制
- 使用后台线程进行计算密集型任务
- 采用惰性加载策略
- 实现数据分页和虚拟滚动

#### UI响应优化
- 避免UI线程阻塞
- 使用硬件加速渲染
- 减少不必要的UI更新
- 优化绑定性能

### 性能监控
- 在调试生成中启用性能计数器
- 记录关键操作的执行时间
- 定期进行性能分析和基准测试

## 协作流程

### 任务管理
- 使用Azure DevOps/GitHub Projects进行任务跟踪
- 每个任务应包含清晰的描述、验收标准和预估工时
- 使用标签分类任务（功能、错误、文档等）

### 代码审查
- 所有代码必须经过至少一名团队成员的审查
- 审查关注点：功能正确性、代码质量、性能、安全性
- 使用Pull Request进行代码审查

### 持续集成
- 每次提交自动运行单元测试
- 静态代码分析（使用SonarQube或ReSharper）
- 生成覆盖率报告
- 自动构建和打包

### 发布流程
1. 创建发布分支
2. 执行回归测试
3. 更新版本号和发布说明
4. 生成发布包
5. 部署到测试环境验证
6. 合并到主分支并打标签
7. 部署到生产环境

### 团队沟通
- 每日站会（15分钟）
- 每周团队会议（1小时）
- 使用Teams/Slack进行即时沟通
- 技术决策文档化存储在Wiki中

[TODO: 确认团队成员职责分配和联系方式] 