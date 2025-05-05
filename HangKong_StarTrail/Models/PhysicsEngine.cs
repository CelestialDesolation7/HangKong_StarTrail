using System.Collections.Generic;
using System.Threading.Tasks;

namespace HangKong_StarTrail.Models
{
    public class PhysicsEngine
    {
        public List<Body> Bodies { get; private set; } = new List<Body>();

        public const double G = 6.67430e-11f; // 万有引力常数

        public PhysicsEngine() { }

        public void AddBody(Body body)
        {
            Bodies.Add(body);
        }

        public void Update(double deltaT)
        {
            // 更新所有天体的当前受到的力，并以此计算加速度
            foreach (var body in Bodies)
            {
                if (body.IsCenter) continue;  // 跳过中心天体，继续处理其他天体
                // 一个天体受到的力是所有其他天体对它的引力之和
                body.Force = Vector2D.ZeroVector;
                foreach (var other in Bodies)
                {
                    if (other == body) continue;
                    double distance = (body.Position - other.Position).Length;
                    double forceMagnitude = G * body.Mass * other.Mass / (distance * distance);
                    Vector2D forceDirection = (other.Position - body.Position).Normalize();

                    body.Force += forceDirection * forceMagnitude;
                }
                body.Acceleration = body.Force / body.Mass;
            }
            // 根据DeltaT更新所有天体的速度和位置
            foreach (var body in Bodies)
            {
                if (body.IsCenter) continue;
                body.Velocity += body.Acceleration * deltaT;
                body.Position += body.Velocity * deltaT;
            }
        }
    }
}
