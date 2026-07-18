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
	private readonly StrategyParam<int> _cooldownCandles;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevShort;
	private decimal _prevLong;
	private bool _hasPrev;
	private int _cooldownRemaining;

	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }
	public int CooldownCandles { get => _cooldownCandles.Value; set => _cooldownCandles.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Twenty200TimeBreakoutStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 20).SetDisplay("Short SMA", "Short SMA period", "Indicators");
		_longPeriod = Param(nameof(LongPeriod), 200).SetDisplay("Long SMA", "Long SMA period", "Indicators");
		_cooldownCandles = Param(nameof(CooldownCandles), 100).SetDisplay("Cooldown", "Candles between signals", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevShort = default;
		_prevLong = default;
		_hasPrev = default;
		_cooldownRemaining = default;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevShort = 0;
		_prevLong = 0;
		_hasPrev = false;
		_cooldownRemaining = 0;

		var shortSma = new SimpleMovingAverage { Length = ShortPeriod };
		var longSma = new SimpleMovingAverage { Length = LongPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(shortSma, longSma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortSma, decimal longSma)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!_hasPrev) { _prevShort = shortSma; _prevLong = longSma; _hasPrev = true; return; }

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevShort = shortSma;
			_prevLong = longSma;
			return;
		}

		if (_prevShort <= _prevLong && shortSma > longSma && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownCandles;
		}
		else if (_prevShort >= _prevLong && shortSma < longSma && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_cooldownRemaining = CooldownCandles;
		}
		_prevShort = shortSma;
		_prevLong = longSma;
	}
}
