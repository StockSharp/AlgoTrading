namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Testinator strategy: RSI-based entry with EMA trend filter.
/// Buys when RSI rises above threshold and price is above EMA.
/// Sells when RSI drops below threshold and price is below EMA.
/// </summary>
public class TestinatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public decimal RsiBuyLevel { get => _rsiBuyLevel.Value; set => _rsiBuyLevel.Value = value; }
	public decimal RsiSellLevel { get => _rsiSellLevel.Value; set => _rsiSellLevel.Value = value; }

	public TestinatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");
		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 55m)
			.SetDisplay("RSI Buy Level", "RSI threshold for buy signal", "Signals");
		_rsiSellLevel = Param(nameof(RsiSellLevel), 45m)
			.SetDisplay("RSI Sell Level", "RSI threshold for sell signal", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		decimal? prevClose = null;
		decimal? prevEma = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, (candle, rsiVal, emaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;

				if (prevClose.HasValue && prevEma.HasValue)
				{
					var crossUp = prevClose.Value <= prevEma.Value && close > emaVal;
					var crossDown = prevClose.Value >= prevEma.Value && close < emaVal;

					if (crossUp && rsiVal > RsiBuyLevel && Position <= 0)
						BuyMarket();
					else if (crossDown && rsiVal < RsiSellLevel && Position >= 0)
						SellMarket();
				}

				prevClose = close;
				prevEma = emaVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}
