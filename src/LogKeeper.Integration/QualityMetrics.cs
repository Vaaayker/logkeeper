namespace LogKeeper.Integration;

/// <summary>Єдина метрика якості: S, R, T і Q = alpha*S + beta*R + gamma*T.</summary>
public static class QualityMetrics
{
    public static double ComputeQ(double s, double r, double t, double alpha, double beta, double gamma)
        => alpha * s + beta * r + gamma * t;
}
