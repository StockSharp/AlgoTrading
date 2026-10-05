using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Consecutive Bearish Candle strategy.
/// A candle is bearish when it closes below the previous close. After Lookback bearish candles in a row inside the
/// StartTime-EndTime window the strategy buys, and it closes the long when a candle closes above the previous candle's high.
/// </summary>
public class ConsecutiveBearishCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private decimal? _prevClose;
	private decimal? _prevHigh;
	private int _bearishCount;

	/// <summary>
	/// Number of consecutive bearish candles.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
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
	/// Start of the trading window.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End of the trading window.
	/// </summary>
	public DateTimeOffset EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ConsecutiveBearishCandleStrategy()
	{
		_lookback = Param(nameof(Lookback), 3)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of consecutive bearish candles", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Start of the trading window", "Time");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End of the trading window", "Time");
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
		_prevClose = null;
		_prevHigh = null;
		_bearishCount = 0;
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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevClose = _prevClose;
		var prevHigh = _prevHigh;
		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;

		if (prevClose is not decimal lastClose || prevHigh is not decimal lastHigh)
			return;

		_bearishCount = candle.ClosePrice < lastClose ? _bearishCount + 1 : 0;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && candle.ClosePrice > lastHigh)
		{
			SellMarket(Position);
			return;
		}

		var inWindow = candle.OpenTime >= StartTime.UtcDateTime && candle.OpenTime <= EndTime.UtcDateTime;

		if (inWindow && _bearishCount >= Lookback && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
	}
}
