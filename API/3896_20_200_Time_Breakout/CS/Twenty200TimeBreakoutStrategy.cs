using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class Twenty200TimeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevShort; private decimal _prevLong; private bool _hasPrev;

	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Twenty200TimeBreakoutStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 20).SetDisplay("Short SMA", "Short SMA period", "Indicators");
		_longPeriod = Param(nameof(LongPeriod), 200).SetDisplay("Long SMA", "Long SMA period", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var shortSma = new SimpleMovingAverage { Length = ShortPeriod };
		var longSma = new SimpleMovingAverage { Length = LongPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(shortSma, longSma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortSma, decimal longSma)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!_hasPrev) { _prevShort = shortSma; _prevLong = longSma; _hasPrev = true; return; }

		if (_prevShort <= _prevLong && shortSma > longSma && Position <= 0)
		{ if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevShort >= _prevLong && shortSma < longSma && Position >= 0)
		{ if (Position > 0) SellMarket(); SellMarket(); }
		_prevShort = shortSma; _prevLong = longSma;
	}
}
