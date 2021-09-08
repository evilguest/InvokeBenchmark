# InvokeBenchmark
This is a simple project inspired by Dmitry Valyukov (https://rsdn.org/account/info/21096).
Idea is to measure the overhead of various methods to perform a direct or indirect call in .Net.

## Usage
1. Checkout
2. Build InvokeBenchmark.sln in the Release configuration
3. Switch to the ./Invoke.Benchmark/bin/Release/Net5.0/ folder
4. Run the Invoke.Benchmark.exe 
*Note* that building/running on Linux is not (yet) supported.
## Result comments 
The "baseline" method represents a direct call to the method that adds one to the ulong reference passed to it.
All the other methods are measured in terms of additonal overhead. Think of Ratio column values minus one - it will show how much time does the measured call method take compared to the "call and increment" operation. 
Diff Ratio column does also scale this to the current winner - managed function pointer call. I.e. it's overhead is set to 1; all the other call method overheads in that column are measured against it.

Once I add the GitHub Actions support, this page will feature the recent stats from the cloud benchmark runs.
