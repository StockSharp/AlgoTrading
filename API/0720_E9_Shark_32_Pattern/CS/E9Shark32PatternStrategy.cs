using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Shark-32 pattern breakout strategy.
/// </summary>
public class E9Shark32PatternStrategy : Strategy
{
	private readonly StrategyParam<decimal> _longStopLoss;
	private readonly StrategyParam<decimal> _shortStopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevLow1;
	private decimal? _prevLow2;
	private decimal? _prevHigh1;
	private decimal? _prevHigh2;

	private decimal? _patternHigh;
	private decimal? _patternLow;
	private decimal? _upperTarget;
	private decimal? _lowerTarget;
	private bool _tradeTaken;
	private decimal _longStopPrice;
	private decimal _shortStopPrice;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss for long positions in percent.
	/// </summary>
	public decimal LongStopLoss
	{
		get => _longStopLoss.Value;
		set => _longStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss for short positions in percent.
	/// </summary>
	public decimal ShortStopLoss
	{
		get => _shortStopLoss.Value;
		set => _shortStopLoss.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public E9Shark32PatternStrategy()
	{
		_longStopLoss = Param(nameof(LongStopLoss), 1m)
			.SetDisplay("Long Stop Loss %", "Stop loss for long positions", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_shortStopLoss = Param(nameof(ShortStopLoss), 1m)
			.SetDisplay("Short Stop Loss %", "Stop loss for short positions", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

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
		_prevLow1 = null;
		_prevLow2 = null;
		_prevHigh1 = null;
		_prevHigh2 = null;
		_patternHigh = null;
		_patternLow = null;
		_upperTarget = null;
		_lowerTarget = null;
		_tradeTaken = false;
		_longStopPrice = 0m;
		_shortStopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var low = candle.LowPrice;
		var high = candle.HighPrice;
		var close = candle.ClosePrice;

		var shark32 = _prevLow2 is decimal low2 &&
			_prevLow1 is decimal low1 &&
			_prevHigh2 is decimal high2 &&
			_prevHigh1 is decimal high1 &&
			low2 < low1 && low1 < low &&
			high2 > high1 && high1 > high;

		if (shark32)
		{
			_patternHigh = _prevHigh2;
			_patternLow = _prevLow2;
			var diff = _patternHigh.Value - _patternLow.Value;
			_upperTarget = _patternHigh + diff;
			_lowerTarget = _patternLow - diff;
			_tradeTaken = false;
		}

		if (_patternHigh is decimal ph && _patternLow is decimal pl && !_tradeTaken)
		{
			if (close > ph && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_longStopPrice = close * (1m - LongStopLoss / 100m);
				_tradeTaken = true;
			}
			else if (close < pl && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_shortStopPrice = close * (1m + ShortStopLoss / 100m);
				_tradeTaken = true;
			}
		}

		if (Position > 0)
		{
			if (_upperTarget is decimal ut && high >= ut)
				SellMarket(Position);
			else if (low <= _longStopPrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (_lowerTarget is decimal lt && low <= lt)
				BuyMarket(Math.Abs(Position));
			else if (high >= _shortStopPrice)
				BuyMarket(Math.Abs(Position));
		}

		_prevLow2 = _prevLow1;
		_prevLow1 = low;
		_prevHigh2 = _prevHigh1;
		_prevHigh1 = high;
	}
}

