using System;

public enum Easing
{
    EaseInSine, EaseOutSine, EaseInOutSine,
    EaseInCubic, EaseOutCubic, EaseInOutCubic,
    EaseInBounce, EaseOutBounce, EaseInOutBounce
}

public class EasingFunctions
{
    public static float ApplyEasingFunction(Easing function, float x)
    {
        switch (function)
        {
            case Easing.EaseInSine:
                return EaseInSine(x);
            case Easing.EaseOutSine:
                return EaseOutSine(x);
            case Easing.EaseInOutSine:
                return EaseInOutSine(x);
            case Easing.EaseInCubic:
                return EaseInCubic(x);
            case Easing.EaseOutCubic:
                return EaseOutCubic(x);
            case Easing.EaseInOutCubic:
                return EaseInOutCubic(x);
            case Easing.EaseInBounce:
                return EaseInBounce(x);
            case Easing.EaseOutBounce:
                return EaseOutBounce(x);
            case Easing.EaseInOutBounce:
                return EaseInOutBounce(x);
            default:
                return x;
        }
    }

    // Sine
    private static float EaseInSine(float x)
    {
        return 1 - (float)MathF.Cos((x * MathF.PI) / 2);
    }

    private static float EaseOutSine(float x)
    {
        return MathF.Sin((x * MathF.PI) / 2);
    }

    private static float EaseInOutSine(float x)
    {
        return -(MathF.Cos(MathF.PI * x) - 1) / 2;
    }

    // Cubic
    private static float EaseInCubic(float x)
    {
        return x * x * x;
    }

    private static float EaseOutCubic(float x)
    {
        return  1 - MathF.Pow(1 - x, 3);
    }

    private static float EaseInOutCubic(float x)
    {
        return x < 0.5 ? 4 * x * x * x : 1 - MathF.Pow(-2 * x + 2, 3) / 2;
    }

    private static float EaseInBounce(float x)
    {
        return 1 - EaseOutBounce(1 - x);
    }

    private static float EaseOutBounce(float x)
    {
        if (x < 1 / 2.75)
        {
            return 7.5625f * x * x;
        }
        else if (x < 2 / 2.75)
        {
            return 7.5625f * (x -= 1.5f / 2.75f) * x + 0.75f;
        }
        else if (x < 2.5 / 2.75)
        {
            return 7.5625f * (x -= 2.25f / 2.75f) * x + 0.9375f;
        }
        else
        {
            return 7.5625f * (x -= 2.625f / 2.75f) * x + 0.984375f;
        }
    }

    private static float EaseInOutBounce(float x)
    {
        return x < 0.5 ? (1 - EaseOutBounce(1 - 2 * x)) / 2 : (1 + EaseOutBounce(2 * x - 1)) / 2;
    }
}
