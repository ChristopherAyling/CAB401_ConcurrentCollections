using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class ConcurrentCollectionDemo
{
    static void Main()
    {
        int numPops = 10000;
        int numPerPop = 2000;
        Console.WriteLine("Demoing stack used sequentially");
        StackDemo.SeqStack(numPops, numPerPop);
        Console.WriteLine("Demoing stack used in parallel");
        StackDemo.UnsafePSeqStack(numPops, numPerPop);
        Console.WriteLine("Demoing Concurrent Stack used sequentially");
        ConcStackDemo.DemoStack();
        Console.WriteLine("Demoing Concurrent Stack used in parallel");
        ConcStackDemo.DemoPStack(numPops, numPerPop);

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
}

class ConcStackDemo
{
    public static void DemoStack()
    {
        int errorCount = 0;

        ConcurrentStack<int> cs = new ConcurrentStack<int>();

        cs.Push(1);
        cs.Push(2);

        int result;

        if (!cs.TryPeek(out result))
        {
            Console.WriteLine("  CS: TryPeek() failed when it should of succeeded");
            errorCount++;
        }
        else if (result != 2)
        {
            Console.WriteLine("  CS: TryPeek() saw {0} instead of 2", result);
            errorCount++;
        }

        if (!cs.TryPop(out result))
        {
            Console.WriteLine("  CS: TryPop() failed when it should have succeeded");
            errorCount++;
        }
        else if (result != 2)
        {
            Console.WriteLine("  CS: TryPop() saw {0} instead of 2", result);
            errorCount++;
        }

        if (errorCount == 0) Console.WriteLine("  OK!");
    }

    public static void DemoPStack(int numPops, int numPerPop)
    {
        int errorCount = 0;

        //Construct a ConcurrentStack
        ConcurrentStack<int> cs = new ConcurrentStack<int>();

        for (int i = 0; i < numPops * numPerPop; i++) cs.Push(i);

        // Now read them back, 3 at a time, concurrently
        Parallel.For(0, numPops, i =>
        {
            int[] range = new int[numPerPop];

            if (cs.TryPopRange(range) != numPerPop)
            {
                Console.WriteLine("  CS: TryPopRange failed unexpectedly");
                Interlocked.Increment(ref errorCount);
            }

            // Each range should be consecutive integers, if the range was extractedd atomically
            // And it should be reverse of the original order...
            if (!range.Skip(1).SequenceEqual(range.Take(range.Length - 1).Select(x => x - 1)))
            {
                Interlocked.Increment(ref errorCount);
            }
        });

        // We should have emptied the thing
        if (!cs.IsEmpty)
        {
            Console.WriteLine("  CS: Expected IsEmpty to be true after emptying");
            errorCount++;
        }

        if (errorCount == 0)
        {
            Console.WriteLine("  OK!");
        }
        else
        {
            Console.WriteLine("  Error count: {0}", errorCount);
        }
    }
}

class StackDemo
{
    public static void SeqStack(int numPops, int numPerPop)
    {
        int errorCount = 0;
        Stack<int> s = new Stack<int>();

        for (int i = 0; i < numPops*numPerPop; i++) s.Push(i);

        for (int i = 0; i < numPops; i++)
        {
            int[] range = new int[numPerPop];

            for (int j = 0; j < range.Length; j++)
            {
                range[j] = s.Pop();
            }
        }

        // We should have emptied the thing
        if (!(s.Count == 0))
        {
            Console.WriteLine("  S: Expected Count to be 0 after emptying. was {0}", s.Count);
            errorCount++;
        }

        if (errorCount == 0)
        {
            Console.WriteLine("  OK!");
        }
        else
        {
            Console.WriteLine("  Error count: {0}", errorCount);
        }
    }
    public static void UnsafePSeqStack(int numPops, int numPerPop)
    {
        int errorCount = 0;

        Stack<int> s = new Stack<int>();

        for (int i = 0; i < numPops * numPerPop; i++) s.Push(i);

        // Now read them back, 3 at a time, concurrently
        Parallel.For(0, numPops, i =>
        {
            int[] range = new int[numPerPop];

            for (int j = 0; j < range.Length; j++)
            {
                range[j] = s.Pop();
            }

            for(int k=1; k<range.Length; k++)
            {
                if (range[k-1] != range[k]+1)
                {
                    Interlocked.Increment(ref errorCount);
                }
            }
        });

        // We should have emptied the thing
        if (!(s.Count == 0))
        {
            Console.WriteLine("  S: Expected Count to be 0 after emptying. was {0}", s.Count);
            errorCount++;
        }

        if (errorCount == 0)
        {
            Console.WriteLine("  OK!");
        } else
        {
            Console.WriteLine("  Error count: {0}", errorCount);
        }
    }
}
