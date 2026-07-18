using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class McGinleyDynamicImprovedStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _signalThresholdPercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _mdPrev;
	private ExponentialMovingAverage _ema;
	private decimal _previousDiff;
	private bool _hasPreviousDiff;
	private int _barsFromSignal;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal SignalThresholdPercent { get => _signalThresholdPercent.Value; set => _signalThresholdPercent.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public McGinleyDynamicImprovedStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "McGinley base period", "General");
		_signalThresholdPercent = Param(nameof(SignalThresholdPercent), 0.25m)
			.SetGreaterThanZero()
			.SetDisplay("Signal Threshold %", "Minimum distance from McGinley in percent", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_mdPrev = null;
		_ema = null;
		_previousDiff = 0m;
		_hasPreviousDiff = false;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_ema = new ExponentialMovingAverage { Length = Period };
		_mdPrev = null;
		_previousDiff = 0m;
		_hasPreviousDiff = false;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_ema.IsFormed)
			return;

		var close = candle.ClosePrice;

		// Calculate McGinley Dynamic
		decimal md;
		if (_mdPrev == null)
		{
			md = close;
		}
		else
		{
			var prev = _mdPrev.Value;
			if (prev == 0m) prev = close;
			var k = 0.6m;
			var period = (decimal)Period;
			var ratio = close / prev;
			var pow = (decimal)Math.Pow((double)ratio, 4.0);
			var denom = k * period * pow;
			if (denom == 0m) denom = 1m;
			md = prev + (close - prev) / denom;
		}
		_mdPrev = md;

		if (close <= 0m)
			return;

		var diff = (close - md) / close * 100m;
		var threshold = SignalThresholdPercent;
		var crossedUp = _hasPreviousDiff && _previousDiff <= threshold && diff > threshold;
		var crossedDown = _hasPreviousDiff && _previousDiff >= -threshold && diff < -threshold;

		_previousDiff = diff;
		_hasPreviousDiff = true;

		_barsFromSignal++;
		if (_barsFromSignal < SignalCooldownBars)
			return;

		if (crossedUp && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (crossedDown && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}
	}
}
