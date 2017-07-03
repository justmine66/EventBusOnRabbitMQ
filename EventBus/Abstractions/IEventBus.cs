using EventBus.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventBus.Abstractions
{
    /// <summary>
    /// 摘要：
    ///     事件总线
    /// 说明：
    ///     集中式事件处理中心
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 摘要：
        ///     表示一个订阅事件的方法
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理</param>
        void Subscribe<T>(IIntegrationEventHandler<T> handler) where T : IntegrationEvent;

        /// <summary>
        /// 摘要：
        ///     表示一个取消订阅事件的方法
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理</param>
        void Unsubscribe<T>(IIntegrationEventHandler<T> handler) where T : IntegrationEvent;

        /// <summary>
        /// 摘要：
        ///     发布时间
        /// </summary>
        /// <param name="event">事件类型</param>
        void Publish(IntegrationEvent @event);
    }
}
