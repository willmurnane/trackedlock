using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrackedLock
{
    public class TrackedLock<T> where T : class
    {
        private const int SPIN_COUNT = 100;
        private const int NOT_HELD = -1;
        private Stopwatch HoldTime;
        private Stack<T> LockAccessOrder;
        private int Holder;
        private ManualResetEvent WaitOnMe;
        private int EnteredQueue = 0;
        private uint AcquireCount = 0;
        private int MaxMsToWait;
        private int CurrentWaiters = 0;
        private string MyName;
#if DEBUG
        // It's relatively expensive to construct a stack frame, so only if we're in debug mode
        // will we actually bother to keep track of who locked the lock.
        private StackFrame HolderSource;
#endif
        public TrackedLock(string myName, int maxMsToWait = 1000)
        {
            MaxMsToWait = maxMsToWait;
            HoldTime = new Stopwatch();
            LockAccessOrder = new Stack<T>();
            Holder = NOT_HELD;
            WaitOnMe = new ManualResetEvent(false);
            MyName = myName;
//            events = new ConcurrentQueue<string>();
#if DEBUG
            HolderSource = null;
#endif
        }
        public void Acquire(T forWhom)
        {
            Stopwatch AcquireDelayTime = new Stopwatch();
            AcquireDelayTime.Start();
            int spins = 0;
            do
            {
                int result = Interlocked.CompareExchange(ref Holder, Thread.CurrentThread.ManagedThreadId, NOT_HELD);
                if (result == NOT_HELD ||
                    result == Thread.CurrentThread.ManagedThreadId)
                {
                    break;
                }
                if (AcquireDelayTime.ElapsedMilliseconds > MaxMsToWait)
                {
                    AcquireDelayTime.Stop();
                    throw new ApplicationException(String.Format(
                        "Waiting for too long on lock acquire. Current holder: {0}"
#if DEBUG
 + " from {1}"
#endif
, Holder
#if DEBUG
, HolderSource
#endif
));
                }
                spins++;
                if (spins < SPIN_COUNT)
                {
                    Interlocked.Increment(ref EnteredQueue);
                    Interlocked.Increment(ref CurrentWaiters);
                    WaitOnMe.WaitOne(MaxMsToWait / 10);
                    Interlocked.Decrement(ref CurrentWaiters);
                }
            } while (true);
            AcquireDelayTime.Stop();
            AcquireCount++;
            LockAccessOrder.Push(forWhom);
            WaitOnMe.Reset();
//            events.Enqueue(forWhom + " got lock");
            HoldTime.Restart();
            AcquireDelayTime.Stop();
#if DEBUG
//            HolderSource = new StackFrame(1, fNeedFileInfo: true);
#endif
        }
        public double Release(T releaser)
        {
            if (LockAccessOrder.Count == 0)
            {
                throw new ApplicationException("Attempt to release unowned lock " + MyName);
            }
            if (!Object.ReferenceEquals(LockAccessOrder.Peek(), releaser))
            {
                throw new ApplicationException(String.Format(
                    "Attempt to release lock in wrong order from {0}; was locked by {1} but is now being unlocked by {2}."
                    , new StackFrame(1, fNeedFileInfo: true), LockAccessOrder.Peek(), releaser));
            }
            LockAccessOrder.Pop();
#if DEBUG
            //HolderSource = null;
#endif
            HoldTime.Stop();
            Holder = NOT_HELD;
            WaitOnMe.Set();
            return HoldTime.Elapsed.TotalMilliseconds;
        }
    }
}
