namespace Storage.Api.Exceptions;

internal sealed class InvalidBucketNameException(string? bucketName)
    : BadRequestException($"Недопустимое имя корзины '{bucketName}'. Разрешены латинские буквы, цифры, '_' и '-'; длина — от 1 до 16 символов. Имена устройств Windows запрещены.");