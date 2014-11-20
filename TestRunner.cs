using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TrackedLock
{
    public class TestRunner
    {
        public class TestResult
        {
            public bool Succeeded;
            public string ErrorMessage;
            public Exception Exception;
            public TestResult(bool succeeded, 
                string errorMessage = null,
                Exception exception = null)
            {
                Succeeded = succeeded;
                ErrorMessage = errorMessage;
                Exception = exception;
            }
        }
        public void RunTests(Type container, TextWriter dest)
        {
            object instance = container.GetConstructor(Type.EmptyTypes).Invoke(null);
            MethodInfo[] potentialTests = container.GetMethods();
            Dictionary<string, TestResult> results = new Dictionary<string, TestResult>();
            foreach (var pTest in potentialTests)
            {
                TestResult result;
                if (pTest.GetParameters().Length == 0 &&
                    pTest.ReturnType == typeof(TestResult))
                {
                    dest.WriteLine("run {0}", pTest.Name);
                    try
                    {
                        result = (TestResult)pTest.Invoke(instance, null);
                    }
                    catch (Exception e)
                    {
                        // e will always have an innerexception, because it'll be an invoke exception
                        // which wraps whatever the test method threw.
                        result = new TestResult(false, e.InnerException.Message, e.InnerException);
                    }
                }
                else continue;
                results.Add(pTest.Name, result);
            }
            SummarizeResults(results, dest);
        }
        public void SummarizeResults(Dictionary<string, TestResult> results, TextWriter writer)
        {
            int totalPass = 0,
                totalFail = 0;
            foreach (var kvp in results)
            {
                if (kvp.Value.Succeeded)
                {
                    totalPass++;
                }
                else
                {
                    totalFail++;
                }
            }
            writer.WriteLine("{0} of {1} tests passed uneventfully.", totalPass, results.Count);
            if (totalFail > 0)
            {
                foreach (var kvp in results)
                {
                    if (!kvp.Value.Succeeded)
                    {
                        writer.WriteLine("Test {0} failed: {1}", kvp.Key, kvp.Value.ErrorMessage);
                        if (kvp.Value.Exception != null)
                        {
                            writer.WriteLine(kvp.Value.Exception.ToString());
                        }
                    }
                }
            }
        }
        public static TestResult ExpectException<T>(Action toDo) where T : Exception
        {
            try
            {
                toDo();
                return new TestResult(false, String.Format(
                    "Expected exception of type {0} when calling method {1}, but didn't get exception",
                    typeof(T).Name, new StackFrame(1).GetMethod().Name));
            } catch (T)
            {
                return new TestResult(true);
            } catch (Exception e)
            {
                return new TestResult(false, String.Format(
                    "Expected exception of type {0} when calling method {1}, but got exception of type {2}",
                    typeof(T).Name, new StackFrame(1).GetMethod().Name, e.GetType().Name),
                    e);
            }
        }
    }
}
