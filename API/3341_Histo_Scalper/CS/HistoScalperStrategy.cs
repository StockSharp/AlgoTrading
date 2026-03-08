namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Multi-indicator scalping strategy combining MACD, RSI, and CCI filters.
/// Buys when all indicators agree on bullish signal. Sells on bearish agreement.
/// </summary>
public class HistoScalperStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _cciPeriod;

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

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public HistoScalperStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergence();
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		decimal? prevMacd = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, rsi, cci, (candle, macdLine, rsiVal, cciVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (prevMacd.HasValue)
				{
					if (prevMacd.Value <= 0 && macdLine > 0 && rsiVal < 70m && cciVal > -100m && Position <= 0)
						BuyMarket();
					else if (prevMacd.Value >= 0 && macdLine < 0 && rsiVal > 30m && cciVal < 100m && Position >= 0)
						SellMarket();
				}

				prevMacd = macdLine;
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
