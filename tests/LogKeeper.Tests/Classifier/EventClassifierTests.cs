using LogKeeper.Classifier;
using LogKeeper.Integration.Data;

namespace LogKeeper.Tests.Classifier;

public class EventClassifierTests
{
    [Fact]
    public void ConfusionMatrix_And_MacroF1_HandExample()
    {
        int[] yTrue = { 0, 0, 1, 1, 2, 2 };
        int[] yPred = { 0, 1, 1, 1, 2, 0 };

        var m = ClassificationMetrics.ConfusionMatrix(yTrue, yPred, 3);

        Assert.Equal(1, m[0, 0]);
        Assert.Equal(1, m[0, 1]);
        Assert.Equal(1, m[2, 0]);
        // F1: клас 0 = 2·1/(2+1+1) = 0.5; клас 1 = 2·2/(4+1+0) = 0.8; клас 2 = 2·1/(2+0+1) = 2/3.
        Assert.Equal((0.5 + 0.8 + 2.0 / 3) / 3, ClassificationMetrics.MacroF1(m), 10);
        Assert.Equal(4.0 / 6, ClassificationMetrics.Accuracy(m), 10);
    }

    [Fact]
    public void BuildFeatures_HasFixedWidth()
    {
        var data = SyntheticLog.Generate(30, 1);
        var x = EventClassifier.BuildFeatures(data.Records, 60_000);

        Assert.Equal(30, x.Length);
        Assert.All(x, row => Assert.Equal(EventClassifier.InputSize, row.Length));
        Assert.Equal(1, x[0][0]); // перше вікно містить лише саму подію
    }

    [Fact]
    public void Train_OnSynthetic_BeatsChanceOnValidation()
    {
        var train = SyntheticLog.Generate(1500, 11);
        var valid = SyntheticLog.Generate(800, 22);

        var clf = EventClassifier.Train(train.Records, train.Labels, epochs: 15);
        var m = ClassificationMetrics.ConfusionMatrix(valid.Labels, clf.Predict(valid.Records), 3);

        Assert.True(ClassificationMetrics.MacroF1(m) > 0.6, $"macro-F1 = {ClassificationMetrics.MacroF1(m)}");
    }
}
