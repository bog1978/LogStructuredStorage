using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Storage.Api.Internal;

/// <summary>
/// Набор вспомогательных методов для работы с Activity
/// </summary>
internal static class ActivityExt
{
    extension(Activity activity)
    {
        public Activity WithDisplayName(string displayName)
        {
            activity.DisplayName = displayName;
            return activity;
        }

        public Activity WithDisplayName([InterpolatedStringHandlerArgument] ref ActivityInterpolatedStringHandler handler)
        {
            activity.DisplayName = handler.Template;
            foreach (var argument in handler.Arguments) 
                activity.SetTag(argument.Key, argument.Value);
            return activity;
        }

        public Activity AddEvent([InterpolatedStringHandlerArgument] ref ActivityInterpolatedStringHandler handler)
        {
            var tags = new ActivityTagsCollection(handler.Arguments);
            var activityEvent = new ActivityEvent(handler.Template, tags: tags);
            activity.AddEvent(activityEvent);
            return activity;
        }

        public Activity SetError(Exception exception)
        {
            activity
                .AddException(exception)
                .SetStatus(ActivityStatusCode.Error, exception.Message);
            return activity;
        }
    }
}