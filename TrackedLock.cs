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
        private Stopwatch AcquireDelayTime,
            HoldTime;
        private T Holder;
        private ManualResetEvent WaitOnMe;
        private ConcurrentQueue<string> events;
        private int EnteredQueue = 0;
        int MaxMsToWait;
#if DEBUG
        // It's relatively expensive to construct a stack frame, so only if we're in debug mode
        // will we actually bother to keep track of who locked the lock.
        private StackFrame HolderSource;
#endif
        public TrackedLock(int maxMsToWait = 1000)
        {
            MaxMsToWait = maxMsToWait;
            AcquireDelayTime = new Stopwatch();
            HoldTime = new Stopwatch();
            Holder = null;
            WaitOnMe = new ManualResetEvent(false);
//            events = new ConcurrentQueue<string>();
#if DEBUG
            HolderSource = null;
#endif
        }
        public void Acquire(T forWhom)
        {
            AcquireDelayTime.Restart();
            int spins = 0;
            while (Interlocked.CompareExchange(ref Holder, forWhom, null) != forWhom)
            {
                if (AcquireDelayTime.ElapsedMilliseconds > MaxMsToWait)
                {
                    AcquireDelayTime.Stop();
                    throw new ApplicationException(String.Format(
                        "Waiting for too long on lock acquire. Current holder: {0}"
#if DEBUG
                        + "from {1}"
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
                    WaitOnMe.WaitOne(MaxMsToWait / 10);
                }
//                events.Enqueue("sleep " + forWhom + " at " + AcquireDelayTime.ElapsedMilliseconds);
            }
            WaitOnMe.Reset();
//            events.Enqueue(forWhom + " got lock");
            HoldTime.Restart();
            AcquireDelayTime.Stop();
#if DEBUG
            HolderSource = new StackFrame(1, fNeedFileInfo: false);
#endif
        }
        public double Release(T releaser)
        {
            T result;
            if ((result = Interlocked.CompareExchange(ref Holder, null, Holder)) != releaser)
            {
                throw new ApplicationException(String.Format(
                    "Attempt to release non-owned lock by from {0}; lock held by {1}"
#if DEBUG
                + " from {2}."
#endif
                    , new StackFrame(1, fNeedFileInfo: true), Holder
#if DEBUG
                    , HolderSource
#endif
                ));
            }
            HoldTime.Stop();
            WaitOnMe.Set();
            return HoldTime.Elapsed.TotalMilliseconds;
        }
    }
}
