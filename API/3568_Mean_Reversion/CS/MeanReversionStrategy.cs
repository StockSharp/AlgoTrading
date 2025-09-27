namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the "Mean Reversion" MetaTrader expert advisor.
/// The strategy buys after a multi-bar sell-off and sells after a multi-bar rally, confirmed by trend and momentum filters.
/// </summary>
public class MeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _barsToCount;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private DataType _momentumType;
	private DataType _macdType;
	private bool _useBaseForMomentum;
	private bool _useBaseForMacd;

	private decimal _pipSize;
	private decimal _fastValue;
	private decimal _slowValue;
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private decimal _entryPrice;
	private decimal _stopLossLevel;
	private decimal _takeProfitLevel;

	private readonly List<decimal> _closeHistory = new();
	private readonly Queue<decimal> _momentumDeviation = new();

	/// <summary>
	/// Initializes default parameters using the values from the original expert advisor.
	/// </summary>
	public MeanReversionStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume used when sending market orders.", "General")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Primary Timeframe", "Candle series used to generate entries.", "General");

		_barsToCount = Param(nameof(BarsToCount), 10)
		.SetGreaterThanZero()
		.SetDisplay("Bars To Count", "Number of previous closes compared when detecting exhaustion.", "Signal")
		.SetCanOptimize(true);

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average.", "Signal")
		.SetCanOptimize(true);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average.", "Signal")
		.SetCanOptimize(true);

		_momentumLength = Param(nameof(MomentumLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Period of the momentum indicator on the higher timeframe.", "Signal")
		.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast Length", "Length of the fast EMA used by the MACD filter.", "Signal")
		.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow Length", "Length of the slow EMA used by the MACD filter.", "Signal")
		.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal Length", "Signal smoothing period for the MACD filter.", "Signal")
		.SetCanOptimize(true);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetNotNegative()
		.SetDisplay("Momentum Threshold", "Absolute deviation from 100 required for confirmation.", "Signal")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips.", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Target distance expressed in pips.", "Risk")
		.SetCanOptimize(true);

		_useBreakEven = Param(nameof(UseBreakEven), false)
		.SetDisplay("Use Break-Even", "Enable stop relocation to the entry price after profits.", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
		.SetNotNegative()
		.SetDisplay("Break-Even Trigger", "Profit in pips required before moving the stop to entry.", "Risk")
		.SetCanOptimize(true);

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
		.SetNotNegative()
		.SetDisplay("Break-Even Offset", "Additional pips added when relocating the stop.", "Risk")
		.SetCanOptimize(true);

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Activate trailing stop management once in profit.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
		.SetNotNegative()
		.SetDisplay("Trailing Distance", "Minimum profit in pips required before trailing.", "Risk")
		.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 40m)
		.SetNotNegative()
		.SetDisplay("Trailing Step", "Offset in pips used when updating the trailing stop.", "Risk")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Order volume used by the strategy.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Primary candle type that drives entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of previous closes required to confirm exhaustion.
	/// </summary>
	public int BarsToCount
	{
		get => _barsToCount.Value;
		set => _barsToCount.Value = value;
	}

	/// <summary>
	/// Length of the fast LWMA.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow LWMA.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum period on the higher timeframe.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Number of fast EMA periods used by the MACD component.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Number of slow EMA periods used by the MACD component.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Number of periods used by the MACD signal line.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Minimal absolute deviation of momentum from 100.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables the break-even stop relocation.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit in pips required before moving the stop to entry.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional offset when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Enables trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Distance in pips that activates the trailing stop.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Step in pips applied when updating the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_momentumType = default;
		_macdType = default;
		_useBaseForMomentum = false;
		_useBaseForMacd = false;

		_pipSize = 0m;
		_fastValue = 0m;
		_slowValue = 0m;
		_macdMain = null;
		_macdSignal = null;
		_entryPrice = 0m;
		_stopLossLevel = 0m;
		_takeProfitLevel = 0m;

		_closeHistory.Clear();
		_momentumDeviation.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		if (!TryGetTimeFrame(CandleType, out var baseFrame))
		throw new InvalidOperationException("MeanReversionStrategy requires a timeframe-based candle type.");

		_momentumType = ResolveMomentumDataType(CandleType, baseFrame);
		_macdType = TimeSpan.FromMinutes(43200).TimeFrame();

		_useBaseForMomentum = _momentumType == CandleType;
		_useBaseForMacd = _macdType == CandleType;

		_fastMa = new LinearWeightedMovingAverage
		{
			Length = FastMaLength,
			CandlePrice = CandlePrice.Typical
		};

		_slowMa = new LinearWeightedMovingAverage
		{
			Length = SlowMaLength,
			CandlePrice = CandlePrice.Typical
		};

		_momentum = new Momentum { Length = MomentumLength };

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength }
			},
			SignalMa = { Length = MacdSignalLength }
		};

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription.BindEx(_fastMa, _slowMa, ProcessBaseCandle);

		if (_useBaseForMomentum)
		baseSubscription.Bind(_momentum, ProcessMomentumCandle);

		if (_useBaseForMacd)
		baseSubscription.BindEx(_macd, ProcessMacdCandle);

		baseSubscription.Start();

		if (!_useBaseForMomentum)
		SubscribeCandles(_momentumType).Bind(_momentum, ProcessMomentumCandle).Start();

		if (!_useBaseForMacd)
		SubscribeCandles(_macdType).BindEx(_macd, ProcessMacdCandle).Start();

		_pipSize = CalculatePipSize();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!fastValue.IsFinal || !slowValue.IsFinal)
		return;

		_fastValue = fastValue.ToDecimal();
		_slowValue = slowValue.ToDecimal();

		UpdateCloseHistory(candle.ClosePrice);
		ManageActivePosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!HasMomentumConfirmation())
		return;

		if (!TryGetMacd(out var macdMain, out var macdSignal))
		return;

		var longSignal = ShouldEnterLong(macdMain, macdSignal);
		var shortSignal = ShouldEnterShort(macdMain, macdSignal);

		if (longSignal && Position <= 0)
		{
			var volume = OrderVolume + (Position < 0 ? -Position : 0m);
			if (volume > 0m)
			{
				BuyMarket(volume);
				InitializeTradeState(candle.ClosePrice, isLong: true);
			}
		}
		else if (shortSignal && Position >= 0)
		{
			var volume = OrderVolume + (Position > 0 ? Position : 0m);
			if (volume > 0m)
			{
				SellMarket(volume);
				InitializeTradeState(candle.ClosePrice, isLong: false);
			}
		}
	}

	private void ProcessMomentumCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var deviation = Math.Abs(momentumValue - 100m);
		UpdateMomentumDeviation(deviation);
	}

	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!macdValue.IsFinal)
		return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdMain || macd.Signal is not decimal macdSignal)
		return;

		_macdMain = macdMain;
		_macdSignal = macdSignal;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			ManageLongPosition(candle);
		}
		else if (Position < 0)
		{
			ManageShortPosition(candle);
		}
		else if (_entryPrice != 0m)
		{
			ResetTradeState();
		}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		var step = _pipSize;
		var profit = candle.ClosePrice - _entryPrice;

		if (UseBreakEven && BreakEvenTriggerPips > 0m && profit > BreakEvenTriggerPips * step)
		{
			var breakEvenLevel = _entryPrice + BreakEvenOffsetPips * step;
			_stopLossLevel = Math.Max(_stopLossLevel, breakEvenLevel);
		}

		if (EnableTrailing && TrailingStopPips > 0m && profit > TrailingStopPips * step)
		{
			var trailingDistance = (TrailingStepPips > 0m ? TrailingStepPips : TrailingStopPips) * step;
			var trailingLevel = candle.ClosePrice - trailingDistance;
			_stopLossLevel = Math.Max(_stopLossLevel, trailingLevel);
		}

		if (_stopLossLevel > 0m && candle.LowPrice <= _stopLossLevel)
		{
			SellMarket(Position);
			ResetTradeState();
			return;
		}

		if (_takeProfitLevel > 0m && candle.HighPrice >= _takeProfitLevel)
		{
			SellMarket(Position);
			ResetTradeState();
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		var step = _pipSize;
		var profit = _entryPrice - candle.ClosePrice;

		if (UseBreakEven && BreakEvenTriggerPips > 0m && profit > BreakEvenTriggerPips * step)
		{
			var breakEvenLevel = _entryPrice - BreakEvenOffsetPips * step;
			_stopLossLevel = _stopLossLevel == 0m ? breakEvenLevel : Math.Min(_stopLossLevel, breakEvenLevel);
		}

		if (EnableTrailing && TrailingStopPips > 0m && profit > TrailingStopPips * step)
		{
			var trailingDistance = (TrailingStepPips > 0m ? TrailingStepPips : TrailingStopPips) * step;
			var trailingLevel = candle.ClosePrice + trailingDistance;
			_stopLossLevel = _stopLossLevel == 0m ? trailingLevel : Math.Min(_stopLossLevel, trailingLevel);
		}

		if (_stopLossLevel > 0m && candle.HighPrice >= _stopLossLevel)
		{
			BuyMarket(-Position);
			ResetTradeState();
			return;
		}

		if (_takeProfitLevel > 0m && candle.LowPrice <= _takeProfitLevel)
		{
			BuyMarket(-Position);
			ResetTradeState();
		}
	}

	private void InitializeTradeState(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		var stopOffset = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
		var takeOffset = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;

		_stopLossLevel = stopOffset > 0m
		? (isLong ? entryPrice - stopOffset : entryPrice + stopOffset)
		: 0m;

		_takeProfitLevel = takeOffset > 0m
		? (isLong ? entryPrice + takeOffset : entryPrice - takeOffset)
		: 0m;
	}

	private void ResetTradeState()
	{
		_entryPrice = 0m;
		_stopLossLevel = 0m;
		_takeProfitLevel = 0m;
	}

	private void UpdateCloseHistory(decimal close)
	{
		_closeHistory.Add(close);

		var maxCount = Math.Max(BarsToCount + 5, 16);
		if (_closeHistory.Count > maxCount)
		_closeHistory.RemoveRange(0, _closeHistory.Count - maxCount);
	}

	private void UpdateMomentumDeviation(decimal value)
	{
		_momentumDeviation.Enqueue(value);
		while (_momentumDeviation.Count > 3)
		_momentumDeviation.Dequeue();
	}

	private bool HasMomentumConfirmation()
	{
		var threshold = MomentumThreshold;
		if (threshold <= 0m)
		return true;

		foreach (var value in _momentumDeviation)
		{
			if (value >= threshold)
			return true;
		}

		return false;
	}

	private bool ShouldEnterLong(decimal macdMain, decimal macdSignal)
	{
		if (_fastValue <= _slowValue)
		return false;

		if (macdMain <= macdSignal)
		return false;

		return HasDownwardExhaustion();
	}

	private bool ShouldEnterShort(decimal macdMain, decimal macdSignal)
	{
		if (_fastValue >= _slowValue)
		return false;

		if (macdMain >= macdSignal)
		return false;

		return HasUpwardExhaustion();
	}

	private bool HasDownwardExhaustion()
	{
		var required = BarsToCount;
		if (required <= 0)
		return false;

		if (_closeHistory.Count <= required)
		return false;

		var last = _closeHistory[^1];
		for (var i = 1; i <= required; i++)
		{
			if (_closeHistory.Count <= i)
			return false;

			if (!(last < _closeHistory[^1 - i]))
			return false;
		}

		return true;
	}

	private bool HasUpwardExhaustion()
	{
		var required = BarsToCount;
		if (required <= 0)
		return false;

		if (_closeHistory.Count <= required)
		return false;

		var last = _closeHistory[^1];
		for (var i = 1; i <= required; i++)
		{
			if (_closeHistory.Count <= i)
			return false;

			if (!(last > _closeHistory[^1 - i]))
			return false;
		}

		return true;
	}

	private bool TryGetMacd(out decimal macdMain, out decimal macdSignal)
	{
		if (_macdMain is decimal main && _macdSignal is decimal signal)
		{
			macdMain = main;
			macdSignal = signal;
			return true;
		}

		macdMain = 0m;
		macdSignal = 0m;
		return false;
	}

	private static bool TryGetTimeFrame(DataType type, out TimeSpan frame)
	{
		if (type.MessageType == typeof(TimeFrameCandleMessage) && type.Arg is TimeSpan span)
		{
			frame = span;
			return true;
		}

		frame = default;
		return false;
	}

	private DataType ResolveMomentumDataType(DataType baseType, TimeSpan baseFrame)
	{
		var minutes = (int)Math.Round(baseFrame.TotalMinutes);
		var index = Array.IndexOf(MetaTraderMinutes, minutes);

		if (index >= 0)
		{
			var newIndex = Math.Min(MetaTraderMinutes.Length - 1, index + 1);
			var resolved = MetaTraderMinutes[newIndex];
			return TimeSpan.FromMinutes(resolved).TimeFrame();
		}

		var multiplied = TimeSpan.FromTicks(baseFrame.Ticks * 4);
		return multiplied.TimeFrame();
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
		return 1m;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 1m;

		return security.Decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private static readonly int[] MetaTraderMinutes = { 1, 5, 15, 30, 60, 240, 1440, 10080, 43200 };
}
