using System.Collections.Generic;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 동일 키·로그 종류(Message/Warning/Error)당 최대 10회만 Verse.Log로 출력. 10회차는 반복 안내 래핑, 이후 호출은 스킵.
	/// </summary>
	public static class RatkinLimitedLog
	{
		public const int MaxEntriesPerKey = 10;

		private static readonly object sync = new object();

		private static readonly Dictionary<int, int> emitCountMessage = new Dictionary<int, int>();

		private static readonly Dictionary<int, int> emitCountWarning = new Dictionary<int, int>();

		private static readonly Dictionary<int, int> emitCountError = new Dictionary<int, int>();

		public static void Message(int key, string text)
		{
			TryEmit(emitCountMessage, key, text, Log.Message);
		}

		public static void Warning(int key, string text)
		{
			TryEmit(emitCountWarning, key, text, Log.Warning);
		}

		public static void Error(int key, string text)
		{
			TryEmit(emitCountError, key, text, Log.Error);
		}

		private static void TryEmit(Dictionary<int, int> counts, int key, string text, System.Action<string> log)
		{
			string toLog;
			lock (sync)
			{
				if (!counts.TryGetValue(key, out var n))
				{
					n = 0;
				}

				if (n >= MaxEntriesPerKey)
				{
					return;
				}

				n++;
				counts[key] = n;
				toLog = n < MaxEntriesPerKey ? text : WrapFinalRepeat(text, n);
			}

			log(toLog);
		}

		private static string WrapFinalRepeat(string text, int occurrence)
		{
			return "[Ratkin 반복 로그] 동일 키로 같은 로그가 반복되어 " + occurrence + "회째(최대 " + MaxEntriesPerKey + "회)입니다. 이후 동일 키 로그는 출력하지 않습니다.\n원문:\n" + text;
		}
	}
}
