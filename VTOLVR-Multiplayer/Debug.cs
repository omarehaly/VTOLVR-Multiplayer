public static class Debug
{
    private const string modPrefix = "[Multiplayer]";
    public static bool ShowDebugMessages = false;

    public static void Log(object message)
    {
        if (!ShowDebugMessages)
            return;

        UnityEngine.Debug.Log($"{modPrefix}{message}");
    }

    public static void LogWarning(object message)
    {
        if (!ShowDebugMessages)
            return;

        UnityEngine.Debug.LogWarning($"{modPrefix}{message}");
    }

    public static void LogError(object message)
    {
        if (!ShowDebugMessages)
            return;

        UnityEngine.Debug.LogError($"{modPrefix}{message}");
    }
}
