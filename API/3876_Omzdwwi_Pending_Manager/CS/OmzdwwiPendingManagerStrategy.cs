using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Omzdwwi Pending Manager strategy - RSI reversal with SMA filter.
/// Buys when RSI crosses below oversold and close is above SMA.
/// Sells when RSI crosses above overbought and close is below SMA.
/// </summary>
public class OmzdwwiPendingManagerStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrev;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OmzdwwiPendingManagerStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI lookback", "Indicators");
		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetDisplay("SMA Period", "SMA lookback", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal sma)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;

		if (!_hasPrev) { _prevRsi = rsi; _hasPrev = true; return; }

		if (_prevRsi >= 30 && rsi < 30 && close > sma && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prevRsi <= 70 && rsi > 70 && close < sma && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		_prevRsi = rsi;
	}
}
