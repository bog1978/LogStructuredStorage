namespace Storage.Api.Lss.Model;

internal sealed record RetentionPolicy(
    TimeSpan TtlHot,
    TimeSpan TtlCold);