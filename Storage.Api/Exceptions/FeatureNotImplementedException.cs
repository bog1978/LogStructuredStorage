using MinimalApi.Hosting.Exceptions;

namespace Storage.Api.Exceptions;

internal sealed class FeatureNotImplementedException(string featureName)
    : InternalServerErrorException($"Функционал не реализован: {featureName.TrimEnd('.')}.");