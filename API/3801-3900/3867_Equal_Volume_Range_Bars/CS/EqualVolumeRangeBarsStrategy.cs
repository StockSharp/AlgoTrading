using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Equal Volume Range Bars strategy - ATR-based volatility breakout.
/// Buys when close breaks above EMA + ATR band.
/// Sells when close breaks below EMA - ATR band.
/// </summary>
public class EqualVolumeRangeBarsStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EqualVolumeRangeBarsStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetDisplay("EMA Period", "EMA lookback", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR lookback", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "ATR band multiplier", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnReseted() { base.OnReseted(); }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var upper = ema + atr * AtrMultiplier;
		var lower = ema - atr * AtrMultiplier;

		if (close > upper && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (close < lower && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
	}
}
