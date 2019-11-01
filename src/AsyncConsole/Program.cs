using HelperLibs;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //ThisWillNotCatchTheException();
            //await ThisWillCatchTheException();
            //await SequentialRunMethod();
            //Console.WriteLine("------------------- SEQUENTIAL COMPLETED -------------------");
            //await NonSequentialRunMethod();
            //Console.Read();
        }
        #region 1-Task and void returns
        public async Task MyAsyncMethodWithoutAwait()
        {
            // No await! Unnecessary overhead for an operation that will actually never yield.
            DoSomethingSync();

            Console.ReadLine();
        }
        public static async void AsyncVoidMethod()
        {
            // Exceptions thrown in an async void method can’t be caught outside of that method. Nearly always bad to use!
            // return Task
            // Only one meaningful use case exist.
        }
        #endregion

        #region 1.1-Async-void
        public static async void AsyncVoidMethodThrowsException()
        {
            throw new Exception("Exception here!");
        }

        public static void ThisWillNotCatchTheException()
        {
            try
            {
                AsyncVoidMethodThrowsException();
            }
            catch (Exception ex)
            {
                //The below line will never be reached
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }
        #endregion

        #region 2-Async-non-void
        public static async Task AsyncTaskMethodThrowsException()
        {
            throw new Exception("Exception here!");
        }

        public static async Task ThisWillCatchTheException()
        {
            try
            {
                await AsyncTaskMethodThrowsException();
            }
            catch (Exception ex)
            {
                //The below line will actually be reached
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

        #region 3-Task Returns
        public async Task<string> AsyncTask()
        {
            //The await is the very last line of the code path - There is no continuation after it.
            return await GetDataAsync();
        }
        public Task<string> JustTask()
        {
            //Return a Task itself so you state machine not generated.
            return GetDataAsync();
        }
        #endregion

        #region 3.1-Task Returns in using and try catch
        public Task<string> ReturnTaskExceptionNotCaught()
        {
            try
            {
                return GetDataAsync();
            }
            catch (Exception ex)
            {
                // Will never be reached
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        public Task<string> ReturnTaskUsingProblem()
        {
            using (var resource = new MyDisposibleDb())
            {
                // By the time the resource is actually referenced, may have been disposed already
                return GetDataAsync(resource);
            }
        }
        #endregion

        #region 4-Sync GetAwaiter
        public void GetAwaiterGetResultExample()
        {
            //This is ok, but if an error is thrown, it will be encapsulated in an AggregateException   
            string data = GetDataAsync().Result;

            //This is better, if an error is thrown, it will be contained in a regular Exception
            data = GetDataAsync().GetAwaiter().GetResult();
        }
        #endregion

        #region 5-ConfigureAwait
        public async Task ConfigureAwaitExample()
        {
            //It is good practice to always use ConfigureAwait(false) in library code.
            //So returning task is dealth with an other thread
            var data = await GetDataAsync().ConfigureAwait(false);
            // Microsoft link: https://channel9.msdn.com/Series/Three-Essential-Tips-for-Async/Async-library-methods-should-consider-using-Task-ConfigureAwait-false-
        }
        #endregion

        #region 6-CancellationToken
        public Task<string> CancellationTokenExample()
        {
            // Flow ->
            // The caller creates a CancellationTokenSource object.
            // The caller calls a cancelable async API, and passes the CancellationToken from the CancellationTokenSource (CancellationTokenSource.Token).
            // The caller requests cancellation using the CancellationTokenSource object(CancellationTokenSource.Cancel()).
            // The task acknowledges the cancellation and cancels itself, typically using the CancellationToken.ThrowIfCancellationRequested method.
            // Mainly used for desktop and mobile apps
            CancellationTokenSource _cts = new CancellationTokenSource();
            try
            {
                var result = GetDataWithCancellationTokenAsync(_cts.Token);
                return result;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancelled!");
                throw;
            }
            // Microsoft link: https://devblogs.microsoft.com/dotnet/async-in-4-5-enabling-progress-and-cancellation-in-async-apis/
        }
        #endregion

        #region 7-Parallel Running Tasks
        public static async Task SequentialRunMethod()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // This method takes about 2.5s to run
            var complexSum = await SlowAndComplexSumAsync();

            // The elapsed time will be approximately 2.5s so far
            Console.WriteLine("Time elapsed when sum completes..." + stopwatch.Elapsed);

            // This method takes about 4s to run
            var complexWord = await SlowAndComplexWordAsync();

            // The elapsed time at this point will be about 6.5s
            Console.WriteLine("Time elapsed when both complete..." + stopwatch.Elapsed);

            // These lines are to prove the outputs are as expected,
            // i.e. 300 for the complex sum and "ABC...XYZ" for the complex word
            Console.WriteLine("Result of complex sum = " + complexSum);
            Console.WriteLine("Result of complex letter processing " + complexWord);

            //Sample from: https://jeremylindsayni.wordpress.com/2019/03/11/using-async-await-and-task-whenall-to-improve-the-overall-speed-of-your-c-code/
        }

        public static async Task NonSequentialRunMethod()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // this task will take about 2.5s to complete
            var sumTask = SlowAndComplexSumAsync();

            // this task will take about 4s to complete
            var wordTask = SlowAndComplexWordAsync();

            // running them in parallel should take about 4s to complete
            await Task.WhenAll(sumTask, wordTask);

            // The elapsed time at this point will only be about 4s
            Console.WriteLine("Time elapsed when both complete..." + stopwatch.Elapsed);

            // These lines are to prove the outputs are as expected,
            // i.e. 300 for the complex sum and "ABC...XYZ" for the complex word
            Console.WriteLine("Result of complex sum = " + sumTask.Result);
            Console.WriteLine("Result of complex letter processing " + wordTask.Result);

        }
        #endregion

        #region private methods
        private void DoSomethingSync()
        {
            Console.WriteLine("Something Sync happening...");
        }
        private Task<string> GetDataAsync(MyDisposibleDb resource = null)
        {
            return Task.FromResult<string>("Hello data from database");
        }
        private async Task<string> GetDataWithCancellationTokenAsync(CancellationToken ct)
        {
            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(1000);
                // Check if cancellation has been requested
                if (ct != null)
                {
                    ct.ThrowIfCancellationRequested();
                }
            }
            return "Done!";

        }
        private static async Task<int> SlowAndComplexSumAsync()
        {
            int sum = 0;
            foreach (var counter in Enumerable.Range(0, 25))
            {
                sum += counter;
                await Task.Delay(100);
            }

            return sum;
        }
        private static async Task<string> SlowAndComplexWordAsync()
        {
            var word = string.Empty;
            foreach (var counter in Enumerable.Range(65, 26))
            {
                word = string.Concat(word, (char)counter);
                await Task.Delay(150);
            }

            return word;
        }
        // Main Credits for samples: https://medium.com/@deep_blue_day/long-story-short-async-await-best-practices-in-net-1f39d7d84050
        #endregion
    }
}
