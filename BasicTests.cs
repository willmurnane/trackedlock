using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackedLock
{
    public class BasicTests
    {
        public TestRunner.TestResult TestGetAndThenRelease()
        {
            TrackedLock<string> myLock = new TrackedLock<string>();
            myLock.Acquire("me");
            double result = myLock.Release("me");
            Console.WriteLine("Lock was held for {0} ms", result);
            return new TestRunner.TestResult(true);
        }
        public TestRunner.TestResult TestUnOwnedRelease()
        {
            TrackedLock<string> myLock = new TrackedLock<string>();
            return TestRunner.ExpectException<ApplicationException>(() => myLock.Release("me"));
        }
        public TestRunner.TestResult TestWrongOwnerRelease()
        {
            TrackedLock<string> myLock = new TrackedLock<string>();
            myLock.Acquire("me");
            return TestRunner.ExpectException<ApplicationException>(() => myLock.Release("her"));
        }
        public TestRunner.TestResult TestTimeout()
        {
            TrackedLock<string> myLock = new TrackedLock<string>();
            myLock.Acquire("her");
            return TestRunner.ExpectException<ApplicationException>(() => myLock.Acquire("me"));
        }
        public TestRunner.TestResult TestReentrancy()
        {
            TrackedLock<string> myLock = new TrackedLock<string>();
            myLock.Acquire("me");
            myLock.Acquire("me");
            return new TestRunner.TestResult(true);
        }

    }
}
