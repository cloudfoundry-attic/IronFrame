using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using NLog;

internal static class StringExtensionMethods
{
    public static SecureString ToSecureString(this string unsecuredString)
    {
        var securedString = new SecureString();
        foreach (var c in unsecuredString)
        {
            securedString.AppendChar(c);
        }

        securedString.MakeReadOnly();

        return securedString;
    }
}

internal static class FuncExtensionMethods
{
    /// <summary>
    /// Retries the specified action until it returns true or up to the specified number
    /// of retries.
    /// </summary>
    public static void RetryUpToNTimes(this Func<bool> action, int maxRetry, int sleepInMilli = 200)
    {
        for (int count = 0; count < maxRetry; count++)
        {
            if (action())
            {
                break;
            }
            else
            {
                Thread.Sleep(sleepInMilli);
            }
        }
    }
}

internal static class SecureStringExtensionMethod
{
    public static string ToUnsecureString(this System.Security.SecureString secureString)
    {
        if (secureString == null)
            throw new ArgumentNullException("securePassword");

        IntPtr unmanagedString = IntPtr.Zero;
        try
        {
            unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
            return Marshal.PtrToStringUni(unmanagedString);
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
        }
    }
}

internal static class DictionaryExtensionMethods
{

    /// <summary>
    /// Create a new dictionary which merges the right dictionary into the left one.
    /// Any conflicting keys are replaced by the right dictionary values.
    /// </summary>
    public static Dictionary<TKey, TVal> Merge<TKey, TVal>(this Dictionary<TKey, TVal> leftDic, Dictionary<TKey, TVal> rightDic)
    {
        Dictionary<TKey, TVal> merged = new Dictionary<TKey, TVal>(leftDic, leftDic.Comparer);

        foreach (var kv in rightDic)
        {
            merged[kv.Key] = rightDic[kv.Key];
        }

        return merged;
    }

    public static Dictionary<string, string> ToDictionary(this StringDictionary dictionary, IEqualityComparer<string> comparer)
    {
        var dict = new Dictionary<string, string>(comparer);

        foreach (DictionaryEntry de in dictionary)
        {
            dict[(string) de.Key] = (string)de.Value;
        }

        return dict;
    }
}

internal static class LoggerExtensionMethods
{
    public static void DebugException(this Logger logger, Exception exception)
    {
        logger.Log(LogLevel.Debug, String.Empty, exception);
    }

    public static void ErrorException(this Logger logger, Exception exception)
    {
        logger.Log(LogLevel.Error, String.Empty, exception);
    }

    public static void WarnException(this Logger logger, Exception exception)
    {
        logger.Log(LogLevel.Warn, String.Empty, exception);
    }
}
