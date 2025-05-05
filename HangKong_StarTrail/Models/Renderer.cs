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
        private readonly SKCanvas _canvas;
        private readonly SKImageInfo _info;

        public Renderer(SKCanvas canvas, SKImageInfo info)
        {
            _canvas = canvas;
            _info = info;
        }

        public void RenderBodies()
        {
            
            // 1. 保存画布状态
            _canvas.Save();

            float w = _info.Width;
            float h = _info.Height;

            /*
            // 2. 创建裁剪路径（只右上角为圆角，其余为直角）
            float radius = 35;
            

            var path = new SKPath();
            path.MoveTo(0, 0);
            path.LineTo(w - radius, 0);
            path.ArcTo(radius, radius, 0, SKPathArcSize.Small, SKPathDirection.Clockwise, w, radius);
            path.LineTo(w, h);
            path.LineTo(0, h);
            path.Close();

            // 3. 应用裁剪
            _canvas.ClipPath(path, SKClipOperation.Intersect, antialias: true);
            */

            // 4. 开始绘制内容（例如一个背景渐变）
            using var paint = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(0, h),
                    new SKColor[] { SKColors.MidnightBlue, SKColors.DarkSlateBlue },
                    null,
                    SKShaderTileMode.Clamp),
                IsAntialias = true
            };

            _canvas.DrawRect(0, 0, w, h, paint);

            // 5. 恢复画布状态
            _canvas.Restore();
        }

        public void RenderTrajectory()
        {
            // Demo中暂不实现
        }

        public void RenderText()
        {
            // Demo中暂不实现
        }
    }
}
