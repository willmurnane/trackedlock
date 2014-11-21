using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackedLock
{
    public class BasicTests
    {
        private TrackedLock<string> MakeLock()
        {
            return new TrackedLock<string>("aLock");
        }
        public TestRunner.TestResult TestGetAndThenRelease()
        {
            TrackedLock<string> myLock = MakeLock();
            myLock.Acquire("me");
            double result = myLock.Release("me");
            Console.WriteLine("Lock was held for {0} ms", result);
            return new TestRunner.TestResult(true);
        }
        public TestRunner.TestResult TestUnOwnedRelease()
        {
            TrackedLock<string> myLock = MakeLock();
            return TestRunner.ExpectException<ApplicationException>(() => myLock.Release("me"));
        }
        public TestRunner.TestResult TestWrongOwnerRelease()
        {
            TrackedLock<string> myLock = MakeLock();
            myLock.Acquire("me");
            return TestRunner.ExpectException<ApplicationException>(() => myLock.Release("her"));
        }
        public TestRunner.TestResult TestReentrancy()
        {
            TrackedLock<string> myLock = MakeLock();
            myLock.Acquire("me");
            myLock.Acquire("him");
            myLock.Acquire("her");
            myLock.Release("her");
            myLock.Release("him");
            myLock.Release("me");
            return new TestRunner.TestResult(true);
        }
        public TestRunner.TestResult TestBadReentrancy()
        {
            TrackedLock<string> myLock = MakeLock();
            myLock.Acquire("me");
            myLock.Acquire("him");
            myLock.Acquire("her");
            myLock.Release("her");
            return TestRunner.ExpectException<ApplicationException>(() => myLock.Release("me"));
        }

    }
}
