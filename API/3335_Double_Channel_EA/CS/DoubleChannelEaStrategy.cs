using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double Channel EA strategy: BB + EMA trend.
/// Buys when close touches lower BB. Sells when close touches upper BB.
/// </summary>
public class DoubleChannelEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<int> _emaPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public DoubleChannelEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for trend", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BbPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		decimal? prevClose = null;
		decimal? prevEma = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, ema, (candle, bbVal, emaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var bbv = (BollingerBandsValue)bbVal;
				if (bbv.UpBand is not decimal upper || bbv.LowBand is not decimal lower)
					return;

				if (emaVal.IsEmpty)
					return;

				var emaDecimal = emaVal.GetValue<decimal>();
				var close = candle.ClosePrice;

				if (prevClose.HasValue && prevEma.HasValue)
				{
					var crossAboveEma = prevClose.Value <= prevEma.Value && close > emaDecimal;
					var crossBelowEma = prevClose.Value >= prevEma.Value && close < emaDecimal;

					if (crossAboveEma && Position <= 0)
						BuyMarket();
					else if (crossBelowEma && Position >= 0)
						SellMarket();
				}

				prevClose = close;
				prevEma = emaDecimal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}
