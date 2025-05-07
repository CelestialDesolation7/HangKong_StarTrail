# 星穹轨道 —— 恒星运行与宇宙知识可视化系统

## 项目概述

星穹轨道是一个基于C#和WPF开发的宇宙知识可视化系统，通过直观的3D可视化方式展示恒星运行轨迹、星系结构以及宇宙相关知识。系统提供交互式界面，用户可以探索恒星运动、查询天文数据并学习宇宙知识。

## 主要功能

- **恒星运行轨迹可视化**：展示行星围绕恒星的运行轨道，支持时间流速控制和多视角观察
- **星系结构可视化**：展示不同类型星系的结构和组成，从恒星系统到宇宙大尺度结构的层级展示
- **天文数据检索与展示**：集成常见天体数据库，支持搜索和数据可视化比较
- **宇宙知识学习模块**：提供交互式教程、宇宙现象解析和知识测验
- **用户交互与定制**：支持界面定制、收藏功能和学习进度跟踪

## 技术栈

- C# (.NET 8.0)
- WPF (Windows Presentation Foundation)
- MVVM架构模式
- HelixToolkit.WPF (3D渲染)
- 其他第三方库（详见架构文档）

## 文档索引

- [产品需求文档 (PRD)](HangKong_StarTrail/docs/PRD.md) - 详细描述产品功能和需求
- [项目架构文档](HangKong_StarTrail/docs/Architecture.md) - 描述系统设计和技术架构
- [用户界面设计文档](HangKong_StarTrail/docs/UI_Design.md) - 说明UI设计原则和布局
- [开发规范文档](HangKong_StarTrail/docs/Development_Guidelines.md) - 指导开发人员遵循统一标准

## 系统要求

- Windows 10/11操作系统
- .NET 8.0 Desktop Runtime
- 4GB RAM（推荐8GB以上）
- DirectX 11兼容显卡
- 500MB可用磁盘空间

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

4. 运行应用程序
   ```
   dotnet run
   ```

## 贡献指南

欢迎贡献代码、提出问题或建议。请查阅[开发规范文档](HangKong_StarTrail/docs/Development_Guidelines.md)了解编码标准和开发流程。

## 许可证

[MIT](LICENSE)

## 项目团队

- 软件构造小组成员
