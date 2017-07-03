using EventBus.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Abstractions
{
    /// <summary>
    /// 摘要：
    ///     事件处理基准接口，所有事件处理类都可实现此接口。
    /// 说明：
    ///     泛型参数支持关联事件源。
    /// </summary>
    /// <typeparam name="TIntegrationEvent">事件基类型（即事件源）</typeparam>
    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
        where TIntegrationEvent : IntegrationEvent
    {
        Task Handle(TIntegrationEvent @event);
    }

    /// <summary>
    /// 摘要：
    ///     事件处理基准接口，所有事件处理类都可实现此接口。
    /// 说明：
    ///     不支持关联事件源。
    /// </summary>
    public interface IIntegrationEventHandler { }
}
