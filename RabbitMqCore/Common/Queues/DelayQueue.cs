using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Common.Queues
{
    public class DelayQueue<T>
    {
        private PriorityQueue<DateTime, T> queue = new PriorityQueue<DateTime, T>();
        public void Enqueue(T item, int delay)
        {
            queue.Push(DateTime.Now.AddMilliseconds(delay), item);
        }

        public T Dequeue()
        {
            try
            {
                if (queue.Peek().Item1 < DateTime.Now)
                    return queue.Pop();
                else
                    return default(T);
            }
            catch (InvalidOperationException ex)
            {
                return default(T);
            }
        }
    }
}
