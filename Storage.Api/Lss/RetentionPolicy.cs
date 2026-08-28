namespace Storage.Api.Lss;

internal sealed record RetentionPolicy(TimeSpan TtlHot, TimeSpan TtlCold);