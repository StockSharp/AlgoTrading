using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-symbol hedging scheduler strategy converted from the MultiHedg_1 expert.
/// Opens positions for up to ten symbols during a configurable time window
/// and optionally closes everything when equity targets or a separate exit window is reached.
/// </summary>
public class MultiHedgingSchedulerStrategy : Strategy
{
	private readonly StrategyParam<Sides?> _tradeDirection;
	private readonly StrategyParam<TimeSpan> _tradeStartTime;
	private readonly StrategyParam<TimeSpan> _tradeDuration;
	private readonly StrategyParam<bool> _enableTimeClose;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<bool> _enableEquityClose;
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<decimal> _lossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<SymbolSlot> _symbols = new();
	private decimal _initialBalance;

	/// <summary>
	/// Trading direction used when opening positions.
	/// </summary>
	public Sides? TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Time of day when the trading window starts.
	/// </summary>
	public TimeSpan TradeStartTime
	{
		get => _tradeStartTime.Value;
		set => _tradeStartTime.Value = value;
	}

	/// <summary>
	/// Duration of the trading and optional closing windows.
	/// </summary>
	public TimeSpan TradeDuration
	{
		get => _tradeDuration.Value;
		set => _tradeDuration.Value = value;
	}

	/// <summary>
	/// Enables the separate time based close window.
	/// </summary>
	public bool UseTimeClose
	{
		get => _enableTimeClose.Value;
		set => _enableTimeClose.Value = value;
	}

	/// <summary>
	/// Time of day when the closing window starts.
	/// </summary>
	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Enables closing when equity reaches profit or loss thresholds.
	/// </summary>
	public bool CloseByEquityPercent
	{
		get => _enableEquityClose.Value;
		set => _enableEquityClose.Value = value;
	}

	/// <summary>
	/// Percentage profit target based on starting balance.
	/// </summary>
	public decimal PercentProfit
	{
		get => _profitPercent.Value;
		set => _profitPercent.Value = value;
	}

	/// <summary>
	/// Percentage loss threshold based on starting balance.
	/// </summary>
	public decimal PercentLoss
	{
		get => _lossPercent.Value;
		set => _lossPercent.Value = value;
	}

	/// <summary>
	/// Candle series driving the scheduling logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="MultiHedgingSchedulerStrategy"/>.
	/// </summary>
	public MultiHedgingSchedulerStrategy()
	{
		_tradeDirection = Param(nameof(TradeDirection), Sides.Buy)
		.SetDisplay("Trade Direction", "Direction used for opening positions", "General");

		_tradeStartTime = Param(nameof(TradeStartTime), new TimeSpan(19, 51, 0))
		.SetDisplay("Trade Start", "Time of day to begin opening positions", "Scheduling");

		_tradeDuration = Param(nameof(TradeDuration), TimeSpan.FromMinutes(5))
		.SetDisplay("Window Length", "Duration of trading and closing windows", "Scheduling");

		_enableTimeClose = Param(nameof(UseTimeClose), true)
		.SetDisplay("Use Close Window", "Enable time based portfolio closing", "Scheduling");

		_closeTime = Param(nameof(CloseTime), new TimeSpan(20, 50, 0))
		.SetDisplay("Close Start", "Time of day to start the close window", "Scheduling");

		_enableEquityClose = Param(nameof(CloseByEquityPercent), true)
		.SetDisplay("Use Equity Targets", "Enable equity based exit", "Risk Management");

		_profitPercent = Param(nameof(PercentProfit), 1m)
		.SetDisplay("Profit %", "Equity percentage gain to close all positions", "Risk Management")
		.SetCanOptimize(true);

		_lossPercent = Param(nameof(PercentLoss), 55m)
		.SetDisplay("Loss %", "Equity percentage loss to close all positions", "Risk Management")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle series driving the scheduler", "General");

		_symbols.Add(CreateSlot(0, true, "EURUSD", 0.1m));
		_symbols.Add(CreateSlot(1, true, "GBPUSD", 0.2m));
		_symbols.Add(CreateSlot(2, true, "GBPJPY", 0.3m));
		_symbols.Add(CreateSlot(3, true, "EURCAD", 0.4m));
		_symbols.Add(CreateSlot(4, true, "USDCHF", 0.5m));
		_symbols.Add(CreateSlot(5, true, "USDJPY", 0.6m));
		_symbols.Add(CreateSlot(6, false, "USDCHF", 0.7m));
		_symbols.Add(CreateSlot(7, false, "GBPUSD", 0.8m));
		_symbols.Add(CreateSlot(8, false, "EURUSD", 0.9m));
		_symbols.Add(CreateSlot(9, false, "USDJPY", 1m));
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var slot in _symbols)
		{
			if (!slot.Enabled.Value)
			continue;

			slot.Security ??= ResolveSecurity(slot.Symbol.Value);

			if (slot.Security != null)
			yield return (slot.Security, CandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_initialBalance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_initialBalance = Portfolio?.CurrentValue ?? 0m;

		foreach (var slot in _symbols)
		{
			if (!slot.Enabled.Value)
			continue;

			slot.Security = ResolveSecurity(slot.Symbol.Value) ?? throw new InvalidOperationException($"Security '{slot.Symbol.Value}' not found.");

			SubscribeCandles(CandleType, true, slot.Security)
			.Bind(candle => ProcessCandle(candle, slot))
			.Start();
		}
	}

	private SymbolSlot CreateSlot(int index, bool enabled, string symbol, decimal volume)
	{
		var displayIndex = index + 1;
		var group = $"Symbol {displayIndex}";

		var enabledParam = Param($"UseSymbol{index}", enabled)
		.SetDisplay($"Enable #{displayIndex}", $"Enable trading for symbol #{displayIndex}", group);

		var symbolParam = Param($"Symbol{index}", symbol)
		.SetDisplay($"Ticker #{displayIndex}", $"Ticker for symbol #{displayIndex}", group);

		var volumeParam = Param($"Volume{index}", volume)
		.SetGreaterThanZero()
		.SetDisplay($"Volume #{displayIndex}", $"Order volume for symbol #{displayIndex}", group)
		.SetCanOptimize(true);

		return new SymbolSlot
		{
			Index = displayIndex,
			Enabled = enabledParam,
			Symbol = symbolParam,
			Volume = volumeParam
		};
	}

	private Security? ResolveSecurity(string code)
	{
		if (string.IsNullOrWhiteSpace(code))
		return null;

		return SecurityProvider?.LookupById(code);
	}

	private void ProcessCandle(ICandleMessage candle, SymbolSlot slot)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (slot.Security == null)
		return;

		var time = candle.CloseTime ?? candle.OpenTime;

		if (CloseByEquityPercent && TryHandleEquityTargets(time))
		return;

		if (UseTimeClose && TryHandleCloseWindow(time))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (TradeDirection is not { } direction)
		return;

		if (!IsWithinWindow(time.TimeOfDay, TradeStartTime, TradeDuration))
		return;

		ExecuteEntry(slot, direction, time);
	}

	private bool TryHandleEquityTargets(DateTimeOffset time)
	{
		if (_initialBalance <= 0m)
		return false;

		var equity = Portfolio?.CurrentValue;
		if (equity == null)
		return false;

		var profitLevel = _initialBalance * (1m + PercentProfit / 100m);
		var lossLevel = _initialBalance * (1m - PercentLoss / 100m);

		if (equity.Value >= profitLevel || equity.Value <= lossLevel)
		{
			CloseAllManagedPositions();
			LogInfo($"Equity target reached at {time:HH:mm:ss}. Equity: {equity.Value:F2}, start balance: {_initialBalance:F2}.");
			return true;
		}

		return false;
	}

	private bool TryHandleCloseWindow(DateTimeOffset time)
	{
		if (!IsWithinWindow(time.TimeOfDay, CloseTime, TradeDuration))
		return false;

		CloseAllManagedPositions();
		LogInfo($"Time close executed at {time:HH:mm:ss}.");
		return true;
	}

	private void ExecuteEntry(SymbolSlot slot, Sides direction, DateTimeOffset time)
	{
		var position = GetPosition(slot);
		var volume = slot.Volume.Value;

		if (volume <= 0m)
		return;

		if (direction == Sides.Buy)
		{
			if (position > 0m)
			return;

			if (position < 0m)
			volume += Math.Abs(position);

			if (volume <= 0m)
			return;

			BuyMarket(volume, slot.Security);
			LogInfo($"Opened long {volume} on {slot.Security.Id} at {time:HH:mm:ss}.");
		}
		else if (direction == Sides.Sell)
		{
			if (position < 0m)
			return;

			if (position > 0m)
			volume += position;

			if (volume <= 0m)
			return;

			SellMarket(volume, slot.Security);
			LogInfo($"Opened short {volume} on {slot.Security.Id} at {time:HH:mm:ss}.");
		}
	}

	private void CloseAllManagedPositions()
	{
		foreach (var slot in _symbols)
		{
			if (!slot.Enabled.Value || slot.Security == null)
			continue;

			var position = GetPosition(slot);

			if (position > 0m)
			{
				SellMarket(position, slot.Security);
			}
			else if (position < 0m)
			{
				BuyMarket(Math.Abs(position), slot.Security);
			}
		}
	}

	private decimal GetPosition(SymbolSlot slot)
	{
		if (slot.Security == null)
		return 0m;

		var value = GetPositionValue(slot.Security, Portfolio);
		return value ?? 0m;
	}

	private static bool IsWithinWindow(TimeSpan current, TimeSpan start, TimeSpan length)
	{
		if (length <= TimeSpan.Zero)
		return current == start;

		var end = start + length;

		if (end < TimeSpan.FromDays(1))
		return current >= start && current < end;

		var overflow = end - TimeSpan.FromDays(1);
		return current >= start || current < overflow;
	}

	private sealed class SymbolSlot
	{
		public required int Index { get; init; }
		public required StrategyParam<bool> Enabled { get; init; }
		public required StrategyParam<string> Symbol { get; init; }
		public required StrategyParam<decimal> Volume { get; init; }
		public Security? Security { get; set; }
	}
}
