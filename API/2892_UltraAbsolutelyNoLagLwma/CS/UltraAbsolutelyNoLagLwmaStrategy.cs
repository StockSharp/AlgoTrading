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

public class UltraAbsolutelyNoLagLwmaStrategy : Strategy
{
	public enum AppliedPrices
	{
		Close = 1,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simplified,
		Quarter,
		TrendFollow0,
		TrendFollow1,
		DeMark
	}

	public enum UltraSmoothMethods
	{
		Sma,
		Ema,
		Smma,
		Lwma,
		Jurik,
		JurX,
		Parabolic,
		T3,
		Vidya,
		Ama
	}
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _baseLength;
	private readonly StrategyParam<AppliedPrices> _appliedPrice;
	private readonly StrategyParam<UltraSmoothMethods> _trendMethod;
	private readonly StrategyParam<int> _startLength;
	private readonly StrategyParam<int> _stepSize;
	private readonly StrategyParam<int> _stepsTotal;
	private readonly StrategyParam<UltraSmoothMethods> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<decimal> _upLevelPercent;
	private readonly StrategyParam<decimal> _downLevelPercent;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<decimal> _takeProfitOffset;

	private WeightedMovingAverage _firstLwma = null!;
	private WeightedMovingAverage _secondLwma = null!;
	private List<LengthIndicator<decimal>> _trendIndicators = null!;
	private LengthIndicator<decimal> _upSmoother = null!;
	private LengthIndicator<decimal> _downSmoother = null!;
	private decimal?[] _previousTrendValues = Array.Empty<decimal?>();
	private bool[] _trendHasHistory = Array.Empty<bool>();
	private bool _indicatorsFormed;
	private readonly List<int> _colorHistory = new();
	private decimal? _previousSmoothedUp;
	private decimal? _previousSmoothedDown;
	private Order _stopOrder;
	private Order _takeOrder;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BaseLength
	{
		get => _baseLength.Value;
		set => _baseLength.Value = value;
	}

	public AppliedPrices AppliedPriceMode
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	public UltraSmoothMethods TrendMethod
	{
		get => _trendMethod.Value;
		set => _trendMethod.Value = value;
	}

	public int StartLength
	{
		get => _startLength.Value;
		set => _startLength.Value = value;
	}

	public int StepSize
	{
		get => _stepSize.Value;
		set => _stepSize.Value = value;
	}

	public int StepsTotal
	{
		get => _stepsTotal.Value;
		set => _stepsTotal.Value = value;
	}

	public UltraSmoothMethods SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	public decimal UpLevelPercent
	{
		get => _upLevelPercent.Value;
		set => _upLevelPercent.Value = value;
	}

	public decimal DownLevelPercent
	{
		get => _downLevelPercent.Value;
		set => _downLevelPercent.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	public UltraAbsolutelyNoLagLwmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for indicator calculations", "General");

		_baseLength = Param(nameof(BaseLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Base LWMA Length", "Length for the initial double LWMA", "Indicator");

		_appliedPrice = Param(nameof(AppliedPriceMode), AppliedPrices.Close)
			.SetDisplay("Applied Price", "Price source for calculations", "Indicator");

		_trendMethod = Param(nameof(TrendMethod), UltraSmoothMethods.Jurik)
			.SetDisplay("Trend Method", "Smoothing method for intermediate curves", "Indicator");

		_startLength = Param(nameof(StartLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Start Length", "Initial length for the smoothing ladder", "Indicator");

		_stepSize = Param(nameof(StepSize), 5)
			.SetGreaterThanZero()
			.SetDisplay("Step Size", "Increment added to each subsequent smoothing length", "Indicator");

		_stepsTotal = Param(nameof(StepsTotal), 10)
			.SetGreaterThanZero()
			.SetDisplay("Steps Total", "Number of smoothing steps to evaluate", "Indicator");

		_smoothingMethod = Param(nameof(SmoothingMethod), UltraSmoothMethods.Jurik)
			.SetDisplay("Smoother Method", "Method used to smooth bullish/bearish counts", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smoother Length", "Length for the final smoothing stage", "Indicator");

		_upLevelPercent = Param(nameof(UpLevelPercent), 80m)
			.SetDisplay("Up Level %", "Threshold highlighting strong bullish pressure", "Indicator");

		_downLevelPercent = Param(nameof(DownLevelPercent), 20m)
			.SetDisplay("Down Level %", "Threshold highlighting strong bearish pressure", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Index of the bar used for signals", "Trading");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
			.SetDisplay("Allow Buy Entries", "Enable opening long positions", "Trading");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
			.SetDisplay("Allow Sell Entries", "Enable opening short positions", "Trading");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
			.SetDisplay("Allow Buy Closes", "Enable closing existing long positions", "Trading");

		_allowSellClose = Param(nameof(AllowSellClose), true)
			.SetDisplay("Allow Sell Closes", "Enable closing existing short positions", "Trading");

		_stopLossOffset = Param(nameof(StopLossOffset), 0m)
			.SetDisplay("Stop Loss Offset", "Absolute stop-loss distance from entry", "Risk");

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0m)
			.SetDisplay("Take Profit Offset", "Absolute take-profit distance from entry", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_colorHistory.Clear();
		_previousSmoothedUp = null;
		_previousSmoothedDown = null;
		_previousTrendValues = Array.Empty<decimal?>();
		_trendHasHistory = Array.Empty<bool>();
		_indicatorsFormed = false;

		CancelProtection();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare the double LWMA filter that removes short-term noise.
		_firstLwma = new WeightedMovingAverage { Length = Math.Max(1, BaseLength) };
		_secondLwma = new WeightedMovingAverage { Length = Math.Max(1, BaseLength) };

		var steps = Math.Max(0, StepsTotal);
		// Build the ladder of smoothing indicators with growing lengths.
		_trendIndicators = new List<LengthIndicator<decimal>>(steps + 1);
		_previousTrendValues = new decimal?[steps + 1];
		_trendHasHistory = new bool[steps + 1];

		for (var i = 0; i <= steps; i++)
		{
			var length = StartLength + StepSize * i;
			_trendIndicators.Add(CreateMovingAverage(TrendMethod, length));
		}

		_upSmoother = CreateMovingAverage(SmoothingMethod, SmoothingLength);
		_downSmoother = CreateMovingAverage(SmoothingMethod, SmoothingLength);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenCandlesFinished(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	// Run the pipeline only on finished candles to stay in sync with the indicator timeframe.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = SelectPrice(candle);
		var time = candle.CloseTime;

		var first = _firstLwma.Process(price, time, true);
		if (!first.IsFinal)
			return;

		var firstValue = first.ToDecimal();
		var second = _secondLwma.Process(firstValue, time, true);
		if (!second.IsFinal)
			return;

		var doubleLwma = second.ToDecimal();
		// The second LWMA produces the smoothed base series for the ladder.

		var bullishCount = 0m;
		var bearishCount = 0m;
		var initialized = true;

		// Update every smoothing stage and measure if it is rising or falling.
		for (var i = 0; i < _trendIndicators.Count; i++)
		{
			var indicator = _trendIndicators[i];
			var value = indicator.Process(doubleLwma, time, true);
			if (!value.IsFinal)
				return;

			var current = value.ToDecimal();

			if (_trendHasHistory[i])
			{
				var previous = _previousTrendValues[i] ?? current;
				if (current > previous)
					bullishCount += 1m;
				else
					bearishCount += 1m;
			}
			else
			{
				initialized = false;
				_trendHasHistory[i] = true;
			}

			_previousTrendValues[i] = current;
		}

		if (!_indicatorsFormed)
		{
			if (!initialized)
				return;

			_indicatorsFormed = true;
		}

		var upValue = _upSmoother.Process(bullishCount, time, true);
		var downValue = _downSmoother.Process(bearishCount, time, true);

		if (!upValue.IsFinal || !downValue.IsFinal)
			return;

		var smoothedUp = upValue.ToDecimal();
		var smoothedDown = downValue.ToDecimal();

		// Convert the smoothed counters into the color code used by the original indicator.
		var color = CalculateColor(smoothedUp, smoothedDown);

		_colorHistory.Insert(0, color);

		var maxHistory = Math.Max(2, SignalBar + 2);
		if (_colorHistory.Count > maxHistory)
			_colorHistory.RemoveAt(_colorHistory.Count - 1);

		if (_colorHistory.Count <= SignalBar + 1)
			return;

		var recent = _colorHistory[SignalBar];
		var older = _colorHistory[SignalBar + 1];

		EvaluateSignals(older, recent, candle);
	}

	private int CalculateColor(decimal smoothedUp, decimal smoothedDown)
	{
		var prevUp = _previousSmoothedUp;
		var prevDown = _previousSmoothedDown;

		_previousSmoothedUp = smoothedUp;
		_previousSmoothedDown = smoothedDown;

		var upThreshold = StepsTotal * UpLevelPercent / 100m;
		var downThreshold = StepsTotal * DownLevelPercent / 100m;

		if (smoothedUp > smoothedDown)
		{
			var isRising = !prevUp.HasValue || prevUp.Value <= smoothedUp;
			var strong = smoothedUp > upThreshold || smoothedDown < downThreshold;
			return strong ? (isRising ? 7 : 8) : (isRising ? 5 : 6);
		}

		if (smoothedUp < smoothedDown)
		{
			var isRising = !prevDown.HasValue || prevDown.Value <= smoothedDown;
			var strong = smoothedUp < downThreshold || smoothedDown > upThreshold;
			return strong ? (isRising ? 1 : 2) : (isRising ? 3 : 4);
		}

		return 0;
	}

	private void EvaluateSignals(int olderColor, int recentColor, ICandleMessage candle)
	{
		// Bullish state switched to bearish: close shorts and optionally open a long position.
		if (olderColor > 4 && recentColor < 5 && recentColor > 0)
		{
			if (AllowSellClose && Position < 0)
				CloseShortPositions();

			if (AllowBuyOpen && Position <= 0)
				OpenLong(candle.ClosePrice);
		}

		// Bearish state switched to bullish: close longs and optionally open a short position.
		if (olderColor < 5 && olderColor > 0 && recentColor > 4)
		{
			if (AllowBuyClose && Position > 0)
				CloseLongPositions();

			if (AllowSellOpen && Position >= 0)
				OpenShort(candle.ClosePrice);
		}
	}

	private void OpenLong(decimal entryPrice)
	{
		CancelProtection();
		BuyMarket(Volume);
		RegisterProtection(true, entryPrice);
	}

	private void OpenShort(decimal entryPrice)
	{
		CancelProtection();
		SellMarket(Volume);
		RegisterProtection(false, entryPrice);
	}

	private void CloseLongPositions()
	{
		if (Position <= 0)
			return;

		CancelProtection();
		SellMarket(Position);
	}

	private void CloseShortPositions()
	{
		if (Position >= 0)
			return;

		CancelProtection();
		BuyMarket(Math.Abs(Position));
	}

	private void RegisterProtection(bool isLong, decimal entryPrice)
	{
		if (StopLossOffset <= 0m && TakeProfitOffset <= 0m)
			return;

		if (StopLossOffset > 0m)
		{
			_stopOrder = isLong
				? SellStop(Volume, entryPrice - StopLossOffset)
				: BuyStop(Volume, entryPrice + StopLossOffset);
		}

		if (TakeProfitOffset > 0m)
		{
			_takeOrder = isLong
				? SellLimit(Volume, entryPrice + TakeProfitOffset)
				: BuyLimit(Volume, entryPrice - TakeProfitOffset);
		}
	}

	private void CancelProtection()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);
		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);

		_stopOrder = null;
		_takeOrder = null;
	}

	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
			return;

		CancelProtection();
	}

	private decimal SelectPrice(ICandleMessage candle)
	{
		return AppliedPriceMode switch
		{
			AppliedPrices.Close => candle.ClosePrice,
			AppliedPrices.Open => candle.OpenPrice,
			AppliedPrices.High => candle.HighPrice,
			AppliedPrices.Low => candle.LowPrice,
			AppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrices.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPrices.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrices.Simplified => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrices.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrices.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
				? candle.HighPrice
				: candle.ClosePrice < candle.OpenPrice
					? candle.LowPrice
					: candle.ClosePrice,
			AppliedPrices.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
				? (candle.HighPrice + candle.ClosePrice) / 2m
				: candle.ClosePrice < candle.OpenPrice
					? (candle.LowPrice + candle.ClosePrice) / 2m
					: candle.ClosePrice,
			AppliedPrices.DeMark => CalculateDeMarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDeMarkPrice(ICandleMessage candle)
	{
		var baseSum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		decimal adjusted;
		if (candle.ClosePrice < candle.OpenPrice)
			adjusted = (baseSum + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			adjusted = (baseSum + candle.HighPrice) / 2m;
		else
			adjusted = (baseSum + candle.ClosePrice) / 2m;

		return ((adjusted - candle.LowPrice) + (adjusted - candle.HighPrice)) / 2m;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(UltraSmoothMethods method, int length)
	{
		var normalizedLength = Math.Max(1, length);

		return method switch
		{
			UltraSmoothMethods.Sma => new SMA { Length = normalizedLength },
			UltraSmoothMethods.Ema => new EMA { Length = normalizedLength },
			UltraSmoothMethods.Smma => new SmoothedMovingAverage { Length = normalizedLength },
			UltraSmoothMethods.Lwma => new WeightedMovingAverage { Length = normalizedLength },
			UltraSmoothMethods.Jurik => new JurikMovingAverage { Length = normalizedLength },
			UltraSmoothMethods.JurX => new JurikMovingAverage { Length = normalizedLength },
			UltraSmoothMethods.T3 => new JurikMovingAverage { Length = normalizedLength },
			UltraSmoothMethods.Vidya => new EMA { Length = normalizedLength },
			UltraSmoothMethods.Ama => new KaufmanAdaptiveMovingAverage { Length = normalizedLength },
			_ => new EMA { Length = normalizedLength },
		};
	}
}
