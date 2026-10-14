using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// High Low Breakout Statistical Analysis strategy.
/// Tracks the high and low of the previous completed day, week or month (TimeframeOption). Depending on EntryOption it buys or
/// sells when the close crosses above the previous high, or buys or sells when the close crosses below the previous low. The
/// position is closed after HoldingPeriod candles.
/// </summary>
public class HighLowBreakoutStatisticalAnalysisStrategy : Strategy
{
	/// <summary>
	/// Which level to trade and in which direction.
	/// </summary>
	public enum EntryOptions
	{
		/// <summary>
		/// Buy when the close crosses above the previous high.
		/// </summary>
		LongAtHigh,

		/// <summary>
		/// Sell when the close crosses above the previous high.
		/// </summary>
		ShortAtHigh,

		/// <summary>
		/// Buy when the close crosses below the previous low.
		/// </summary>
		LongAtLow,

		/// <summary>
		/// Sell when the close crosses below the previous low.
		/// </summary>
		ShortAtLow,
	}

	/// <summary>
	/// Period the reference high and low are taken from.
	/// </summary>
	public enum TimeframeOptions
	{
		/// <summary>
		/// Previous day.
		/// </summary>
		Daily,

		/// <summary>
		/// Previous week.
		/// </summary>
		Weekly,

		/// <summary>
		/// Previous month.
		/// </summary>
		Monthly,
	}

	private readonly StrategyParam<EntryOptions> _entryOption;
	private readonly StrategyParam<TimeframeOptions> _timeframeOption;
	private readonly StrategyParam<int> _holdingPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _periodStart;
	private decimal _periodHigh;
	private decimal _periodLow;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;
	private int _barsInPosition;

	/// <summary>
	/// Which level to trade and in which direction.
	/// </summary>
	public EntryOptions EntryOption
	{
		get => _entryOption.Value;
		set => _entryOption.Value = value;
	}

	/// <summary>
	/// Period the reference high and low are taken from.
	/// </summary>
	public TimeframeOptions TimeframeOption
	{
		get => _timeframeOption.Value;
		set => _timeframeOption.Value = value;
	}

	/// <summary>
	/// Candles to hold a position.
	/// </summary>
	public int HoldingPeriod
	{
		get => _holdingPeriod.Value;
		set => _holdingPeriod.Value = value;
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
	public HighLowBreakoutStatisticalAnalysisStrategy()
	{
		_entryOption = Param(nameof(EntryOption), EntryOptions.LongAtHigh)
			.SetDisplay("Entry Option", "Which level to trade and in which direction", "Trading");

		_timeframeOption = Param(nameof(TimeframeOption), TimeframeOptions.Daily)
			.SetDisplay("Timeframe Option", "Period the reference high and low are taken from", "Trading");

		_holdingPeriod = Param(nameof(HoldingPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Holding Period", "Candles to hold a position", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_periodStart = null;
		_periodHigh = 0m;
		_periodLow = 0m;
		_prevHigh = null;
		_prevLow = null;
		_prevClose = null;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

	private DateTime GetPeriodStart(DateTime time)
	{
		var date = time.Date;

		return TimeframeOption switch
		{
			TimeframeOptions.Weekly => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
			TimeframeOptions.Monthly => new DateTime(date.Year, date.Month, 1, 0, 0, 0, date.Kind),
			_ => date,
		};
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var periodStart = GetPeriodStart(candle.OpenTime);

		if (_periodStart != periodStart)
		{
			if (_periodStart != null)
			{
				_prevHigh = _periodHigh;
				_prevLow = _periodLow;
			}

			_periodStart = periodStart;
			_periodHigh = candle.HighPrice;
			_periodLow = candle.LowPrice;
		}
		else
		{
			_periodHigh = Math.Max(_periodHigh, candle.HighPrice);
			_periodLow = Math.Min(_periodLow, candle.LowPrice);
		}

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		_prevClose = close;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			_barsInPosition++;

			if (_barsInPosition >= HoldingPeriod)
			{
				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(-Position);
			}

			return;
		}

		if (prevClose is not decimal lastClose || _prevHigh is not decimal high || _prevLow is not decimal low)
			return;

		var crossHigh = lastClose <= high && close > high;
		var crossLow = lastClose >= low && close < low;

		switch (EntryOption)
		{
			case EntryOptions.LongAtHigh when crossHigh:
			case EntryOptions.LongAtLow when crossLow:
				BuyMarket(Volume);
				_barsInPosition = 0;
				break;
			case EntryOptions.ShortAtHigh when crossHigh:
			case EntryOptions.ShortAtLow when crossLow:
				SellMarket(Volume);
				_barsInPosition = 0;
				break;
		}
	}
}
