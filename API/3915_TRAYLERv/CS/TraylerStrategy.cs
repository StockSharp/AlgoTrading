using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class TraylerStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose; private decimal _prevEma; private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TraylerStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20).SetDisplay("EMA Period", "EMA lookback", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetDisplay("ATR Period", "ATR lookback", "Indicators");
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
		if (!_hasPrev) { _prevClose = close; _prevEma = ema; _hasPrev = true; return; }

		if (_prevClose <= _prevEma && close > ema && Position <= 0)
		{ if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevClose >= _prevEma && close < ema && Position >= 0)
		{ if (Position > 0) SellMarket(); SellMarket(); }
		_prevClose = close; _prevEma = ema;
	}
}
