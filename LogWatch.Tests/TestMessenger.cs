using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;

namespace LogWatch.Tests {
    public class TestMessenger : IMessenger {
        private readonly Dictionary<Type, Delegate> subscribers = new Dictionary<Type, Delegate>();

        public TestMessenger() {
            this.SentMessages = new List<object>();
        }

        public List<object> SentMessages { get; set; }

        public void Register<TMessage>(object recipient, Action<TMessage> action) {
            this.subscribers[typeof (TMessage)] = action;
        }

        public void Register<TMessage>(object recipient, object token, Action<TMessage> action) {
            this.subscribers[typeof (TMessage)] = action;
        }

        public void Register<TMessage>(
            object recipient,
            object token,
            bool receiveDerivedMessagesToo,
            Action<TMessage> action) {
            this.subscribers[typeof (TMessage)] = action;
        }

        public void Register<TMessage>(object recipient, bool receiveDerivedMessagesToo, Action<TMessage> action) {
            this.subscribers[typeof (TMessage)] = action;
        }

        public void Send<TMessage>(TMessage message) {
            this.SentMessages.Add(message);
            this.InvokeSubscriber(message);
        }

        public void Send<TMessage, TTarget>(TMessage message) {
            this.SentMessages.Add(message);
            this.InvokeSubscriber(message);
        }

        public void Send<TMessage>(TMessage message, object token) {
            this.SentMessages.Add(message);
            this.InvokeSubscriber(message);
        }

        public void Unregister(object recipient) {
        }

        public void Unregister<TMessage>(object recipient) {
        }

        public void Unregister<TMessage>(object recipient, object token) {
        }

        public void Unregister<TMessage>(object recipient, Action<TMessage> action) {
        }

        public void Unregister<TMessage>(object recipient, object token, Action<TMessage> action) {
        }

        private void InvokeSubscriber<TMessage>(TMessage message) {
            foreach (var subscriber in this.subscribers)
                if (subscriber.Key == message.GetType())
                    subscriber.Value.DynamicInvoke(message);
        }
    }
}