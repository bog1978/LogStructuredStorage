using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Storage.Api;

internal static class StorageTelemetry
{
    public static readonly ActivitySource Activity = new("Storage.Activity", "1.0");
    public static readonly Meter Meter = new("Storage.Meter", "1.0");

    /// <summary>
    /// Счетчик обращений к хранилищу. Должен содержать теги:<br/>
    /// 1. node - имя узла;<br/>
    /// 2. bucket - имя корзины;<br/>
    /// 3. operation - тип операции: 'read', 'write', 'delete';<br/>
    /// 4. status - статус операции: 'success', 'error'.
    /// </summary>
    public static readonly Counter<long> OperationCounter = Meter.CreateCounter<long>(
        "lss.operation.counter", description: "Счетчик обращений к хранилищу.");

    /// <summary>
    /// Гистограмма длительности обращения к хранилищу. Должна содержать теги:<br/>
    /// 1. node - имя узла;<br/>
    /// 2. bucket - имя корзины;<br/>
    /// 3. operation - тип операции: 'read', 'write', 'delete';<br/>
    /// В качестве единицы измерения используется миллисекунды.
    /// </summary>
    public static readonly Histogram<double> OperationDurationHistogram = Meter.CreateHistogram(
        "lss.operation.duration",
        unit: "мс",
        description: "Длительность операции в расчете на 1 файл (мс).",
        advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries = [10, 20, 30, 50, 100, 200, 300, 500, 1000]
        });

    /// <summary>
    /// Гистограмма размера файла. Должна содержать теги:<br/>
    /// 1. node - имя узла;<br/>
    /// 2. bucket - имя корзины;<br/>
    /// 3. operation - тип операции: 'read', 'write', 'delete';<br/>
    /// В качестве единицы измерения используется Мб.
    /// </summary>
    public static readonly Histogram<double> FileSizeHistogram = Meter.CreateHistogram(
        "lss.file.size",
        unit: "Мб",
        description: "Размер файла (Мб).",
        advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries = [1, 2, 3, 4, 5, 7, 10]
        });
}