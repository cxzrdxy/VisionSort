using System;

namespace DobotArm.Client
{
    /// <summary>机械臂位姿（x/y/z/r + 4 个关节角）。</summary>
    public struct ArmPose
    {
        public double X;
        public double Y;
        public double Z;
        public double R;
        public double J1;
        public double J2;
        public double J3;
        public double J4;

        public override string ToString()
        {
            return string.Format("X={0:F2} Y={1:F2} Z={2:F2} R={3:F2}", X, Y, Z, R);
        }
    }

    /// <summary>机械臂通信异常（桥接进程不可达、超时、应答格式不对、机械臂拒绝命令等）。</summary>
    public sealed class ArmException : Exception
    {
        public ArmException(string message) : base(message) { }
        public ArmException(string message, Exception inner) : base(message, inner) { }
    }
}
