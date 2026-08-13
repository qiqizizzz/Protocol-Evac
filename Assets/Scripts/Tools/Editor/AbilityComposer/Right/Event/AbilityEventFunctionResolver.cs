/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 动画事件方法解析器，扫描预览层级中的合法 Function
 * │  类    名: AbilityEventFunctionResolver.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Tools.Editor.AbilityComposer.Right.Event
{
    public static class AbilityEventFunctionResolver
    {
        // 从动画采样根节点及其子层级按接收组件类型收集合法方法
        public static Dictionary<string, List<string>> Resolve(GameObject animationEventReceiver)
        {
            Dictionary<string, List<string>> functionGroups = new Dictionary<string, List<string>>();
            if (animationEventReceiver == null)
                return functionGroups;

            MonoBehaviour[] receivers = animationEventReceiver.GetComponentsInChildren<MonoBehaviour>(true);
            for (int receiverIndex = 0; receiverIndex < receivers.Length; receiverIndex++)
            {
                MonoBehaviour receiver = receivers[receiverIndex];
                if (receiver == null)
                    continue;

                MethodInfo[] methods = receiver.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
                string receiverTypeName = receiver.GetType().Name;
                if (!functionGroups.TryGetValue(receiverTypeName, out List<string> functionNames))
                {
                    functionNames = new List<string>();
                    functionGroups.Add(receiverTypeName, functionNames);
                }

                for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                {
                    MethodInfo method = methods[methodIndex];
                    if (!IsAnimationEventMethod(method) || functionNames.Contains(method.Name))
                        continue;

                    functionNames.Add(method.Name);
                }

                if (functionNames.Count == 0)
                    functionGroups.Remove(receiverTypeName);
            }

            foreach (List<string> functionNames in functionGroups.Values)
                functionNames.Sort(StringComparer.Ordinal);

            return functionGroups;
        }

        // 判断方法是否符合 Unity Animation Event 的调用签名
        private static bool IsAnimationEventMethod(MethodInfo method)
        {
            if (method.IsSpecialName || method.IsStatic || method.ReturnType != typeof(void))
                return false;

            Type declaringType = method.DeclaringType;
            if (declaringType == typeof(object) || declaringType == typeof(Component)
                || declaringType == typeof(Behaviour) || declaringType == typeof(MonoBehaviour))
                return false;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0)
                return true;

            if (parameters.Length != 1)
                return false;

            Type parameterType = parameters[0].ParameterType;
            return parameterType == typeof(float)
                   || parameterType == typeof(int)
                   || parameterType == typeof(string)
                   || parameterType == typeof(AnimationEvent)
                   || typeof(UnityEngine.Object).IsAssignableFrom(parameterType);
        }
    }
}
