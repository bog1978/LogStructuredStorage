namespace Storage.Api.Exceptions;

internal sealed class InvalidFileKeyException(string fileKey)
    : BadRequestException($"Некорректный ключ файла '{fileKey}'.");
