using System.Collections.Generic;

namespace OnlineSupport
{
    public class CustomQ<T> : Queue<T>
    {
        public CustomQ()
            : base()
        {
        }

        public CustomQ(int capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// removes item from queue
        /// </summary>
        /// <param name="q">waiting users</param>
        /// <param name="value">item to be removed</param>
        /// <returns>modified 'Queue' object</returns>
        public static Queue<T> RemoveItem(Queue<T> q, T value)
        {
            Queue<T> queue = new Queue<T>();
            foreach (T item in q)
            {
                if (!Comparer<T>.Equals(item, value))
                    queue.Enqueue(item);
            }

            return queue;
        }

        /// <summary>
        /// returns the position of given item in queue, -1 if it isn't in the queue
        /// </summary>
        /// <param name="q">waiting users</param>
        /// <param name="value">item we're checking the position of</param>
        /// <returns>position of passed in "value"</returns>
        public static int Position(Queue<T> q, T value)
        {
            int index = 1;
            foreach (T item in q)
            {
                if (Comparer<T>.Equals(item, value))
                {
                    return index;
                }
                index += 1;
            }
            return -1;
        }
    }
}