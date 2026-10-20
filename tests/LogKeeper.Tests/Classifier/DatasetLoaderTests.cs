using LogKeeper.Classifier;
using Xunit;

namespace LogKeeper.Tests.Classifier;

public class DatasetLoaderTests
{
    [Fact]
    public void LoadEvents_ReadsSmallCsv_AndClassCountsCountsClasses()
    {
        string path = Path.GetTempFileName(); // Створюємо тимчасовий файл.

        try
        {
            File.WriteAllText(path, "time,message,class\n" + "10:00,start,0\n" + "10:01,error,1\n" + "10:02,warning,2\n" + "10:03,finish,0\n"); // Записуємо маленький тестовий CSV.

            var events = DatasetLoader.LoadEvents(path); // Завантажуємо записи з CSV.

            Assert.Equal(4, events.Count); // Перевіряємо кількість записів.
            
            Assert.Equal("10:00", events[0]["time"]); // Перевіряємо, що назви колонок і значення прочитані правильно.
            Assert.Equal("start", events[0]["message"]);
            Assert.Equal("0", events[0]["class"]);

            var counts = DatasetLoader.ClassCounts(events); // Підраховуємо кількість записів кожного класу.

            Assert.Equal(2, counts[0]);
            Assert.Equal(1, counts[1]);
            Assert.Equal(1, counts[2]);
        }
        finally
        {
            File.Delete(path); // Видаляємо тимчасовий файл після тесту.
        }
    }

    [Fact]
    public void LoadEvents_HeaderOnly_ReturnsEmptyList()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "time,message,class\n"); // CSV містить тільки заголовок без записів.

            var events = DatasetLoader.LoadEvents(path); 

            Assert.Empty(events); // Записів бути не повинно.

            var counts = DatasetLoader.ClassCounts(events); // Для порожнього списку всі лічильники мають бути нульовими.

            Assert.Equal(0, counts[0]);
            Assert.Equal(0, counts[1]);
            Assert.Equal(0, counts[2]);
        }
        finally
        {
            File.Delete(path); 
        }
    }
}