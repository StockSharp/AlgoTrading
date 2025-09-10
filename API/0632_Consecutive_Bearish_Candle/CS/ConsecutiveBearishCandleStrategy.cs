using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buys after consecutive bearish candles and exits when price breaks the previous high.
/// </summary>
public class ConsecutiveBearishCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private int _bearCount;
	private decimal _prevClose;
	private decimal _prevHigh;

	/// <summary>
	/// Number of consecutive bearish candles required to enter a long position.
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
	/// Start time for trading window.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End time for trading window.
	/// </summary>
	public DateTimeOffset EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ConsecutiveBearishCandleStrategy()
	{
		_lookback = Param(nameof(Lookback), 3)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of consecutive bearish candles to trigger entry", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Start of trading window", "Time Settings");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End of trading window", "Time Settings");
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
		_bearCount = 0;
		_prevClose = 0m;
		_prevHigh = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		if (_prevClose == 0m)
		{
			_prevClose = candle.ClosePrice;
			_prevHigh = candle.HighPrice;
			return;
		}

		var withinWindow = candle.OpenTime >= StartTime && candle.OpenTime <= EndTime;

		if (candle.ClosePrice < _prevClose)
			_bearCount++;
		else if (candle.ClosePrice > _prevClose)
			_bearCount = 0;

		if (Position <= 0 && withinWindow && _bearCount >= Lookback)
			BuyMarket();

		if (Position > 0 && candle.ClosePrice > _prevHigh)
			SellMarket(Position);

		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;
	}
}
