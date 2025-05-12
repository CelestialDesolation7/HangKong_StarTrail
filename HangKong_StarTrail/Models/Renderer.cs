using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SkiaSharp;
using System.Threading;
using System.Diagnostics;

namespace HangKong_StarTrail.Models
{
    public class Renderer
    {
        public PhysicsEngine _physicsEngine;
        private double _timeStep;
        private double _pixelToDistanceRatio;
        private object _renderLock = new object();
        private SKPaint _bodyPaint;
        private SKPaint _glowPaint;
        private SKPaint _highlightPaint;
        private SKPaint _starPaint;
        private float _starOffsetX;
        private float _starOffsetY;
        private readonly Random _random = new Random();
        private readonly List<Star> _stars = new List<Star>();
        private const int STAR_COUNT = 200;
        private float _canvasWidth;
        private float _canvasHeight;
        // 最小显示半径
        public double _minimumDisplayRadius = 15;
        public double _velocityLengthFactor = 60;
        public void InitializeRenderer()
        {
            _starOffsetX = 0;
            _starOffsetY = 0;
        }
        private class Star
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Size { get; set; }
            public float Brightness { get; set; }
            public float TwinkleSpeed { get; set; }
            public float TwinklePhase { get; set; }
        }

        public Renderer(PhysicsEngine physicsEngine)
        {
            _physicsEngine = physicsEngine;
            _timeStep = 1.0;
            _pixelToDistanceRatio = 1.0;

            // 初始化天体画笔
            _bodyPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            // 初始化气辉画笔
            _glowPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                BlendMode = SKBlendMode.Screen
            };

            // 初始化高光画笔
            _highlightPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                BlendMode = SKBlendMode.Screen
            };

            // 初始化星星画笔
            _starPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                BlendMode = SKBlendMode.Screen
            };
        }

        public void SetTimeStep(double timeStep)
        {
            _timeStep = timeStep;
        }

        public void SetPixelToDistanceRatio(double ratio)
        {
            _pixelToDistanceRatio = ratio;
        }

        private void GenerateStars(float width, float height)
        {
            _canvasWidth = width;
            _canvasHeight = height;
            _stars.Clear();

            for (int i = 0; i < STAR_COUNT; i++)
            {
                _stars.Add(new Star
                {
                    X = (float)_random.NextDouble() * width,
                    Y = (float)_random.NextDouble() * height,
                    Size = (float)(_random.NextDouble() * 1.5 + 0.5),
                    Brightness = (float)(_random.NextDouble() * 0.5 + 0.5),
                    TwinkleSpeed = (float)(_random.NextDouble() * 0.02 + 0.01),
                    TwinklePhase = (float)(_random.NextDouble() * Math.PI * 2)
                });
            }
        }

        public void RenderBackground(SKCanvas canvas)
        {
            // 获取画布尺寸
            float width = canvas.DeviceClipBounds.Width;
            float height = canvas.DeviceClipBounds.Height;

            // 如果画布尺寸改变或星星未生成，重新生成星星
            if (_stars.Count == 0 || width != _canvasWidth || height != _canvasHeight)
            {
                GenerateStars(width, height);
            }

            // 绘制黑色背景
            canvas.Clear(SKColors.Black);

            // 绘制星星
            foreach (var star in _stars)
            {
                // 计算闪烁效果
                float twinkle = (float)(Math.Sin(DateTime.Now.Ticks * star.TwinkleSpeed + star.TwinklePhase) * 0.3 + 0.7);
                byte alpha = (byte)(star.Brightness * twinkle * 255);

                // 绘制星星
                _starPaint.Color = new SKColor(255, 255, 255, alpha);
                canvas.DrawCircle((star.X + _starOffsetX) % width, (star.Y + _starOffsetY) % height, star.Size, _starPaint);

                // 为较亮的星星添加光晕
                if (alpha > 100)
                {
                    _starPaint.Color = new SKColor(255, 255, 255, (byte)(alpha * 0.3));
                    canvas.DrawCircle((star.X + _starOffsetX + width) % width, (star.Y + _starOffsetY + height) % height, star.Size * 2, _starPaint);
                }
            }
        }

        public void RenderBodies(SKCanvas canvas)
        {
            // 使用并行渲染
            Parallel.ForEach(_physicsEngine.Bodies, body =>
            {
                lock (_renderLock)
                {
                    RenderBody(canvas, body);
                }
            });
        }

        private void RenderBody(SKCanvas canvas, Body body)
        {
            var displayPos = body.DisplayPosition;
            // Calculate display radius based on physical radius and pixel-to-distance ratio
            float calculatedRadius = (float)(body.PhysicalRadius * _pixelToDistanceRatio);
            // Use MIN_DISPLAY_RADIUS if calculated radius is too small
            float radius = Math.Max(calculatedRadius, (float)_minimumDisplayRadius);

            // 创建基础颜色
            var centerColor = body.DisplayColor;
            var edgeColor = new SKColor(
                (byte)(centerColor.Red * 0.5),
                (byte)(centerColor.Green * 0.5),
                (byte)(centerColor.Blue * 0.5),
                centerColor.Alpha
            );

            // 绘制主体
            using (var shader = SKShader.CreateRadialGradient(
                new SKPoint((float)displayPos.X, (float)displayPos.Y),
                radius,
                new[] { centerColor, edgeColor },
                new[] { 0.0f, 1.0f },
                SKShaderTileMode.Clamp))
            {
                _bodyPaint.Shader = shader;
                canvas.DrawCircle((float)displayPos.X, (float)displayPos.Y, radius, _bodyPaint);
            }

            // 绘制气辉效果
            using (var glowShader = SKShader.CreateRadialGradient(
                new SKPoint((float)displayPos.X, (float)displayPos.Y),
                radius * 3.0f,  // 增大气辉范围
                new[]
                {
                    new SKColor(centerColor.Red, centerColor.Green, centerColor.Blue, 0),
                    new SKColor(centerColor.Red, centerColor.Green, centerColor.Blue, 50),  // 增加气辉亮度
                    new SKColor(centerColor.Red, centerColor.Green, centerColor.Blue, 0)
                },
                new[] { 0.0f, 0.5f, 1.0f },
                SKShaderTileMode.Clamp))
            {
                _glowPaint.Shader = glowShader;
                canvas.DrawCircle((float)displayPos.X, (float)displayPos.Y, radius * 3.0f, _glowPaint);
            }

            // 绘制柔和的高光
            _highlightPaint.Color = new SKColor(255, 255, 255, 25);
            float highlightRadius = radius * 0.3f;
            float highlightOffset = radius * 0.2f;
            canvas.DrawCircle(
                (float)displayPos.X - highlightOffset,
                (float)displayPos.Y - highlightOffset,
                highlightRadius,
                _highlightPaint
            );
        }

        public void RenderVelocityArrow(SKCanvas canvas)
        {
            foreach (var body in _physicsEngine.Bodies)
            {
                // 计算箭头长度（像素）
                double arrowLength = _velocityLengthFactor * _timeStep * body.Velocity.Length * _pixelToDistanceRatio;

                // 如果速度太小，不绘制箭头
                if (arrowLength < 1) continue;

                // 计算箭头终点
                Vector2D velocityDirection = body.Velocity.Normalize();
                Vector2D arrowEnd = body.DisplayPosition + velocityDirection * arrowLength;

                // 创建箭头画笔
                using var arrowPaint = new SKPaint
                {
                    Color = body.DisplayColor,
                    StrokeWidth = 2,
                    IsAntialias = true
                };

                // 绘制箭头主体
                canvas.DrawLine(
                    (float)body.DisplayPosition.X,
                    (float)body.DisplayPosition.Y,
                    (float)arrowEnd.X,
                    (float)arrowEnd.Y,
                    arrowPaint
                );

                // 计算箭头头部
                double arrowHeadLength = Math.Min(arrowLength * 0.2, 10); // 箭头头部长度
                double arrowHeadAngle = Math.PI / 6; // 30度角

                // 计算箭头头部的两个点
                Vector2D backDirection = -velocityDirection;
                Vector2D perpendicular = new Vector2D(-velocityDirection.Y, velocityDirection.X);

                Vector2D arrowHead1 = arrowEnd + backDirection * arrowHeadLength + perpendicular * arrowHeadLength * Math.Sin(arrowHeadAngle);
                Vector2D arrowHead2 = arrowEnd + backDirection * arrowHeadLength - perpendicular * arrowHeadLength * Math.Sin(arrowHeadAngle);

                // 绘制箭头头部
                canvas.DrawLine(
                    (float)arrowEnd.X,
                    (float)arrowEnd.Y,
                    (float)arrowHead1.X,
                    (float)arrowHead1.Y,
                    arrowPaint
                );
                canvas.DrawLine(
                    (float)arrowEnd.X,
                    (float)arrowEnd.Y,
                    (float)arrowHead2.X,
                    (float)arrowHead2.Y,
                    arrowPaint
                );
            }
        }

        public void UpdatePhysics(double deltaT)
        {
            _physicsEngine.Update(deltaT);
        }

        public void AddBody(Body body)
        {
            _physicsEngine.Bodies.Add(body);
        }

        public void UpdateStarOffset(float cameraMoveX, float cameraMoveY)
        {
            _starOffsetX = (_starOffsetX + cameraMoveX) % _canvasWidth;
            _starOffsetY = (_starOffsetY + cameraMoveY) % _canvasHeight;
        }

        public void Dispose()
        {
            _bodyPaint?.Dispose();
            _glowPaint?.Dispose();
            _highlightPaint?.Dispose();
            _starPaint?.Dispose();
        }
    }
}
