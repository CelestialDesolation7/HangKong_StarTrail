using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SkiaSharp;
using System.Threading;

namespace HangKong_StarTrail.Models
{
    public class Renderer
    {
        public PhysicsEngine _physicsEngine;
        private object _renderLock = new object();
        private SKPaint _bodyPaint;
        private SKPaint _glowPaint;
        private SKPaint _highlightPaint;
        private SKPaint _starPaint;
        private readonly Random _random = new Random();
        private readonly List<Star> _stars = new List<Star>();
        private const int STAR_COUNT = 200;

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

            // 生成星星
            GenerateStars();
        }

        private void GenerateStars()
        {
            _stars.Clear();
            for (int i = 0; i < STAR_COUNT; i++)
            {
                _stars.Add(new Star
                {
                    X = (float)_random.NextDouble() * 1000,
                    Y = (float)_random.NextDouble() * 1000,
                    Size = (float)(_random.NextDouble() * 1.5 + 0.5),
                    Brightness = (float)(_random.NextDouble() * 0.5 + 0.5),
                    TwinkleSpeed = (float)(_random.NextDouble() * 0.02 + 0.01),
                    TwinklePhase = (float)(_random.NextDouble() * Math.PI * 2)
                });
            }
        }

        public void RenderBackground(SKCanvas canvas)
        {
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
                canvas.DrawCircle(star.X, star.Y, star.Size, _starPaint);

                // 为较亮的星星添加光晕
                if (alpha > 100)
                {
                    _starPaint.Color = new SKColor(255, 255, 255, (byte)(alpha * 0.3));
                    canvas.DrawCircle(star.X, star.Y, star.Size * 2, _starPaint);
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
            float radius = (float)body.RenderRadius;

            // 创建基础颜色
            var centerColor = new SKColor(
                body.DisplayColor.R,
                body.DisplayColor.G,
                body.DisplayColor.B,
                body.DisplayColor.A
            );
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

        public void UpdatePhysics(double deltaT)
        {
            _physicsEngine.Update(deltaT);
        }

        public void AddBody(Body body)
        {
            _physicsEngine.Bodies.Add(body);
        }

        public void RenderText(SKCanvas canvas)
        {
            // TODO: 实现文本渲染
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
