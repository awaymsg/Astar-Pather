using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class V3Curve
{
    public static Vector3 Lerp(Vector3 start, Vector3 end, float t)
    {
        t = Float.Saturate(t);
        return start + (end - start) * t;
    }

    public static Vector3 CubicInterp(Vector3 start, Vector3 end, Vector3 control, float t)
    {
        t = Float.Saturate(t);
        var e0 = V3Curve.Lerp(start, control, t);
        var e1 = V3Curve.Lerp(control, end, t);

        return V3Curve.Lerp(e0, e1, t);
    }
}
