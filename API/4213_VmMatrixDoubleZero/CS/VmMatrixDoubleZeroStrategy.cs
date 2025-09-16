using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor vMATRIXDoubleZero.
/// Combines rounded price breakouts with configurable multi-bar filters, volume checks, and ATR-based volatility gates.
/// Includes optional daily CCI confirmation, range compression detection, and adaptive take-profit calculation borrowed from the original code.
/// </summary>
public class VmMatrixDoubleZeroStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<bool> _closeOnBiasFlip;
	private readonly StrategyParam<bool> _enableLongs;
	private readonly StrategyParam<int> _longStopLossPips;
	private readonly StrategyParam<int> _longTakeProfitPips;
	private readonly StrategyParam<bool> _enableShorts;
	private readonly StrategyParam<int> _shortStopLossPips;
	private readonly StrategyParam<int> _shortTakeProfitPips;
	private readonly StrategyParam<bool> _useBiasFilter;
	private readonly StrategyParam<int> _longK1;
	private readonly StrategyParam<int> _longK2;
	private readonly StrategyParam<int> _longK3;
	private readonly StrategyParam<int> _longK4;
	private readonly StrategyParam<int> _longK5;
	private readonly StrategyParam<int> _longK6;
	private readonly StrategyParam<decimal> _longQc;
	private readonly StrategyParam<decimal> _longQg;
	private readonly StrategyParam<int> _shortK1;
	private readonly StrategyParam<int> _shortK2;
	private readonly StrategyParam<int> _shortK3;
	private readonly StrategyParam<int> _shortK4;
	private readonly StrategyParam<int> _shortK5;
	private readonly StrategyParam<int> _shortK6;
	private readonly StrategyParam<decimal> _shortQc;
	private readonly StrategyParam<decimal> _shortQg;
	private readonly StrategyParam<bool> _useRangeFilter;
	private readonly StrategyParam<int> _rangeBars;
	private readonly StrategyParam<int> _rangeThresholdPips;
	private readonly StrategyParam<bool> _useVolumeFilter;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<bool> _useVsaFilter;
	private readonly StrategyParam<int> _atrShift;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<bool> _useDailyCciFilter;
	private readonly StrategyParam<int> _dailyCciPeriod;
	private readonly StrategyParam<bool> _useDynamicTakeProfit;
	private readonly StrategyParam<int> _weightSn1;
	private readonly StrategyParam<int> _weightSn2;
	private readonly StrategyParam<int> _weightSn3;
	private readonly StrategyParam<int> _weightSn4;
	private readonly StrategyParam<int> _swingPivot;
	private readonly StrategyParam<bool> _useSecondaryFilter;
	private readonly StrategyParam<decimal> _xb2;
	private readonly StrategyParam<decimal> _yb2;
	private readonly StrategyParam<decimal> _xs2;
	private readonly StrategyParam<decimal> _ys2;
	private readonly StrategyParam<int> _secondaryPivot;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _baseAtr = null!;
	private AverageTrueRange _hourAtrFast = null!;
	private AverageTrueRange _hourAtrSlow = null!;
	private CommodityChannelIndex _dailyCci = null!;

	private readonly List<ICandleMessage> _history = new();
	private readonly List<decimal> _baseAtrHistory = new();
	private readonly List<decimal> _hourAtrFastHistory = new();
	private readonly List<decimal> _hourAtrSlowHistory = new();

	private decimal? _dailyCciValue;
	private decimal _pipSize;

	private decimal? _longEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakeProfitPrice;

	private decimal? _shortEntryPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfitPrice;

	/// <summary>
	/// Initializes strategy parameters with defaults taken from the original expert advisor.
	/// </summary>
	public VmMatrixDoubleZeroStrategy()
	{
		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Trading window start hour (terminal time)", "General");
		_endHour = Param(nameof(EndHour), 23)
		.SetDisplay("End Hour", "Trading window end hour (terminal time)", "General");
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetDisplay("Order Volume", "Base order size in lots or contracts", "General")
		.SetCanOptimize(true);
		_useTrailingStop = Param(nameof(UseTrailingStop), false)
		.SetDisplay("Use Trailing Stop", "Enable original trailing-stop behavior", "General");
		_closeOnBiasFlip = Param(nameof(CloseOnBiasFlip), false)
		.SetDisplay("Close On Bias Flip", "Close opposite exposure when the bias reverses", "General");
		_enableLongs = Param(nameof(EnableLongs), true)
		.SetDisplay("Enable Long Entries", "Allow algorithm to open buy trades", "Long");
		_longStopLossPips = Param(nameof(LongStopLossPips), 80)
		.SetDisplay("Long Stop (pips)", "Stop-loss distance for buy trades", "Long")
		.SetCanOptimize(true);
		_longTakeProfitPips = Param(nameof(LongTakeProfitPips), 50)
		.SetDisplay("Long Take Profit (pips)", "Take-profit distance for buy trades", "Long")
		.SetCanOptimize(true);
		_enableShorts = Param(nameof(EnableShorts), true)
		.SetDisplay("Enable Short Entries", "Allow algorithm to open sell trades", "Short");
		_shortStopLossPips = Param(nameof(ShortStopLossPips), 80)
		.SetDisplay("Short Stop (pips)", "Stop-loss distance for sell trades", "Short")
		.SetCanOptimize(true);
		_shortTakeProfitPips = Param(nameof(ShortTakeProfitPips), 50)
		.SetDisplay("Short Take Profit (pips)", "Take-profit distance for sell trades", "Short")
		.SetCanOptimize(true);
		_useBiasFilter = Param(nameof(UseBiasFilter), true)
		.SetDisplay("Use Matrix Filter", "Enable multi-bar bias comparison filter", "Filters");
		_longK1 = Param(nameof(LongK1), 1)
		.SetDisplay("Long k1", "Shift for long filter component 1", "Filters");
		_longK2 = Param(nameof(LongK2), 2)
		.SetDisplay("Long k2", "Shift for long filter component 2", "Filters");
		_longK3 = Param(nameof(LongK3), 3)
		.SetDisplay("Long k3", "Shift for long filter component 3", "Filters");
		_longK4 = Param(nameof(LongK4), 4)
		.SetDisplay("Long k4", "Shift for long filter component 4", "Filters");
		_longK5 = Param(nameof(LongK5), 3)
		.SetDisplay("Long k5", "Shift for long filter component 5", "Filters");
		_longK6 = Param(nameof(LongK6), 4)
		.SetDisplay("Long k6", "Shift for long filter component 6", "Filters");
		_longQc = Param(nameof(LongQc), 4m)
		.SetDisplay("Long qc", "Multiplier applied to long filter 3", "Filters");
		_longQg = Param(nameof(LongQg), 4m)
		.SetDisplay("Long qg", "Multiplier applied to long filter 3 (alt)", "Filters");
		_shortK1 = Param(nameof(ShortK1), 1)
		.SetDisplay("Short k1", "Shift for short filter component 1", "Filters");
		_shortK2 = Param(nameof(ShortK2), 2)
		.SetDisplay("Short k2", "Shift for short filter component 2", "Filters");
		_shortK3 = Param(nameof(ShortK3), 3)
		.SetDisplay("Short k3", "Shift for short filter component 3", "Filters");
		_shortK4 = Param(nameof(ShortK4), 4)
		.SetDisplay("Short k4", "Shift for short filter component 4", "Filters");
		_shortK5 = Param(nameof(ShortK5), 3)
		.SetDisplay("Short k5", "Shift for short filter component 5", "Filters");
		_shortK6 = Param(nameof(ShortK6), 4)
		.SetDisplay("Short k6", "Shift for short filter component 6", "Filters");
		_shortQc = Param(nameof(ShortQc), 4m)
		.SetDisplay("Short qc", "Multiplier applied to short filter 3", "Filters");
		_shortQg = Param(nameof(ShortQg), 4m)
		.SetDisplay("Short qg", "Multiplier applied to short filter 3 (alt)", "Filters");
		_useRangeFilter = Param(nameof(UseRangeFilter), false)
		.SetDisplay("Use Range Filter", "Block signals when recent range exceeds the limit", "Filters");
		_rangeBars = Param(nameof(RangeBars), 15)
		.SetDisplay("Range Bars", "Number of candles evaluated by the range filter", "Filters");
		_rangeThresholdPips = Param(nameof(RangeThresholdPips), 70)
		.SetDisplay("Range Threshold (pips)", "Maximum allowed high-low span", "Filters");
		_useVolumeFilter = Param(nameof(UseVolumeFilter), false)
		.SetDisplay("Use Volume Filter", "Require previous candle volume above threshold", "Filters");
		_minimumVolume = Param(nameof(MinimumVolume), 1000m)
		.SetDisplay("Minimum Volume", "Volume threshold for the optional volume filter", "Filters");
		_useVsaFilter = Param(nameof(UseVsaFilter), false)
		.SetDisplay("Use ATR Acceleration", "Compare recent ATR values for a volatility surge", "Filters");
		_atrShift = Param(nameof(AtrShift), 2)
		.SetDisplay("ATR Shift", "Bars back used for ATR comparison", "Filters");
		_atrPeriod = Param(nameof(AtrPeriod), 2)
		.SetDisplay("ATR Period", "Length for the ATR comparison filter", "Filters");
		_useDailyCciFilter = Param(nameof(UseDailyCciFilter), false)
		.SetDisplay("Use Daily CCI", "Confirm bias using daily CCI sign", "Filters");
		_dailyCciPeriod = Param(nameof(DailyCciPeriod), 15)
		.SetDisplay("Daily CCI Period", "Length of the daily CCI confirmation", "Filters");
		_useDynamicTakeProfit = Param(nameof(UseDynamicTakeProfit), false)
		.SetDisplay("Use Dynamic TP", "Blend ATR and swing metrics into take-profit", "Take Profit");
		_weightSn1 = Param(nameof(WeightSn1), 100)
		.SetDisplay("Weight sn1", "Weight applied to hourly ATR delta", "Take Profit");
		_weightSn2 = Param(nameof(WeightSn2), 100)
		.SetDisplay("Weight sn2", "Weight applied to hourly ATR level", "Take Profit");
		_weightSn3 = Param(nameof(WeightSn3), 100)
		.SetDisplay("Weight sn3", "Weight applied to hourly ATR(25)", "Take Profit");
		_weightSn4 = Param(nameof(WeightSn4), 100)
		.SetDisplay("Weight sn4", "Weight applied to swing-based term", "Take Profit");
		_swingPivot = Param(nameof(SwingPivot), 10)
		.SetDisplay("Swing Pivot", "Number of bars separating swing references", "Take Profit");
		_useSecondaryFilter = Param(nameof(UseSecondaryFilter), false)
		.SetDisplay("Use Secondary Filter", "Enable weighted high/low combination", "Filters");
		_xb2 = Param(nameof(Xb2), 100m)
		.SetDisplay("XB2", "Weight selector for secondary filter component 1", "Filters");
		_yb2 = Param(nameof(Yb2), 100m)
		.SetDisplay("YB2", "Weight selector for secondary filter component 2", "Filters");
		_xs2 = Param(nameof(Xs2), 100m)
		.SetDisplay("XS2", "Weight selector for secondary filter component 3", "Filters");
		_ys2 = Param(nameof(Ys2), 100m)
		.SetDisplay("YS2", "Weight selector for secondary filter component 4", "Filters");
		_secondaryPivot = Param(nameof(SecondaryPivot), 5)
		.SetDisplay("Secondary Pivot", "Lookback for the weighted swing filter", "Filters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for signal generation", "General");
	}

	/// <summary>
	/// Trading window start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading window end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Base order size.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Enables trailing-stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Closes opposite exposure whenever the bias flips.
	/// </summary>
	public bool CloseOnBiasFlip
	{
		get => _closeOnBiasFlip.Value;
		set => _closeOnBiasFlip.Value = value;
	}

	/// <summary>
	/// Allows long signals.
	/// </summary>
	public bool EnableLongs
	{
		get => _enableLongs.Value;
		set => _enableLongs.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades expressed in pips.
	/// </summary>
	public int LongStopLossPips
	{
		get => _longStopLossPips.Value;
		set => _longStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long trades expressed in pips.
	/// </summary>
	public int LongTakeProfitPips
	{
		get => _longTakeProfitPips.Value;
		set => _longTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Allows short signals.
	/// </summary>
	public bool EnableShorts
	{
		get => _enableShorts.Value;
		set => _enableShorts.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades expressed in pips.
	/// </summary>
	public int ShortStopLossPips
	{
		get => _shortStopLossPips.Value;
		set => _shortStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades expressed in pips.
	/// </summary>
	public int ShortTakeProfitPips
	{
		get => _shortTakeProfitPips.Value;
		set => _shortTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables the multi-bar bias filter taken from the EA.
	/// </summary>
	public bool UseBiasFilter
	{
		get => _useBiasFilter.Value;
		set => _useBiasFilter.Value = value;
	}

	/// <summary>
	/// Primary candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Range filter lookback.
	/// </summary>
	public int RangeBars
	{
		get => _rangeBars.Value;
		set => _rangeBars.Value = value;
	}

	/// <summary>
	/// Range filter threshold in pips.
	/// </summary>
	public int RangeThresholdPips
	{
		get => _rangeThresholdPips.Value;
		set => _rangeThresholdPips.Value = value;
	}

	/// <summary>
	/// Minimum volume threshold.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minimumVolume.Value;
		set => _minimumVolume.Value = value;
	}

	/// <summary>
	/// Enables the ATR acceleration filter.
	/// </summary>
	public bool UseVsaFilter
	{
		get => _useVsaFilter.Value;
		set => _useVsaFilter.Value = value;
	}

	/// <summary>
	/// Enables the volume filter.
	/// </summary>
	public bool UseVolumeFilter
	{
		get => _useVolumeFilter.Value;
		set => _useVolumeFilter.Value = value;
	}

	/// <summary>
	/// Enables the range filter.
	/// </summary>
	public bool UseRangeFilter
	{
		get => _useRangeFilter.Value;
		set => _useRangeFilter.Value = value;
	}

	/// <summary>
	/// Enables the secondary weighted swing filter.
	/// </summary>
	public bool UseSecondaryFilter
	{
		get => _useSecondaryFilter.Value;
		set => _useSecondaryFilter.Value = value;
	}

	/// <summary>
	/// Enables the dynamic take-profit modifier.
	/// </summary>
	public bool UseDynamicTakeProfit
	{
		get => _useDynamicTakeProfit.Value;
		set => _useDynamicTakeProfit.Value = value;
	}

	/// <summary>
	/// Enables the daily CCI confirmation.
	/// </summary>
	public bool UseDailyCciFilter
	{
		get => _useDailyCciFilter.Value;
		set => _useDailyCciFilter.Value = value;
	}

	/// <summary>
	/// Hourly ATR comparison shift.
	/// </summary>
	public int AtrShift
	{
		get => _atrShift.Value;
		set => _atrShift.Value = value;
	}

	/// <summary>
	/// ATR period used by the volatility filter.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Daily CCI period.
	/// </summary>
	public int DailyCciPeriod
	{
		get => _dailyCciPeriod.Value;
		set => _dailyCciPeriod.Value = value;
	}

	/// <summary>
	/// Secondary filter pivot.
	/// </summary>
	public int SecondaryPivot
	{
		get => _secondaryPivot.Value;
		set => _secondaryPivot.Value = value;
	}

	/// <summary>
	/// Swing pivot used by the dynamic take-profit component.
	/// </summary>
	public int SwingPivot
	{
		get => _swingPivot.Value;
		set => _swingPivot.Value = value;
	}

	/// <summary>
	/// Weight applied to hourly ATR delta.
	/// </summary>
	public int WeightSn1
	{
		get => _weightSn1.Value;
		set => _weightSn1.Value = value;
	}

	/// <summary>
	/// Weight applied to hourly ATR level.
	/// </summary>
	public int WeightSn2
	{
		get => _weightSn2.Value;
		set => _weightSn2.Value = value;
	}

	/// <summary>
	/// Weight applied to slow hourly ATR.
	/// </summary>
	public int WeightSn3
	{
		get => _weightSn3.Value;
		set => _weightSn3.Value = value;
	}

	/// <summary>
	/// Weight applied to swing-based component.
	/// </summary>
	public int WeightSn4
	{
		get => _weightSn4.Value;
		set => _weightSn4.Value = value;
	}

	/// <summary>
	/// Weight selector for first secondary component.
	/// </summary>
	public decimal Xb2
	{
		get => _xb2.Value;
		set => _xb2.Value = value;
	}

	/// <summary>
	/// Weight selector for second secondary component.
	/// </summary>
	public decimal Yb2
	{
		get => _yb2.Value;
		set => _yb2.Value = value;
	}

	/// <summary>
	/// Weight selector for third secondary component.
	/// </summary>
	public decimal Xs2
	{
		get => _xs2.Value;
		set => _xs2.Value = value;
	}

	/// <summary>
	/// Weight selector for fourth secondary component.
	/// </summary>
	public decimal Ys2
	{
		get => _ys2.Value;
		set => _ys2.Value = value;
	}

	/// <summary>
	/// Shift for long filter element 1.
	/// </summary>
	public int LongK1
	{
		get => _longK1.Value;
		set => _longK1.Value = value;
	}

	/// <summary>
	/// Shift for long filter element 2.
	/// </summary>
	public int LongK2
	{
		get => _longK2.Value;
		set => _longK2.Value = value;
	}

	/// <summary>
	/// Shift for long filter element 3.
	/// </summary>
	public int LongK3
	{
		get => _longK3.Value;
		set => _longK3.Value = value;
	}

	/// <summary>
	/// Shift for long filter element 4.
	/// </summary>
	public int LongK4
	{
		get => _longK4.Value;
		set => _longK4.Value = value;
	}

	/// <summary>
	/// Shift for long filter element 5.
	/// </summary>
	public int LongK5
	{
		get => _longK5.Value;
		set => _longK5.Value = value;
	}

	/// <summary>
	/// Shift for long filter element 6.
	/// </summary>
	public int LongK6
	{
		get => _longK6.Value;
		set => _longK6.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the third long filter comparison.
	/// </summary>
	public decimal LongQc
	{
		get => _longQc.Value;
		set => _longQc.Value = value;
	}

	/// <summary>
	/// Alternative multiplier applied to the long filter.
	/// </summary>
	public decimal LongQg
	{
		get => _longQg.Value;
		set => _longQg.Value = value;
	}

	/// <summary>
	/// Shift for short filter element 1.
	/// </summary>
	public int ShortK1
	{
		get => _shortK1.Value;
		set => _shortK1.Value = value;
	}

	/// <summary>
	/// Shift for short filter element 2.
	/// </summary>
	public int ShortK2
	{
		get => _shortK2.Value;
		set => _shortK2.Value = value;
	}

	/// <summary>
	/// Shift for short filter element 3.
	/// </summary>
	public int ShortK3
	{
		get => _shortK3.Value;
		set => _shortK3.Value = value;
	}

	/// <summary>
	/// Shift for short filter element 4.
	/// </summary>
	public int ShortK4
	{
		get => _shortK4.Value;
		set => _shortK4.Value = value;
	}

	/// <summary>
	/// Shift for short filter element 5.
	/// </summary>
	public int ShortK5
	{
		get => _shortK5.Value;
		set => _shortK5.Value = value;
	}

	/// <summary>
	/// Shift for short filter element 6.
	/// </summary>
	public int ShortK6
	{
		get => _shortK6.Value;
		set => _shortK6.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the third short filter comparison.
	/// </summary>
	public decimal ShortQc
	{
		get => _shortQc.Value;
		set => _shortQc.Value = value;
	}

	/// <summary>
	/// Alternative multiplier applied to the short filter.
	/// </summary>
	public decimal ShortQg
	{
		get => _shortQg.Value;
		set => _shortQg.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_history.Clear();
		_baseAtrHistory.Clear();
		_hourAtrFastHistory.Clear();
		_hourAtrSlowHistory.Clear();
		_dailyCciValue = null;
		_pipSize = 0m;
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_baseAtr = new AverageTrueRange { Length = Math.Max(1, AtrPeriod) };
		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription.Bind(_baseAtr, ProcessBaseCandle).Start();

		_hourAtrFast = new AverageTrueRange { Length = 1 };
		_hourAtrSlow = new AverageTrueRange { Length = 25 };
		var hourSubscription = SubscribeCandles(TimeSpan.FromHours(1).TimeFrame());
		hourSubscription.Bind(_hourAtrFast, _hourAtrSlow, ProcessHourCandle).Start();

		_dailyCci = new CommodityChannelIndex { Length = Math.Max(1, DailyCciPeriod) };
		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription.Bind(_dailyCci, ProcessDailyCci).Start();

		StartProtection();
	}

	private void ProcessBaseCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_history.Add(candle);
		TrimList(_history, GetRequiredHistory());

		if (atrValue > 0m)
		{
			_baseAtrHistory.Add(atrValue);
			TrimList(_baseAtrHistory, GetRequiredAtrHistory());
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_pipSize <= 0m)
		_pipSize = CalculatePipSize();

		ApplyTrailing(candle);

		if (ApplyStops(candle))
		return;

		if (CloseOnBiasFlip && ApplyBiasExit())
		return;

		TryEnterPositions(candle);
	}

	private void ProcessHourCandle(ICandleMessage candle, decimal fastAtr, decimal slowAtr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (fastAtr > 0m)
		{
			_hourAtrFastHistory.Add(fastAtr);
			TrimList(_hourAtrFastHistory, 100);
		}

		if (slowAtr > 0m)
		{
			_hourAtrSlowHistory.Add(slowAtr);
			TrimList(_hourAtrSlowHistory, 100);
		}
	}

	private void ProcessDailyCci(ICandleMessage candle, decimal cci)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_dailyCciValue = cci;
	}

	private void TryEnterPositions(ICandleMessage candle)
	{
		var hour = candle.CloseTime.Hour;
		if (!IsWithinTradingWindow(hour))
		return;

		if (EnableLongs)
		TryEnterLong(candle);

		if (EnableShorts)
		TryEnterShort(candle);
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (!HasLongBias())
		return;

		if (!PassesCommonFilters(candle, true))
		return;

		if (Position > 0)
		return;

		if (Position < 0)
		{
			if (!CloseOnBiasFlip)
			return;

			BuyMarket(Math.Abs(Position));
			ResetShortState();
		}

		var volume = NormalizeVolume(OrderVolume);
		if (volume <= 0m)
		return;

		var entryPrice = candle.ClosePrice;
		BuyMarket(volume);

		_longEntryPrice = entryPrice;
		_longStopPrice = LongStopLossPips > 0 ? entryPrice - LongStopLossPips * _pipSize : null;

		var dynamicAddition = CalculateDynamicTakeProfitAdjustment();
		var baseDistance = LongTakeProfitPips * _pipSize + dynamicAddition;
		var multiplier = UseTrailingStop ? 10m : 1m;
		_longTakeProfitPrice = baseDistance > 0m ? entryPrice + baseDistance * multiplier : null;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (!HasShortBias())
		return;

		if (!PassesCommonFilters(candle, false))
		return;

		if (Position < 0)
		return;

		if (Position > 0)
		{
			if (!CloseOnBiasFlip)
			return;

			SellMarket(Math.Abs(Position));
			ResetLongState();
		}

		var volume = NormalizeVolume(OrderVolume);
		if (volume <= 0m)
		return;

		var entryPrice = candle.ClosePrice;
		SellMarket(volume);

		_shortEntryPrice = entryPrice;
		_shortStopPrice = ShortStopLossPips > 0 ? entryPrice + ShortStopLossPips * _pipSize : null;

		var dynamicAddition = CalculateDynamicTakeProfitAdjustment();
		var baseDistance = ShortTakeProfitPips * _pipSize + dynamicAddition;
		var multiplier = UseTrailingStop ? 10m : 1m;
		_shortTakeProfitPrice = baseDistance > 0m ? entryPrice - baseDistance * multiplier : null;
	}

	private bool PassesCommonFilters(ICandleMessage candle, bool isLong)
	{
		if (UseVolumeFilter && candle.TotalVolume <= MinimumVolume)
		return false;

		if (UseDailyCciFilter)
		{
			if (_dailyCciValue is not decimal dailyCci)
			return false;

			if (isLong && dailyCci <= 0m)
			return false;

			if (!isLong && dailyCci >= 0m)
			return false;
		}

		if (UseRangeFilter && !IsWithinRangeLimit())
		return false;

		if (UseVsaFilter && !HasAtrAcceleration())
		return false;

		if (UseSecondaryFilter)
		{
			var score = CalculateSecondaryScore();
			if (isLong && score <= 0m)
			return false;

			if (!isLong && score >= 0m)
			return false;
		}

		return true;
	}

	private bool ApplyStops(ICandleMessage candle)
	{
		var closed = false;

		if (Position > 0)
		{
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				closed = true;
			}
			else if (_longTakeProfitPrice.HasValue && candle.HighPrice >= _longTakeProfitPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				closed = true;
			}
		}
		else if (Position < 0)
		{
			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				closed = true;
			}
			else if (_shortTakeProfitPrice.HasValue && candle.LowPrice <= _shortTakeProfitPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				closed = true;
			}
		}

		return closed;
	}

	private void ApplyTrailing(ICandleMessage candle)
	{
		if (!UseTrailingStop)
		return;

		var stopDistanceLong = LongStopLossPips * _pipSize;
		var stopDistanceShort = ShortStopLossPips * _pipSize;
		var thresholdLong = stopDistanceLong * 2m;
		var thresholdShort = stopDistanceShort * 2m;

		if (Position > 0 && _longStopPrice.HasValue && stopDistanceLong > 0m)
		{
			var gain = candle.ClosePrice - _longStopPrice.Value;
			if (gain > thresholdLong)
			{
				var newStop = candle.ClosePrice - stopDistanceLong;
				if (newStop > _longStopPrice.Value)
				_longStopPrice = newStop;
			}
		}
		else if (Position < 0 && _shortStopPrice.HasValue && stopDistanceShort > 0m)
		{
			var gain = _shortStopPrice.Value - candle.ClosePrice;
			if (gain > thresholdShort)
			{
				var newStop = candle.ClosePrice + stopDistanceShort;
				if (newStop < _shortStopPrice.Value)
				_shortStopPrice = newStop;
			}
		}
	}

	private bool ApplyBiasExit()
	{
		if (Position > 0 && HasShortBias())
		{
			SellMarket(Math.Abs(Position));
			ResetLongState();
			return true;
		}

		if (Position < 0 && HasLongBias())
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return true;
		}

		return false;
	}

	private bool HasLongBias()
	{
		if (!TryGetCandle(LongK1, out var c1) || !TryGetCandle(LongK2, out var c2) ||
		!TryGetCandle(LongK3, out var c3) || !TryGetCandle(LongK4, out var c4) ||
		!TryGetCandle(LongK5, out var c5) || !TryGetCandle(LongK6, out var c6))
		return false;

		if (!TryGetClose(1, out var currentClose) || !TryGetClose(2, out var prevClose))
		return false;

		var rounded = RoundToTwoDecimals(currentClose);

		var filterPassed = !UseBiasFilter ||
		(GetMidpointDeviation(c1) > GetMidpointDeviation(c2) &&
		GetMidpointDeviation(c3) > LongQc * GetMidpointDeviation(c4) &&
		GetMidpointDeviation(c5) > LongQg * GetMidpointDeviation(c6));

		return filterPassed && prevClose < rounded && currentClose > rounded;
	}

	private bool HasShortBias()
	{
		if (!TryGetCandle(ShortK1, out var c1) || !TryGetCandle(ShortK2, out var c2) ||
		!TryGetCandle(ShortK3, out var c3) || !TryGetCandle(ShortK4, out var c4) ||
		!TryGetCandle(ShortK5, out var c5) || !TryGetCandle(ShortK6, out var c6))
		return false;

		if (!TryGetClose(1, out var currentClose) || !TryGetClose(2, out var prevClose))
		return false;

		var rounded = RoundToTwoDecimals(currentClose);

		var filterPassed = !UseBiasFilter ||
		(GetMidpointDeviation(c1) > GetMidpointDeviation(c2) &&
		GetMidpointDeviation(c3) > ShortQc * GetMidpointDeviation(c4) &&
		GetMidpointDeviation(c5) > ShortQg * GetMidpointDeviation(c6));

		return filterPassed && prevClose > rounded && currentClose < rounded;
	}

	private static decimal GetMidpointDeviation(ICandleMessage candle)
	{
		return candle.ClosePrice - (candle.HighPrice + candle.LowPrice) / 2m;
	}

	private bool IsWithinTradingWindow(int hour)
	{
		return hour >= StartHour && hour <= EndHour;
	}

	private bool IsWithinRangeLimit()
	{
		var needed = Math.Max(1, RangeBars);
		if (_history.Count <= needed)
		return false;

		var highest = decimal.MinValue;
		var lowest = decimal.MaxValue;

		for (var i = 1; i <= needed; i++)
		{
			if (!TryGetCandle(i, out var candle))
			return false;

			highest = Math.Max(highest, candle.HighPrice);
			lowest = Math.Min(lowest, candle.LowPrice);
		}

		var range = highest - lowest;
		var limit = RangeThresholdPips * _pipSize;
		return limit <= 0m || range < limit;
	}

	private bool HasAtrAcceleration()
	{
		if (_baseAtrHistory.Count < Math.Max(2, AtrShift))
		return false;

		var latest = _baseAtrHistory[^1];
		var compareIndex = _baseAtrHistory.Count - AtrShift;
		if (compareIndex < 0 || compareIndex >= _baseAtrHistory.Count)
		return false;

		var previous = _baseAtrHistory[compareIndex];
		return latest > previous;
	}

	private decimal CalculateSecondaryScore()
	{
		if (!TryGetCandle(1, out var c1) || !TryGetCandle(SecondaryPivot, out var cp) ||
		!TryGetCandle(SecondaryPivot * 2, out var cp2))
		return 0m;

		var a1 = c1.HighPrice - cp.HighPrice;
		var a2 = cp.HighPrice - cp2.HighPrice;
		var a3 = c1.LowPrice - cp.LowPrice;
		var a4 = cp.LowPrice - cp2.LowPrice;

		var w1 = Xb2 - 50m;
		var w2 = Xs2 - 50m;
		var w3 = Yb2 - 50m;
		var w4 = Ys2 - 50m;

		return w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4;
	}

	private decimal CalculateDynamicTakeProfitAdjustment()
	{
		if (!UseDynamicTakeProfit)
		return 0m;

		if (_hourAtrFastHistory.Count < 2)
		return 0m;

		if (!TryGetCandle(SwingPivot, out var swing1) || !TryGetCandle(SwingPivot * 2, out var swing2))
		return 0m;

		var atrDelta = _hourAtrFastHistory[^1] - _hourAtrFastHistory[^2];
		var atrLevel = _hourAtrFastHistory[^1];
		var atrSlow = _hourAtrSlowHistory.Count > 0 ? _hourAtrSlowHistory[^1] : 0m;
		var swingTerm = swing1.HighPrice - swing2.HighPrice;

		var w1 = WeightSn1 - 50m;
		var w2 = WeightSn2 - 50m;
		var w3 = WeightSn3 - 50m;
		var w4 = WeightSn4 - 50m;

		var result = w1 * atrDelta + w2 * atrLevel + w3 * atrSlow + w4 * swingTerm;
		return result / 100m;
	}

	private bool TryGetCandle(int shift, out ICandleMessage candle)
	{
		candle = null!;
		if (shift <= 0)
		return false;

		var index = _history.Count - shift;
		if (index < 0 || index >= _history.Count)
		return false;

		candle = _history[index];
		return true;
	}

	private bool TryGetClose(int shift, out decimal close)
	{
		close = 0m;
		if (!TryGetCandle(shift, out var candle))
		return false;

		close = candle.ClosePrice;
		return true;
	}

	private static decimal RoundToTwoDecimals(decimal value)
	{
		return Math.Round(value, 2, MidpointRounding.AwayFromZero);
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 0.0001m;

		var current = step;
		var digits = 0;

		while (current < 1m && digits < 10)
		{
			current *= 10m;
			digits++;
		}

		return digits == 3 || digits == 5 ? step * 10m : step;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		return volume > 0m ? volume : 0m;
	}

	private int GetRequiredHistory()
	{
		var values = new[]
		{
			LongK1, LongK2, LongK3, LongK4, LongK5, LongK6,
			ShortK1, ShortK2, ShortK3, ShortK4, ShortK5, ShortK6,
			RangeBars + 2,
			SwingPivot * 2 + 2,
			SecondaryPivot * 2 + 2
		};

		var max = 0;
		foreach (var value in values)
		max = Math.Max(max, value);

		return Math.Max(50, max + 5);
	}

	private int GetRequiredAtrHistory()
	{
		return Math.Max(10, AtrShift + 5);
	}

	private static void TrimList<T>(List<T> list, int max)
	{
		if (list.Count <= max)
		return;

		var remove = list.Count - max;
		list.RemoveRange(0, remove);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position <= 0)
		ResetLongState();

		if (Position >= 0)
		ResetShortState();
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}
}
