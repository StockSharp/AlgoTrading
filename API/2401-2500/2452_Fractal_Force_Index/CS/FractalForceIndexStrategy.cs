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
/// Fractal Force Index strategy.
/// Uses EMA of price changes as a momentum measure.
/// Opens or closes positions based on indicator level crossovers.
/// </summary>
public class FractalForceIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevClose;
	private bool _hasPrev;

	/// <summary>
	/// EMA smoothing period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// The type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FractalForceIndexStrategy"/>.
	/// </summary>
	public FractalForceIndexStrategy()
	{
		_period = Param(nameof(Period), 21)
			.SetGreaterThanZero()
			.SetDisplay("Period", "EMA length", "Indicator")
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = 0m;
		_prevClose = 0m;
		_hasPrev = false;
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

		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (_hasPrev)
		{
			// Force-like momentum: price relative to EMA direction
			var crossedAbove = _prevClose <= _prevEma && close > emaValue;
			var crossedBelow = _prevClose >= _prevEma && close < emaValue;

			if (crossedAbove && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (crossedBelow && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevClose = close;
		_prevEma = emaValue;
		_hasPrev = true;
	}
}
