/*
 * ┌──────────────────────────────────┐
 * │  描    述: 客户端轻量日志封装，Release 构建自动抹除日志
 * │  类    名: QLog.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Utils.log
{
    public static class QLog
    {
        [Conditional("UNITY_EDITOR")]
        public static void Info(object msg) => Debug.Log(msg);

        [Conditional("UNITY_EDITOR")]
        public static void Warning(object msg) => Debug.LogWarning(msg);

        [Conditional("UNITY_EDITOR")]
        public static void Error(object msg) => Debug.LogError(msg);
    }
}