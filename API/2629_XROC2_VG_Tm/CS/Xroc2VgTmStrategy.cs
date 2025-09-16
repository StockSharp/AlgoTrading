using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XROC2 VG with time filter strategy converted from MetaTrader 5.
/// </summary>
public class Xroc2VgTmStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rocPeriod1;
	private readonly StrategyParam<int> _rocPeriod2;
	private readonly StrategyParam<int> _smoothLength1;
	private readonly StrategyParam<int> _smoothLength2;
	private readonly StrategyParam<SmoothingMethod> _smoothMethod1;
	private readonly StrategyParam<SmoothingMethod> _smoothMethod2;
	private readonly StrategyParam<RocCalculationType> _rocType;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private readonly List<decimal> _closeHistory = new();
	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();

	private IIndicator _smoothFast;
	private IIndicator _smoothSlow;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Rate-of-change calculation mode.
	/// </summary>
	public enum RocCalculationType
	{
		/// <summary>Momentum (difference between closes).</summary>
		Momentum,

		/// <summary>Rate of change in percent.</summary>
		RateOfChange,

		/// <summary>Relative rate of change (fraction).</summary>
		Percent,

		/// <summary>Price ratio.</summary>
		Ratio,

		/// <summary>Price ratio scaled by 100.</summary>
		RatioPercent
	}

	/// <summary>
	/// Smoothing method used for ROC lines.
	/// </summary>
	public enum SmoothingMethod
	{
		/// <summary>Simple moving average.</summary>
		Simple,

		/// <summary>Exponential moving average.</summary>
		Exponential,

		/// <summary>Smoothed moving average.</summary>
		Smoothed,

		/// <summary>Weighted moving average.</summary>
		Weighted
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Xroc2VgTmStrategy"/> class.
	/// </summary>
	public Xroc2VgTmStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_rocPeriod1 = Param(nameof(RocPeriod1), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast ROC Period", "Lookback for the first ROC line", "Indicator")
			.SetCanOptimize(true);

		_rocPeriod2 = Param(nameof(RocPeriod2), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow ROC Period", "Lookback for the second ROC line", "Indicator")
			.SetCanOptimize(true);

		_smoothLength1 = Param(nameof(SmoothLength1), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Smoothing", "Smoothing length for the first line", "Indicator");

		_smoothLength2 = Param(nameof(SmoothLength2), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow Smoothing", "Smoothing length for the second line", "Indicator");

		_smoothMethod1 = Param(nameof(SmoothMethod1), SmoothingMethod.Exponential)
			.SetDisplay("Fast Method", "Smoothing method for the first line", "Indicator");

		_smoothMethod2 = Param(nameof(SmoothMethod2), SmoothingMethod.Exponential)
			.SetDisplay("Slow Method", "Smoothing method for the second line", "Indicator");

		_rocType = Param(nameof(RocType), RocCalculationType.Momentum)
			.SetDisplay("ROC Mode", "Calculation used for rate of change", "Indicator");

		_signalShift = Param(nameof(SignalShift), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Shift", "Bars back to read the signals", "Logic");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
			.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
			.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
			.SetDisplay("Allow Long Exit", "Enable closing long positions by indicator", "Trading");

		_allowSellClose = Param(nameof(AllowSellClose), true)
			.SetDisplay("Allow Short Exit", "Enable closing short positions by indicator", "Trading");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Restrict trading to a time window", "Timing");

		_startTime = Param(nameof(StartTime), TimeSpan.Zero)
			.SetDisplay("Start Time", "Session start time", "Timing");

		_endTime = Param(nameof(EndTime), new TimeSpan(23, 59, 0))
			.SetDisplay("End Time", "Session end time", "Timing");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for new positions", "Trading");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Protective stop distance in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit", "Target distance in price units", "Risk");
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback of the first ROC line.
	/// </summary>
	public int RocPeriod1
	{
		get => _rocPeriod1.Value;
		set => _rocPeriod1.Value = value;
	}

	/// <summary>
	/// Lookback of the second ROC line.
	/// </summary>
	public int RocPeriod2
	{
		get => _rocPeriod2.Value;
		set => _rocPeriod2.Value = value;
	}

	/// <summary>
	/// Smoothing length applied to the first line.
	/// </summary>
	public int SmoothLength1
	{
		get => _smoothLength1.Value;
		set => _smoothLength1.Value = value;
	}

	/// <summary>
	/// Smoothing length applied to the second line.
	/// </summary>
	public int SmoothLength2
	{
		get => _smoothLength2.Value;
		set => _smoothLength2.Value = value;
	}

	/// <summary>
	/// Smoothing method for the first line.
	/// </summary>
	public SmoothingMethod SmoothMethod1
	{
		get => _smoothMethod1.Value;
		set => _smoothMethod1.Value = value;
	}

	/// <summary>
	/// Smoothing method for the second line.
	/// </summary>
	public SmoothingMethod SmoothMethod2
	{
		get => _smoothMethod2.Value;
		set => _smoothMethod2.Value = value;
	}

	/// <summary>
	/// Type of ROC calculation.
	/// </summary>
	public RocCalculationType RocType
	{
		get => _rocType.Value;
		set => _rocType.Value = value;
	}

	/// <summary>
	/// Number of bars back used for signal evaluation.
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	/// <summary>
	/// Enables closing long positions by indicator signals.
	/// </summary>
	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	/// <summary>
	/// Enables closing short positions by indicator signals.
	/// </summary>
	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	/// <summary>
	/// Turns the time filter on or off.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading session start time.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading session end time.
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Order volume used for new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Protective stop distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
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

		_closeHistory.Clear();
		_fastHistory.Clear();
		_slowHistory.Clear();

		_smoothFast = null;
		_smoothSlow = null;

		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_smoothFast = CreateSmoothingIndicator(SmoothMethod1, SmoothLength1);
		_smoothSlow = CreateSmoothingIndicator(SmoothMethod2, SmoothLength2);

		_closeHistory.Clear();
		_fastHistory.Clear();
		_slowHistory.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smoothFast);
			DrawIndicator(area, _smoothSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var capacity = Math.Max(Math.Max(RocPeriod1, RocPeriod2) + SignalShift + 5, 8);
		UpdateHistory(_closeHistory, candle.ClosePrice, capacity);

		var fastRoc = CalculateRoc(RocPeriod1);
		var slowRoc = CalculateRoc(RocPeriod2);

		if (fastRoc is null || slowRoc is null)
			return;

		var fastValue = _smoothFast.Process(new DecimalIndicatorValue(_smoothFast, fastRoc.Value, candle.OpenTime));
		var slowValue = _smoothSlow.Process(new DecimalIndicatorValue(_smoothSlow, slowRoc.Value, candle.OpenTime));

		if (!fastValue.IsFinal || !slowValue.IsFinal)
			return;

		if (fastValue is not DecimalIndicatorValue fastResult || slowValue is not DecimalIndicatorValue slowResult)
			return;

		var historyCapacity = SignalShift + 3;
		UpdateHistory(_fastHistory, fastResult.Value, historyCapacity);
		UpdateHistory(_slowHistory, slowResult.Value, historyCapacity);

		if (_fastHistory.Count <= SignalShift + 1 || _slowHistory.Count <= SignalShift + 1)
			return;

		var fastCurrent = _fastHistory[SignalShift];
		var fastPrevious = _fastHistory[SignalShift + 1];
		var slowCurrent = _slowHistory[SignalShift];
		var slowPrevious = _slowHistory[SignalShift + 1];

		var buyOpenSignal = AllowBuyOpen && fastPrevious > slowPrevious && fastCurrent <= slowCurrent;
		var sellOpenSignal = AllowSellOpen && fastPrevious < slowPrevious && fastCurrent >= slowCurrent;
		var buyCloseSignal = AllowBuyClose && fastPrevious < slowPrevious;
		var sellCloseSignal = AllowSellClose && fastPrevious > slowPrevious;

		var tradeAllowed = !UseTimeFilter || IsWithinTradeWindow(candle.OpenTime);

		if (UseTimeFilter && !tradeAllowed && Position != 0)
		{
			ClosePosition();
			ResetPositionState();
			return;
		}

		if (TryApplyRiskManagement(candle))
			return;

		if (sellCloseSignal && Position < 0)
		{
			BuyMarket(-Position);
			ResetPositionState();
			return;
		}

		if (buyCloseSignal && Position > 0)
		{
			SellMarket(Position);
			ResetPositionState();
			return;
		}

		if (!tradeAllowed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		if (buyOpenSignal && OrderVolume > 0m)
		{
			BuyMarket(OrderVolume);
			_longEntryPrice = candle.ClosePrice;
			_shortEntryPrice = null;
		}
		else if (sellOpenSignal && OrderVolume > 0m)
		{
			SellMarket(OrderVolume);
			_shortEntryPrice = candle.ClosePrice;
			_longEntryPrice = null;
		}
	}

	private bool TryApplyRiskManagement(ICandleMessage candle)
	{
		if (StopLoss <= 0m && TakeProfit <= 0m)
			return false;

		if (Position > 0 && _longEntryPrice is decimal longEntry)
		{
			if (StopLoss > 0m)
			{
				var stopLevel = longEntry - StopLoss;
				if (candle.LowPrice <= stopLevel)
				{
					SellMarket(Position);
					ResetPositionState();
					return true;
				}
			}

			if (TakeProfit > 0m)
			{
				var targetLevel = longEntry + TakeProfit;
				if (candle.HighPrice >= targetLevel)
				{
					SellMarket(Position);
					ResetPositionState();
					return true;
				}
			}
		}
		else if (Position < 0 && _shortEntryPrice is decimal shortEntry)
		{
			if (StopLoss > 0m)
			{
				var stopLevel = shortEntry + StopLoss;
				if (candle.HighPrice >= stopLevel)
				{
					BuyMarket(-Position);
					ResetPositionState();
					return true;
				}
			}

			if (TakeProfit > 0m)
			{
				var targetLevel = shortEntry - TakeProfit;
				if (candle.LowPrice <= targetLevel)
				{
					BuyMarket(-Position);
					ResetPositionState();
					return true;
				}
			}
		}

		return false;
	}

	private decimal? CalculateRoc(int period)
	{
		if (period <= 0 || _closeHistory.Count <= period)
			return null;

		var current = _closeHistory[0];
		var previous = _closeHistory[period];

		if (previous == 0m && (RocType == RocCalculationType.RateOfChange || RocType == RocCalculationType.Percent || RocType == RocCalculationType.Ratio || RocType == RocCalculationType.RatioPercent))
			return null;

		return RocType switch
		{
			RocCalculationType.Momentum => current - previous,
			RocCalculationType.RateOfChange => previous == 0m ? null : (decimal?)((current / previous) - 1m) * 100m,
			RocCalculationType.Percent => previous == 0m ? null : (decimal?)((current - previous) / previous),
			RocCalculationType.Ratio => previous == 0m ? null : (decimal?)(current / previous),
			RocCalculationType.RatioPercent => previous == 0m ? null : (decimal?)(current / previous * 100m),
			_ => current - previous
		};
	}

	private bool IsWithinTradeWindow(DateTimeOffset time)
	{
		var currentMinutes = time.TimeOfDay.TotalMinutes;
		var startMinutes = StartTime.TotalMinutes;
		var endMinutes = EndTime.TotalMinutes;

		if (startMinutes < endMinutes)
			return currentMinutes >= startMinutes && currentMinutes < endMinutes;

		if (startMinutes > endMinutes)
			return currentMinutes >= startMinutes || currentMinutes < endMinutes;

		return false;
	}

	private static void UpdateHistory(List<decimal> history, decimal value, int capacity)
	{
		history.Insert(0, value);
		if (history.Count > capacity)
			history.RemoveAt(history.Count - 1);
	}

	private void ResetPositionState()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	private static IIndicator CreateSmoothingIndicator(SmoothingMethod method, int length)
	{
		var indicator = method switch
		{
			SmoothingMethod.Simple => new SimpleMovingAverage { Length = length },
			SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			SmoothingMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length }
		};

		return indicator;
	}
}
