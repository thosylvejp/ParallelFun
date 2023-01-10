using System.Diagnostics;

namespace ParallelFun;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var random = new Random();
        const int countToSearchFor = 2534;
        var numbers = Enumerable.Range(0, 100_000_000).Select(_ => random.Next(0, 100_000_000)).ToList();
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var found = ContainsSingleThreaded(numbers, countToSearchFor);
        Console.WriteLine("Single Threaded: " + stopWatch.Elapsed);
        Console.WriteLine("Found value: " + found);
        stopWatch.Restart();
        found = ContainsParallelForEachMulti(numbers, countToSearchFor);
        Console.WriteLine("ParallelForEach: " + stopWatch.Elapsed);
        Console.WriteLine("Found value: " + found);
        stopWatch.Restart();
        found = await ContainsHandheldParallel(numbers, countToSearchFor);
        Console.WriteLine("Handheld: " + stopWatch.Elapsed);
        Console.WriteLine("Found value: " + found);
    }

    private static bool ContainsParallelForEachMulti(List<int> counts, int countToSearchFor)
    {
        var foundValue = false;

        Parallel.ForEach(
            counts,
            body: count =>
            {
                if (count == countToSearchFor)
                    foundValue = true;
            }
        );

        return foundValue;
    }

    private static bool ContainsSingleThreaded(List<int> counts, int countToSearchFor)
    {
        foreach (var count in counts)
            if (countToSearchFor == count)
                return true;

        return false;
    }

    private static Task<bool> ContainsHandheldParallel(List<int> counts, int countToSearchFor)
    {
        var tcs = new TaskCompletionSource<bool>();

        var processorCount = System.Environment.ProcessorCount;
        var sliceLength = counts.Count / processorCount;
        var tasks = new List<Task>();
        for (var i = 0; i < processorCount; i++)
        {
            var j = i;
            var task = Task.Run(() =>
            {
                var start = j * sliceLength;
                var end = (j + 1) * sliceLength;
                if (j == processorCount - 1)
                    end = counts.Count;
                for (var k = start; k < end; k++)
                    if (counts[k] == countToSearchFor)
                    {
                        tcs.TrySetResult(true);
                        return;
                    }
                        
            });
            tasks.Add(task);
        }

        async Task AfterAll()
        {
            foreach (var task in tasks)
                await task;

            tcs.TrySetResult(false);
        }

        _ = AfterAll();
        
        return tcs.Task;
    }
}