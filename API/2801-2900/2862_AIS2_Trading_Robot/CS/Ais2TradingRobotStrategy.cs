namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// AIS2 Trading Robot strategy (simplified).
/// Breakout strategy using candle range with ATR filter.
/// Buys when close is near high of candle and ATR shows volatility.
/// Sells when close is near low of candle.
/// </summary>
public class Ais2TradingRobotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _breakoutThreshold;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal BreakoutThreshold
	{
		get => _breakoutThreshold.Value;
		set => _breakoutThreshold.Value = value;
	}

	public Ais2TradingRobotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for volatility", "Indicators");

		_breakoutThreshold = Param(nameof(BreakoutThreshold), 0.85m)
			.SetDisplay("Breakout Threshold", "Candle body ratio threshold (0-1)", "Signals");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind((ICandleMessage candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var range = candle.HighPrice - candle.LowPrice;
				if (range <= 0)
					return;

				var bodyRatio = (candle.ClosePrice - candle.LowPrice) / range;

				// Buy on strong bullish candle (close near high)
				if (bodyRatio > BreakoutThreshold && candle.ClosePrice > candle.OpenPrice && Position <= 0)
				{
					BuyMarket();
				}
				// Sell on strong bearish candle (close near low)
				else if (bodyRatio < (1m - BreakoutThreshold) && candle.ClosePrice < candle.OpenPrice && Position >= 0)
				{
					SellMarket();
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}
