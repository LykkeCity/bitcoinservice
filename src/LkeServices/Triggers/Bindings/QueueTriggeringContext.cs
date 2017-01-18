using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LkeServices.Triggers.Bindings
{

    public class QueueTriggeringContext
    {
        private QueueLengthBasedDelayStrategy DelayStrategy { get; set; }

        public QueueTriggeringContext(DateTimeOffset messageInsertionTime)
        {
            Time = messageInsertionTime;
        }

        public DateTimeOffset Time { get; set; }

        public void MoveMessageToEnd(string newMessageVersion)
        {
            NewMessageVersion = newMessageVersion;
            MovingAction = MessageMovingAction.MoveToEnd;
        }

        public void MoveMessageToPoision()
        {
            MovingAction = MessageMovingAction.MoveToPoison;
        }

        public void SetCountQueueBasedDelay(int maxDelayMs, int reduceDelayPerItemMs)
        {
            DelayStrategy = new QueueLengthBasedDelayStrategy(maxDelayMs, reduceDelayPerItemMs);
        }


        internal string NewMessageVersion { get; set; }

        internal MessageMovingAction MovingAction { get; set; }              

        internal Task Delay(int queueLength)
        {
            return DelayStrategy != null ? DelayStrategy.Delay(queueLength) : Task.CompletedTask;
        }

        internal enum MessageMovingAction
        {
            Default,
            MoveToEnd,
            MoveToPoison
        }


        private class QueueLengthBasedDelayStrategy
        {
            private readonly int _maxDelayMs;
            private readonly int _reduceDelayPerItemMs;

            public QueueLengthBasedDelayStrategy(int maxDelayMs, int reduceDelayPerItemMs)
            {
                _maxDelayMs = maxDelayMs;
                _reduceDelayPerItemMs = reduceDelayPerItemMs;
            }

            internal Task Delay(int queueLength)
            {
                var delay = _maxDelayMs - _reduceDelayPerItemMs * queueLength;
                if (delay > 0)
                    return Task.Delay(delay);
                return Task.CompletedTask;
            }
        }
    }
}
