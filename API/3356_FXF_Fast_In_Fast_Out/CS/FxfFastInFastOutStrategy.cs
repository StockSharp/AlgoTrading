namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Fast In Fast Out scalping strategy: quick entry/exit using EMA and RSI.
/// </summary>
public class FxfFastInFastOutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public FxfFastInFastOutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		decimal? prevRsi = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, rsi, (candle, emaVal, rsiVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;

				if (prevRsi.HasValue)
				{
					if (prevRsi.Value <= 50 && rsiVal > 50 && close > emaVal && Position <= 0)
						BuyMarket();
					else if (prevRsi.Value >= 50 && rsiVal < 50 && close < emaVal && Position >= 0)
						SellMarket();
				}

				prevRsi = rsiVal;
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
