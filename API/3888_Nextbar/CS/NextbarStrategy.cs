using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class NextbarStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen; private decimal _prevClose; private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NextbarStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20).SetDisplay("EMA Period", "EMA filter", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		if (!_hasPrev) { _prevOpen = candle.OpenPrice; _prevClose = close; _hasPrev = true; return; }

		var prevBullish = _prevClose > _prevOpen;
		var prevBearish = _prevClose < _prevOpen;

		if (prevBullish && close > ema && Position <= 0)
		{ if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (prevBearish && close < ema && Position >= 0)
		{ if (Position > 0) SellMarket(); SellMarket(); }
		_prevOpen = candle.OpenPrice; _prevClose = close;
	}
}
