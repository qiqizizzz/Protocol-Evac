/*
 * ┌──────────────────────────────────┐
 * │  描    述: 游戏总入口，负责初始化架构并驱动全局系统
 * │  类    名: GameApp.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Framework.QF;
using UnityEngine;

namespace Game
{
    public class GameApp : MonoBehaviour, IController
    {
        #region 生命周期
        private void Awake()
        {
            GameArchitecture.InitArchitecture();
        }

        private void Update()
        {
            
        }
        #endregion
        
        public IArchitecture GetArchitecture()
        {
            return GameArchitecture.Interface;
        }
    }
}