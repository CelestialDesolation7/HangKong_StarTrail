using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangKong_StarTrail.Models
{
    public struct Vector2D
    {
        private double x, y;
        private double length;

        public double X
        {
            get => x;
            set
            {
                x = value;
                length = Math.Sqrt(x * x + y * y);
            }
        }

        public double Y
        {
            get => y;
            set
            {
                y = value;
                length = Math.Sqrt(x * x + y * y);
            }
        }

        public double Length => length;

        public Vector2D(double x, double y)
        {
            this.x = x;
            this.y = y;
            length = Math.Sqrt(x * x + y * y);
        }

        // 运算符重载
        public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.X + b.X, a.Y + b.Y);
        public static Vector2D operator -(Vector2D a, Vector2D b) => new Vector2D(a.X - b.X, a.Y - b.Y);
        public static Vector2D operator *(Vector2D a, double k) => new Vector2D(a.X * k, a.Y * k);
        public static Vector2D operator *(double k, Vector2D a) => a * k;
        public static Vector2D operator /(Vector2D a, double k) => new Vector2D(a.X / k, a.Y / k);
        public static Vector2D operator -(Vector2D a) => new Vector2D(-a.X, -a.Y);

        public static Vector2D ZeroVector => new Vector2D(0, 0);

        public Vector2D Normalize() => length == 0 ? ZeroVector : this * (1.0 / length);
    }
}
