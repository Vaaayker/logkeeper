namespace LogKeeper.Classifier;

/// <summary>Багатошаровий перцептрон, написаний самостійно (без ML-бібліотек).</summary>
public sealed class Mlp
{
    // Розміри всіх шарів мережі.
    // Наприклад: { 8, 16, 3 }:
    // 8 входів, 16 нейронів прихованого шару та 3 виходи.
    private readonly int[] layerSizes;

    // Ваги між сусідніми шарами.
    private readonly double[][][] weights;

    // Зсуви (bias) для кожного шару, крім вхідного.
    private readonly double[][] biases;

    public Mlp(int[] layerSizes, int seed)
    {
        if (layerSizes == null || layerSizes.Length < 2)    
            throw new ArgumentException("Мережа повинна містити щонайменше вхідний і вихідний шари.");

        // Перевіряємо розміри шарів.
        for (int i = 0; i < layerSizes.Length; i++)
        {
            if (layerSizes[i] <= 0)
                throw new ArgumentException("Розмір кожного шару повинен бути більшим за 0.");
        }

        this.layerSizes = (int[])layerSizes.Clone();

        weights = new double[layerSizes.Length - 1][][];
        biases = new double[layerSizes.Length - 1][];

        var random = new Random(seed);

        // Ініціалізація ваг для кожної пари сусідніх шарів.
        for (int layer = 0; layer < layerSizes.Length - 1; layer++)
        {
            int fanIn = layerSizes[layer];
            int fanOut = layerSizes[layer + 1];

            weights[layer] = new double[fanOut][];
            biases[layer] = new double[fanOut];

            // He initialization: N(0, sqrt(2 / fanIn)).
            double stdDev = Math.Sqrt(2.0 / fanIn);

            for (int neuron = 0; neuron < fanOut; neuron++)
            {
                weights[layer][neuron] = new double[fanIn];

                for (int input = 0; input < fanIn; input++)
                {
                    weights[layer][neuron][input] = NextGaussian(random) * stdDev;
                }

                // Зсуви починаються з нуля.
                biases[layer][neuron] = 0.0;
            }
        }
    }

    // Генерує випадкове число з нормального розподілу N(0, 1).
    private static double NextGaussian(Random random)
    {
        double u1 = 1.0 - random.NextDouble();
        double u2 = 1.0 - random.NextDouble();

        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    // ReLU: від'ємні значення стають 0.
    public static double[] Relu(double[] z)
    {
        var result = new double[z.Length];

        for (int i = 0; i < z.Length; i++)
        {
            if (z[i] > 0.0)
                result[i] = z[i];
            else
                result[i] = 0.0;
        }

        return result;
    }

    // Softmax перетворює значення у ймовірності.
    public static double[] Softmax(double[] z)
    {
        var result = new double[z.Length];

        // Знаходимо максимальне значення вручну.
        double max = z[0];

        for (int i = 1; i < z.Length; i++)
        {
            if (z[i] > max)
                max = z[i];
        }

        double sum = 0.0;

        // Віднімаємо max для числової стабільності.
        for (int i = 0; i < z.Length; i++)
        {
            result[i] = Math.Exp(z[i] - max);
            sum += result[i];
        }

        // Нормалізуємо значення.
        for (int i = 0; i < result.Length; i++)
        {
            result[i] /= sum;
        }

        return result;
    }

    // Прямий прохід через нейронну мережу.
    public double[] Forward(double[] x)
    {
        if (x == null || x.Length != layerSizes[0])
            throw new ArgumentException($"Очікується {layerSizes[0]} вхідних значень.");

        double[] activation = (double[])x.Clone();

        // Проходимо через усі шари.
        for (int layer = 0; layer < weights.Length; layer++)
        {
            int neuronCount = weights[layer].Length;
            var z = new double[neuronCount];

            for (int neuron = 0; neuron < neuronCount; neuron++)
            {
                double sum = biases[layer][neuron];

                for (int input = 0; input < activation.Length; input++)
                {
                    sum += weights[layer][neuron][input] * activation[input];
                }

                z[neuron] = sum;
            }

            // На вихідному шарі — Softmax,
            // на прихованих шарах — ReLU.
            if (layer == weights.Length - 1)
                activation = Softmax(z);
            else
                activation = Relu(z);
        }

        return activation;
    }

    // Повертає номер класу з найбільшою ймовірністю
    // для кожного вхідного рядка.
    public int[] Predict(double[][] x)
    {
        var predictions = new int[x.Length];

        for (int row = 0; row < x.Length; row++)
        {
            double[] probabilities = Forward(x[row]);

            int bestClass = 0;
            double bestProbability = probabilities[0];

            // Шукаємо індекс найбільшої ймовірності.
            for (int i = 1; i < probabilities.Length; i++)
            {
                if (probabilities[i] > bestProbability)
                {
                    bestProbability = probabilities[i];
                    bestClass = i;
                }
            }

            predictions[row] = bestClass;
        }

        return predictions;
    }


    // Навчання з типовими параметрами.
    // 500 епох, швидкість навчання 0.01.
    public void Fit(double[][] x, int[] y)
    {
        Fit(x, y, 500, 0.01);
    }

    // Навчання методом зворотного поширення помилки та SGD.
    public void Fit(double[][] x, int[] y, int epochs, double learningRate)
    {
        // Перевіряємо вхідні дані.
        if (x == null)
            throw new ArgumentNullException(nameof(x));

        if (y == null)
            throw new ArgumentNullException(nameof(y));

        if (x.Length == 0 || x.Length != y.Length)
            throw new ArgumentException(
                "Кількість прикладів повинна збігатися з кількістю міток і бути більшою за нуль.");

        if (epochs <= 0)
            throw new ArgumentOutOfRangeException(nameof(epochs));

        if (!double.IsFinite(learningRate) || learningRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(learningRate));

        // Перевіряємо розмірність і значення навчальних прикладів.
        for (int sample = 0; sample < x.Length; sample++)
        {
            if (x[sample] == null || x[sample].Length != layerSizes[0])
                throw new ArgumentException("Неправильна кількість вхідних ознак.");

            for (int i = 0; i < x[sample].Length; i++)
            {
                if (!double.IsFinite(x[sample][i]))
                    throw new ArgumentException("Ознаки повинні бути скінченними числами.");
            }

            if (y[sample] < 0 || y[sample] >= layerSizes[layerSizes.Length - 1])
                throw new ArgumentException("Неправильний номер класу.");
        }

        int lastLayer = weights.Length - 1;

        // Одна епоха — один повний прохід по навчальних даних.
        for (int epoch = 0; epoch < epochs; epoch++)
        {
            for (int sample = 0; sample < x.Length; sample++)
            {
                // Зберігаємо виходи кожного шару.
                double[][] activations = new double[layerSizes.Length][];

                // Вхідний шар отримує ознаки поточного прикладу.
                activations[0] = x[sample];

                // КРОК 1. Пряме поширення сигналу.
                for (int layer = 0; layer < weights.Length; layer++)
                {
                    int neuronCount = layerSizes[layer + 1];
                    double[] z = new double[neuronCount];

                    for (int neuron = 0; neuron < neuronCount; neuron++)
                    {
                        double sum = biases[layer][neuron];

                        for (int input = 0; input < layerSizes[layer]; input++)
                        {
                            sum += weights[layer][neuron][input]
                                 * activations[layer][input];
                        }

                        z[neuron] = sum;
                    }

                    // Приховані шари використовують ReLU.
                    // Вихідний шар використовує Softmax.
                    if (layer == lastLayer)
                        activations[layer + 1] = Softmax(z);
                    else
                        activations[layer + 1] = Relu(z);
                }

                // КРОК 2. Обчислюємо помилку вихідного шару.
                double[][] deltas = new double[weights.Length][];

                int outputCount = layerSizes[layerSizes.Length - 1];
                deltas[lastLayer] = new double[outputCount];

                // Для Softmax + cross-entropy похідна:
                // прогнозована ймовірність мінус правильна мітка.
                for (int neuron = 0; neuron < outputCount; neuron++)
                {
                    double target = neuron == y[sample] ? 1.0 : 0.0;

                    deltas[lastLayer][neuron] =
                        activations[lastLayer + 1][neuron] - target;
                }

                // КРОК 3. Зворотне поширення помилки.
                // Рухаємося від останнього прихованого шару до першого.
                for (int layer = lastLayer - 1; layer >= 0; layer--)
                {
                    int neuronCount = layerSizes[layer + 1];
                    deltas[layer] = new double[neuronCount];

                    for (int neuron = 0; neuron < neuronCount; neuron++)
                    {
                        double error = 0.0;

                        // Враховуємо вплив нейрона на наступний шар.
                        for (int nextNeuron = 0;
                             nextNeuron < layerSizes[layer + 2];
                             nextNeuron++)
                        {
                            error += weights[layer + 1][nextNeuron][neuron]
                                   * deltas[layer + 1][nextNeuron];
                        }

                        // Похідна ReLU:
                        // 1 для додатних значень, 0 для решти.
                        if (activations[layer + 1][neuron] > 0.0)
                            deltas[layer][neuron] = error;
                        else
                            deltas[layer][neuron] = 0.0;
                    }
                }

                // КРОК 4. SGD — оновлюємо ваги після кожного прикладу.
                // Усі помилки вже обчислені зі старими вагами.
                for (int layer = 0; layer < weights.Length; layer++)
                {
                    for (int neuron = 0;
                         neuron < layerSizes[layer + 1];
                         neuron++)
                    {
                        for (int input = 0;
                             input < layerSizes[layer];
                             input++)
                        {
                            double gradient =
                                deltas[layer][neuron]
                                * activations[layer][input];

                            weights[layer][neuron][input] -=
                                learningRate * gradient;
                        }

                        // Також оновлюємо зсув нейрона.
                        biases[layer][neuron] -=
                            learningRate * deltas[layer][neuron];
                    }
                }
            }
        }
    }
}