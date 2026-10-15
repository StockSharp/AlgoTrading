using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTC Seasonality strategy.
/// Opens a long (IsLong) or short position at the first candle of EntryHour on EntryDay and closes it at the first candle of
/// ExitHour on ExitDay. Days and hours are in Eastern Standard Time (UTC-5).
/// </summary>
public class BtcSeasonalityStrategy : Strategy
{
	private static readonly TimeSpan _estOffset = TimeSpan.FromHours(-5);

	private readonly StrategyParam<DayOfWeek> _entryDay;
	private readonly StrategyParam<DayOfWeek> _exitDay;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<bool> _isLong;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// EST day of the entry.
	/// </summary>
	public DayOfWeek EntryDay
	{
		get => _entryDay.Value;
		set => _entryDay.Value = value;
	}

	/// <summary>
	/// EST day of the exit.
	/// </summary>
	public DayOfWeek ExitDay
	{
		get => _exitDay.Value;
		set => _exitDay.Value = value;
	}

	/// <summary>
	/// EST hour of the entry.
	/// </summary>
	public int EntryHour
	{
		get => _entryHour.Value;
		set => _entryHour.Value = value;
	}

	/// <summary>
	/// EST hour of the exit.
	/// </summary>
	public int ExitHour
	{
		get => _exitHour.Value;
		set => _exitHour.Value = value;
	}

	/// <summary>
	/// Trade long when true, short otherwise.
	/// </summary>
	public bool IsLong
	{
		get => _isLong.Value;
		set => _isLong.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BtcSeasonalityStrategy()
	{
		_entryDay = Param(nameof(EntryDay), DayOfWeek.Saturday)
			.SetDisplay("Entry Day", "EST day of the entry", "Schedule");

		_exitDay = Param(nameof(ExitDay), DayOfWeek.Monday)
			.SetDisplay("Exit Day", "EST day of the exit", "Schedule");

		_entryHour = Param(nameof(EntryHour), 10)
			.SetRange(0, 23)
			.SetDisplay("Entry Hour", "EST hour of the entry", "Schedule");

		_exitHour = Param(nameof(ExitHour), 10)
			.SetRange(0, 23)
			.SetDisplay("Exit Hour", "EST hour of the exit", "Schedule");

		_isLong = Param(nameof(IsLong), true)
			.SetDisplay("Is Long", "Trade long when true, short otherwise", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var est = candle.OpenTime.Add(_estOffset);

		if (Position != 0)
		{
			if (est.DayOfWeek == ExitDay && est.Hour == ExitHour)
			{
				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(-Position);
			}

			return;
		}

		if (est.DayOfWeek != EntryDay || est.Hour != EntryHour)
			return;

		// Avoid reopening right after the exit when entry and exit share the same moment.
		if (est.DayOfWeek == ExitDay && est.Hour == ExitHour)
			return;

		if (IsLong)
			BuyMarket(Volume);
		else
			SellMarket(Volume);
	}
}
