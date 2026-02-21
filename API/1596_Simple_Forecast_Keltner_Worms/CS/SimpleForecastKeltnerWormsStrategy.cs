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
/// Simple Forecast - Keltner Worms Strategy - trades when price crosses dynamic Keltner Channel boundaries.
/// </summary>
public class SimpleForecastKeltnerWormsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Channel calculation period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SimpleForecastKeltnerWormsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");

		_length = Param(nameof(Length), 10)
			.SetDisplay("Length", "Channel calculation period", "Indicators")
			;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new EMA { Length = Length };
		_atr = new AverageTrueRange { Length = Length };

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ma = _ema.Process(new DecimalIndicatorValue(_ema, candle.ClosePrice, candle.OpenTime)).ToDecimal();
		var range = _atr.Process(candle).ToDecimal();

		var mult = 0m;
		while (Math.Abs(candle.ClosePrice - ma) > range * mult)
			mult++;

		var upper = ma + range * mult;
		var lower = ma - range * mult;

		if (candle.ClosePrice > upper && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < lower && Position >= 0)
			SellMarket();
	}
}
