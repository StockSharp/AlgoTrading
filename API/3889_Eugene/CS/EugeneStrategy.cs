using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class EugeneStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose; private decimal _prevSma; private bool _hasPrev;

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EugeneStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 20).SetDisplay("SMA Period", "SMA lookback", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetDisplay("RSI Period", "RSI lookback", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal rsi)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		if (!_hasPrev) { _prevClose = close; _prevSma = sma; _hasPrev = true; return; }

		if (_prevClose <= _prevSma && close > sma && rsi < 70 && Position <= 0)
		{ if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevClose >= _prevSma && close < sma && rsi > 30 && Position >= 0)
		{ if (Position > 0) SellMarket(); SellMarket(); }
		_prevClose = close; _prevSma = sma;
	}
}
