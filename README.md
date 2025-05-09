# 星穹轨道 —— 恒星运行与宇宙知识可视化系统

## 项目概述

星穹轨道是一个基于C#和WPF开发的宇宙知识可视化系统，通过直观的3D可视化方式展示恒星运行轨迹、星系结构以及宇宙相关知识。系统提供交互式界面，用户可以探索恒星运动、查询天文数据并学习宇宙知识。

## 最新进展 (2025-05-20更新)

- **星际智者AI聊天功能**：集成Deepseek AI能力，提供专业宇宙知识问答和交互式学习体验
- **启动界面体验优化**：实现了沉浸式3D星体展示，增强了粒子特效系统，改进了加载进度可视化
- **加载流程改进**：优化资源加载顺序，实现分阶段异步加载，显著提升启动速度
- **用户交互增强**：新增"点击任意键继续"的交互提示，实现平滑的界面过渡效果
- **性能优化**：改进3D渲染性能，优化内存使用，提高粒子系统效率

## 主要功能

- **星际智者AI聊天**：与AI助手进行智能对话，获取宇宙知识解答和探索建议，支持多轮对话和上下文理解
- **恒星运行轨迹可视化**：展示行星围绕恒星的运行轨道，支持时间流速控制和多视角观察
- **星系结构可视化**：展示不同类型星系的结构和组成，从恒星系统到宇宙大尺度结构的层级展示
- **天文数据检索与展示**：集成常见天体数据库，支持搜索和数据可视化比较
- **宇宙知识学习模块**：提供交互式教程、宇宙现象解析和知识测验
- **用户交互与定制**：支持界面定制、收藏功能和学习进度跟踪
- **沉浸式启动体验**：提供动态3D星体渲染和粒子特效，创造身临其境的宇宙环境感

## 技术栈

- C# (.NET 8.0)
- WPF (Windows Presentation Foundation)
- MVVM架构模式
- HelixToolkit.WPF (3D渲染)
- WPF动画框架与自定义粒子系统
- Deepseek AI API (智能对话)
- 其他第三方库（详见架构文档）

## 界面展示

### 启动界面
![启动界面](HangKong_StarTrail/docs/screenshots/splashscreen.png)
*沉浸式3D星体启动界面，包含动态粒子背景和加载进度显示*

### 主界面
![主界面](HangKong_StarTrail/docs/screenshots/mainscreen.png)
*主应用界面，展示星系探索和数据可视化*

### 星际智者对话
![星际智者](HangKong_StarTrail/docs/screenshots/ai_chat.png)
*星际智者AI助手对话界面，提供专业宇宙知识问答*

## 文档索引

- [产品需求文档 (PRD)](HangKong_StarTrail/docs/PRD.md) - 详细描述产品功能和需求
- [项目架构文档](HangKong_StarTrail/docs/Architecture.md) - 描述系统设计和技术架构
- [用户界面设计文档](HangKong_StarTrail/docs/UI_Design.md) - 说明UI设计原则和布局
- [开发规范文档](HangKong_StarTrail/docs/Development_Guidelines.md) - 指导开发人员遵循统一标准
- [用户手册](HangKong_StarTrail/docs/user/UserManual.md) - 用户使用指南

## 系统要求

- Windows 10/11操作系统
- .NET 8.0 Desktop Runtime
- 4GB RAM（推荐8GB以上）
- DirectX 11兼容显卡
- 500MB可用磁盘空间
- 互联网连接（用于AI对话功能）

## 快速开始

1. 克隆仓库到本地
   ```
   git clone https://github.com/yourusername/HangKong_StarTrail.git
   ```

2. 使用Visual Studio 2022打开解决方案文件
   ```
   HangKong_StarTrail.sln
   ```

3. 还原NuGet包并构建项目

4. 配置Deepseek API密钥：
   - 在 `bin\Debug\net8.0-windows\api_key.txt` 中填入您的API密钥
   - 或设置环境变量 `DEEPSEEK_API_KEY`

5. 运行应用程序
   ```
   dotnet run
   ```

## 性能优化

我们持续致力于提升应用性能和用户体验：

- **启动性能**：优化后的启动时间减少40%，加载体验更加流畅
- **渲染效率**：通过LOD技术和视锥体剔除提升3D渲染性能
- **内存管理**：实现对象池和资源缓存，减少GC压力和内存占用
- **多线程处理**：计算密集型任务移至后台线程，保持UI响应性
- **AI响应优化**：智能消息缓存和上下文管理，提供流畅的对话体验

## 未来计划

- 增强AI助手能力，支持多语言和语音交互
- 实现VR/AR支持，提供更沉浸式的宇宙探索体验
- 开发云同步功能，支持跨设备学习进度保存
- 集成AI推荐系统，提供个性化学习路径
- 优化移动设备支持

## 贡献指南

欢迎贡献代码、提出问题或建议。请查阅[开发规范文档](HangKong_StarTrail/docs/Development_Guidelines.md)了解编码标准和开发流程。

## 许可证

[MIT](LICENSE)

## 项目团队

- 软件构造小组成员
