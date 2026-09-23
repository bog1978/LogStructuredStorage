using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Storage.Api;

internal static class StorageTelemetry
{
    public static readonly ActivitySource Activity = new("Storage.Activity", "1.0");
    public static readonly Meter Meter = new("Storage.Meter", "1.0");
    
    // Счётчики
    public static readonly Counter<long> BlobSuccessCounter = Meter.CreateCounter<long>(
        "archive.blob.success",
        description: "Общее количество успешных обращении к хранилищу.");

    public static readonly Counter<long> BlobErrorCounter = Meter.CreateCounter<long>(
        "archive.blob.error",
        description: "Общее количество ошибок при обращении к хранилищу.");

    public static readonly Histogram<double> BlobSizeHistogram = Meter.CreateHistogram(
        "archive.blob.size",
        unit: "Мб",
        description: "Размер двоичных данных (Мб).",
        advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries = [1, 2, 3, 4, 5, 7, 10]
        });

    public static readonly Histogram<double> BlobAgeHistogram = Meter.CreateHistogram(
        "archive.blob.age",
        unit: "мин",
        description: "Возраст двоичных данных (мин).",
        advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries = [1, 3, 5, 10, 20, 30, 60]
        });

    public static readonly Histogram<double> BlobDurationHistogram = Meter.CreateHistogram(
        "archive.blob.duration",
        unit: "мс",
        description: "Длительность операции в расчете на 1 файл (мс).",
        advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries = [10, 20, 30, 50, 100, 200, 300, 500, 1000]
        });
}