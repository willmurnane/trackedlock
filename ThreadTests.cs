using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrackedLock
{
    public class ThreadTests
    {
        private static int SharedState;
        private class IncrementWorkerState
        {
            public TrackedLock<string> TheLock;
            public int ThreadId;
            public int MyMax;
            public IncrementWorkerState(TrackedLock<string> theLock, int threadId, int myMax)
            {
                TheLock = theLock;
                ThreadId = threadId;
                MyMax = myMax;
            }
        }
        private static void IncrementWorker(object _state)
        {
            IncrementWorkerState state = (IncrementWorkerState)_state;
            string myIdentifier = "thread" + state.ThreadId;
            for (int i = 0; i < state.MyMax; i++)
            {
                state.TheLock.Acquire(myIdentifier);
                SharedState++;
                state.TheLock.Release(myIdentifier);
                // Do some useless busy work so someone else can get the lock
                for (int j = 0; j < 10 * 1000; j++)
                    ;
            }
        }
        private TestRunner.TestResult IncrementTest(int[] perThreadMax)
        {
            SharedState = 0;
            TrackedLock<string> theLock = new TrackedLock<string>();
            Thread[] threads = new Thread[perThreadMax.Length];
            IncrementWorkerState[] states = new IncrementWorkerState[threads.Length];
            int expectedMax = perThreadMax.Sum();
            for (int i = 0; i < perThreadMax.Length; i++)
            {
                threads[i] = new Thread(new ParameterizedThreadStart(IncrementWorker));
                states[i] = new IncrementWorkerState(theLock, i, perThreadMax[i]);
            }
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Start(states[i]);
            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join(perThreadMax[i]);
            }
            if (expectedMax == SharedState)
            {
                return new TestRunner.TestResult(true);
            } else
            {
                return new TestRunner.TestResult(false, String.Format(
                    "Count was wrong: {0} versus expected {1} from array [{2}]",
                    SharedState, expectedMax, String.Join(", ", perThreadMax)));
            }
        }

        public TestRunner.TestResult PerformIncrementTestOne()
        {
            return IncrementTest(new int[] { 100, 200, 100, 400 });
        }
        public TestRunner.TestResult PerformIncrementTestTwo()
        {
            return IncrementTest(new int[] { 100000, 100000 });
        }
        public TestRunner.TestResult PerformIncrementTestThree()
        {
            return IncrementTest(new int[] { 1000, 1000, 1000, 1000, 1000, 1000 });
        }
    }
}
