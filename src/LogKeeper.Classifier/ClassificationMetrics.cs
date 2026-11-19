namespace LogKeeper.Classifier;

/// <summary>Матриця помилок, accuracy і macro-F1 для класифікатора.</summary>
public static class ClassificationMetrics
{
    /// <summary>m[true, predicted] — кількість прикладів.</summary>
    public static int[,] ConfusionMatrix(IReadOnlyList<int> yTrue, IReadOnlyList<int> yPred, int classCount)
    {
        ArgumentNullException.ThrowIfNull(yTrue);
        ArgumentNullException.ThrowIfNull(yPred);
        if (yTrue.Count != yPred.Count)
            throw new ArgumentException("yTrue і yPred мають бути однакової довжини.");

        var m = new int[classCount, classCount];
        for (int i = 0; i < yTrue.Count; i++)
            m[yTrue[i], yPred[i]]++;
        return m;
    }

    public static double Accuracy(int[,] m)
    {
        int k = m.GetLength(0), correct = 0, total = 0;
        for (int i = 0; i < k; i++)
            for (int j = 0; j < k; j++)
            {
                total += m[i, j];
                if (i == j) correct += m[i, j];
            }
        return total == 0 ? 0 : (double)correct / total;
    }

    /// <summary>F1 кожного класу; клас без прикладів і без прогнозів має F1 = 0.</summary>
    public static double[] PerClassF1(int[,] m)
    {
        int k = m.GetLength(0);
        var f1 = new double[k];
        for (int c = 0; c < k; c++)
        {
            int tp = m[c, c], fp = 0, fn = 0;
            for (int o = 0; o < k; o++)
            {
                if (o == c) continue;
                fp += m[o, c];
                fn += m[c, o];
            }
            f1[c] = tp == 0 ? 0 : 2.0 * tp / (2.0 * tp + fp + fn);
        }
        return f1;
    }

    public static double MacroF1(int[,] m) => PerClassF1(m).Average();
}
