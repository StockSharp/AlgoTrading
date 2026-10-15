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
/// Strategy that trades breakouts of a moving average channel built from highs and lows.
/// </summary>
public class XmaRangeChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;

	private ExponentialMovingAverage _highMa = null!;
	private ExponentialMovingAverage _lowMa = null!;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period for channel construction.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public XmaRangeChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for analysis", "General");

		_length = Param(nameof(Length), 7)
			.SetGreaterThanZero()
			.SetDisplay("Channel Length", "Period for high and low moving averages", "Indicator")
			;
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
		_highMa = default;
		_lowMa = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highMa = new ExponentialMovingAverage { Length = Length };
		_lowMa = new ExponentialMovingAverage { Length = Length };

		Indicators.Add(_highMa);
		Indicators.Add(_lowMa);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only finished candles are used.
		if (candle.State != CandleStates.Finished)
			return;

		// Update moving averages with high and low prices.
		var upper = _highMa.Process(new DecimalIndicatorValue(_highMa, candle.HighPrice, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var lower = _lowMa.Process(new DecimalIndicatorValue(_lowMa, candle.LowPrice, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_highMa.IsFormed || !_lowMa.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Breakout above the upper band - go long.
		if (candle.ClosePrice > upper && Position <= 0)
		{
			BuyMarket();
		}
		// Breakout below the lower band - go short.
		else if (candle.ClosePrice < lower && Position >= 0)
		{
			SellMarket();
		}
	}
}