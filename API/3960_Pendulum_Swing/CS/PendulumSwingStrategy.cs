using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Pendulum 1_01" MetaTrader strategy.
/// Replicates the idea of symmetric stop entries that scale the volume after each fill.
/// </summary>
public class PendulumSwingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _maxLevels;
	private readonly StrategyParam<int> _manualStepPips;
	private readonly StrategyParam<bool> _useDynamicRange;
	private readonly StrategyParam<decimal> _rangeFraction;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _slippagePips;
	private readonly StrategyParam<bool> _useGlobalTargets;
	private readonly StrategyParam<decimal> _globalTakePercent;
	private readonly StrategyParam<decimal> _globalStopPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _currentStep;
	private decimal _pendingBuyPrice;
	private decimal _pendingSellPrice;
	private decimal _pendingBuyVolume;
	private decimal _pendingSellVolume;
	private int _longLevel;
	private int _shortLevel;
	private decimal _initialEquity;
	private decimal _takeProfitMoney;
	private decimal _stopLossMoney;
	private decimal _entryPrice;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public PendulumSwingStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base volume", "Initial lot used for the very first pending stop.", "Risk")
			
			.SetOptimize(0.1m, 1m, 0.1m);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume multiplier", "Progression factor applied after each filled level.", "Risk")
			
			.SetOptimize(1.2m, 3m, 0.2m);

		_maxLevels = Param(nameof(MaxLevels), 8)
			.SetGreaterThanZero()
			.SetDisplay("Maximum levels", "How many successive fills are allowed per direction before pausing new stops.", "Risk");

		_manualStepPips = Param(nameof(ManualStepPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Manual step (pips)", "Fallback distance between price and stop entries when daily range is not available.", "Entry")
			
			.SetOptimize(20, 120, 10);

		_useDynamicRange = Param(nameof(UseDynamicRange), true)
			.SetDisplay("Use daily range", "Derive the pending distance from the previous daily candle range.", "Entry");

		_rangeFraction = Param(nameof(RangeFraction), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Range fraction", "Portion of the last finished daily range that becomes the base step.", "Entry")
			
			.SetOptimize(0.1m, 0.5m, 0.05m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 10)
			.SetDisplay("Take profit (pips)", "Local profit target for the active position. Zero disables local exits.", "Exit")
			
			.SetOptimize(5, 40, 5);

		_slippagePips = Param(nameof(SlippagePips), 3)
			.SetDisplay("Safety buffer (pips)", "Extra distance added to the computed step to mimic the MetaTrader slippage allowance.", "Entry");

		_useGlobalTargets = Param(nameof(UseGlobalTargets), true)
			.SetDisplay("Use global targets", "Close all positions once account equity reaches configured percentages.", "Exit");

		_globalTakePercent = Param(nameof(GlobalTakePercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Global take-profit %", "Equity growth that triggers closing of every position.", "Exit")
			
			.SetOptimize(0.5m, 3m, 0.5m);

		_globalStopPercent = Param(nameof(GlobalStopPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Global stop-loss %", "Drawdown that forces a full liquidation.", "Exit")
			
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Trading candle", "Primary timeframe used to manage pending stops.", "Data");
	}

	/// <summary>
	/// Base pending order volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied after each fill.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum number of fills per direction.
	/// </summary>
	public int MaxLevels
	{
		get => _maxLevels.Value;
		set => _maxLevels.Value = value;
	}

	/// <summary>
	/// Manual step used when no daily range is available.
	/// </summary>
	public int ManualStepPips
	{
		get => _manualStepPips.Value;
		set => _manualStepPips.Value = value;
	}

	/// <summary>
	/// Whether to derive the step from the daily candle.
	/// </summary>
	public bool UseDynamicRange
	{
		get => _useDynamicRange.Value;
		set => _useDynamicRange.Value = value;
	}

	/// <summary>
	/// Fraction of the daily range used as the base step.
	/// </summary>
	public decimal RangeFraction
	{
		get => _rangeFraction.Value;
		set => _rangeFraction.Value = value;
	}

	/// <summary>
	/// Local take-profit measured in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Slippage buffer measured in pips.
	/// </summary>
	public int SlippagePips
	{
		get => _slippagePips.Value;
		set => _slippagePips.Value = value;
	}

	/// <summary>
	/// Whether global take-profit and stop-loss are enabled.
	/// </summary>
	public bool UseGlobalTargets
	{
		get => _useGlobalTargets.Value;
		set => _useGlobalTargets.Value = value;
	}

	/// <summary>
	/// Equity gain that triggers a full liquidation.
	/// </summary>
	public decimal GlobalTakePercent
	{
		get => _globalTakePercent.Value;
		set => _globalTakePercent.Value = value;
	}

	/// <summary>
	/// Equity drawdown that forces a liquidation.
	/// </summary>
	public decimal GlobalStopPercent
	{
		get => _globalStopPercent.Value;
		set => _globalStopPercent.Value = value;
	}

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, CandleType);

		if (UseDynamicRange)
		{
			yield return (Security, TimeSpan.FromHours(6).TimeFrame());
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_currentStep = 0m;
		_pendingBuyPrice = 0m;
		_pendingSellPrice = 0m;
		_pendingBuyVolume = 0m;
		_pendingSellVolume = 0m;
		_longLevel = 0;
		_shortLevel = 0;
		_initialEquity = 0m;
		_takeProfitMoney = 0m;
		_stopLossMoney = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		Volume = BaseVolume;
		InitializePipSize();
		InitializeEquityTargets();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessTradingCandle)
			.Start();

		if (UseDynamicRange)
		{
			var dailySub = SubscribeCandles(TimeSpan.FromHours(6).TimeFrame());
			dailySub
				.Bind(ProcessDailyCandle)
				.Start();
		}
	}

	private void ProcessTradingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (UseGlobalTargets)
			CheckGlobalTargets();

		ManageLocalTakeProfit(candle);
		EnsurePendulumOrders(candle);
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (!UseDynamicRange || candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0m)
			return;

		var dynamicStep = range * RangeFraction;
		if (dynamicStep <= 0m)
			return;

		_currentStep = dynamicStep;
	}

	private void EnsurePendulumOrders(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = GetEffectiveStep();
		if (step <= 0m)
			return;

		var buffer = SlippagePips > 0 ? SlippagePips * _pipSize : 0m;

		// Check if pending buy level was breached
		if (_pendingBuyPrice > 0m && _pendingBuyVolume > 0m && candle.HighPrice >= _pendingBuyPrice)
		{
			BuyMarket(_pendingBuyVolume);
			_entryPrice = candle.ClosePrice;
			_longLevel = Math.Min(_longLevel + 1, MaxLevels);
			_pendingBuyPrice = 0m;
			_pendingBuyVolume = 0m;
		}

		// Check if pending sell level was breached
		if (_pendingSellPrice > 0m && _pendingSellVolume > 0m && candle.LowPrice <= _pendingSellPrice)
		{
			SellMarket(_pendingSellVolume);
			_entryPrice = candle.ClosePrice;
			_shortLevel = Math.Min(_shortLevel + 1, MaxLevels);
			_pendingSellPrice = 0m;
			_pendingSellVolume = 0m;
		}

		if (Position == 0m)
		{
			_longLevel = 0;
			_shortLevel = 0;
		}

		// Set new pending levels
		_pendingBuyPrice = candle.ClosePrice + step + buffer;
		_pendingSellPrice = candle.ClosePrice - step - buffer;
		_pendingBuyVolume = GetNextVolume(Sides.Buy);
		_pendingSellVolume = GetNextVolume(Sides.Sell);
	}

	private decimal GetNextVolume(Sides side)
	{
		var baseVolume = BaseVolume;
		if (baseVolume <= 0m)
			return 0m;

		var level = side == Sides.Buy ? _longLevel : _shortLevel;
		if (level >= MaxLevels)
			return 0m;

		var multiplier = VolumeMultiplier <= 0m ? 1m : VolumeMultiplier;
		var scaled = baseVolume * (decimal)Math.Pow((double)multiplier, level);
		return AdjustVolume(scaled);
	}

	private void ManageLocalTakeProfit(ICandleMessage candle)
	{
		if (TakeProfitPips <= 0 || Position == 0m)
			return;

		if (_pipSize <= 0m || _entryPrice <= 0m)
			return;

		var diff = candle.ClosePrice - _entryPrice;
		var threshold = TakeProfitPips * _pipSize;

		if (Position > 0 && diff >= threshold)
		{
			SellMarket(Position);
			_longLevel = 0;
		}
		else if (Position < 0 && -diff >= threshold)
		{
			BuyMarket(-Position);
			_shortLevel = 0;
		}
	}

	private void CheckGlobalTargets()
	{
		if (_initialEquity <= 0m)
			return;

		var currentValue = Portfolio?.CurrentValue ?? _initialEquity + PnL;
		var profit = currentValue - _initialEquity;

		if (_takeProfitMoney > 0m && profit >= _takeProfitMoney)
		{
			CloseAllPositions();
		}
		else if (_stopLossMoney > 0m && -profit >= _stopLossMoney)
		{
			CloseAllPositions();
		}
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}

		_pendingBuyPrice = 0m;
		_pendingSellPrice = 0m;
		_pendingBuyVolume = 0m;
		_pendingSellVolume = 0m;
	}

	private decimal GetEffectiveStep()
	{
		var manualStep = ManualStepPips > 0 && _pipSize > 0m
			? ManualStepPips * _pipSize
			: 0m;

		var step = _currentStep > 0m ? _currentStep : manualStep;
		if (step <= 0m)
			step = manualStep;

		return step;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep;
		if (step is > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step.Value, MidpointRounding.AwayFromZero));
			volume = steps * step.Value;
		}

		var minVol = security.MinVolume;
		if (minVol is > 0m && volume < minVol.Value)
			volume = minVol.Value;

		var maxVol = security.MaxVolume;
		if (maxVol is > 0m && volume > maxVol.Value)
			volume = maxVol.Value;

		return volume;
	}

	private void InitializePipSize()
	{
		var security = Security;
		if (security == null)
		{
			_pipSize = 0.01m;
			return;
		}

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
			step = 0.01m;

		_pipSize = step;
	}

	private void InitializeEquityTargets()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
		{
			_initialEquity = 0m;
			_takeProfitMoney = 0m;
			_stopLossMoney = 0m;
			return;
		}

		var currentValue = portfolio.CurrentValue ?? 0m;
		if (currentValue <= 0m)
			currentValue = portfolio.BeginValue ?? 0m;

		_initialEquity = currentValue;

		_takeProfitMoney = _initialEquity * GlobalTakePercent / 100m;
		_stopLossMoney = _initialEquity * GlobalStopPercent / 100m;
	}

}

