
using System;
using System.Collections.Generic;
using LogKeeper.Core;

namespace LogKeeper.Classifier;

/// <summary>
/// Побудова числових ознак із вікна історії записів журналу.
/// </summary>
public static class FeatureBuilder
{
    // У форматі журналу передбачено 10 категорій: від 0 до 9.
    public const int CategoryCount = 10;

    // Чотири загальні ознаки та десять лічильників категорій.
    public const int FeatureCount = 4 + CategoryCount;

    public static double[] Build(IReadOnlyList<LogRecord> window)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));

        // Масив завжди має однакову довжину — 14 елементів.
        double[] features = new double[FeatureCount];

        // Для порожнього вікна повертаємо 14 нулів.
        if (window.Count == 0)
            return features;

        double severitySum = 0.0;
        int criticalCount = 0;

        // Перебираємо всі записи у вікні.
        for (int i = 0; i < window.Count; i++)
        {
            LogRecord record = window[i];

            // Перевіряємо допустиме значення категорії.
            if (record.Category >= CategoryCount)
                throw new ArgumentException(
                    "Категорія повинна бути в діапазоні від 0 до 9.",
                    nameof(window));

            // Накопичуємо значення Severity.
            severitySum += record.Severity;

            // Підраховуємо критичні події.
            if (record.Critical)
                criticalCount++;

            // Збільшуємо лічильник відповідної категорії.
            // Категорія 0 -> features[4].
            // Категорія 9 -> features[13].
            features[4 + record.Category]++;
        }

        // 1. Кількість подій у вікні фіксованої тривалості.
        features[0] = window.Count;

        // 2. Зміна MetricValue між останнім і першим записами.
        // Записи мають бути впорядковані за часом.
        features[1] = (double)window[window.Count - 1].MetricValue
                    - window[0].MetricValue;

        // 3. Середнє значення Severity.
        features[2] = severitySum / window.Count;

        // 4. Частка критичних подій.
        features[3] = (double)criticalCount / window.Count;

        return features;
    }
}
