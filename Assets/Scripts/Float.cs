using UnityEngine;

class Float
{
    public static float Saturate(float t)
    {
        return Mathf.Clamp(t, 0.0f, 1.0f);
    }

    public static float CubicInterp(float start, float end, float ctrl, float t)
    {
        t = Saturate(t);
        var e0 = Mathf.Lerp(start, ctrl, t);
        var e1 = Mathf.Lerp(ctrl, end, t);

        return Mathf.Lerp(e0, e1, t);
    }
}
