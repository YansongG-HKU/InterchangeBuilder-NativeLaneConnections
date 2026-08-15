using Colossal.Logging;

namespace InterchangeBuilder.LaneConnections;

internal static class UpgradeLog
{
    private static readonly ILog Logger = LogManager
        .GetLogger("InterchangeBuilder.LaneConnections")
        .SetShowsErrorsInUI(false);

    internal static void Info(string message) => Logger.Info(message);

    internal static void Warn(string message) => Logger.Warn(message);

    internal static void Error(string message) => Logger.Error(message);
}
