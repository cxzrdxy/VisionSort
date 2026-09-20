namespace DobotArm.Client
{
    /// <summary>
    /// 点动的轴（枚举值＝官方 SetJOGCmd 的**正向**方向码；反向就是 +1）。
    /// 这套码在坐标模式下对应 A/B/C/D 四个轴，我们固定用坐标模式，
    /// 所以 A=X、B=Y、C=Z、D=R，与界面上的 X±/Y±/Z± 标签一致。
    /// </summary>
    public enum JogAxis
    {
        /// <summary>X 轴（正向码 1，反向 2）</summary>
        X = 1,
        /// <summary>Y 轴（正向码 3，反向 4）</summary>
        Y = 3,
        /// <summary>Z 轴（正向码 5，反向 6）</summary>
        Z = 5,
        /// <summary>R 轴（正向码 7，反向 8）</summary>
        R = 7
    }
}
