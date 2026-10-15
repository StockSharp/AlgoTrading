using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MorningPullbackCorridorStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _cooldownCandles;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private int _cooldownRemaining;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int CooldownCandles { get => _cooldownCandles.Value; set => _cooldownCandles.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MorningPullbackCorridorStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 20).SetDisplay("Fast EMA", "Fast EMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 80).SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
		_cooldownCandles = Param(nameof(CooldownCandles), 100).SetDisplay("Cooldown", "Candles between signals", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = default;
		_prevSlow = default;
		_hasPrev = default;
		_cooldownRemaining = default;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
		_cooldownRemaining = 0;

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!_hasPrev) { _prevFast = fast; _prevSlow = slow; _hasPrev = true; return; }

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		if (_prevFast <= _prevSlow && fast > slow && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownCandles;
		}
		else if (_prevFast >= _prevSlow && fast < slow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_cooldownRemaining = CooldownCandles;
		}
		_prevFast = fast;
		_prevSlow = slow;
	}
}
