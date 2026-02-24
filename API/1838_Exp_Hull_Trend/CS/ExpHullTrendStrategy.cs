using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

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

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;

	// Manual WMA for final smoothing
	private readonly List<decimal> _finalBuffer = new();
	private int _finalLength;

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
			.SetDisplay("Hull Length", "Base period for Hull calculation", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for processing", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_finalLength = Math.Max(1, (int)Math.Sqrt(Length));

		var wmaHalf = new WeightedMovingAverage { Length = Math.Max(1, Length / 2) };
		var wmaFull = new WeightedMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wmaHalf, wmaFull, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private decimal CalcWma(decimal newVal)
	{
		_finalBuffer.Add(newVal);
		if (_finalBuffer.Count > _finalLength)
			_finalBuffer.RemoveAt(0);

		if (_finalBuffer.Count < _finalLength)
			return newVal;

		decimal sumWeight = 0;
		decimal sumVal = 0;
		for (int i = 0; i < _finalBuffer.Count; i++)
		{
			var w = i + 1;
			sumVal += _finalBuffer[i] * w;
			sumWeight += w;
		}
		return sumVal / sumWeight;
	}

	private void ProcessCandle(ICandleMessage candle, decimal halfValue, decimal fullValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = 2m * halfValue - fullValue; // intermediate Hull value
		var slow = CalcWma(fast); // smoothed Hull

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
