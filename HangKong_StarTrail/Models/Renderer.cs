using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SkiaSharp;

namespace HangKong_StarTrail.Models
{
    public class Renderer
    {
        public Renderer() { }

        public void RenderBodies(List<Body> bodies, SKCanvas canvas)
        {
            foreach (var body in bodies)
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

        public void RenderTrajectory(List<Body> bodies, SKCanvas canvas)
        {
            // TODO: 实现轨迹渲染
        }

        public void RenderText(List<Body> bodies, SKCanvas canvas)
        {
            // TODO: 实现文本渲染
        }
    }
}
