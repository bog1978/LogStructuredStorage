using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Storage.Api;

internal static class StorageTelemetry
{
    public static readonly ActivitySource Activity = new("Storage.Activity", "1.0");
    public static readonly Meter Meter = new("Storage.Meter", "1.0");
    
    public static readonly Counter<long> ChannelResetSequence = Meter.CreateCounter<long>(
        "archive.channel.reset.sequence.total",
        description: "Общее количество сбросов последовательности сегментов канала.");

    public static readonly Counter<long> ChannelDownloadErrors = Meter.CreateCounter<long>(
        "archive.channel.download.errors.total",
        description: "Общее количество ошибок при скачивании плейлиста канала.");

    public static readonly Counter<long> ChannelOtherErrors = Meter.CreateCounter<long>(
        "archive.channel.other.errors.total",
        description: "Общее количество других неожиданных ошибок при мониторинге канала.");

    public static readonly Counter<long> ChannelNewSegments = Meter.CreateCounter<long>(
        "archive.channel.new.segments.total",
        description: "Общее количество обнаруженных новых сегментов в канале.");
}