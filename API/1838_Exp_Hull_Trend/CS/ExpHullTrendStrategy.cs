using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exp Hull Trend strategy based on Hull moving average cross.
/// Opens long when fast hull crosses above smoothed hull and short on opposite.
/// </summary>
public class ExpHullTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _wmaHalf;
	private WeightedMovingAverage _wmaFull;
	private WeightedMovingAverage _wmaFinal;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Base period for Hull moving average.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Type of candles for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpHullTrendStrategy"/>.
	/// </summary>
	public ExpHullTrendStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetDisplay("Hull Length", "Base period for Hull calculation", "Indicator")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for processing", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_wmaHalf?.Reset();
		_wmaFull?.Reset();
		_wmaFinal?.Reset();
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_wmaHalf = new WeightedMovingAverage { Length = Math.Max(1, Length / 2) };
		_wmaFull = new WeightedMovingAverage { Length = Length };
		_wmaFinal = new WeightedMovingAverage { Length = Math.Max(1, (int)Math.Sqrt(Length)) };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_wmaHalf, _wmaFull, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _wmaFinal);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue halfValue, IIndicatorValue fullValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var half = halfValue.ToDecimal();
		var full = fullValue.ToDecimal();

		var fast = 2m * half - full; // intermediate Hull value
		var slow = _wmaFinal.Process(fast, candle.ServerTime, true).ToDecimal(); // smoothed Hull

		if (!_prevFast.HasValue)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (crossUp && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (crossDown && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
