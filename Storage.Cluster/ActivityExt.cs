using System.Diagnostics;

namespace Storage.Cluster;

/// <summary>
/// Набор вспомогательных методов для работы с Activity
/// </summary>
public static class ActivityExt
{
    public static Activity WithDisplayName(this Activity activity, string displayName)
    {
        activity.DisplayName = displayName;
        return activity;
    }
}