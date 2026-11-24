using System;

namespace NobodyOrderAutomation;

internal static class ShipAllButtonLayout
{
    internal static float ResolveDimension(float nativeValue, float fallback)
    {
        float absolute = Math.Abs(nativeValue);
        if (float.IsNaN(absolute) || float.IsInfinity(absolute) || absolute < 1f)
        {
            return Math.Max(1f, Math.Abs(fallback));
        }

        return absolute;
    }

    internal static float ComputeLeftAnchoredX(float sourceX, float sourceWidth, float gap)
    {
        return sourceX - Math.Abs(sourceWidth) - Math.Max(0f, gap);
    }
}
