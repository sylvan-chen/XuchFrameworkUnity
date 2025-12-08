using System;
using DigiEden.Framework.Utils;

namespace DigiEden.Framework
{
    /// <summary>
    /// 状态基类
    /// </summary>
    /// <typeparam name="T">状态机所有者的类型</typeparam>
    /// <remarks>
    /// 每一个状态类型代表状态机所有者的一种状态。
    /// </remarks>
    public abstract class StateBase<T> where T : class
    {
        internal void Init(Fsm<T> fsm)
        {
            if (fsm == null)
            {
                Log.Error("[StateBase] Init failed. FSM is null.");
                return;
            }

            OnInit(fsm);
        }

        internal void Enter(Fsm<T> fsm)
        {
            if (fsm == null)
            {
                Log.Error("[StateBase] Enter failed. FSM is null.");
                return;
            }

            OnEnter(fsm);
        }

        internal void Exit(Fsm<T> fsm)
        {
            if (fsm == null)
            {
                Log.Error("[StateBase] Exit failed. FSM is null.");
                return;
            }

            OnExit(fsm);
        }

        internal void Update(Fsm<T> fsm, float deltaTime, float unscaledDeltaTime)
        {
            if (fsm == null)
            {
                Log.Error("[StateBase] Update failed. FSM is null.");
                return;
            }

            OnUpdate(fsm, deltaTime, unscaledDeltaTime);
        }

        internal void Shutdown(Fsm<T> fsm)
        {
            if (fsm == null)
            {
                Log.Error("[StateBase] Shutdown failed. FSM is null.");
                return;
            }

            OnShutdown(fsm);
        }

        /// <summary>
        /// 初始化状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnInit(Fsm<T> fsm) { }

        /// <summary>
        /// 进入状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnEnter(Fsm<T> fsm) { }

        /// <summary>
        /// 退出状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnExit(Fsm<T> fsm) { }

        /// <summary>
        /// 更新状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        /// <param name="deltaTime">两帧之间的间隔时间</param>
        /// <param name="unscaledDeltaTime">不受时间缩放影响的两帧之间的间隔时间</param>
        public virtual void OnUpdate(Fsm<T> fsm, float deltaTime, float unscaledDeltaTime) { }

        /// <summary>
        /// 状态机关闭时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnShutdown(Fsm<T> fsm) { }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <typeparam name="TState">目标状态类型</typeparam>
        /// <param name="fsm">所属状态机实例</param>
        protected void ChangeState<TState>(Fsm<T> fsm) where TState : StateBase<T>
        {
            if (typeof(TState) == GetType())
            {
                Log.Error($"[StateBase] Cannot change state to itself '{typeof(TState).FullName}'.");
                return;
            }

            fsm.ChangeState<TState>();
        }
    }
}