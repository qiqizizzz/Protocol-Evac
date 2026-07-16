/*
 * ┌──────────────────────────────────┐
 * │  描    述: 客户端轻量日志封装，Release 构建自动抹除日志
 * │  类    名: QLog.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
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

        /// <summary>
        /// 在编辑器中记录异常，并在所有构建中抛出异常
        /// </summary>
        /// <param name="exception">需要抛出的异常</param>
        public static void Throw(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

#if UNITY_EDITOR
            Debug.LogException(exception);
#endif
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
