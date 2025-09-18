
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Zone recovery strategy converted from the "ZONE RECOVERY BUTTON" MetaTrader expert advisor.
/// The logic alternates hedging orders around a reference price and mimics the manual buttons with configurable parameters.
/// </summary>
public class ZoneRecoveryButtonStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<ZoneRecoveryStartDirection> _startDirection;
	private readonly StrategyParam<bool> _autoRestart;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _zoneRecoveryPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<bool> _useVolumeMultiplier;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _volumeIncrement;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingProfitThreshold;
	private readonly StrategyParam<decimal> _trailingDrawdown;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _totalEquityRiskPercent;

	private readonly List<TradeStep> _steps = new();

	private ZoneRecoveryStartDirection _currentDirection;
	private bool _isLongCycle;
	private decimal _cycleBasePrice;
	private int _nextStepIndex;
	private decimal _peakCycleProfit;
	private decimal _equityHigh;

	/// <summary>
	/// Initializes a new instance of the <see cref="ZoneRecoveryButtonStrategy"/> class.
	/// </summary>
	public ZoneRecoveryButtonStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for monitoring price", "General");

		_startDirection = Param(nameof(StartDirection), ZoneRecoveryStartDirection.Buy)
			.SetDisplay("Start Direction", "Initial manual button emulation", "General");

		_autoRestart = Param(nameof(AutoRestart), true)
			.SetDisplay("Auto Restart", "Automatically restart the cycle after closing", "General");

		_takeProfitPips = Param(nameof(TakeProfitPips), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance that ends the basket in profit", "Risk Management");

		_zoneRecoveryPips = Param(nameof(ZoneRecoveryPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Zone Width (pips)", "Gap that triggers the opposite hedge", "Risk Management");

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "First trade volume", "Position Sizing");

		_useVolumeMultiplier = Param(nameof(UseVolumeMultiplier), true)
			.SetDisplay("Use Multiplier", "Multiply volume for the next hedge", "Position Sizing");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Factor applied to the previous lot", "Position Sizing");

		_volumeIncrement = Param(nameof(VolumeIncrement), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Increment", "Additive step when multiplier is disabled", "Position Sizing");

		_maxTrades = Param(nameof(MaxTrades), 100)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum orders in one cycle", "Risk Management");

		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
			.SetDisplay("Use Money TP", "Close basket at profit target in currency", "Risk Management");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Money TP", "Target profit in account currency", "Risk Management");

		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
			.SetDisplay("Use Percent TP", "Close basket at percent of balance", "Risk Management");

		_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Percent TP", "Target profit percentage", "Risk Management");

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Lock profit using recovery trailing", "Risk Management");

		_trailingProfitThreshold = Param(nameof(TrailingProfitThreshold), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Start", "Profit required before trailing activates", "Risk Management");

		_trailingDrawdown = Param(nameof(TrailingDrawdown), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Drawdown", "Allowed giveback after trailing start", "Risk Management");

		_useEquityStop = Param(nameof(UseEquityStop), true)
			.SetDisplay("Use Equity Stop", "Emergency exit based on floating loss", "Risk Management");

		_totalEquityRiskPercent = Param(nameof(TotalEquityRiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Equity Risk %", "Maximum floating loss before closing", "Risk Management");
	}

	/// <summary>
	/// Candle type used for monitoring price changes.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initial direction that mimics pressing the BUY or SELL button.
	/// </summary>
	public ZoneRecoveryStartDirection StartDirection
	{
		get => _startDirection.Value;
		set => _startDirection.Value = value;
	}

	/// <summary>
	/// Restart the recovery cycle automatically after closing.
	/// </summary>
	public bool AutoRestart
	{
		get => _autoRestart.Value;
		set => _autoRestart.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips from the reference price.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Zone width in pips before opening the opposite hedge.
	/// </summary>
	public decimal ZoneRecoveryPips
	{
		get => _zoneRecoveryPips.Value;
		set => _zoneRecoveryPips.Value = value;
	}

	/// <summary>
	/// Volume of the first trade in the cycle.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Use multiplicative scaling when adding hedges.
	/// </summary>
	public bool UseVolumeMultiplier
	{
		get => _useVolumeMultiplier.Value;
		set => _useVolumeMultiplier.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied to the previous order.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Additive volume increment when multiplier is disabled.
	/// </summary>
	public decimal VolumeIncrement
	{
		get => _volumeIncrement.Value;
		set => _volumeIncrement.Value = value;
	}

	/// <summary>
	/// Maximum number of trades allowed in the basket.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Enable profit target in account currency.
	/// </summary>
	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Profit target in account currency.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable profit target based on account percentage.
	/// </summary>
	public bool UsePercentTakeProfit
	{
		get => _usePercentTakeProfit.Value;
		set => _usePercentTakeProfit.Value = value;
	}

	/// <summary>
	/// Profit target as a percentage of current balance.
	/// </summary>
	public decimal PercentTakeProfit
	{
		get => _percentTakeProfit.Value;
		set => _percentTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable trailing of floating profit.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit level that starts trailing behaviour.
	/// </summary>
	public decimal TrailingProfitThreshold
	{
		get => _trailingProfitThreshold.Value;
		set => _trailingProfitThreshold.Value = value;
	}

	/// <summary>
	/// Maximum profit giveback after the trailing threshold.
	/// </summary>
	public decimal TrailingDrawdown
	{
		get => _trailingDrawdown.Value;
		set => _trailingDrawdown.Value = value;
	}

	/// <summary>
	/// Enable emergency equity stop logic.
	/// </summary>
	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	/// <summary>
	/// Maximum floating loss percentage relative to the equity peak.
	/// </summary>
	public decimal TotalEquityRiskPercent
	{
		get => _totalEquityRiskPercent.Value;
		set => _totalEquityRiskPercent.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
		{
			yield return (Security, CandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_steps.Clear();
		_currentDirection = ZoneRecoveryStartDirection.None;
		_isLongCycle = false;
		_cycleBasePrice = 0m;
		_nextStepIndex = 0;
		_peakCycleProfit = 0m;
		_equityHigh = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentDirection = StartDirection;
		_equityHigh = GetCurrentEquity();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();

		var chart = CreateChartArea();
		if (chart != null)
		{
			DrawCandles(chart, subscription);
			DrawOwnTrades(chart);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateEquityHigh();

		if (_steps.Count > 0)
		{
			HandleExistingCycle(candle.ClosePrice);
		}
		else
		{
			TryStartCycle(candle.ClosePrice);
		}
	}

	private void TryStartCycle(decimal price)
	{
		if (_currentDirection == ZoneRecoveryStartDirection.None)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		StartCycle(_currentDirection == ZoneRecoveryStartDirection.Buy, price);
	}

	private void StartCycle(bool isLong, decimal price)
	{
		if (InitialVolume <= 0m)
			return;

		_steps.Clear();
		_isLongCycle = isLong;
		_cycleBasePrice = price;
		_nextStepIndex = 1;
		_peakCycleProfit = 0m;
		_equityHigh = GetCurrentEquity();

		ExecuteOrder(isLong, InitialVolume, price);
	}

	private void HandleExistingCycle(decimal price)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var takeProfitOffset = GetPriceOffset(TakeProfitPips);
		if (takeProfitOffset > 0m)
		{
			if (_isLongCycle && price >= _cycleBasePrice + takeProfitOffset)
			{
				CloseCycle();
				return;
			}

			if (!_isLongCycle && price <= _cycleBasePrice - takeProfitOffset)
			{
				CloseCycle();
				return;
			}
		}

		var cycleProfit = CalculateCycleProfit(price);

		if (UseMoneyTakeProfit && MoneyTakeProfit > 0m && cycleProfit >= MoneyTakeProfit)
		{
			CloseCycle();
			return;
		}

		if (UsePercentTakeProfit && PercentTakeProfit > 0m && TryGetPercentTarget(out var percentTarget) && cycleProfit >= percentTarget)
		{
			CloseCycle();
			return;
		}

		if (UseEquityStop && TotalEquityRiskPercent > 0m && cycleProfit < 0m)
		{
			var maxLoss = _equityHigh * TotalEquityRiskPercent / 100m;
			if (Math.Abs(cycleProfit) >= maxLoss)
			{
				CloseCycle();
				return;
			}
		}

		if (EnableTrailing && TrailingProfitThreshold > 0m && TrailingDrawdown > 0m)
		{
			if (cycleProfit >= TrailingProfitThreshold)
			{
				_peakCycleProfit = Math.Max(_peakCycleProfit, cycleProfit);
			}

			if (_peakCycleProfit > 0m && cycleProfit <= _peakCycleProfit - TrailingDrawdown)
			{
				CloseCycle();
				return;
			}
		}
		else
		{
			_peakCycleProfit = 0m;
		}

		if (_steps.Count >= MaxTrades)
			return;

		if (!ShouldOpenNextTrade(price))
			return;

		var nextIsBuy = GetNextDirection();
		var volume = GetNextVolume();

		ExecuteOrder(nextIsBuy, volume, price);
		_nextStepIndex++;
	}

	private bool ShouldOpenNextTrade(decimal price)
	{
		var zoneOffset = GetPriceOffset(ZoneRecoveryPips);
		if (zoneOffset <= 0m)
			return false;

		var nextIsBuy = GetNextDirection();

		if (_isLongCycle)
		{
			if (nextIsBuy)
				return price >= _cycleBasePrice;

			return price <= _cycleBasePrice - zoneOffset;
		}

		if (nextIsBuy)
			return price >= _cycleBasePrice + zoneOffset;

		return price <= _cycleBasePrice;
	}

	private bool GetNextDirection()
	{
		var isOddStep = _nextStepIndex % 2 == 1;
		if (_isLongCycle)
			return !isOddStep;

		return isOddStep;
	}

	private decimal GetNextVolume()
	{
		if (_steps.Count == 0)
			return InitialVolume;

		var lastVolume = _steps[^1].Volume;
		decimal nextVolume;

		if (UseVolumeMultiplier)
		{
			nextVolume = lastVolume * VolumeMultiplier;
		}
		else
		{
			nextVolume = lastVolume + VolumeIncrement;
		}

		return nextVolume <= 0m ? InitialVolume : decimal.Round(nextVolume, 6);
	}

	private decimal CalculateCycleProfit(decimal price)
	{
		if (_steps.Count == 0 || Security == null)
			return 0m;

		var priceStep = Security.PriceStep ?? 0m;
		var stepPrice = Security.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
			return 0m;

		decimal pnl = 0m;
		foreach (var step in _steps)
		{
			var diff = price - step.Price;
			var stepsCount = diff / priceStep;
			var direction = step.IsBuy ? 1m : -1m;
			pnl += stepsCount * stepPrice * step.Volume * direction;
		}

		return pnl;
	}

	private bool TryGetPercentTarget(out decimal target)
	{
		target = 0m;
		if (Portfolio == null)
			return false;

		var balance = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
		if (balance <= 0m)
			return false;

		target = balance * PercentTakeProfit / 100m;
		return true;
	}

	private decimal GetPriceOffset(decimal pips)
	{
		if (Security == null)
			return 0m;

		var priceStep = Security.PriceStep ?? 0m;
		return priceStep <= 0m ? 0m : pips * priceStep;
	}

	private void ExecuteOrder(bool isBuy, decimal volume, decimal price)
	{
		if (volume <= 0m)
			return;

		if (isBuy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_steps.Add(new TradeStep(isBuy, price, volume));
	}

	private void CloseCycle()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		CancelActiveOrders();

		_steps.Clear();
		_nextStepIndex = 0;
		_cycleBasePrice = 0m;
		_peakCycleProfit = 0m;
		_equityHigh = GetCurrentEquity();

		if (!AutoRestart)
		{
			_currentDirection = ZoneRecoveryStartDirection.None;
		}
	}

	private void UpdateEquityHigh()
	{
		var equity = GetCurrentEquity();
		if (equity <= 0m)
			return;

		if (_steps.Count == 0)
		{
			_equityHigh = equity;
			return;
		}

		_equityHigh = Math.Max(_equityHigh, equity);
	}

	private decimal GetCurrentEquity()
	{
		if (Portfolio == null)
			return 0m;

		return Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
	}

	private sealed record TradeStep(bool IsBuy, decimal Price, decimal Volume);
}

/// <summary>
/// Available start directions for the recovery cycle.
/// </summary>
public enum ZoneRecoveryStartDirection
{
	/// <summary>
	/// Do not open any trades automatically.
	/// </summary>
	None,

	/// <summary>
	/// Start with a BUY position.
	/// </summary>
	Buy,

	/// <summary>
	/// Start with a SELL position.
	/// </summary>
	Sell
}
