namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// N Trades Per Set Martingale: RSI-based entry with position tracking.
/// Buys when RSI oversold, sells when RSI overbought.
/// </summary>
public class NTradesPerSetMartingaleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public NTradesPerSetMartingaleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		decimal? prevRsi = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, (candle, rsiVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (prevRsi.HasValue)
				{
					var crossBelowOversold = prevRsi.Value >= 30m && rsiVal < 30m;
					var crossAboveOverbought = prevRsi.Value <= 70m && rsiVal > 70m;

					if (crossBelowOversold && Position <= 0)
						BuyMarket();
					else if (crossAboveOverbought && Position >= 0)
						SellMarket();
				}

				prevRsi = rsiVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
}
