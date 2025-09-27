namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

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

	private Highest _entryHighest;
	private Lowest _entryLowest;
	private Highest _exitHighest;
	private Lowest _exitLowest;
	private Shift _entryHighShift;
	private Shift _entryLowShift;
	private Shift _exitHighShift;
	private Shift _exitLowShift;

	public BreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations", "General");

		_entryPeriod = Param(nameof(EntryPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Entry Period", "Lookback bars for breakout detection", "Entry")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_entryShift = Param(nameof(EntryShift), 1)
			.SetNotNegative()
			.SetDisplay("Entry Shift", "Bars to delay the Donchian breakout levels", "Entry")
			.SetCanOptimize(true)
			.SetOptimize(0, 3, 1);

		_exitPeriod = Param(nameof(ExitPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Exit Period", "Lookback bars for trailing exits", "Exit")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_exitShift = Param(nameof(ExitShift), 1)
			.SetNotNegative()
			.SetDisplay("Exit Shift", "Bars to delay the trailing channel", "Exit")
			.SetCanOptimize(true)
			.SetOptimize(0, 3, 1);

		_useMiddleLine = Param(nameof(UseMiddleLine), true)
			.SetDisplay("Use Middle Line", "Use the Donchian midline as an exit filter", "Exit");

		_riskPerTrade = Param(nameof(RiskPerTrade), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Per Trade", "Fraction of equity risked per trade", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.005m, 0.03m, 0.005m);
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

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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
			.BindEx(_entryHighest, _entryLowest, _exitHighest, _exitLowest, ProcessCandle)
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

		StartProtection();
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue entryHighValue,
		IIndicatorValue entryLowValue,
		IIndicatorValue exitHighValue,
		IIndicatorValue exitLowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var time = candle.OpenTime;

		// Obtain Donchian bands and apply the configured shift.
		var entryUpper = entryHighValue.ToDecimal();
		var entryLower = entryLowValue.ToDecimal();

		if (_entryHighShift != null)
		{
		entryUpper = _entryHighShift.Process(entryUpper, time, true).ToDecimal();
		if (!_entryHighShift.IsFormed)
		return;
		}

		if (_entryLowShift != null)
		{
		entryLower = _entryLowShift.Process(entryLower, time, true).ToDecimal();
		if (!_entryLowShift.IsFormed)
		return;
		}

		var exitUpper = exitHighValue.ToDecimal();
		var exitLower = exitLowValue.ToDecimal();

		if (_exitHighShift != null)
		{
		exitUpper = _exitHighShift.Process(exitUpper, time, true).ToDecimal();
		if (!_exitHighShift.IsFormed)
		return;
		}

		if (_exitLowShift != null)
		{
		exitLower = _exitLowShift.Process(exitLower, time, true).ToDecimal();
		if (!_exitLowShift.IsFormed)
		return;
		}

		var exitMiddle = (exitUpper + exitLower) / 2m;
		var exitLong = UseMiddleLine ? Math.Max(exitMiddle, exitLower) : exitLower;
		var exitShort = UseMiddleLine ? Math.Min(exitMiddle, exitUpper) : exitUpper;

		var step = GetPriceStep();
		if (step <= 0m)
		step = 1m;

		var triggerLong = entryUpper + step;
		var triggerShort = entryLower - step;

		// Manage trailing exits before evaluating new entries.
		if (Position > 0m && candle.LowPrice <= exitLong)
		{
		SellMarket(Position);
		}
		else if (Position < 0m && candle.HighPrice >= exitShort)
		{
		BuyMarket(Math.Abs(Position));
		}

		// Enter long positions on breakouts above the shifted channel.
		if (Position <= 0m && candle.HighPrice >= triggerLong)
		{
		var stopDistance = triggerLong - exitLong;
		if (stopDistance > 0m)
		{
		var volume = CalculateVolume(stopDistance);
		if (volume > 0m)
		{
		if (Position < 0m)
		BuyMarket(Math.Abs(Position));

		BuyMarket(volume);
		}
		}
		}
		// Enter short positions on breakouts below the shifted channel.
		else if (Position >= 0m && candle.LowPrice <= triggerShort)
		{
		var stopDistance = exitShort - triggerShort;
		if (stopDistance > 0m)
		{
		var volume = CalculateVolume(stopDistance);
		if (volume > 0m)
		{
		if (Position > 0m)
		SellMarket(Position);

		SellMarket(volume);
		}
		}
		}
	}

	private decimal CalculateVolume(decimal stopDistance)
	{
	if (stopDistance <= 0m)
	return 0m;

	var security = Security;
	var portfolio = Portfolio;

	if (security == null || portfolio == null)
	return AlignVolume(Volume > 0m ? Volume : 0m);

	var capital = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
	if (capital <= 0m || RiskPerTrade <= 0m)
	return AlignVolume(Volume > 0m ? Volume : 0m);

	var step = GetPriceStep();
	if (step <= 0m)
	step = 1m;

	var stepPrice = security.StepPrice ?? step;
	if (stepPrice <= 0m)
	stepPrice = step;

	// Convert the price-based stop into monetary risk per unit.
	var riskPerUnit = stopDistance / step * stepPrice;
	if (riskPerUnit <= 0m)
	return AlignVolume(Volume > 0m ? Volume : 0m);

	var volume = capital * RiskPerTrade / riskPerUnit;
	return AlignVolume(volume);
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

	var min = security.VolumeMin ?? 0m;
	if (min > 0m && volume < min)
	volume = min;

	var max = security.VolumeMax ?? 0m;
	if (max > 0m && volume > max)
	volume = max;

	return volume;
	}
}
