
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on engulfing candlestick pattern.
/// Supports bullish or bearish setup and trades long or short.
/// Holds position for a fixed number of bars.
/// </summary>
public class EngulfingCandlestickStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _holdPeriods;
	private readonly StrategyParam<PatternType> _pattern;
	private readonly StrategyParam<TradeSide> _side;

	private ICandleMessage _previousCandle;
	private int _barsInPosition;

	/// <summary>
	/// Engulfing pattern type.
	/// </summary>
	public enum PatternType
	{
		Bullish,
		Bearish
	}

	/// <summary>
	/// Trade direction.
	/// </summary>
	public enum TradeSide
	{
		Long,
		Short
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bars to hold the position.
	/// </summary>
	public int HoldPeriods
	{
		get => _holdPeriods.Value;
		set => _holdPeriods.Value = value;
	}

	/// <summary>
	/// Selected engulfing pattern.
	/// </summary>
	public PatternType Pattern
	{
		get => _pattern.Value;
		set => _pattern.Value = value;
	}

	/// <summary>
	/// Trade side when pattern triggers.
	/// </summary>
	public TradeSide Side
	{
		get => _side.Value;
		set => _side.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EngulfingCandlestickStrategy"/>.
	/// </summary>
	public EngulfingCandlestickStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_holdPeriods = Param(nameof(HoldPeriods), 17)
			.SetRange(1, 50)
			.SetDisplay("Hold Periods", "Bars to hold open position", "General")
			.SetCanOptimize(true);

		_pattern = Param(nameof(Pattern), PatternType.Bullish)
			.SetDisplay("Pattern", "Engulfing pattern type", "General");

		_side = Param(nameof(Side), TradeSide.Long)
			.SetDisplay("Trade Side", "Direction of entry", "General");
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

		_previousCandle = null;
		_barsInPosition = 0;
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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			_barsInPosition++;

			if (_barsInPosition >= HoldPeriods)
			{
				ClosePosition();
			}
		}

		if (_previousCandle != null && Position == 0)
		{
			var bullish = candle.ClosePrice > candle.OpenPrice &&
				_previousCandle.ClosePrice < _previousCandle.OpenPrice &&
				candle.OpenPrice < _previousCandle.ClosePrice &&
				candle.ClosePrice > _previousCandle.OpenPrice;

			var bearish = candle.ClosePrice < candle.OpenPrice &&
				_previousCandle.ClosePrice > _previousCandle.OpenPrice &&
				candle.OpenPrice > _previousCandle.ClosePrice &&
				candle.ClosePrice < _previousCandle.OpenPrice;

			var patternDetected = Pattern == PatternType.Bullish ? bullish : bearish;

			if (patternDetected)
			{
				if (Side == TradeSide.Long)
					BuyMarket();
				else
					SellMarket();

				_barsInPosition = 0;
			}
		}

		_previousCandle = candle;
	}
}
