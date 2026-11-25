/*
 * ┌──────────────────────────────────────────────────┐
 * │  描    述: 角色移动方向解析测试，覆盖第一与第三人称视角
 * │  类    名: CharacterMoveDirectionResolverTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Core;
using Framework.Character.Runtime.Core.View;
using NUnit.Framework;
using UnityEngine;

namespace Tests.CharacterRuntime
{
    public sealed class CharacterMoveDirectionResolverTests
    {
        private GameObject m_gameObject;
        private CharacterRuntimeTestContext m_context;

        [SetUp]
        public void SetUp()
        {
            m_gameObject = new GameObject("CharacterMoveDirectionResolverTests");
            m_context = new CharacterRuntimeTestContext(m_gameObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_gameObject);
        }

        // 验证第一人称移动方向跟随角色自身朝向
        [Test]
        public void Resolve_FirstPerson_UsesCharacterForward()
        {
            m_gameObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            m_context.View.ViewMode = CharacterViewMode.FirstPerson;

            Vector3 direction = CharacterMoveDirectionResolver.Resolve(m_context, Vector2.up);

            AssertVector(direction, Vector3.right);
        }

        // 验证第三人称移动方向跟随相机水平朝向
        [Test]
        public void Resolve_ThirdPerson_UsesCameraYaw()
        {
            m_gameObject.transform.rotation = Quaternion.identity;
            m_context.View.ViewMode = CharacterViewMode.ThirdPerson;
            m_context.View.CameraYaw = 90f;

            Vector3 direction = CharacterMoveDirectionResolver.Resolve(m_context, Vector2.up);

            AssertVector(direction, Vector3.right);
        }

        // 验证斜向移动输入会被限制为单位长度
        [Test]
        public void Resolve_DiagonalInput_ReturnsNormalizedDirection()
        {
            m_gameObject.transform.rotation = Quaternion.identity;
            m_context.View.ViewMode = CharacterViewMode.FirstPerson;

            Vector3 direction = CharacterMoveDirectionResolver.Resolve(m_context, Vector2.one);

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
            AssertVector(direction, new Vector3(1f, 0f, 1f).normalized);
        }

        // 验证第三人称前方向只保留相机水平分量
        [Test]
        public void ResolveForward_ThirdPerson_FlattensCameraDirection()
        {
            m_context.View.ViewMode = CharacterViewMode.ThirdPerson;
            m_context.View.CameraYaw = 90f;

            Vector3 forward = CharacterMoveDirectionResolver.ResolveForward(m_context);

            AssertVector(forward, Vector3.right);
        }

        // 按分量比较向量，避免浮点误差导致断言失败
        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }
    }
}
