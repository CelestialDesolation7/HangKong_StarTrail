using System;
using System.Drawing;
using System.Collections.Generic;

namespace HangKong_StarTrail.Models
{
    public class Body
    {
        public string Name { get; set; }
        // 物理属性 
        // 仅在初始化时对用户可见，之后不再修改
        public Vector2D Position { get; set; }           // 物理真实位置
        public Vector2D Velocity { get; set; }           // 速度
        public double Mass { get; set; }                 // 质量
        // 永远对用户隐藏
        public Vector2D Acceleration { get; set; }       // 当前加速度
        public Vector2D Force { get; set; }               // 当前受到的力 

        // 绘制属性
        // 仅在初始化时对用户可见，之后不再修改
        public int RenderRadius { get; set; }        // 绘制半径
        public bool IsCenter { get; set; }              // 是否为中心天体
        public Color DisplayColor { get; set; }                // 绘制颜色
        // 永远对用户隐藏
        public Vector2D DisplayPosition { get; set; }    // 画布上的位置

        // 轨迹记录
        private const int MAX_TRAJECTORY_POINTS = 128;  // 最大轨迹点数
        private readonly Queue<Vector2D> _trajectoryPoints = new Queue<Vector2D>();  // 轨迹点队列

        public Body(string name_in, Vector2D position_in, Vector2D velocity_in, double mass_in, int displayRadius_in, bool isCenter_in, Color color_in)
        {
            Name = name_in;
            Position = position_in;
            Velocity = velocity_in;
            Mass = mass_in;
            IsCenter = isCenter_in;
            DisplayColor = color_in;
            RenderRadius = displayRadius_in;

            Force = Vector2D.ZeroVector; // 初始化力为零
            Acceleration = Vector2D.ZeroVector; // 初始化加速度为零
        }

        // 添加轨迹点
        public void AddTrajectoryPoint(Vector2D point)
        {
            _trajectoryPoints.Enqueue(point);
            if (_trajectoryPoints.Count > MAX_TRAJECTORY_POINTS)
            {
                _trajectoryPoints.Dequeue();
            }
        }

        // 获取轨迹点
        public Vector2D[] GetTrajectoryPoints()
        {
            return _trajectoryPoints.ToArray();
        }

        // 清除轨迹
        public void ClearTrajectory()
        {
            _trajectoryPoints.Clear();
        }
    }
}
