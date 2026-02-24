using System;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reverse Day Fractal strategy.
/// Goes long when current bar makes a lower low but closes bullish (close > open).
/// Goes short when current bar makes a higher high but closes bearish (close < open).
/// </summary>
public class ReverseDayFractalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevLow;
	private decimal? _prevHigh;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ReverseDayFractalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevLow = null;
		_prevHigh = null;

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

		if (_prevLow.HasValue && _prevHigh.HasValue)
		{
			// Bullish reversal: lower low but bullish close
			var isBullishReversal = candle.LowPrice < _prevLow.Value && candle.ClosePrice > candle.OpenPrice;

			// Bearish reversal: higher high but bearish close
			var isBearishReversal = candle.HighPrice > _prevHigh.Value && candle.ClosePrice < candle.OpenPrice;

			if (isBullishReversal && Position <= 0)
			{
				BuyMarket();
			}
			else if (isBearishReversal && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevLow = candle.LowPrice;
		_prevHigh = candle.HighPrice;
	}
}
