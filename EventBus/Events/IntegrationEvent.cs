using System;
using System.Collections.Generic;
using System.Text;

namespace EventBus.Events
{
    /// <summary>
    /// 摘要：
    ///     事件基类型，所有的事件源都要实现此类。
    /// 说明：
    ///     表示事件处理所需的参数，也叫事件源；根据事件源可以区分出不同的事件类型，故按用途命名为事件类型。
    /// </summary>
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }

        public Guid Id { get; }
        public DateTime CreationDate { get; }
    }
}
