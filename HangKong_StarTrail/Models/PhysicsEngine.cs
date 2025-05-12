using System.Collections.Generic;
using System.Threading.Tasks;

namespace HangKong_StarTrail.Models
{
    public class PhysicsEngine
    {
        public List<Body> Bodies { get; private set; } = new List<Body>();

        public const double G = 6.67430e-11f; // 万有引力常数
        public double timeElapsed = 0; // 经过的时间
        public PhysicsEngine() { }

        public void Update(double deltaT)
        {
            // 并行更新每个天体的受力和加速度
            Parallel.ForEach(Bodies, body =>
            {
                if (body.IsCenter) return;

                Vector2D totalForce = Vector2D.ZeroVector;

                foreach (var other in Bodies)
                {
                    if (other == body) continue;
                    Vector2D r = other.Position - body.Position;
                    double distance = r.Length;
                    if (distance == 0) continue;  // 避免除零
                    double forceMagnitude = G * body.Mass * other.Mass / (distance * distance);
                    Vector2D forceDirection = r.Normalize();
                    totalForce += forceDirection * forceMagnitude;
                }

                body.Force = totalForce;
                body.Acceleration = totalForce / body.Mass;
            });

            // 串行更新速度和位置（因为并不耗时，且可能涉及 UI 的位置同步）
            foreach (var body in Bodies)
            {
                if (body.IsCenter) continue;
                body.Velocity += body.Acceleration * deltaT;
                body.Position += body.Velocity * deltaT;
            }

            // 更新经过的时间
            timeElapsed += deltaT;
        }
    }
}
