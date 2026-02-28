using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ManualEaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi; private bool _hasPrev;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ManualEaStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 9).SetDisplay("RSI Period", "RSI lookback", "Indicators");
		_emaPeriod = Param(nameof(EmaPeriod), 20).SetDisplay("EMA Period", "EMA filter", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal ema)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		if (!_hasPrev) { _prevRsi = rsi; _hasPrev = true; return; }

		if (_prevRsi <= 20 && rsi > 20 && close > ema && Position <= 0)
		{ if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevRsi >= 80 && rsi < 80 && close < ema && Position >= 0)
		{ if (Position > 0) SellMarket(); SellMarket(); }
		_prevRsi = rsi;
	}
}
