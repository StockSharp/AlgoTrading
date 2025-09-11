using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time-based short strategy: sells on specified Friday hour and buys back on specified Monday hour.
/// </summary>
public class FridayBondShortStrategy : Strategy
{
	private readonly StrategyParam<int> _entryDay;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _exitDay;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<DataType> _candleType;

	private readonly TimeZoneInfo _etZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

	/// <summary>
	/// Day of week to enter short position (2=Mon..6=Fri).
	/// </summary>
	public int EntryDay
	{
		get => _entryDay.Value;
		set => _entryDay.Value = value;
	}

	/// <summary>
	/// Hour to enter short position in ET.
	/// </summary>
	public int EntryHour
	{
		get => _entryHour.Value;
		set => _entryHour.Value = value;
	}

	/// <summary>
	/// Day of week to exit position (2=Mon..6=Fri).
	/// </summary>
	public int ExitDay
	{
		get => _exitDay.Value;
		set => _exitDay.Value = value;
	}

	/// <summary>
	/// Hour to exit position in ET.
	/// </summary>
	public int ExitHour
	{
		get => _exitHour.Value;
		set => _exitHour.Value = value;
	}

	/// <summary>
	/// Candle type used for time checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public FridayBondShortStrategy()
	{
		_entryDay = Param(nameof(EntryDay), 6)
			.SetDisplay("Entry Day", "Day of week to enter short (2=Mon..6=Fri)", "General")
			.SetCanOptimize(true)
			.SetOptimize(2, 6, 1);

		_entryHour = Param(nameof(EntryHour), 13)
			.SetDisplay("Entry Hour", "Hour to enter short (ET)", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_exitDay = Param(nameof(ExitDay), 2)
			.SetDisplay("Exit Day", "Day of week to exit (2=Mon..6=Fri)", "General")
			.SetCanOptimize(true)
			.SetOptimize(2, 6, 1);

		_exitHour = Param(nameof(ExitHour), 13)
			.SetDisplay("Exit Hour", "Hour to exit position (ET)", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used to check time", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var et = TimeZoneInfo.ConvertTime(candle.OpenTime, _etZone);
		var day = (int)et.DayOfWeek + 1;
		var hour = et.Hour;

		if (day == EntryDay && hour == EntryHour && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (day == ExitDay && hour == ExitHour && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
