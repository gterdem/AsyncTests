using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibs
{

    // Holy book: https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md
    public class MyMathLibrary
    {
        public Task<int> AddAsync(int a, int b)
        {
            // BAD This example wastes a thread-pool thread to return a trivially computed value.
            //return Task.Run(() => { a + b}); 

            // GOOD This example uses Task.FromResult to return the trivially computed value. It does not use any extra threads as a result.
            return Task.FromResult(a + b);
        }

        //ValueTask is under System.Threading.Tasks.Extensions comes with C#7
        public ValueTask<int> AddAgainAsync(int a, int b)
        {
            //This example uses a ValueTask<int> to return the trivially computed value. 
            //It does not use any extra threads as a result. It also does not allocate an object on the managed heap.
            return new ValueTask<int>(a + b);
        }
    }
}
