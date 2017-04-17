# 測試將 Parallel.For 放在 Task 中的範例，
### 以 System.Timers.Timer 來模擬 Service 每隔一段時間就 Run 起來的狀況
### 一般來說使用 平行處理最好是 task 的時間都大致相同，才會有最佳的執行狀態
## 實際問題說明:
### 透過 Service 定期從 DB 取得要執行的 Task ，但是有的 Task 長的很誇張
### 這樣會導致影響到下次 Timer 再起來執行的時間，
### 這時可以將 Parallel.For 再包到 Task 中
### 但因為包到了 Task 之中，所以每個在 Parallel.For 中的 Task 需要記錄執行的 Status 哦!
### 例如執行時，要先記錄為 執行中， 以避免下次 Timer 起來時，取到已在執行的 工作，
### 當然，另外也可以用一個 ConcurrentDictionary or ConcurrentBag 去記錄已執行的 工作也可以哦! 

```C#
class Program
{
	public static class ThreadSafeRandom
	{
		private static Random global = new Random();

		[ThreadStatic]
		private static Random local;

		public static int Next()
		{
			Random inst = local;
			if (inst == null)
			{
				int seed;
				lock (global)
				{
					seed = global.Next();
				}

				local = inst = new Random(seed);
			}

			var result = inst.Next(1, 100);
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}) next value:{result} ... start to sleep  ");
			Thread.Sleep(result * 200);
			//值超過70故意給它錯
			if (result > 70) throw new Exception($"{Thread.CurrentThread.ManagedThreadId})grater then 70");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}) next value:{result} ... after sleep");
			return result;
		}
	}

	static void Main(string[] args)
	{
		var Tasks = new List<Task>();
		int i = 0;
		var timer = new System.Timers.Timer(5000);
		timer.Elapsed += (sender, e) =>
		{
			timer.Stop();
			i++;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"timer start... {DateTime.Now} *********");
			var task = Task.Factory.StartNew(() =>
			{
				var stopwatch = new Stopwatch();
				stopwatch.Start();
				
				Parallel.For(0, 10,
					index =>
					{
						Console.ForegroundColor = ConsoleColor.White;
						Console.WriteLine($"{i} - {index} Start");
						try
						{
							ThreadSafeRandom.Next();
						}
						catch (Exception pex)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine($"{i} - exception ... {pex.ToString()}");
						}
						Console.WriteLine($"{i} - {index} End");
					});
				stopwatch.Stop();
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine(
					$"*{i}* loop time in milliseconds: {stopwatch.ElapsedMilliseconds} ***************** ");
			});
			Tasks.Add(task);
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"timer end... {DateTime.Now} *********");
			if (i < 5)
			{
				timer.Start();
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"timer stop ... *********");
			}
		};
		timer.Start();


		while (!Tasks.Any())
		{
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine("task not run ..press key ....");
			Console.ReadKey();
		}
		while (Tasks.Any())
		{
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			foreach (var t in Tasks)
				Console.WriteLine($"TaskId:{t.Id}, Status:{t.Status}");
			if (Console.ReadKey().Key == ConsoleKey.Q) break;
		}
		Console.ForegroundColor = ConsoleColor.DarkYellow;
		Console.WriteLine("... press key to  end ....");
		Console.ReadKey();
	}
}

```