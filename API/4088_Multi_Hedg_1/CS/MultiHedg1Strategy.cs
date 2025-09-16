using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-currency hedging strategy converted from the MetaTrader MultiHedg_1 expert advisor.
/// It schedules synchronized entries across up to ten symbols and applies equity based protections.
/// </summary>
public class MultiHedg1Strategy : Strategy
{
	private readonly StrategyParam<bool>[] _useSymbolParams;
	private readonly StrategyParam<Security>[] _symbolParams;
	private readonly StrategyParam<decimal>[] _volumeParams;
	private readonly StrategyParam<bool> _sellParam;
	private readonly StrategyParam<int> _tradeHourParam;
	private readonly StrategyParam<int> _tradeMinuteParam;
	private readonly StrategyParam<int> _durationSecondsParam;
	private readonly StrategyParam<bool> _useCloseTimeParam;
	private readonly StrategyParam<int> _closeHourParam;
	private readonly StrategyParam<int> _closeMinuteParam;
	private readonly StrategyParam<bool> _closeByPercentParam;
	private readonly StrategyParam<decimal> _profitPercentParam;
	private readonly StrategyParam<decimal> _lossPercentParam;
	private readonly StrategyParam<DataType> _candleTypeParam;

	private readonly List<TargetInfo> _activeTargets = new();

	private decimal _balanceReference;

	private sealed class TargetInfo
	{
		public TargetInfo(int index, Security security, decimal volume)
		{
			Index = index;
			Security = security;
			Volume = volume;
		}

		public int Index { get; }
		public Security Security { get; }
		public decimal Volume { get; }
	}

	/// <summary>
	/// Initializes strategy parameters and default configuration.
	/// </summary>
	public MultiHedg1Strategy()
	{
		_useSymbolParams = new StrategyParam<bool>[10];
		_symbolParams = new StrategyParam<Security>[10];
		_volumeParams = new StrategyParam<decimal>[10];

		var defaultUsage = new[] { true, true, true, true, true, true, false, false, false, false };
		var defaultVolumes = new[] { 0.1m, 0.2m, 0.3m, 0.4m, 0.5m, 0.6m, 0.7m, 0.8m, 0.9m, 1m };
		var useNames = new[]
		{
			nameof(UseSymbol1),
			nameof(UseSymbol2),
			nameof(UseSymbol3),
			nameof(UseSymbol4),
			nameof(UseSymbol5),
			nameof(UseSymbol6),
			nameof(UseSymbol7),
			nameof(UseSymbol8),
			nameof(UseSymbol9),
			nameof(UseSymbol10)
		};
		var symbolNames = new[]
		{
			nameof(Symbol1),
			nameof(Symbol2),
			nameof(Symbol3),
			nameof(Symbol4),
			nameof(Symbol5),
			nameof(Symbol6),
			nameof(Symbol7),
			nameof(Symbol8),
			nameof(Symbol9),
			nameof(Symbol10)
		};
		var volumeNames = new[]
		{
			nameof(Symbol1Volume),
			nameof(Symbol2Volume),
			nameof(Symbol3Volume),
			nameof(Symbol4Volume),
			nameof(Symbol5Volume),
			nameof(Symbol6Volume),
			nameof(Symbol7Volume),
			nameof(Symbol8Volume),
			nameof(Symbol9Volume),
			nameof(Symbol10Volume)
		};

		for (var i = 0; i < 10; i++)
		{
			var displayIndex = i + 1;
			_useSymbolParams[i] = Param(useNames[i], defaultUsage[i])
				.SetDisplay($"Use Symbol {displayIndex}", $"Enable trading for symbol slot {displayIndex}", "Symbols");

			_symbolParams[i] = Param<Security>(symbolNames[i])
				.SetDisplay($"Symbol {displayIndex}", $"Security assigned to slot {displayIndex}", "Symbols");

			_volumeParams[i] = Param(volumeNames[i], defaultVolumes[i])
				.SetGreaterThanZero()
				.SetDisplay($"Symbol {displayIndex} Volume", $"Order volume used for symbol {displayIndex}", "Symbols");
		}

		_sellParam = Param(nameof(Sell), false)
			.SetDisplay("Sell Mode", "True sends sell orders, false sends buy orders", "Orders");

		_tradeHourParam = Param(nameof(TradeHour), 19)
			.SetRange(0, 23)
			.SetDisplay("Trade Hour", "Hour (platform time) when the entry window opens", "Schedule");

		_tradeMinuteParam = Param(nameof(TradeMinute), 51)
			.SetRange(0, 59)
			.SetDisplay("Trade Minute", "Minute when the entry window opens", "Schedule");

		_durationSecondsParam = Param(nameof(DurationSeconds), 300)
			.SetGreaterThanZero()
			.SetDisplay("Duration (seconds)", "Length of both entry and exit windows in seconds", "Schedule");

		_useCloseTimeParam = Param(nameof(UseCloseTime), true)
			.SetDisplay("Use Close Time", "Enable the timed exit window", "Schedule");

		_closeHourParam = Param(nameof(CloseHour), 20)
			.SetRange(0, 23)
			.SetDisplay("Close Hour", "Hour (platform time) when the exit window opens", "Schedule");

		_closeMinuteParam = Param(nameof(CloseMinute), 50)
			.SetRange(0, 59)
			.SetDisplay("Close Minute", "Minute when the exit window opens", "Schedule");

		_closeByPercentParam = Param(nameof(CloseByPercent), true)
			.SetDisplay("Use Equity Targets", "Close all positions when portfolio equity hits thresholds", "Risk");

		_profitPercentParam = Param(nameof(PercentProfit), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Percent Profit", "Equity gain percentage that triggers a full close", "Risk");

		_lossPercentParam = Param(nameof(PercentLoss), 55.0m)
			.SetGreaterThanZero()
			.SetDisplay("Percent Loss", "Equity drawdown percentage that triggers a full close", "Risk");

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used to drive the schedule evaluation", "Data");
	}

	/// <summary>
	/// Determines whether the strategy should send sell orders.
	/// </summary>
	public bool Sell
	{
		get => _sellParam.Value;
		set => _sellParam.Value = value;
	}

	/// <summary>
	/// Hour when the entry window opens.
	/// </summary>
	public int TradeHour
	{
		get => _tradeHourParam.Value;
		set => _tradeHourParam.Value = value;
	}

	/// <summary>
	/// Minute when the entry window opens.
	/// </summary>
	public int TradeMinute
	{
		get => _tradeMinuteParam.Value;
		set => _tradeMinuteParam.Value = value;
	}

	/// <summary>
	/// Duration in seconds shared by the entry and exit windows.
	/// </summary>
	public int DurationSeconds
	{
		get => _durationSecondsParam.Value;
		set => _durationSecondsParam.Value = value;
	}

	/// <summary>
	/// Enables or disables the timed exit window.
	/// </summary>
	public bool UseCloseTime
	{
		get => _useCloseTimeParam.Value;
		set => _useCloseTimeParam.Value = value;
	}

	/// <summary>
	/// Hour when the exit window opens.
	/// </summary>
	public int CloseHour
	{
		get => _closeHourParam.Value;
		set => _closeHourParam.Value = value;
	}

	/// <summary>
	/// Minute when the exit window opens.
	/// </summary>
	public int CloseMinute
	{
		get => _closeMinuteParam.Value;
		set => _closeMinuteParam.Value = value;
	}

	/// <summary>
	/// Enables the equity based protective exit.
	/// </summary>
	public bool CloseByPercent
	{
		get => _closeByPercentParam.Value;
		set => _closeByPercentParam.Value = value;
	}

	/// <summary>
	/// Equity gain percentage that closes all trades.
	/// </summary>
	public decimal PercentProfit
	{
		get => _profitPercentParam.Value;
		set => _profitPercentParam.Value = value;
	}

	/// <summary>
	/// Equity drawdown percentage that closes all trades.
	/// </summary>
	public decimal PercentLoss
	{
		get => _lossPercentParam.Value;
		set => _lossPercentParam.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate schedule conditions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public bool UseSymbol1
	{
		get => _useSymbolParams[0].Value;
		set => _useSymbolParams[0].Value = value;
	}

	public Security Symbol1
	{
		get => _symbolParams[0].Value;
		set => _symbolParams[0].Value = value;
	}

	public decimal Symbol1Volume
	{
		get => _volumeParams[0].Value;
		set => _volumeParams[0].Value = value;
	}

	public bool UseSymbol2
	{
		get => _useSymbolParams[1].Value;
		set => _useSymbolParams[1].Value = value;
	}

	public Security Symbol2
	{
		get => _symbolParams[1].Value;
		set => _symbolParams[1].Value = value;
	}

	public decimal Symbol2Volume
	{
		get => _volumeParams[1].Value;
		set => _volumeParams[1].Value = value;
	}

	public bool UseSymbol3
	{
		get => _useSymbolParams[2].Value;
		set => _useSymbolParams[2].Value = value;
	}

	public Security Symbol3
	{
		get => _symbolParams[2].Value;
		set => _symbolParams[2].Value = value;
	}

	public decimal Symbol3Volume
	{
		get => _volumeParams[2].Value;
		set => _volumeParams[2].Value = value;
	}

	public bool UseSymbol4
	{
		get => _useSymbolParams[3].Value;
		set => _useSymbolParams[3].Value = value;
	}

	public Security Symbol4
	{
		get => _symbolParams[3].Value;
		set => _symbolParams[3].Value = value;
	}

	public decimal Symbol4Volume
	{
		get => _volumeParams[3].Value;
		set => _volumeParams[3].Value = value;
	}

	public bool UseSymbol5
	{
		get => _useSymbolParams[4].Value;
		set => _useSymbolParams[4].Value = value;
	}

	public Security Symbol5
	{
		get => _symbolParams[4].Value;
		set => _symbolParams[4].Value = value;
	}

	public decimal Symbol5Volume
	{
		get => _volumeParams[4].Value;
		set => _volumeParams[4].Value = value;
	}

	public bool UseSymbol6
	{
		get => _useSymbolParams[5].Value;
		set => _useSymbolParams[5].Value = value;
	}

	public Security Symbol6
	{
		get => _symbolParams[5].Value;
		set => _symbolParams[5].Value = value;
	}

	public decimal Symbol6Volume
	{
		get => _volumeParams[5].Value;
		set => _volumeParams[5].Value = value;
	}

	public bool UseSymbol7
	{
		get => _useSymbolParams[6].Value;
		set => _useSymbolParams[6].Value = value;
	}

	public Security Symbol7
	{
		get => _symbolParams[6].Value;
		set => _symbolParams[6].Value = value;
	}

	public decimal Symbol7Volume
	{
		get => _volumeParams[6].Value;
		set => _volumeParams[6].Value = value;
	}

	public bool UseSymbol8
	{
		get => _useSymbolParams[7].Value;
		set => _useSymbolParams[7].Value = value;
	}

	public Security Symbol8
	{
		get => _symbolParams[7].Value;
		set => _symbolParams[7].Value = value;
	}

	public decimal Symbol8Volume
	{
		get => _volumeParams[7].Value;
		set => _volumeParams[7].Value = value;
	}

	public bool UseSymbol9
	{
		get => _useSymbolParams[8].Value;
		set => _useSymbolParams[8].Value = value;
	}

	public Security Symbol9
	{
		get => _symbolParams[8].Value;
		set => _symbolParams[8].Value = value;
	}

	public decimal Symbol9Volume
	{
		get => _volumeParams[8].Value;
		set => _volumeParams[8].Value = value;
	}

	public bool UseSymbol10
	{
		get => _useSymbolParams[9].Value;
		set => _useSymbolParams[9].Value = value;
	}

	public Security Symbol10
	{
		get => _symbolParams[9].Value;
		set => _symbolParams[9].Value = value;
	}

	public decimal Symbol10Volume
	{
		get => _volumeParams[9].Value;
		set => _volumeParams[9].Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (CandleType == null)
			yield break;

		for (var i = 0; i < 10; i++)
		{
			var security = _symbolParams[i].Value;
			if (security == null)
				continue;

			if (!_useSymbolParams[i].Value)
				continue;

			yield return (security, CandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_activeTargets.Clear();
		_balanceReference = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_activeTargets.Clear();

		for (var i = 0; i < 10; i++)
		{
			if (!_useSymbolParams[i].Value)
				continue;

			var security = _symbolParams[i].Value;
			if (security == null)
				continue;

			var volume = _volumeParams[i].Value;
			if (volume <= 0m)
				continue;

			var target = new TargetInfo(i, security, volume);
			_activeTargets.Add(target);

			// Subscribe to candles of each configured security to drive the schedule evaluation.
			SubscribeCandles(CandleType, security)
				.Bind(candle => ProcessCandle(target, candle))
				.Start();
		}

		if (_activeTargets.Count == 0)
			throw new InvalidOperationException("At least one enabled symbol must be configured.");

		// Store the initial account balance reference used by the equity protection checks.
		_balanceReference = Portfolio?.BeginValue ?? Portfolio?.CurrentValue ?? 0m;
	}

	private void ProcessCandle(TargetInfo target, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Refresh the balance reference whenever all managed positions are closed.
		UpdateBalanceReference();

		// Apply equity based protection before evaluating time windows.
		TryCloseByPercent();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var timeOfDay = candle.OpenTime.TimeOfDay;
		var duration = TimeSpan.FromSeconds(DurationSeconds);

		if (UseCloseTime)
		{
			var closeStart = new TimeSpan(CloseHour, CloseMinute, 0);
			if (IsWithinWindow(timeOfDay, closeStart, duration))
				CloseAllPositions();
		}

		var tradeStart = new TimeSpan(TradeHour, TradeMinute, 0);
		if (IsWithinWindow(timeOfDay, tradeStart, duration))
			OpenTargets();
	}

	private void UpdateBalanceReference()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;

		if (_activeTargets.Count == 0)
		{
			_balanceReference = equity;
			return;
		}

		if (AreAllTargetsFlat())
			_balanceReference = equity;
	}

	private void TryCloseByPercent()
	{
		if (!CloseByPercent)
			return;

		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		var balance = _balanceReference;

		if (balance <= 0m)
			return;

		var profitThreshold = balance * (1m + PercentProfit / 100m);
		var lossThreshold = balance * (1m - PercentLoss / 100m);

		if (PercentProfit > 0m && equity >= profitThreshold)
		{
			CloseAllPositions();
			return;
		}

		if (PercentLoss > 0m && equity <= lossThreshold)
			CloseAllPositions();
	}

	private bool IsWithinWindow(TimeSpan time, TimeSpan start, TimeSpan duration)
	{
		return duration > TimeSpan.Zero && time >= start && time < start + duration;
	}

	private void OpenTargets()
	{
		foreach (var target in _activeTargets)
		{
			OpenTarget(target);
		}
	}

	private void OpenTarget(TargetInfo target)
	{
		var security = target.Security;
		if (security == null)
			return;

		var direction = Sell ? Sides.Sell : Sides.Buy;
		var position = GetNetPosition(security);
		var volume = target.Volume;

		if (volume <= 0m)
			return;

		if (direction == Sides.Buy)
		{
			if (position > 0m)
				return;

			if (position < 0m)
				volume += Math.Abs(position);

			BuyMarket(volume, security);
		}
		else
		{
			if (position < 0m)
				return;

			if (position > 0m)
				volume += position;

			SellMarket(volume, security);
		}
	}

	private void CloseAllPositions()
	{
		foreach (var target in _activeTargets)
		{
			var security = target.Security;
			if (security == null)
				continue;

			if (GetNetPosition(security) == 0m)
				continue;

			// Close the entire position for the managed security.
			ClosePosition(security);
		}
	}

	private bool AreAllTargetsFlat()
	{
		foreach (var target in _activeTargets)
		{
			if (GetNetPosition(target.Security) != 0m)
				return false;
		}

		return true;
	}

	private decimal GetNetPosition(Security security)
	{
		if (security == null || Portfolio == null)
			return 0m;

		return GetPositionValue(security, Portfolio) ?? 0m;
	}
}
