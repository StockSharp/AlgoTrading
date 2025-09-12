using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Utility strategy that parses session strings into components.
/// </summary>
public class SessionInputParserStrategy : Strategy
{
	private readonly StrategyParam<string> _sessionSimple;
	private readonly StrategyParam<string> _sessionWithWeekdays;

	public string SessionSimple
	{
		get => _sessionSimple.Value;
		set => _sessionSimple.Value = value;
	}

	public string SessionWithWeekdays
	{
		get => _sessionWithWeekdays.Value;
		set => _sessionWithWeekdays.Value = value;
	}

	public SessionInputParserStrategy()
	{
		_sessionSimple = Param(nameof(SessionSimple), "0800-1530")
			.SetDisplay("Simple Session", "Session without weekdays", "General");

		_sessionWithWeekdays = Param(nameof(SessionWithWeekdays), "0800-1530:1234567")
			.SetDisplay("Session With Weekdays", "Session including weekdays", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return Array.Empty<(Security, DataType)>();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var simple = ParseSession(SessionSimple);
		var withWeekdays = ParseSession(SessionWithWeekdays);

		this.AddInfoLog($"Simple: start {simple.hStart:D2}:{simple.mStart:D2}, end {simple.hEnd:D2}:{simple.mEnd:D2}, weekdays [{string.Join(',', simple.weekdays)}]");
		this.AddInfoLog($"With weekdays: start {withWeekdays.hStart:D2}:{withWeekdays.mStart:D2}, end {withWeekdays.hEnd:D2}:{withWeekdays.mEnd:D2}, weekdays [{string.Join(',', withWeekdays.weekdays)}]");
	}

	private (int hStart, int mStart, int hEnd, int mEnd, int[] weekdays) ParseSession(string session)
	{
		string sessionWithoutWeekdays;
		string weekdaysPart = null;

		if (session.Contains(":"))
		{
			var split = session.Split(':');
			sessionWithoutWeekdays = split[0];
			weekdaysPart = split[1];
		}
		else
		{
			sessionWithoutWeekdays = session;
		}

		var startStr = sessionWithoutWeekdays.Substring(0, 4);
		var endStr = sessionWithoutWeekdays.Substring(5, 4);

		var hStart = int.Parse(startStr.Substring(0, 2));
		var mStart = int.Parse(startStr.Substring(2, 2));
		var hEnd = int.Parse(endStr.Substring(0, 2));
		var mEnd = int.Parse(endStr.Substring(2, 2));

		int[] weekdays = Array.Empty<int>();
		if (!string.IsNullOrEmpty(weekdaysPart))
			weekdays = weekdaysPart.Select(c => int.Parse(c.ToString())).ToArray();

		return (hStart, mStart, hEnd, mEnd, weekdays);
	}
}

