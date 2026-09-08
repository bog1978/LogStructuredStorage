namespace Storage.Api.Lss.Model;

internal enum PartTypeEnum : byte
{
    /// <summary>
    /// Горячий раздел позволяет: читать, писать, переводить в тёплый.
    /// </summary>
    Hot = 1,

    /// <summary>
    /// Тёплый раздел позволяет: читать, переводить в холодный.
    /// </summary>
    Warm = 2,

    /// <summary>
    /// Холодный раздел позволяет: читать, удалять.
    /// </summary>
    Cold = 3,
    
    /// <summary>
    /// Раздел был удален в процессе применения политики хранения.
    /// </summary>
    Deleted = 4,
}