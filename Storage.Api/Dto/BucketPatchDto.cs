using System.ComponentModel.DataAnnotations;

namespace Storage.Api.Dto;

internal sealed record BucketPatchDto(
    string? NodeId,
    [property: Range(typeof(TimeSpan), "00:00:00", "36500.00:00:00", ErrorMessage = "Срок хранения должен быть неотрицательным и не больше 100 лет.")]
    TimeSpan? TtlHot,
    [property: Range(typeof(TimeSpan), "00:00:00", "36500.00:00:00", ErrorMessage = "Срок хранения должен быть неотрицательным и не больше 100 лет.")]
    TimeSpan? TtlCold);
