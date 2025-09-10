using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Dragonfly and Gravestone Doji candlestick patterns.
/// Buys after a Dragonfly Doji and sells after a Gravestone Doji.
/// </summary>
public class BjCandlePatternsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _dojiThreshold;

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum body size as percentage of candle range to consider a doji.
	/// </summary>
	public decimal DojiThreshold
	{
		get => _dojiThreshold.Value;
		set => _dojiThreshold.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BjCandlePatternsStrategy"/>.
	/// </summary>
	public BjCandlePatternsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_dojiThreshold = Param(nameof(DojiThreshold), 0.1m)
			.SetRange(0.05m, 0.2m)
			.SetDisplay("Doji Threshold", "Max body size as % of range", "Pattern")
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

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range == 0m)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var topWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var bottomWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		var bodyRatio = body / range;
		var topRatio = topWick / range;
		var bottomRatio = bottomWick / range;

		var isDragonfly = bodyRatio <= DojiThreshold && topRatio <= DojiThreshold && bottomRatio >= 0.5m;
		var isGravestone = bodyRatio <= DojiThreshold && bottomRatio <= DojiThreshold && topRatio >= 0.5m;

		if (isDragonfly && Position <= 0)
		{
			BuyMarket();
		}
		else if (isGravestone && Position >= 0)
		{
			SellMarket();
		}
	}
}

