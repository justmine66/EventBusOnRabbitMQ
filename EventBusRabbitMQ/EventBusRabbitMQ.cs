using EventBus.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using EventBus.Events;
using RabbitMQ.Client;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace EventBusRabbitMQ
{
    /// <summary>
    /// 摘要：
    ///     基于RabbitMQ实现的事件总线。
    /// 说明：
    ///     基于RabbitMQ提供分布式事件集中式处理的支持。
    /// </summary>
    public class EventBusRabbitMQ : IEventBus, IDisposable
    {
        private readonly string _brokerName = "event_bus_on_rabbitMQ";//消息代理名称
        private readonly string _connectionString;//连接到消息服务器的地址，可以为主机名或者IP地址。
        private readonly Dictionary<string, List<IIntegrationEventHandler>> _handlers;//存储事件处理的字典
        private readonly List<Type> _eventTypes;//存储事件类型的列表

        private IModel _model;
        private IConnection _connection;
        private string _queueName;//队列名称

        public EventBusRabbitMQ(string connectionString)
        {
            _connectionString = connectionString;
            _handlers = new Dictionary<string, List<IIntegrationEventHandler>>();
            _eventTypes = new List<Type>();
        }

        /// <summary>
        /// 摘要：
        ///     表示一个发布事件的方法
        /// 说明：
        ///     将数据类型作为消息发布到RabbitMQ服务器
        /// </summary>
        /// <param name="event">事件类型</param>
        public void Publish(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name;//获取事件类型名称
            var factory = new ConnectionFactory() { HostName = _connectionString };
            using (var connection = factory.CreateConnection())//建立socket连接
            using (var channel = connection.CreateModel())//建立通道
            {
                channel.ExchangeDeclare(exchange: _brokerName,
                                    type: "direct");//声明direct类型的交换机

                string message = JsonConvert.SerializeObject(@event);//序列化事件类型（参数）
                var body = Encoding.UTF8.GetBytes(message);//转化事件类型（参数）为RabbitMQ传递的消息类型（即二进制块）
                //发布消息到交换机。
                channel.BasicPublish(exchange: _brokerName,//将代理名称作为交换机的名称
                                     routingKey: eventName,//将事件类型名称作为队列名称
                                     basicProperties: null,
                                     body: body);
            }
        }

        /// <summary>
        /// 摘要：
        ///     表示一个订阅事件的方法
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理</param>
        public void Subscribe<T>(IIntegrationEventHandler<T> handler) where T : IntegrationEvent
        {
            var eventName = typeof(T).Name;
            if (_handlers.ContainsKey(eventName))
            {
                _handlers[eventName].Add(handler);
            }
            else
            {
                var channel = GetChannel();
                channel.QueueBind(queue: _queueName,
                                  exchange: _brokerName,
                                  routingKey: eventName);//从交换机根据事件类型获取消息。
                //记录事件处理
                _handlers.Add(eventName, new List<IIntegrationEventHandler>());
                _handlers[eventName].Add(handler);
                _eventTypes.Add(typeof(T));//记录事件类型
            }
        }

        /// <summary>
        /// 摘要：
        ///     表示一个取消订阅事件的方法
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理</param>
        public void Unsubscribe<T>(IIntegrationEventHandler<T> handler) where T : IntegrationEvent
        {
            var eventName = typeof(T).Name;
            if (_handlers.ContainsKey(eventName) && _handlers[eventName].Contains(handler))
            {//如果该事件类型存在事件处理
                _handlers[eventName].Remove(handler);//移除事件处理

                if (_handlers[eventName].Count == 0)//如果该事件类型不存在其他的事件处理
                {
                    _handlers.Remove(eventName);//从事件处理字典中移除事件类型
                    var eventType = _eventTypes.Single(e => e.Name == eventName);
                    _eventTypes.Remove(eventType);//从时间类型列表中移除事件类型。
                    _model.QueueUnbind(queue: _queueName,
                        exchange: _brokerName,
                        routingKey: eventName);//与交换机解除绑定。

                    if (_handlers.Keys.Count == 0)
                    {
                        _queueName = string.Empty;
                        _model.Dispose();
                        _connection.Dispose();
                    }

                }
            }
        }

        public void Dispose()
        {
            _handlers.Clear();
            _model?.Dispose();
            _connection?.Dispose();
        }

        /// <summary>
        /// 获取通道
        /// </summary>
        /// <returns></returns>
        private IModel GetChannel()
        {
            if (_model != null)
            {
                return _model;
            }
            else
            {
                (_model, _connection) = CreateConnection();
                return _model;
            }
        }

        /// <summary>
        /// 建立连接
        /// </summary>
        /// <returns></returns>
        private (IModel model, IConnection connection) CreateConnection()
        {
            var factory = new ConnectionFactory() { HostName = _connectionString };
            var con = factory.CreateConnection();
            var channel = con.CreateModel();

            channel.ExchangeDeclare(exchange: _brokerName, type: "direct");//声明交换机
            if (string.IsNullOrEmpty(_queueName)) _queueName = channel.QueueDeclare().QueueName;//声明临时队列，并记录名称。
            //建立通道消费事件
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var eventName = ea.RoutingKey;
                var message = Encoding.UTF8.GetString(ea.Body);
                //异步处理事件
                await ProcessEvent(eventName, message);
            };
            //等待消费消息。。。。。
            channel.BasicConsume(queue: _queueName,
                                 noAck: true,
                                 consumer: consumer);

            return (channel, con);
        }

        /// <summary>
        /// 异步处理事件
        /// </summary>
        /// <param name="eventName">事件类型名称</param>
        /// <param name="message">消息（即事件[类型|参数]）</param>
        /// <returns></returns>
        private async Task ProcessEvent(string eventName, string message)
        {
            if (_handlers.ContainsKey(eventName))//如果该事件类型存在事件处理
            {
                Type eventType = _eventTypes.Single(t => t.Name == eventName);//获取事件类型
                var integrationEvent = JsonConvert.DeserializeObject(message, eventType);//将消息反序列化成事件类型对象。
                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);//基于事件类型，创建具体的事件处理类型。
                var handlers = _handlers[eventName];//获取事件处理

                foreach (var handler in handlers)
                {
                    //将事件源作为参数，调用具体的事件处理方法。
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                }
            }
        }
    }
}
