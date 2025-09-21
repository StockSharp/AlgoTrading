using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Renko duplex strategy with MMRec money-management translated from Exp_AdaptiveRenko_MMRec_Duplex.
/// Recreates the dual Renko streams and the loss-driven position sizing that alternates between normal and reduced volumes.
/// </summary>
public class ExpAdaptiveRenkoMmrecDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<AdaptiveRenkoVolatilityMode> _longVolatilityMode;
	private readonly StrategyParam<AdaptiveRenkoVolatilityMode> _shortVolatilityMode;
	private readonly StrategyParam<int> _longVolatilityPeriod;
	private readonly StrategyParam<int> _shortVolatilityPeriod;
	private readonly StrategyParam<decimal> _longSensitivity;
	private readonly StrategyParam<decimal> _shortSensitivity;
	private readonly StrategyParam<AdaptiveRenkoPriceMode> _longPriceMode;
	private readonly StrategyParam<AdaptiveRenkoPriceMode> _shortPriceMode;
	private readonly StrategyParam<decimal> _longMinimumBrickPoints;
	private readonly StrategyParam<decimal> _shortMinimumBrickPoints;
	private readonly StrategyParam<int> _longSignalBarOffset;
	private readonly StrategyParam<int> _shortSignalBarOffset;
	private readonly StrategyParam<bool> _longEntriesEnabled;
	private readonly StrategyParam<bool> _longExitsEnabled;
	private readonly StrategyParam<bool> _shortEntriesEnabled;
	private readonly StrategyParam<bool> _shortExitsEnabled;
	private readonly StrategyParam<int> _longTotalTrigger;
	private readonly StrategyParam<int> _shortTotalTrigger;
	private readonly StrategyParam<int> _longLossTrigger;
	private readonly StrategyParam<int> _shortLossTrigger;
	private readonly StrategyParam<decimal> _longSmallMoneyManagement;
	private readonly StrategyParam<decimal> _shortSmallMoneyManagement;
	private readonly StrategyParam<decimal> _longMoneyManagement;
	private readonly StrategyParam<decimal> _shortMoneyManagement;
	private readonly StrategyParam<MarginModeOption> _longMarginMode;
	private readonly StrategyParam<MarginModeOption> _shortMarginMode;
	private readonly StrategyParam<decimal> _longStopLossPoints;
	private readonly StrategyParam<decimal> _longTakeProfitPoints;
	private readonly StrategyParam<decimal> _shortStopLossPoints;
	private readonly StrategyParam<decimal> _shortTakeProfitPoints;
	private readonly StrategyParam<decimal> _longDeviationSteps;
	private readonly StrategyParam<decimal> _shortDeviationSteps;

	private readonly AdaptiveRenkoProcessor _longProcessor = new();
	private readonly AdaptiveRenkoProcessor _shortProcessor = new();

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longEntryVolume;
	private decimal? _shortEntryVolume;
	private readonly Queue<decimal> _longPnls = new();
	private readonly Queue<decimal> _shortPnls = new();
	private decimal _priceStep;

	public ExpAdaptiveRenkoMmrecDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Timeframe used to derive long-side signals", "Long Side");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Timeframe used to derive short-side signals", "Short Side");

		_longVolatilityMode = Param(nameof(LongVolatilityMode), AdaptiveRenkoVolatilityMode.AverageTrueRange)
			.SetDisplay("Long Volatility Source", "Volatility measure controlling long Renko brick size", "Long Side");

		_shortVolatilityMode = Param(nameof(ShortVolatilityMode), AdaptiveRenkoVolatilityMode.AverageTrueRange)
			.SetDisplay("Short Volatility Source", "Volatility measure controlling short Renko brick size", "Short Side");

		_longVolatilityPeriod = Param(nameof(LongVolatilityPeriod), 10)
			.SetRange(1, 500)
			.SetDisplay("Long Volatility Period", "Lookback period for the volatility calculation", "Long Side")
			.SetCanOptimize(true);

		_shortVolatilityPeriod = Param(nameof(ShortVolatilityPeriod), 10)
			.SetRange(1, 500)
			.SetDisplay("Short Volatility Period", "Lookback period for the volatility calculation", "Short Side")
			.SetCanOptimize(true);

		_longSensitivity = Param(nameof(LongSensitivity), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Long Sensitivity", "Multiplier applied to volatility for long bricks", "Long Side")
			.SetCanOptimize(true);

		_shortSensitivity = Param(nameof(ShortSensitivity), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Short Sensitivity", "Multiplier applied to volatility for short bricks", "Short Side")
			.SetCanOptimize(true);

		_longPriceMode = Param(nameof(LongPriceMode), AdaptiveRenkoPriceMode.Close)
			.SetDisplay("Long Price Mode", "Price source used when building long bricks", "Long Side");

		_shortPriceMode = Param(nameof(ShortPriceMode), AdaptiveRenkoPriceMode.Close)
			.SetDisplay("Short Price Mode", "Price source used when building short bricks", "Short Side");

		_longMinimumBrickPoints = Param(nameof(LongMinimumBrickPoints), 2m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Long Minimum Brick", "Minimal brick height in points for long bricks", "Long Side");

		_shortMinimumBrickPoints = Param(nameof(ShortMinimumBrickPoints), 2m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Short Minimum Brick", "Minimal brick height in points for short bricks", "Short Side");

		_longSignalBarOffset = Param(nameof(LongSignalBarOffset), 1)
			.SetRange(0, 10)
			.SetDisplay("Long Signal Offset", "Number of closed bars to delay long signals", "Long Side");

		_shortSignalBarOffset = Param(nameof(ShortSignalBarOffset), 1)
			.SetRange(0, 10)
			.SetDisplay("Short Signal Offset", "Number of closed bars to delay short signals", "Short Side");

		_longEntriesEnabled = Param(nameof(LongEntriesEnabled), true)
			.SetDisplay("Enable Long Entries", "Allow long-side market entries", "Long Side");

		_longExitsEnabled = Param(nameof(LongExitsEnabled), true)
			.SetDisplay("Enable Long Exits", "Allow long-side exits triggered by Renko", "Long Side");

		_shortEntriesEnabled = Param(nameof(ShortEntriesEnabled), true)
			.SetDisplay("Enable Short Entries", "Allow short-side market entries", "Short Side");

		_shortExitsEnabled = Param(nameof(ShortExitsEnabled), true)
			.SetDisplay("Enable Short Exits", "Allow short-side exits triggered by Renko", "Short Side");

		_longTotalTrigger = Param(nameof(LongTotalTrigger), 5)
			.SetNotNegative()
			.SetDisplay("Long Total Trigger", "Number of recent long trades inspected by the MMRec module", "MMRec");

		_shortTotalTrigger = Param(nameof(ShortTotalTrigger), 5)
			.SetNotNegative()
			.SetDisplay("Short Total Trigger", "Number of recent short trades inspected by the MMRec module", "MMRec");

		_longLossTrigger = Param(nameof(LongLossTrigger), 3)
			.SetNotNegative()
			.SetDisplay("Long Loss Trigger", "Losing trades required to switch long volume to the reduced value", "MMRec");

		_shortLossTrigger = Param(nameof(ShortLossTrigger), 3)
			.SetNotNegative()
			.SetDisplay("Short Loss Trigger", "Losing trades required to switch short volume to the reduced value", "MMRec");

		_longSmallMoneyManagement = Param(nameof(LongSmallMoneyManagement), 0.01m)
			.SetNotNegative()
			.SetDisplay("Long Reduced MM", "Money-management value used after long losing streaks", "MMRec");

		_shortSmallMoneyManagement = Param(nameof(ShortSmallMoneyManagement), 0.01m)
			.SetNotNegative()
			.SetDisplay("Short Reduced MM", "Money-management value used after short losing streaks", "MMRec");

		_longMoneyManagement = Param(nameof(LongMoneyManagement), 0.1m)
			.SetNotNegative()
			.SetDisplay("Long Base MM", "Default money-management value for long entries", "MMRec");

		_shortMoneyManagement = Param(nameof(ShortMoneyManagement), 0.1m)
			.SetNotNegative()
			.SetDisplay("Short Base MM", "Default money-management value for short entries", "MMRec");

		_longMarginMode = Param(nameof(LongMarginMode), MarginModeOption.Lot)
			.SetDisplay("Long Margin Mode", "Interpretation of the long money-management value", "MMRec");

		_shortMarginMode = Param(nameof(ShortMarginMode), MarginModeOption.Lot)
			.SetDisplay("Short Margin Mode", "Interpretation of the short money-management value", "MMRec");

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Long Stop Loss", "Protective stop distance in points for long trades", "Risk");

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 2000m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Long Take Profit", "Profit target distance in points for long trades", "Risk");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Short Stop Loss", "Protective stop distance in points for short trades", "Risk");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 2000m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Short Take Profit", "Profit target distance in points for short trades", "Risk");

		_longDeviationSteps = Param(nameof(LongDeviationSteps), 10m)
			.SetNotNegative()
			.SetDisplay("Long Deviation", "Expected slippage for informational purposes (price steps)", "Trading");

		_shortDeviationSteps = Param(nameof(ShortDeviationSteps), 10m)
			.SetNotNegative()
			.SetDisplay("Short Deviation", "Expected slippage for informational purposes (price steps)", "Trading");
	}

	/// <summary>
	/// Candle stream used to compute long-side Renko structures.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Candle stream used to compute short-side Renko structures.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Volatility mode for the long Renko stream.
	/// </summary>
	public AdaptiveRenkoVolatilityMode LongVolatilityMode
	{
		get => _longVolatilityMode.Value;
		set => _longVolatilityMode.Value = value;
	}

	/// <summary>
	/// Volatility mode for the short Renko stream.
	/// </summary>
	public AdaptiveRenkoVolatilityMode ShortVolatilityMode
	{
		get => _shortVolatilityMode.Value;
		set => _shortVolatilityMode.Value = value;
	}

	/// <summary>
	/// Lookback period for the long-side volatility indicator.
	/// </summary>
	public int LongVolatilityPeriod
	{
		get => _longVolatilityPeriod.Value;
		set => _longVolatilityPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for the short-side volatility indicator.
	/// </summary>
	public int ShortVolatilityPeriod
	{
		get => _shortVolatilityPeriod.Value;
		set => _shortVolatilityPeriod.Value = value;
	}

	/// <summary>
	/// Volatility multiplier that scales long-side bricks.
	/// </summary>
	public decimal LongSensitivity
	{
		get => _longSensitivity.Value;
		set => _longSensitivity.Value = value;
	}

	/// <summary>
	/// Volatility multiplier that scales short-side bricks.
	/// </summary>
	public decimal ShortSensitivity
	{
		get => _shortSensitivity.Value;
		set => _shortSensitivity.Value = value;
	}

	/// <summary>
	/// Price source used while building long bricks.
	/// </summary>
	public AdaptiveRenkoPriceMode LongPriceMode
	{
		get => _longPriceMode.Value;
		set => _longPriceMode.Value = value;
	}

	/// <summary>
	/// Price source used while building short bricks.
	/// </summary>
	public AdaptiveRenkoPriceMode ShortPriceMode
	{
		get => _shortPriceMode.Value;
		set => _shortPriceMode.Value = value;
	}

	/// <summary>
	/// Minimal brick height for the long Renko stream (expressed in points).
	/// </summary>
	public decimal LongMinimumBrickPoints
	{
		get => _longMinimumBrickPoints.Value;
		set => _longMinimumBrickPoints.Value = value;
	}

	/// <summary>
	/// Minimal brick height for the short Renko stream (expressed in points).
	/// </summary>
	public decimal ShortMinimumBrickPoints
	{
		get => _shortMinimumBrickPoints.Value;
		set => _shortMinimumBrickPoints.Value = value;
	}

	/// <summary>
	/// Number of closed bars to wait before using a long-side signal.
	/// </summary>
	public int LongSignalBarOffset
	{
		get => _longSignalBarOffset.Value;
		set => _longSignalBarOffset.Value = value;
	}

	/// <summary>
	/// Number of closed bars to wait before using a short-side signal.
	/// </summary>
	public int ShortSignalBarOffset
	{
		get => _shortSignalBarOffset.Value;
		set => _shortSignalBarOffset.Value = value;
	}

	/// <summary>
	/// Enables long-side entries.
	/// </summary>
	public bool LongEntriesEnabled
	{
		get => _longEntriesEnabled.Value;
		set => _longEntriesEnabled.Value = value;
	}

	/// <summary>
	/// Enables Renko-driven exits for long positions.
	/// </summary>
	public bool LongExitsEnabled
	{
		get => _longExitsEnabled.Value;
		set => _longExitsEnabled.Value = value;
	}

	/// <summary>
	/// Enables short-side entries.
	/// </summary>
	public bool ShortEntriesEnabled
	{
		get => _shortEntriesEnabled.Value;
		set => _shortEntriesEnabled.Value = value;
	}

	/// <summary>
	/// Enables Renko-driven exits for short positions.
	/// </summary>
	public bool ShortExitsEnabled
	{
		get => _shortExitsEnabled.Value;
		set => _shortExitsEnabled.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long positions expressed in indicator points.
	/// </summary>
	public decimal LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long positions expressed in indicator points.
	/// </summary>
	public decimal LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short positions expressed in indicator points.
	/// </summary>
	public decimal ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short positions expressed in indicator points.
	/// </summary>
	public decimal ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Number of recent long trades evaluated by the money-management recovery block.
	/// </summary>
	public int LongTotalTrigger
	{
		get => _longTotalTrigger.Value;
		set => _longTotalTrigger.Value = value;
	}

	/// <summary>
	/// Number of recent short trades evaluated by the money-management recovery block.
	/// </summary>
	public int ShortTotalTrigger
	{
		get => _shortTotalTrigger.Value;
		set => _shortTotalTrigger.Value = value;
	}

	/// <summary>
	/// Losing long trades required to switch to the reduced long-side volume.
	/// </summary>
	public int LongLossTrigger
	{
		get => _longLossTrigger.Value;
		set => _longLossTrigger.Value = value;
	}

	/// <summary>
	/// Losing short trades required to switch to the reduced short-side volume.
	/// </summary>
	public int ShortLossTrigger
	{
		get => _shortLossTrigger.Value;
		set => _shortLossTrigger.Value = value;
	}

	/// <summary>
	/// Money-management value applied after a long losing streak.
	/// </summary>
	public decimal LongSmallMoneyManagement
	{
		get => _longSmallMoneyManagement.Value;
		set => _longSmallMoneyManagement.Value = value;
	}

	/// <summary>
	/// Money-management value applied after a short losing streak.
	/// </summary>
	public decimal ShortSmallMoneyManagement
	{
		get => _shortSmallMoneyManagement.Value;
		set => _shortSmallMoneyManagement.Value = value;
	}

	/// <summary>
	/// Default long-side money-management value.
	/// </summary>
	public decimal LongMoneyManagement
	{
		get => _longMoneyManagement.Value;
		set => _longMoneyManagement.Value = value;
	}

	/// <summary>
	/// Default short-side money-management value.
	/// </summary>
	public decimal ShortMoneyManagement
	{
		get => _shortMoneyManagement.Value;
		set => _shortMoneyManagement.Value = value;
	}

	/// <summary>
	/// Interpretation of the long money-management parameter.
	/// </summary>
	public MarginModeOption LongMarginMode
	{
		get => _longMarginMode.Value;
		set => _longMarginMode.Value = value;
	}

	/// <summary>
	/// Interpretation of the short money-management parameter.
	/// </summary>
	public MarginModeOption ShortMarginMode
	{
		get => _shortMarginMode.Value;
		set => _shortMarginMode.Value = value;
	}

	/// <summary>
	/// Informational slippage setting for long trades expressed in price steps.
	/// </summary>
	public decimal LongDeviationSteps
	{
		get => _longDeviationSteps.Value;
		set => _longDeviationSteps.Value = value;
	}

	/// <summary>
	/// Informational slippage setting for short trades expressed in price steps.
	/// </summary>
	public decimal ShortDeviationSteps
	{
		get => _shortDeviationSteps.Value;
		set => _shortDeviationSteps.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, LongCandleType);

		if (ShortCandleType != LongCandleType)
			yield return (Security, ShortCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
_longProcessor.Reset();
_shortProcessor.Reset();
_longEntryPrice = null;
_shortEntryPrice = null;
_longEntryVolume = null;
_shortEntryVolume = null;
_longPnls.Clear();
_shortPnls.Clear();
		_priceStep = 1m;
}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longProcessor.Reset();
		_shortProcessor.Reset();
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longEntryVolume = null;
		_shortEntryVolume = null;
		_longPnls.Clear();
		_shortPnls.Clear();
		_priceStep = GetPriceStep();

		var longIndicator = CreateVolatilityIndicator(LongVolatilityMode, LongVolatilityPeriod);
		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription.BindEx(longIndicator, ProcessLongCandle);

		var shortIndicator = CreateVolatilityIndicator(ShortVolatilityMode, ShortVolatilityPeriod);

		if (ShortCandleType == LongCandleType)
		{
			longSubscription.BindEx(shortIndicator, ProcessShortCandle);
			longSubscription.Start();
		}
		else
		{
			longSubscription.Start();
			var shortSubscription = SubscribeCandles(ShortCandleType);
			shortSubscription.BindEx(shortIndicator, ProcessShortCandle);
			shortSubscription.Start();
		}
	}

	private void ProcessLongCandle(ICandleMessage candle, IIndicatorValue volatilityValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageLongRisk(candle);

		if (!volatilityValue.IsFinal)
			return;

		var volatility = volatilityValue.ToDecimal();
		var snapshot = _longProcessor.Process(candle, volatility, LongSensitivity, LongMinimumBrickPoints, LongPriceMode, LongSignalBarOffset, _priceStep);

		if (snapshot == null)
			return;

		var signal = _longProcessor.GetSnapshot(LongSignalBarOffset);
		if (signal == null)
			return;

		if (LongExitsEnabled && Position > 0 && signal.Value.Trend == RenkoTrend.Down)
		{
			TryCloseLong("Adaptive Renko bearish reversal", candle);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (LongEntriesEnabled && signal.Value.Trend == RenkoTrend.Up)
		{
			TryOpenLong(candle);
		}
	}

	private void ProcessShortCandle(ICandleMessage candle, IIndicatorValue volatilityValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageShortRisk(candle);

		if (!volatilityValue.IsFinal)
			return;

		var volatility = volatilityValue.ToDecimal();
		var snapshot = _shortProcessor.Process(candle, volatility, ShortSensitivity, ShortMinimumBrickPoints, ShortPriceMode, ShortSignalBarOffset, _priceStep);

		if (snapshot == null)
			return;

		var signal = _shortProcessor.GetSnapshot(ShortSignalBarOffset);
		if (signal == null)
			return;

		if (ShortExitsEnabled && Position < 0 && signal.Value.Trend == RenkoTrend.Up)
		{
			TryCloseShort("Adaptive Renko bullish reversal", candle);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ShortEntriesEnabled && signal.Value.Trend == RenkoTrend.Down)
		{
			TryOpenShort(candle);
		}
	}

	private void ManageLongRisk(ICandleMessage candle)
	{
		if (Position <= 0)
		{
			_longEntryPrice = null;
			_longEntryVolume = null;
			return;
		}

		if (_longEntryPrice == null)
			_longEntryPrice = candle.ClosePrice;

		if (_longEntryVolume == null && Position > 0)
			_longEntryVolume = Math.Abs(Position);

		if (LongStopLossPoints > 0m)
		{
			var stopDistance = LongStopLossPoints * _priceStep;
			if (stopDistance > 0m && candle.LowPrice <= _longEntryPrice.Value - stopDistance)
			{
				TryCloseLong("Long stop loss reached", candle);
				return;
			}
		}

		if (LongTakeProfitPoints > 0m)
		{
			var targetDistance = LongTakeProfitPoints * _priceStep;
			if (targetDistance > 0m && candle.HighPrice >= _longEntryPrice.Value + targetDistance)
			{
				TryCloseLong("Long take profit reached", candle);
			}
		}
	}

	private void ManageShortRisk(ICandleMessage candle)
	{
		if (Position >= 0)
		{
			_shortEntryPrice = null;
			_shortEntryVolume = null;
			return;
		}

		if (_shortEntryPrice == null)
			_shortEntryPrice = candle.ClosePrice;

		if (_shortEntryVolume == null && Position < 0)
			_shortEntryVolume = Math.Abs(Position);

		if (ShortStopLossPoints > 0m)
		{
			var stopDistance = ShortStopLossPoints * _priceStep;
			if (stopDistance > 0m && candle.HighPrice >= _shortEntryPrice.Value + stopDistance)
			{
				TryCloseShort("Short stop loss reached", candle);
				return;
			}
		}

		if (ShortTakeProfitPoints > 0m)
		{
			var targetDistance = ShortTakeProfitPoints * _priceStep;
			if (targetDistance > 0m && candle.LowPrice <= _shortEntryPrice.Value - targetDistance)
			{
				TryCloseShort("Short take profit reached", candle);
			}
		}
	}

	private void TryOpenLong(ICandleMessage candle)
	{
		if (Position > 0)
			return;

		if (Position < 0)
			TryCloseShort("Reversing to long", candle);

		// Keep only the latest results inside the MMRec window.
		TrimQueue(_longPnls, LongTotalTrigger);
		// Switch to the reduced volume after a configured number of losses.
		var mm = ShouldUseReducedVolume(_longPnls, LongTotalTrigger, LongLossTrigger) ? LongSmallMoneyManagement : LongMoneyManagement;
		// Convert the money-management value into an executable volume.
		var volume = CalculateVolume(mm, LongMarginMode, LongStopLossPoints, candle.ClosePrice);

		if (volume <= 0m)
		{
			LogWarning("Volume must be positive to open a long position.");
			return;
		}

		BuyMarket(volume);
		_longEntryPrice = candle.ClosePrice;
		_longEntryVolume = volume;
		_shortEntryPrice = null;
		_shortEntryVolume = null;

		LogInfo($"Long entry triggered at {candle.ClosePrice:F5}. MM value: {mm:F4}, slippage steps: {LongDeviationSteps:F2}.");
	}


	private void TryOpenShort(ICandleMessage candle)
	{
		if (Position < 0)
			return;

		if (Position > 0)
			TryCloseLong("Reversing to short", candle);

		// Mirror the MMRec window for short trades.
		TrimQueue(_shortPnls, ShortTotalTrigger);
		var mm = ShouldUseReducedVolume(_shortPnls, ShortTotalTrigger, ShortLossTrigger) ? ShortSmallMoneyManagement : ShortMoneyManagement;
		var volume = CalculateVolume(mm, ShortMarginMode, ShortStopLossPoints, candle.ClosePrice);

		if (volume <= 0m)
		{
			LogWarning("Volume must be positive to open a short position.");
			return;
		}

		SellMarket(volume);
		_shortEntryPrice = candle.ClosePrice;
		_shortEntryVolume = volume;
		_longEntryPrice = null;
		_longEntryVolume = null;

		LogInfo($"Short entry triggered at {candle.ClosePrice:F5}. MM value: {mm:F4}, slippage steps: {ShortDeviationSteps:F2}.");
	}


	private void TryCloseLong(string reason, ICandleMessage candle)
	{
		if (Position <= 0)
		{
			_longEntryPrice = null;
			_longEntryVolume = null;
			return;
		}

		var volume = Position > 0m ? Position : _longEntryVolume ?? 0m;
		if (volume <= 0m)
		{
			_longEntryPrice = null;
			_longEntryVolume = null;
			return;
		}

		var entryPrice = _longEntryPrice ?? candle.ClosePrice;
		SellMarket(volume);
		RegisterTradeResult(Sides.Buy, (candle.ClosePrice - entryPrice) * volume);
		_longEntryPrice = null;
		_longEntryVolume = null;

		LogInfo($"Long exit: {reason} at {candle.ClosePrice:F5}.");
	}


	private void TryCloseShort(string reason, ICandleMessage candle)
	{
		if (Position >= 0)
		{
			_shortEntryPrice = null;
			_shortEntryVolume = null;
			return;
		}

		var volume = Position < 0m ? Math.Abs(Position) : _shortEntryVolume ?? 0m;
		if (volume <= 0m)
		{
			_shortEntryPrice = null;
			_shortEntryVolume = null;
			return;
		}

		var entryPrice = _shortEntryPrice ?? candle.ClosePrice;
		BuyMarket(volume);
		RegisterTradeResult(Sides.Sell, (entryPrice - candle.ClosePrice) * volume);
		_shortEntryPrice = null;
		_shortEntryVolume = null;

		LogInfo($"Short exit: {reason} at {candle.ClosePrice:F5}.");
	}


	private static IIndicator CreateVolatilityIndicator(AdaptiveRenkoVolatilityMode mode, int period)
	{
		return mode switch
		{
			AdaptiveRenkoVolatilityMode.AverageTrueRange => new AverageTrueRange { Length = period },
			AdaptiveRenkoVolatilityMode.StandardDeviation => new StandardDeviation { Length = period },
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported volatility mode"),
		};
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
			return 1m;

		if (security.PriceStep != null && security.PriceStep.Value > 0m)
			return security.PriceStep.Value;

		if (security.MinStep != null && security.MinStep.Value > 0m)
			return security.MinStep.Value;

		return 1m;
	}


	private decimal CalculateVolume(decimal mmValue, MarginModeOption mode, decimal stopSteps, decimal price)
	{
		if (mmValue == 0m)
			return 0m;

		if (mmValue < 0m)
			return NormalizeVolume(Math.Abs(mmValue));

		var capital = Portfolio?.CurrentValue ?? 0m;

		decimal volume;

		switch (mode)
		{
			case MarginModeOption.FreeMargin:
			case MarginModeOption.Balance:
			{
				if (capital <= 0m || price <= 0m)
					return NormalizeVolume(mmValue);

				volume = capital * mmValue / price;
				break;
			}
			case MarginModeOption.LossFreeMargin:
			case MarginModeOption.LossBalance:
			{
				var distance = stopSteps > 0m ? stopSteps * _priceStep : price;
				if (capital <= 0m || distance <= 0m)
					return NormalizeVolume(mmValue);

				volume = capital * mmValue / distance;
				break;
			}
			case MarginModeOption.Lot:
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
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
			volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

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
			history.Dequeue();
	}

	private static bool ShouldUseReducedVolume(IEnumerable<decimal> history, int totalTrigger, int lossTrigger)
	{
		if (lossTrigger <= 0 || totalTrigger <= 0)
			return false;

		var losses = 0;
		var inspected = 0;

		foreach (var pnl in history)
		{
			inspected++;
			if (pnl < 0m)
				losses++;

			if (inspected >= totalTrigger)
				break;
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
			queue.Dequeue();
	}

	private enum RenkoTrend
	{
		None = 0,
		Up = 1,
		Down = -1
	}

	private readonly struct RenkoSnapshot
	{
		public RenkoSnapshot(DateTimeOffset time, RenkoTrend trend, decimal? support, decimal? resistance)
		{
			Time = time;
			Trend = trend;
			Support = support;
			Resistance = resistance;
		}

		public DateTimeOffset Time { get; }

		public RenkoTrend Trend { get; }

		public decimal? Support { get; }

		public decimal? Resistance { get; }
	}


	/// <summary>
	/// Margin interpretation modes reproduced from the MetaTrader expert.
	/// </summary>
	public enum MarginModeOption
	{
		/// <summary>
		/// Treat the money-management value as a share of free margin.
		/// </summary>
		FreeMargin = 0,

		/// <summary>
		/// Treat the money-management value as a share of balance.
		/// </summary>
		Balance = 1,

		/// <summary>
		/// Risk a share of capital using the configured stop-loss distance relative to free margin.
		/// </summary>
		LossFreeMargin = 2,

		/// <summary>
		/// Risk a share of balance using the configured stop-loss distance.
		/// </summary>
		LossBalance = 3,

		/// <summary>
		/// Interpret the value directly as volume in lots.
		/// </summary>
		Lot = 4
	}

	private sealed class AdaptiveRenkoProcessor
	{
		private readonly List<RenkoSnapshot> _history = new();
		private bool _initialized;
		private decimal _up;
		private decimal _down;
		private decimal _brick;
		private RenkoTrend _trend;

		public RenkoSnapshot? Process(ICandleMessage candle, decimal volatility, decimal sensitivity, decimal minimumBrickPoints, AdaptiveRenkoPriceMode priceMode, int signalOffset, decimal step)
		{
			var (high, low) = priceMode == AdaptiveRenkoPriceMode.Close
				? (candle.ClosePrice, candle.ClosePrice)
				: (candle.HighPrice, candle.LowPrice);

			var minBrick = Math.Max(minimumBrickPoints * step, 0m);

			if (!_initialized)
			{
				var range = Math.Max(high - low, 0m);
				var initialBrick = Math.Max(sensitivity * range, minBrick);

				_up = high;
				_down = low;
				_brick = initialBrick > 0m ? initialBrick : minBrick;
				_trend = RenkoTrend.None;
				_initialized = true;

				var initialSnapshot = new RenkoSnapshot(GetCandleTime(candle), RenkoTrend.None, null, null);
				AppendSnapshot(initialSnapshot, signalOffset);
				return initialSnapshot;
			}

			var up = _up;
			var down = _down;
			var brick = _brick > 0m ? _brick : minBrick;
			var trend = _trend;

			var adjustedBrick = Math.Max(sensitivity * Math.Abs(volatility), minBrick);
			if (adjustedBrick <= 0m)
				adjustedBrick = minBrick;

			if (brick <= 0m)
				brick = adjustedBrick > 0m ? adjustedBrick : minBrick;

			if (high > up + brick)
			{
				if (brick > 0m)
				{
					var diff = high - up;
					var bricks = Math.Floor(diff / brick);
					if (bricks < 1m)
						bricks = 1m;
					up += bricks * brick;
				}
				else
				{
					up = high;
				}

				brick = adjustedBrick;
				down = up - brick;
			}

			if (low < down - brick)
			{
				if (brick > 0m)
				{
					var diff = down - low;
					var bricks = Math.Floor(diff / brick);
					if (bricks < 1m)
						bricks = 1m;
					down -= bricks * brick;
				}
				else
				{
					down = low;
				}

				brick = adjustedBrick;
				up = down + brick;
			}

			if (_up < up)
				trend = RenkoTrend.Up;

			if (_down > down)
				trend = RenkoTrend.Down;

			_up = up;
			_down = down;
			_brick = brick;
			_trend = trend;

			var support = trend == RenkoTrend.Up ? down - brick : (decimal?)null;
			var resistance = trend == RenkoTrend.Down ? up + brick : (decimal?)null;

			var snapshot = new RenkoSnapshot(GetCandleTime(candle), trend, support, resistance);
			AppendSnapshot(snapshot, signalOffset);
			return snapshot;
		}

		public RenkoSnapshot? GetSnapshot(int shift)
		{
			if (shift < 0)
				shift = 0;

			var index = _history.Count - 1 - shift;
			if (index < 0)
				return null;

			return _history[index];
		}

		public void Reset()
		{
			_history.Clear();
			_initialized = false;
			_up = 0m;
			_down = 0m;
			_brick = 0m;
			_trend = RenkoTrend.None;
		}

		private void AppendSnapshot(RenkoSnapshot snapshot, int signalOffset)
		{
			_history.Add(snapshot);
			var maxHistory = Math.Max(signalOffset + 3, 8);
			var overflow = _history.Count - maxHistory;
			if (overflow > 0)
				_history.RemoveRange(0, overflow);
		}

		private static DateTimeOffset GetCandleTime(ICandleMessage candle)
		{
			if (candle.CloseTime != default)
				return candle.CloseTime;

			return candle.Time;
		}
	}

	public enum AdaptiveRenkoVolatilityMode
	{
		AverageTrueRange,
		StandardDeviation
	}

	public enum AdaptiveRenkoPriceMode
	{
		HighLow,
		Close
	}
}
