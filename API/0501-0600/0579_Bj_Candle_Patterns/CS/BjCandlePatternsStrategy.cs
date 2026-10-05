using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bj Candle Patterns strategy.
/// A doji has a body of at most DojiThreshold of its high-low range. A Dragonfly Doji also has an upper wick of at most
/// DojiThreshold of the range (a long lower wick) and goes long; a Gravestone Doji has a lower wick of at most DojiThreshold of the
/// range (a long upper wick) and goes short. The opposite pattern reverses the position.
/// </summary>
public class BjCandlePatternsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _dojiThreshold;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Maximum body and short-wick size as a fraction of the candle range.
	/// </summary>
	public decimal DojiThreshold
	{
		get => _dojiThreshold.Value;
		set => _dojiThreshold.Value = value;
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
	/// Constructor.
	/// </summary>
	public BjCandlePatternsStrategy()
	{
		_dojiThreshold = Param(nameof(DojiThreshold), 0.1m)
			.SetRange(0m, 1m)
			.SetDisplay("Doji Threshold", "Maximum body and short-wick size as a fraction of the range", "Patterns");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0)
			return;

		var bodyTop = Math.Max(candle.OpenPrice, candle.ClosePrice);
		var bodyBottom = Math.Min(candle.OpenPrice, candle.ClosePrice);
		var limit = DojiThreshold * range;

		if (bodyTop - bodyBottom > limit)
			return;

		var dragonfly = candle.HighPrice - bodyTop <= limit;
		var gravestone = bodyBottom - candle.LowPrice <= limit;

		if (dragonfly && !gravestone && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (gravestone && !dragonfly && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
