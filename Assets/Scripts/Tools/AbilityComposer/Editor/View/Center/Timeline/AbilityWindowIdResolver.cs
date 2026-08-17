/*
 * ┌───────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 窗口 Id 解析器，按窗口类型和动画片段生成可读标识
 * │  类    名: AbilityWindowIdResolver.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tools.AbilityComposer.Editor.View.Center.Timeline
{
    public static class AbilityWindowIdResolver
    {
        private const string UNKNOWN_CLIP_ID = "unknown_clip";
        private const string HIT_PREFIX = "hit";
        private const string STEP_PREFIX = "step";
        private const string MOVEMENT_LOCK_PREFIX = "movement_lock";
        private const string VFX_PREFIX = "vfx";
        private const string AUDIO_PREFIX = "audio";

        // 根据窗口类型、动画片段和现有窗口生成下一个可读 Id
        public static string CreateWindowId(AbilityWindowDraftType type, AnimationClip clip,
            IEnumerable<AbilityWindowDraft> existingWindows)
        {
            string baseId = $"{GetTypePrefix(type)}_{ResolveClipId(clip)}";
            int nextIndex = ResolveNextIndex(baseId, existingWindows);
            return $"{baseId}_{nextIndex:00}";
        }

        // 判断当前 Id 是否需要按目标窗口类型重新生成
        public static bool ShouldRefreshWindowId(string id, AbilityWindowDraftType type)
        {
            if (string.IsNullOrEmpty(id))
                return true;

            if (IsHexId(id))
                return true;

            string expectedPrefix = $"{GetTypePrefix(type)}_";
            if (id.StartsWith(expectedPrefix))
                return false;

            return HasKnownTypePrefix(id);
        }

        // 获取窗口类型对应的可读前缀
        private static string GetTypePrefix(AbilityWindowDraftType type)
        {
            return type switch
            {
                AbilityWindowDraftType.Hit => HIT_PREFIX,
                AbilityWindowDraftType.StepAdvance => STEP_PREFIX,
                AbilityWindowDraftType.MovementLock => MOVEMENT_LOCK_PREFIX,
                AbilityWindowDraftType.Vfx => VFX_PREFIX,
                AbilityWindowDraftType.Audio => AUDIO_PREFIX,
                _ => HIT_PREFIX
            };
        }

        // 将动画片段名称转换成 snake_case Id 片段
        private static string ResolveClipId(AnimationClip clip)
        {
            if (clip == null || string.IsNullOrEmpty(clip.name))
                return UNKNOWN_CLIP_ID;

            return SanitizeName(clip.name);
        }

        // 解析同名前缀下的下一个序号
        private static int ResolveNextIndex(string baseId, IEnumerable<AbilityWindowDraft> existingWindows)
        {
            int maxIndex = 0;
            string idPrefix = $"{baseId}_";
            foreach (AbilityWindowDraft windowDraft in existingWindows)
            {
                if (windowDraft == null || string.IsNullOrEmpty(windowDraft.Id))
                    continue;

                if (!windowDraft.Id.StartsWith(idPrefix))
                    continue;

                string indexText = windowDraft.Id.Substring(idPrefix.Length);
                if (!int.TryParse(indexText, out int index))
                    continue;

                if (index > maxIndex)
                    maxIndex = index;
            }

            return maxIndex + 1;
        }

        // 将任意名称规整为小写下划线格式
        private static string SanitizeName(string rawName)
        {
            StringBuilder builder = new StringBuilder(rawName.Length);
            bool previousWasSeparator = true;
            bool previousWasLowerOrDigit = false;
            for (int charIndex = 0; charIndex < rawName.Length; charIndex++)
            {
                char currentChar = rawName[charIndex];
                if (char.IsLetterOrDigit(currentChar))
                {
                    bool currentIsUpper = char.IsUpper(currentChar);
                    if (currentIsUpper && previousWasLowerOrDigit && !previousWasSeparator)
                        builder.Append('_');

                    char normalizedChar = char.ToLowerInvariant(currentChar);
                    builder.Append(normalizedChar);
                    previousWasSeparator = false;
                    previousWasLowerOrDigit = char.IsLower(normalizedChar) || char.IsDigit(normalizedChar);
                    continue;
                }

                if (!previousWasSeparator)
                    builder.Append('_');

                previousWasSeparator = true;
                previousWasLowerOrDigit = false;
            }

            while (builder.Length > 0 && builder[builder.Length - 1] == '_')
                builder.Length--;

            return builder.Length == 0 ? UNKNOWN_CLIP_ID : builder.ToString();
        }

        // 判断 Id 是否是旧版纯十六进制随机串
        private static bool IsHexId(string id)
        {
            if (id.Length != 16 && id.Length != 32)
                return false;

            for (int charIndex = 0; charIndex < id.Length; charIndex++)
            {
                char currentChar = id[charIndex];
                bool isHexChar = currentChar >= '0' && currentChar <= '9'
                    || currentChar >= 'a' && currentChar <= 'f'
                    || currentChar >= 'A' && currentChar <= 'F';
                if (!isHexChar)
                    return false;
            }

            return true;
        }

        // 判断 Id 是否使用了 Ability 窗口的已知类型前缀
        private static bool HasKnownTypePrefix(string id)
        {
            return id.StartsWith($"{HIT_PREFIX}_")
                || id.StartsWith($"{STEP_PREFIX}_")
                || id.StartsWith($"{MOVEMENT_LOCK_PREFIX}_")
                || id.StartsWith($"{VFX_PREFIX}_")
                || id.StartsWith($"{AUDIO_PREFIX}_");
        }
    }
}
