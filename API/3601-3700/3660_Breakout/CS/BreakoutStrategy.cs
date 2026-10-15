namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Donchian breakout strategy converted from the MQL5 BreakoutStrategy expert.
/// </summary>
public class BreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _entryPeriod;
	private readonly StrategyParam<int> _entryShift;
	private readonly StrategyParam<int> _exitPeriod;
	private readonly StrategyParam<int> _exitShift;
	private readonly StrategyParam<bool> _useMiddleLine;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<int> _signalCooldownBars;

	private Highest _entryHighest;
	private Lowest _entryLowest;
	private Highest _exitHighest;
	private Lowest _exitLowest;
	private Shift _entryHighShift;
	private Shift _entryLowShift;
	private Shift _exitHighShift;
	private Shift _exitLowShift;
	private int _cooldownRemaining;

	public BreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations", "General");

		_entryPeriod = Param(nameof(EntryPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Entry Period", "Lookback bars for breakout detection", "Entry")
			
			.SetOptimize(10, 40, 5);

		_entryShift = Param(nameof(EntryShift), 1)
			.SetNotNegative()
			.SetDisplay("Entry Shift", "Bars to delay the Donchian breakout levels", "Entry")
			
			.SetOptimize(0, 3, 1);

		_exitPeriod = Param(nameof(ExitPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Exit Period", "Lookback bars for trailing exits", "Exit")
			
			.SetOptimize(10, 40, 5);

		_exitShift = Param(nameof(ExitShift), 1)
			.SetNotNegative()
			.SetDisplay("Exit Shift", "Bars to delay the trailing channel", "Exit")
			
			.SetOptimize(0, 3, 1);

		_useMiddleLine = Param(nameof(UseMiddleLine), true)
			.SetDisplay("Use Middle Line", "Use the Donchian midline as an exit filter", "Exit");

		_riskPerTrade = Param(nameof(RiskPerTrade), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Per Trade", "Fraction of equity risked per trade", "Risk")
			
			.SetOptimize(0.005m, 0.03m, 0.005m);

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 4)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new breakout entry", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EntryPeriod
	{
		get => _entryPeriod.Value;
		set => _entryPeriod.Value = value;
	}

	public int EntryShift
	{
		get => _entryShift.Value;
		set => _entryShift.Value = value;
	}

	public int ExitPeriod
	{
		get => _exitPeriod.Value;
		set => _exitPeriod.Value = value;
	}

	public int ExitShift
	{
		get => _exitShift.Value;
		set => _exitShift.Value = value;
	}

	public bool UseMiddleLine
	{
		get => _useMiddleLine.Value;
		set => _useMiddleLine.Value = value;
	}

	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}

	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
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

		_entryHighest = null;
		_entryLowest = null;
		_exitHighest = null;
		_exitLowest = null;
		_entryHighShift = null;
		_entryLowShift = null;
		_exitHighShift = null;
		_exitLowShift = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_cooldownRemaining = 0;

		// Create Donchian channel components for entries and exits.
		_entryHighest = new() { Length = EntryPeriod };
		_entryLowest = new() { Length = EntryPeriod };
		_exitHighest = new() { Length = ExitPeriod };
		_exitLowest = new() { Length = ExitPeriod };

		// Create shift indicators only when an offset is required.
		_entryHighShift = EntryShift > 0 ? new Shift { Length = EntryShift } : null;
		_entryLowShift = EntryShift > 0 ? new Shift { Length = EntryShift } : null;
		_exitHighShift = ExitShift > 0 ? new Shift { Length = ExitShift } : null;
		_exitLowShift = ExitShift > 0 ? new Shift { Length = ExitShift } : null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _entryHighest);
			DrawIndicator(area, _entryLowest);
			DrawIndicator(area, _exitHighest);
			DrawIndicator(area, _exitLowest);
			DrawOwnTrades(area);
		}

		StartProtection(null, null);
	}

	private void ProcessCandle(
		ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var time = candle.OpenTime;
		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var entryHighValue = _entryHighest.Process(new CandleIndicatorValue(_entryHighest, candle));
		var entryLowValue = _entryLowest.Process(new CandleIndicatorValue(_entryLowest, candle));
		var exitHighValue = _exitHighest.Process(new CandleIndicatorValue(_exitHighest, candle));
		var exitLowValue = _exitLowest.Process(new CandleIndicatorValue(_exitLowest, candle));

		if (!_entryHighest.IsFormed || !_entryLowest.IsFormed || !_exitHighest.IsFormed || !_exitLowest.IsFormed)
			return;

		// Obtain Donchian bands and apply the configured shift.
		var entryUpper = entryHighValue.ToDecimal();
		var entryLower = entryLowValue.ToDecimal();

		if (_entryHighShift != null)
		{
		var shiftedValue = _entryHighShift.Process(new DecimalIndicatorValue(_entryHighShift, entryUpper, time) { IsFinal = true });
		if (!_entryHighShift.IsFormed || shiftedValue.IsEmpty)
		return;
		entryUpper = shiftedValue.ToDecimal();
		}

		if (_entryLowShift != null)
		{
		var shiftedValue = _entryLowShift.Process(new DecimalIndicatorValue(_entryLowShift, entryLower, time) { IsFinal = true });
		if (!_entryLowShift.IsFormed || shiftedValue.IsEmpty)
		return;
		entryLower = shiftedValue.ToDecimal();
		}

		var exitUpper = exitHighValue.ToDecimal();
		var exitLower = exitLowValue.ToDecimal();

		if (_exitHighShift != null)
		{
		var shiftedValue = _exitHighShift.Process(new DecimalIndicatorValue(_exitHighShift, exitUpper, time) { IsFinal = true });
		if (!_exitHighShift.IsFormed || shiftedValue.IsEmpty)
		return;
		exitUpper = shiftedValue.ToDecimal();
		}

		if (_exitLowShift != null)
		{
		var shiftedValue = _exitLowShift.Process(new DecimalIndicatorValue(_exitLowShift, exitLower, time) { IsFinal = true });
		if (!_exitLowShift.IsFormed || shiftedValue.IsEmpty)
		return;
		exitLower = shiftedValue.ToDecimal();
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var exitMiddle = (exitUpper + exitLower) / 2m;
		var exitLong = UseMiddleLine ? Math.Max(exitMiddle, exitLower) : exitLower;
		var exitShort = UseMiddleLine ? Math.Min(exitMiddle, exitUpper) : exitUpper;

		var step = GetPriceStep();
		if (step <= 0m)
		step = 1m;

		var triggerLong = entryUpper;
		var triggerShort = entryLower;

		// Manage trailing exits before evaluating new entries.
		if (Position > 0m && candle.LowPrice <= exitLong)
		{
		SellMarket(Position);
		_cooldownRemaining = SignalCooldownBars;
		}
		else if (Position < 0m && candle.HighPrice >= exitShort)
		{
		BuyMarket(Math.Abs(Position));
		_cooldownRemaining = SignalCooldownBars;
		}

		// Enter long positions on breakouts above the shifted channel.
		if (_cooldownRemaining == 0 && Position <= 0m && candle.HighPrice >= triggerLong)
		{
		var stopDistance = triggerLong - exitLong;
		if (stopDistance > 0m)
		{
		var volume = CalculateVolume(stopDistance);
		if (volume > 0m)
		{
		BuyMarket(volume + (Position < 0m ? Math.Abs(Position) : 0m));
		_cooldownRemaining = SignalCooldownBars;
		}
		}
		}
		// Enter short positions on breakouts below the shifted channel.
		else if (_cooldownRemaining == 0 && Position >= 0m && candle.LowPrice <= triggerShort)
		{
		var stopDistance = exitShort - triggerShort;
		if (stopDistance > 0m)
		{
		var volume = CalculateVolume(stopDistance);
		if (volume > 0m)
		{
		SellMarket(volume + (Position > 0m ? Position : 0m));
		_cooldownRemaining = SignalCooldownBars;
		}
		}
		}
	}

	private decimal CalculateVolume(decimal stopDistance)
	{
	return Volume > 0m ? Volume : 1m;
	}

	private decimal GetPriceStep()
	{
	return Security?.PriceStep ?? 0m;
	}

	private decimal AlignVolume(decimal volume)
	{
	if (volume <= 0m)
	return 0m;

	var security = Security;
	if (security == null)
	return volume;

	// Align the requested volume to exchange constraints.
	var step = security.VolumeStep ?? 0m;
	if (step > 0m)
	{
	var steps = decimal.Floor(volume / step);
	volume = steps * step;
	if (volume <= 0m)
	volume = step;
	}

	var min = security.MinVolume ?? 0m;
	if (min > 0m && volume < min)
	volume = min;

	var max = security.MaxVolume ?? 0m;
	if (max > 0m && volume > max)
	volume = max;

	return volume;
	}
}

