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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual Vortex indicator strategy with money-management recovery logic converted from Exp_VortexIndicator_MMRec_Duplex.
/// Maintains independent long and short indicator streams and reduces trade size after configurable losing streaks.
/// </summary>
public class VortexIndicatorMmrecDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowLongExits;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _allowShortExits;
	private readonly StrategyParam<int> _longTotalTrigger;
	private readonly StrategyParam<int> _shortTotalTrigger;
	private readonly StrategyParam<int> _longLossTrigger;
	private readonly StrategyParam<int> _shortLossTrigger;
	private readonly StrategyParam<decimal> _longSmallMoneyManagement;
	private readonly StrategyParam<decimal> _shortSmallMoneyManagement;
	private readonly StrategyParam<decimal> _longMoneyManagement;
	private readonly StrategyParam<decimal> _shortMoneyManagement;
	private readonly StrategyParam<MarginModeOptions> _longMarginMode;
	private readonly StrategyParam<MarginModeOptions> _shortMarginMode;
	private readonly StrategyParam<decimal> _longStopLossSteps;
	private readonly StrategyParam<decimal> _shortStopLossSteps;
	private readonly StrategyParam<decimal> _longTakeProfitSteps;
	private readonly StrategyParam<decimal> _shortTakeProfitSteps;
	private readonly StrategyParam<decimal> _longSlippageSteps;
	private readonly StrategyParam<decimal> _shortSlippageSteps;
	private readonly StrategyParam<int> _maxHistory;

	private VortexIndicator _longVortex = null!;
	private VortexIndicator _shortVortex = null!;

	private readonly List<(decimal plus, decimal minus)> _longHistory = new();
	private readonly List<(decimal plus, decimal minus)> _shortHistory = new();

	private readonly Queue<decimal> _longPnls = new();
	private readonly Queue<decimal> _shortPnls = new();

	private decimal _priceStep;
	private decimal? _longStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfitPrice;

	private decimal? _entryPrice;
	private decimal _entryVolume;
	private Sides? _currentSide;

	/// <summary>
	/// Initializes strategy parameters replicated from the original expert advisor.
	/// </summary>
	public VortexIndicatorMmrecDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Long Candle Type", "Timeframe used for the long Vortex calculations.", "General");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Short Candle Type", "Timeframe used for the short Vortex calculations.", "General");

		_longLength = Param(nameof(LongLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Long Vortex Length", "Period for the long Vortex indicator.", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(7, 42, 7);

		_shortLength = Param(nameof(ShortLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Short Vortex Length", "Period for the short Vortex indicator.", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(7, 42, 7);

		_longSignalBar = Param(nameof(LongSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Long Signal Bar", "Closed-bar offset used to evaluate long signals.", "Signals");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Short Signal Bar", "Closed-bar offset used to evaluate short signals.", "Signals");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
		.SetDisplay("Allow Long Entries", "Enable opening long trades when VI+ crosses above VI-.", "Trading");

		_allowLongExits = Param(nameof(AllowLongExits), true)
		.SetDisplay("Allow Long Exits", "Enable closing long trades when VI- dominates VI+.", "Trading");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
		.SetDisplay("Allow Short Entries", "Enable opening short trades when VI+ crosses below VI-.", "Trading");

		_allowShortExits = Param(nameof(AllowShortExits), true)
		.SetDisplay("Allow Short Exits", "Enable closing short trades when VI+ recovers above VI-.", "Trading");

		_longTotalTrigger = Param(nameof(LongTotalTrigger), 5)
		.SetNotNegative()
		.SetDisplay("Long Total Trigger", "Number of recent long trades inspected by the recovery logic.", "Risk");

		_shortTotalTrigger = Param(nameof(ShortTotalTrigger), 5)
		.SetNotNegative()
		.SetDisplay("Short Total Trigger", "Number of recent short trades inspected by the recovery logic.", "Risk");

		_longLossTrigger = Param(nameof(LongLossTrigger), 3)
		.SetNotNegative()
		.SetDisplay("Long Loss Trigger", "Losing trades required to switch to the reduced long volume.", "Risk");

		_shortLossTrigger = Param(nameof(ShortLossTrigger), 3)
		.SetNotNegative()
		.SetDisplay("Short Loss Trigger", "Losing trades required to switch to the reduced short volume.", "Risk");

		_longSmallMoneyManagement = Param(nameof(LongSmallMoneyManagement), 0.01m)
		.SetNotNegative()
		.SetDisplay("Long Reduced MM", "Money-management value used after a long losing streak.", "Risk");

		_shortSmallMoneyManagement = Param(nameof(ShortSmallMoneyManagement), 0.01m)
		.SetNotNegative()
		.SetDisplay("Short Reduced MM", "Money-management value used after a short losing streak.", "Risk");

		_longMoneyManagement = Param(nameof(LongMoneyManagement), 0.1m)
		.SetNotNegative()
		.SetDisplay("Long Base MM", "Default money-management setting for long trades.", "Risk");

		_shortMoneyManagement = Param(nameof(ShortMoneyManagement), 0.1m)
		.SetNotNegative()
		.SetDisplay("Short Base MM", "Default money-management setting for short trades.", "Risk");

		_longMarginMode = Param(nameof(LongMarginMode), MarginModeOptions.Lot)
		.SetDisplay("Long Margin Mode", "Interpretation of the long money-management value.", "Risk");

		_shortMarginMode = Param(nameof(ShortMarginMode), MarginModeOptions.Lot)
		.SetDisplay("Short Margin Mode", "Interpretation of the short money-management value.", "Risk");

		_longStopLossSteps = Param(nameof(LongStopLossSteps), 1000m)
		.SetNotNegative()
		.SetDisplay("Long Stop Loss", "Protective distance below the long entry expressed in price steps.", "Risk");

		_shortStopLossSteps = Param(nameof(ShortStopLossSteps), 1000m)
		.SetNotNegative()
		.SetDisplay("Short Stop Loss", "Protective distance above the short entry expressed in price steps.", "Risk");

		_longTakeProfitSteps = Param(nameof(LongTakeProfitSteps), 2000m)
		.SetNotNegative()
		.SetDisplay("Long Take Profit", "Profit target above the long entry expressed in price steps.", "Risk");

		_shortTakeProfitSteps = Param(nameof(ShortTakeProfitSteps), 2000m)
		.SetNotNegative()
		.SetDisplay("Short Take Profit", "Profit target below the short entry expressed in price steps.", "Risk");

		_longSlippageSteps = Param(nameof(LongSlippageSteps), 10m)
		.SetNotNegative()
		.SetDisplay("Long Slippage", "Expected slippage for long trades in price steps (informational).", "Trading");

		_shortSlippageSteps = Param(nameof(ShortSlippageSteps), 10m)
		.SetNotNegative()
		.SetDisplay("Short Slippage", "Expected slippage for short trades in price steps (informational).", "Trading");
	}

	/// <summary>
	/// Candle type used for long-side calculations.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for short-side calculations.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Long Vortex indicator period.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Short Vortex indicator period.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Closed-bar shift for evaluating long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Closed-bar shift for evaluating short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool AllowLongEntries
	{
		get => _allowLongEntries.Value;
		set => _allowLongEntries.Value = value;
	}

	/// <summary>
	/// Enable closing long positions.
	/// </summary>
	public bool AllowLongExits
	{
		get => _allowLongExits.Value;
		set => _allowLongExits.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool AllowShortEntries
	{
		get => _allowShortEntries.Value;
		set => _allowShortEntries.Value = value;
	}

	/// <summary>
	/// Enable closing short positions.
	/// </summary>
	public bool AllowShortExits
	{
		get => _allowShortExits.Value;
		set => _allowShortExits.Value = value;
	}

	/// <summary>
	/// Number of recent long trades considered by the recovery counter.
	/// </summary>
	public int LongTotalTrigger
	{
		get => _longTotalTrigger.Value;
		set => _longTotalTrigger.Value = value;
	}

	/// <summary>
	/// Number of recent short trades considered by the recovery counter.
	/// </summary>
	public int ShortTotalTrigger
	{
		get => _shortTotalTrigger.Value;
		set => _shortTotalTrigger.Value = value;
	}

	/// <summary>
	/// Long losing trades required to switch to the reduced volume.
	/// </summary>
	public int LongLossTrigger
	{
		get => _longLossTrigger.Value;
		set => _longLossTrigger.Value = value;
	}

	/// <summary>
	/// Short losing trades required to switch to the reduced volume.
	/// </summary>
	public int ShortLossTrigger
	{
		get => _shortLossTrigger.Value;
		set => _shortLossTrigger.Value = value;
	}

	/// <summary>
	/// Reduced long money-management value.
	/// </summary>
	public decimal LongSmallMoneyManagement
	{
		get => _longSmallMoneyManagement.Value;
		set => _longSmallMoneyManagement.Value = value;
	}

	/// <summary>
	/// Reduced short money-management value.
	/// </summary>
	public decimal ShortSmallMoneyManagement
	{
		get => _shortSmallMoneyManagement.Value;
		set => _shortSmallMoneyManagement.Value = value;
	}

	/// <summary>
	/// Default long money-management value.
	/// </summary>
	public decimal LongMoneyManagement
	{
		get => _longMoneyManagement.Value;
		set => _longMoneyManagement.Value = value;
	}

	/// <summary>
	/// Default short money-management value.
	/// </summary>
	public decimal ShortMoneyManagement
	{
		get => _shortMoneyManagement.Value;
		set => _shortMoneyManagement.Value = value;
	}

	/// <summary>
	/// Margin interpretation for long trades.
	/// </summary>
	public MarginModeOptions LongMarginMode
	{
		get => _longMarginMode.Value;
		set => _longMarginMode.Value = value;
	}

	/// <summary>
	/// Margin interpretation for short trades.
	/// </summary>
	public MarginModeOptions ShortMarginMode
	{
		get => _shortMarginMode.Value;
		set => _shortMarginMode.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades expressed in price steps.
	/// </summary>
	public decimal LongStopLossSteps
	{
		get => _longStopLossSteps.Value;
		set => _longStopLossSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades expressed in price steps.
	/// </summary>
	public decimal ShortStopLossSteps
	{
		get => _shortStopLossSteps.Value;
		set => _shortStopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long trades expressed in price steps.
	/// </summary>
	public decimal LongTakeProfitSteps
	{
		get => _longTakeProfitSteps.Value;
		set => _longTakeProfitSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades expressed in price steps.
	/// </summary>
	public decimal ShortTakeProfitSteps
	{
		get => _shortTakeProfitSteps.Value;
		set => _shortTakeProfitSteps.Value = value;
	}

	/// <summary>
	/// Informational slippage setting for long trades.
	/// </summary>
	public decimal LongSlippageSteps
	{
		get => _longSlippageSteps.Value;
		set => _longSlippageSteps.Value = value;
	}

	/// <summary>
	/// Informational slippage setting for short trades.
	/// </summary>
	public decimal ShortSlippageSteps
	{
		get => _shortSlippageSteps.Value;
		set => _shortSlippageSteps.Value = value;
	}

	/// <summary>
	/// Maximum number of indicator points stored for divergence detection.
	/// </summary>
	public int MaxHistory
	{
		get => _maxHistory.Value;
		set => _maxHistory.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longHistory.Clear();
		_shortHistory.Clear();
		_longPnls.Clear();
		_shortPnls.Clear();

		ResetLongState();
		ResetShortState();

		_entryPrice = null;
		_entryVolume = 0m;
		_currentSide = null;

		_longVortex = null!;
		_shortVortex = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 1m;
		}

		Volume = NormalizeVolume(1m);

		_longVortex = new VortexIndicator { Length = LongLength };
		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription
		.Bind(_longVortex, ProcessLongCandle)
		.Start();

		_shortVortex = new VortexIndicator { Length = ShortLength };
		var shortSubscription = SubscribeCandles(ShortCandleType);
		shortSubscription
		.Bind(_shortVortex, ProcessShortCandle)
		.Start();

		StartProtection();

		base.OnStarted(time);
	}

	private void ProcessLongCandle(ICandleMessage candle, decimal viPlus, decimal viMinus)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (CheckRiskManagement(candle.ClosePrice))
		{
			return;
		}

		AppendHistory(_longHistory, (viPlus, viMinus));

		if (!_longVortex.IsFormed)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (!TryGetHistoryPair(_longHistory, LongSignalBar, out var previous, out var current))
		{
			return;
		}

		var crossUp = previous.plus <= previous.minus && current.plus > current.minus;
		var exit = current.minus > current.plus;

		if (exit && AllowLongExits)
		{
			CloseLongPosition(candle.ClosePrice);
		}

		if (crossUp && AllowLongEntries)
		{
			TryOpenLong(candle.ClosePrice);
		}
	}

	private void ProcessShortCandle(ICandleMessage candle, decimal viPlus, decimal viMinus)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (CheckRiskManagement(candle.ClosePrice))
		{
			return;
		}

		AppendHistory(_shortHistory, (viPlus, viMinus));

		if (!_shortVortex.IsFormed)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (!TryGetHistoryPair(_shortHistory, ShortSignalBar, out var previous, out var current))
		{
			return;
		}

		var crossDown = previous.plus >= previous.minus && current.plus < current.minus;
		var exit = current.plus > current.minus;

		if (exit && AllowShortExits)
		{
			CloseShortPosition(candle.ClosePrice);
		}

		if (crossDown && AllowShortEntries)
		{
			TryOpenShort(candle.ClosePrice);
		}
	}

	private void TryOpenLong(decimal price)
	{
		if (_currentSide == Sides.Buy)
		{
			return;
		}

		if (_currentSide == Sides.Sell)
		{
			CloseShortPosition(price);
		}

		TrimQueue(_longPnls, LongTotalTrigger);
		var mm = ShouldUseReducedVolume(_longPnls, LongTotalTrigger, LongLossTrigger)
		? LongSmallMoneyManagement
		: LongMoneyManagement;

		var volume = CalculateVolume(mm, LongMarginMode, LongStopLossSteps, price);
		if (volume <= 0m)
		{
			return;
		}

		BuyMarket(volume);

		_currentSide = Sides.Buy;
		_entryPrice = price;
		_entryVolume = volume;

		_longStopPrice = LongStopLossSteps > 0m ? price - GetStepValue(LongStopLossSteps) : null;
		_longTakeProfitPrice = LongTakeProfitSteps > 0m ? price + GetStepValue(LongTakeProfitSteps) : null;

		ResetShortState();
	}

	private void TryOpenShort(decimal price)
	{
		if (_currentSide == Sides.Sell)
		{
			return;
		}

		if (_currentSide == Sides.Buy)
		{
			CloseLongPosition(price);
		}

		TrimQueue(_shortPnls, ShortTotalTrigger);
		var mm = ShouldUseReducedVolume(_shortPnls, ShortTotalTrigger, ShortLossTrigger)
		? ShortSmallMoneyManagement
		: ShortMoneyManagement;

		var volume = CalculateVolume(mm, ShortMarginMode, ShortStopLossSteps, price);
		if (volume <= 0m)
		{
			return;
		}

		SellMarket(volume);

		_currentSide = Sides.Sell;
		_entryPrice = price;
		_entryVolume = volume;

		_shortStopPrice = ShortStopLossSteps > 0m ? price + GetStepValue(ShortStopLossSteps) : null;
		_shortTakeProfitPrice = ShortTakeProfitSteps > 0m ? price - GetStepValue(ShortTakeProfitSteps) : null;

		ResetLongState();
	}

	private void CloseLongPosition(decimal price)
	{
		if (_currentSide != Sides.Buy)
		{
			ResetLongState();
			return;
		}

		var volume = Position > 0m ? Position : _entryVolume;
		if (volume <= 0m)
		{
			ResetLongState();
			return;
		}

		SellMarket(volume);

		var entry = _entryPrice ?? price;
		var pnl = (price - entry) * volume;
		RegisterTradeResult(Sides.Buy, pnl);

		_currentSide = null;
		_entryPrice = null;
		_entryVolume = 0m;

		ResetLongState();
	}

	private void CloseShortPosition(decimal price)
	{
		if (_currentSide != Sides.Sell)
		{
			ResetShortState();
			return;
		}

		var volume = Position < 0m ? Math.Abs(Position) : _entryVolume;
		if (volume <= 0m)
		{
			ResetShortState();
			return;
		}

		BuyMarket(volume);

		var entry = _entryPrice ?? price;
		var pnl = (entry - price) * volume;
		RegisterTradeResult(Sides.Sell, pnl);

		_currentSide = null;
		_entryPrice = null;
		_entryVolume = 0m;

		ResetShortState();
	}

	private bool CheckRiskManagement(decimal price)
	{
		if (_currentSide == Sides.Buy)
		{
			if (_longStopPrice is decimal stop && price <= stop)
			{
				CloseLongPosition(price);
				return true;
			}

			if (_longTakeProfitPrice is decimal take && price >= take)
			{
				CloseLongPosition(price);
				return true;
			}
		}
		else if (_currentSide == Sides.Sell)
		{
			if (_shortStopPrice is decimal stop && price >= stop)
			{
				CloseShortPosition(price);
				return true;
			}

			if (_shortTakeProfitPrice is decimal take && price <= take)
			{
				CloseShortPosition(price);
				return true;
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}

		return false;
	}

	private void AppendHistory(List<(decimal plus, decimal minus)> history, (decimal plus, decimal minus) value)
	{
		history.Add(value);
		if (history.Count > MaxHistory)
		{
			history.RemoveAt(0);
		}
	}

	private static bool TryGetHistoryPair(List<(decimal plus, decimal minus)> history, int signalBar, out (decimal plus, decimal minus) previous, out (decimal plus, decimal minus) current)
	{
		previous = default;
		current = default;

		var currentIndex = history.Count - 1 - signalBar;
		var previousIndex = currentIndex - 1;

		if (currentIndex < 0 || previousIndex < 0)
		{
			return false;
		}

		current = history[currentIndex];
		previous = history[previousIndex];
		return true;
	}

	private decimal CalculateVolume(decimal mmValue, MarginModeOptions mode, decimal stopSteps, decimal price)
	{
		if (mmValue <= 0m)
		{
			return 0m;
		}

		var capital = Portfolio?.CurrentValue ?? 0m;
		if (mode == MarginModeOptions.Lot)
		{
			return NormalizeVolume(mmValue);
		}

		if (capital <= 0m)
		{
			return NormalizeVolume(mmValue);
		}

		decimal volume;

		switch (mode)
		{
			case MarginModeOptions.FreeMargin:
			case MarginModeOptions.Balance:
			{
				volume = price > 0m ? capital * mmValue / price : 0m;
				break;
			}
			case MarginModeOptions.LossFreeMargin:
			case MarginModeOptions.LossBalance:
			{
				var distance = stopSteps > 0m ? GetStepValue(stopSteps) : 0m;
				if (distance <= 0m && price > 0m)
				{
					distance = price;
				}

				volume = distance > 0m ? capital * mmValue / distance : 0m;
				break;
			}
			default:
			{
				volume = mmValue;
				break;
			}
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security == null)
		{
			return volume;
		}

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;
		}

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		{
			volume = minVolume;
		}

		var maxVolume = Security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
		{
			volume = maxVolume;
		}

		return volume;
	}

	private void RegisterTradeResult(Sides side, decimal pnl)
	{
		var history = side == Sides.Buy ? _longPnls : _shortPnls;
		var totalTrigger = side == Sides.Buy ? LongTotalTrigger : ShortTotalTrigger;

		if (totalTrigger <= 0)
		{
			history.Clear();
			return;
		}

		history.Enqueue(pnl);

		while (history.Count > totalTrigger)
		{
			history.Dequeue();
		}
	}

	private static bool ShouldUseReducedVolume(IEnumerable<decimal> history, int totalTrigger, int lossTrigger)
	{
		if (lossTrigger <= 0 || totalTrigger <= 0)
		{
			return false;
		}

		var losses = 0;
		var inspected = 0;

		foreach (var pnl in history)
		{
			inspected++;
			if (pnl < 0m)
			{
				losses++;
			}

			if (inspected >= totalTrigger)
			{
				break;
			}
		}

		return losses >= lossTrigger;
	}

	private static void TrimQueue(Queue<decimal> queue, int maxCount)
	{
		if (maxCount <= 0)
		{
			queue.Clear();
			return;
		}

		while (queue.Count > maxCount)
		{
			queue.Dequeue();
		}
	}

	private void ResetLongState()
	{
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void ResetShortState()
	{
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}

	private decimal GetStepValue(decimal steps)
	{
		return steps * _priceStep;
	}

	/// <summary>
	/// Margin interpretation modes reproduced from the MetaTrader expert.
	/// </summary>
	public enum MarginModeOptions
	{
		/// <summary>
		/// Treat the money-management value as a share of free margin or balance.
		/// </summary>
		FreeMargin = 0,

		/// <summary>
		/// Treat the money-management value as a direct share of balance.
		/// </summary>
		Balance = 1,

		/// <summary>
		/// Risk a share of capital using the configured stop-loss distance.
		/// </summary>
		LossFreeMargin = 2,

		/// <summary>
		/// Risk a share of balance using the configured stop-loss distance.
		/// </summary>
		LossBalance = 3,

		/// <summary>
		/// Interpret the value as a direct volume in lots.
		/// </summary>
		Lot = 4
	}
}

