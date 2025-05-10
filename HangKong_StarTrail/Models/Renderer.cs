using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SkiaSharp;

namespace HangKong_StarTrail.Models
{
    public class Renderer(PhysicsEngine _physicsEngine)
    {
        public PhysicsEngine _physicsEngine = _physicsEngine;

        public void RenderBodies(SKCanvas canvas)
        {
            foreach (var body in _physicsEngine.Bodies)
            {
                using var paint = new SKPaint
                {
                    Color = new SKColor(
                        body.DisplayColor.R,
                        body.DisplayColor.G,
                        body.DisplayColor.B,
                        body.DisplayColor.A),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };

                canvas.DrawCircle(
                    (float)body.DisplayPosition.X,
                    (float)body.DisplayPosition.Y,
                    body.RenderRadius,
                    paint);
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

        public void RenderTrajectory(SKCanvas canvas)
        {
            // TODO: 实现轨迹渲染
        }

        public void RenderText(SKCanvas canvas)
        {
            // TODO: 实现文本渲染
        }
    }
}
