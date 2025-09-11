using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA crossovers with trailing stop.
/// </summary>
public class EmaCrossoverShortFocusTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _midEmaLength;
	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<decimal> _trailOffset;
	private readonly StrategyParam<decimal> _trailDistance;

	private decimal _longStop;
	private decimal _shortStop;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Short EMA period.
	/// </summary>
	public int ShortEmaLength
	{
		get => _shortEmaLength.Value;
		set => _shortEmaLength.Value = value;
	}

	/// <summary>
	/// Middle EMA period.
	/// </summary>
	public int MidEmaLength
	{
		get => _midEmaLength.Value;
		set => _midEmaLength.Value = value;
	}

	/// <summary>
	/// Long EMA period.
	/// </summary>
	public int LongEmaLength
	{
		get => _longEmaLength.Value;
		set => _longEmaLength.Value = value;
	}

	/// <summary>
	/// Trailing stop offset.
	/// </summary>
	public decimal TrailOffset
	{
		get => _trailOffset.Value;
		set => _trailOffset.Value = value;
	}

	/// <summary>
	/// Trailing stop distance from extreme.
	/// </summary>
	public decimal TrailDistance
	{
		get => _trailDistance.Value;
		set => _trailDistance.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EmaCrossoverShortFocusTrailingStopStrategy"/>.
	/// </summary>
	public EmaCrossoverShortFocusTrailingStopStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_shortEmaLength = Param(nameof(ShortEmaLength), 13)
			.SetRange(5, 50)
			.SetDisplay("Short EMA", "Short EMA length", "Indicators")
			.SetCanOptimize(true);

		_midEmaLength = Param(nameof(MidEmaLength), 25)
			.SetRange(5, 50)
			.SetDisplay("Mid EMA", "Middle EMA length", "Indicators")
			.SetCanOptimize(true);

		_longEmaLength = Param(nameof(LongEmaLength), 33)
			.SetRange(5, 100)
			.SetDisplay("Long EMA", "Long EMA length", "Indicators")
			.SetCanOptimize(true);

		_trailOffset = Param(nameof(TrailOffset), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Trail Offset", "Offset for trailing stop", "Risk")
			.SetCanOptimize(true);

		_trailDistance = Param(nameof(TrailDistance), 10m)
			.SetRange(2m, 50m)
			.SetDisplay("Trail Distance", "Distance from extreme", "Risk")
			.SetCanOptimize(true);
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

		var shortEma = new ExponentialMovingAverage { Length = ShortEmaLength };
		var midEma = new ExponentialMovingAverage { Length = MidEmaLength };
		var longEma = new ExponentialMovingAverage { Length = LongEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortEma, midEma, longEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, midEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortEma, decimal midEma, decimal longEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (shortEma >= longEma && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_longStop = candle.ClosePrice - TrailDistance;
			_shortStop = 0m;
		}
		else if (shortEma <= longEma && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_shortStop = candle.ClosePrice + TrailDistance;
			_longStop = 0m;
		}

		if (shortEma < longEma && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_longStop = 0m;
		}
		else if (shortEma > midEma && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_shortStop = 0m;
		}

		if (Position > 0)
		{
			var candidate = candle.HighPrice - TrailDistance;
			if (candidate > _longStop)
				_longStop = candidate;

			if (candle.ClosePrice <= _longStop - TrailOffset)
			{
				SellMarket(Math.Abs(Position));
				_longStop = 0m;
			}
		}
		else if (Position < 0)
		{
			var candidate = candle.LowPrice + TrailDistance;
			if (_shortStop == 0m || candidate < _shortStop)
				_shortStop = candidate;

			if (candle.ClosePrice >= _shortStop + TrailOffset)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = 0m;
			}
		}
	}
}
