using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// HMA cross strategy with threshold and cooldown to reduce signal noise.
/// </summary>
public class MhHullMovingAverageBasedTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _hullPeriod;
	private readonly StrategyParam<decimal> _signalThresholdPercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private HullMovingAverage _hma;
	private decimal _prevDiffPercent;
	private bool _hasPrevDiff;
	private int _barsFromSignal;

	/// <summary>
	/// Period for Hull Moving Average.
	/// </summary>
	public int HullPeriod
	{
		get => _hullPeriod.Value;
		set => _hullPeriod.Value = value;
	}

	/// <summary>
	/// Minimum price to HMA distance in percent required for a signal.
	/// </summary>
	public decimal SignalThresholdPercent
	{
		get => _signalThresholdPercent.Value;
		set => _signalThresholdPercent.Value = value;
	}

	/// <summary>
	/// Minimum bars between entries.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the strategy.
	/// </summary>
	public MhHullMovingAverageBasedTradingStrategy()
	{
		_hullPeriod = Param(nameof(HullPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("Hull Period", "Period for Hull Moving Average", "Indicators");

		_signalThresholdPercent = Param(nameof(SignalThresholdPercent), 0.15m)
			.SetGreaterThanZero()
			.SetDisplay("Signal Threshold %", "Minimum distance from HMA", "Indicators");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_hma = null;
		_prevDiffPercent = 0m;
		_hasPrevDiff = false;
		_barsFromSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_hma = new HullMovingAverage { Length = HullPeriod };
		_prevDiffPercent = 0m;
		_hasPrevDiff = false;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_hma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hma.IsFormed)
			return;

		var price = candle.ClosePrice;
		if (price <= 0m)
			return;

		var diffPercent = (price - hmaValue) / price * 100m;
		var threshold = SignalThresholdPercent;
		var crossedUp = _hasPrevDiff && _prevDiffPercent <= threshold && diffPercent > threshold;
		var crossedDown = _hasPrevDiff && _prevDiffPercent >= -threshold && diffPercent < -threshold;

		_prevDiffPercent = diffPercent;
		_hasPrevDiff = true;

		_barsFromSignal++;
		if (_barsFromSignal < SignalCooldownBars)
			return;

		if (crossedUp && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_barsFromSignal = 0;
		}
		else if (crossedDown && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_barsFromSignal = 0;
		}
	}
}
