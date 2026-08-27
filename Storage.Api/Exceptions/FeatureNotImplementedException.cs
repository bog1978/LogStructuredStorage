namespace Storage.Api.Exceptions;

public sealed class FeatureNotImplementedException(string featureName)
    : InternalServerErrorException($"Функционал не реализован: {featureName.TrimEnd('.')}.");