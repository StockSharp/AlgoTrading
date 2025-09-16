using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Fractal Weight Oscillator indicator.
/// Combines RSI, MFI, Williams %R and DeMarker into a smoothed oscillator
/// and trades level crossings in direct or counter-trend mode.
/// </summary>
public class FractalWeightOscillatorStrategy : Strategy
{
	private readonly StrategyParam<TrendMode> _trendMode;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<AppliedPrice> _rsiPrice;
	private readonly StrategyParam<AppliedPrice> _mfiPrice;
	private readonly StrategyParam<MfiVolumeType> _mfiVolumeType;
	private readonly StrategyParam<decimal> _rsiWeight;
	private readonly StrategyParam<decimal> _mfiWeight;
	private readonly StrategyParam<decimal> _wprWeight;
	private readonly StrategyParam<decimal> _deMarkerWeight;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<bool> _buyOpenEnabled;
	private readonly StrategyParam<bool> _sellOpenEnabled;
	private readonly StrategyParam<bool> _buyCloseEnabled;
	private readonly StrategyParam<bool> _sellCloseEnabled;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private WilliamsR _williams = null!;
	private LengthIndicator<decimal>? _smoother;
	private SimpleMovingAverage _deMaxSma = null!;
	private SimpleMovingAverage _deMinSma = null!;

	private readonly List<decimal> _oscillatorHistory = new();
	private decimal _previousHigh;
	private decimal _previousLow;
	private bool _hasPreviousCandle;
	private readonly Queue<decimal> _positiveFlow = new();
	private readonly Queue<decimal> _negativeFlow = new();
	private decimal _previousTypical;
	private bool _hasPreviousTypical;
	private decimal _positiveSum;
	private decimal _negativeSum;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Trading direction mode.
	/// </summary>
	public TrendMode TrendMode
	{
		get => _trendMode.Value;
		set => _trendMode.Value = value;
	}

	/// <summary>
	/// Number of closed bars used for signal evaluation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Base period for all component oscillators.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Smoothing window applied to the combined oscillator.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Type of moving average used for smoothing.
	/// </summary>
	public SmoothingMethod SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Applied price source for RSI calculation.
	/// </summary>
	public AppliedPrice RsiPrice
	{
		get => _rsiPrice.Value;
		set => _rsiPrice.Value = value;
	}

	/// <summary>
	/// Applied price source for MFI calculation.
	/// </summary>
	public AppliedPrice MfiPrice
	{
		get => _mfiPrice.Value;
		set => _mfiPrice.Value = value;
	}

	/// <summary>
	/// Volume type used by the MFI component.
	/// </summary>
	public MfiVolumeType MfiVolume
	{
		get => _mfiVolumeType.Value;
		set => _mfiVolumeType.Value = value;
	}

	/// <summary>
	/// Weight of the RSI contribution.
	/// </summary>
	public decimal RsiWeight
	{
		get => _rsiWeight.Value;
		set => _rsiWeight.Value = value;
	}

	/// <summary>
	/// Weight of the MFI contribution.
	/// </summary>
	public decimal MfiWeight
	{
		get => _mfiWeight.Value;
		set => _mfiWeight.Value = value;
	}

	/// <summary>
	/// Weight of the Williams %R contribution.
	/// </summary>
	public decimal WprWeight
	{
		get => _wprWeight.Value;
		set => _wprWeight.Value = value;
	}

	/// <summary>
	/// Weight of the DeMarker contribution.
	/// </summary>
	public decimal DeMarkerWeight
	{
		get => _deMarkerWeight.Value;
		set => _deMarkerWeight.Value = value;
	}

	/// <summary>
	/// Upper threshold of the oscillator.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold of the oscillator.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpenEnabled
	{
		get => _buyOpenEnabled.Value;
		set => _buyOpenEnabled.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpenEnabled
	{
		get => _sellOpenEnabled.Value;
		set => _sellOpenEnabled.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on opposite signals.
	/// </summary>
	public bool BuyCloseEnabled
	{
		get => _buyCloseEnabled.Value;
		set => _buyCloseEnabled.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on opposite signals.
	/// </summary>
	public bool SellCloseEnabled
	{
		get => _sellCloseEnabled.Value;
		set => _sellCloseEnabled.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FractalWeightOscillatorStrategy()
	{
		_trendMode = Param(nameof(TrendMode), TrendMode.Direct)
		.SetDisplay("Trend Mode", "Follow trend or counter-trend", "Trading");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Signal Bar", "Offset for signal evaluation", "Trading");

		_period = Param(nameof(Period), 30)
		.SetGreaterThanZero()
		.SetDisplay("Period", "Length for component oscillators", "Indicators");

		_smoothingLength = Param(nameof(SmoothingLength), 30)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "Window for smoothing", "Indicators");

		_smoothingMethod = Param(nameof(SmoothingMethod), SmoothingMethod.Smma)
		.SetDisplay("Smoothing Method", "Moving average type for smoothing", "Indicators");

		_rsiPrice = Param(nameof(RsiPrice), AppliedPrice.Close)
		.SetDisplay("RSI Price", "Applied price for RSI", "Indicators");

		_mfiPrice = Param(nameof(MfiPrice), AppliedPrice.Typical)
		.SetDisplay("MFI Price", "Applied price for MFI", "Indicators");

		_mfiVolumeType = Param(nameof(MfiVolume), MfiVolumeType.Tick)
		.SetDisplay("MFI Volume", "Volume source for MFI", "Indicators");

		_rsiWeight = Param(nameof(RsiWeight), 1m)
		.SetGreaterThanZero()
		.SetDisplay("RSI Weight", "Weight of RSI component", "Weights");

		_mfiWeight = Param(nameof(MfiWeight), 1m)
		.SetGreaterThanZero()
		.SetDisplay("MFI Weight", "Weight of MFI component", "Weights");

		_wprWeight = Param(nameof(WprWeight), 1m)
		.SetGreaterThanZero()
		.SetDisplay("WPR Weight", "Weight of Williams %R component", "Weights");

		_deMarkerWeight = Param(nameof(DeMarkerWeight), 1m)
		.SetGreaterThanZero()
		.SetDisplay("DeMarker Weight", "Weight of DeMarker component", "Weights");

		_highLevel = Param(nameof(HighLevel), 70m)
		.SetDisplay("High Level", "Upper oscillator threshold", "Trading");

		_lowLevel = Param(nameof(LowLevel), 30m)
		.SetDisplay("Low Level", "Lower oscillator threshold", "Trading");

		_buyOpenEnabled = Param(nameof(BuyOpenEnabled), true)
		.SetDisplay("Enable Long Entries", "Allow buying", "Trading");

		_sellOpenEnabled = Param(nameof(SellOpenEnabled), true)
		.SetDisplay("Enable Short Entries", "Allow selling", "Trading");

		_buyCloseEnabled = Param(nameof(BuyCloseEnabled), true)
		.SetDisplay("Close Long", "Allow long exit on signals", "Trading");

		_sellCloseEnabled = Param(nameof(SellCloseEnabled), true)
		.SetDisplay("Close Short", "Allow short exit on signals", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pts)", "Stop-loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pts)", "Take-profit distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for processing", "General");
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

		_oscillatorHistory.Clear();
		_previousHigh = 0m;
		_previousLow = 0m;
		_hasPreviousCandle = false;
		_positiveFlow.Clear();
		_negativeFlow.Clear();
		_previousTypical = 0m;
		_hasPreviousTypical = false;
		_positiveSum = 0m;
		_negativeSum = 0m;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

	_rsi = new RelativeStrengthIndex { Length = Period };
	_williams = new WilliamsR { Length = Period };
	_deMaxSma = new SimpleMovingAverage { Length = Period };
	_deMinSma = new SimpleMovingAverage { Length = Period };
		_smoother = CreateSmoother(SmoothingMethod, SmoothingLength);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var rsiInput = GetPrice(candle, RsiPrice);
		var rsiValue = _rsi.Process(new CandleIndicatorValue(candle, rsiInput));
		var mfiInput = GetPrice(candle, MfiPrice);
		var wprValue = _williams.Process(new CandleIndicatorValue(candle, candle.ClosePrice));

		if (!rsiValue.IsFinal || !wprValue.IsFinal)
		return;

		var mfi = CalculateMfi(candle, mfiInput);
		if (mfi is null)
		return;

		var deMarker = CalculateDeMarker(candle);
		if (deMarker is null)
		return;

		var rsi = rsiValue.GetValue<decimal>();
		var mfiValue = mfi.Value;
		var wpr = wprValue.GetValue<decimal>();
		var totalWeight = RsiWeight + MfiWeight + WprWeight + DeMarkerWeight;

		if (totalWeight <= 0m)
		return;

		var weighted = (RsiWeight * rsi
		+ MfiWeight * mfiValue
		+ WprWeight * (100m + wpr)
		+ DeMarkerWeight * (deMarker.Value * 100m)) / totalWeight;

		var smoothed = ApplySmoothing(weighted);
		if (smoothed is null)
		return;

		_oscillatorHistory.Add(smoothed.Value);
		TrimHistory();

		if (_oscillatorHistory.Count < SignalBar + 2)
		return;

		var currentIndex = _oscillatorHistory.Count - 1 - SignalBar;
		if (currentIndex <= 0)
		return;

		var current = _oscillatorHistory[currentIndex];
		var previous = _oscillatorHistory[currentIndex - 1];

		CheckRisk(candle);

		var crossBelowLow = previous > LowLevel && current <= LowLevel;
		var crossAboveHigh = previous < HighLevel && current >= HighLevel;

		var openBuy = false;
		var closeBuy = false;
		var openSell = false;
		var closeSell = false;

		if (TrendMode == TrendMode.Direct)
		{
			if (crossBelowLow)
			{
				openBuy = BuyOpenEnabled;
				closeSell = SellCloseEnabled;
			}

			if (crossAboveHigh)
			{
				openSell = SellOpenEnabled;
				closeBuy = BuyCloseEnabled;
			}
		}
		else
		{
			if (crossBelowLow)
			{
				openSell = SellOpenEnabled;
				closeBuy = BuyCloseEnabled;
			}

			if (crossAboveHigh)
			{
				openBuy = BuyOpenEnabled;
				closeSell = SellCloseEnabled;
			}
		}

		if (closeBuy && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			ResetRisk();
		}

		if (closeSell && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ResetRisk();
		}

		if (openBuy && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			SetRiskLevels(candle, Sides.Buy);
		}
		else if (openSell && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			SetRiskLevels(candle, Sides.Sell);
		}
	}

	private decimal? ApplySmoothing(decimal value)
	{
		if (_smoother is null)
		return value;

		var smoothed = _smoother.Process(new DecimalIndicatorValue(_smoother, value));
		return smoothed.IsFinal ? smoothed.GetValue<decimal>() : null;
	}

	private decimal? CalculateDeMarker(ICandleMessage candle)
	{
		if (!_hasPreviousCandle)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			_hasPreviousCandle = true;
			return null;
		}

		var deMax = Math.Max(candle.HighPrice - _previousHigh, 0m);
		var deMin = Math.Max(_previousLow - candle.LowPrice, 0m);

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;

		var deMaxValue = _deMaxSma.Process(new DecimalIndicatorValue(_deMaxSma, deMax));
		var deMinValue = _deMinSma.Process(new DecimalIndicatorValue(_deMinSma, deMin));

		if (!deMaxValue.IsFinal || !deMinValue.IsFinal)
		return null;

		var maxAvg = deMaxValue.GetValue<decimal>();
		var minAvg = deMinValue.GetValue<decimal>();
		var denom = maxAvg + minAvg;

		if (denom == 0m)
		return 0.5m;

		return maxAvg / denom;
	}

	private decimal? CalculateMfi(ICandleMessage candle, decimal price)
	{
		var volume = GetVolume(candle);

		if (!_hasPreviousTypical)
		{
			_previousTypical = price;
			_hasPreviousTypical = true;
			_positiveFlow.Clear();
			_negativeFlow.Clear();
			_positiveSum = 0m;
			_negativeSum = 0m;
			return null;
		}

		var flow = price * volume;
		var positive = price > _previousTypical ? flow : 0m;
		var negative = price < _previousTypical ? flow : 0m;

		_previousTypical = price;

		_positiveSum += positive;
		_negativeSum += negative;
		_positiveFlow.Enqueue(positive);
		_negativeFlow.Enqueue(negative);

		if (_positiveFlow.Count > Period)
		{
			_positiveSum -= _positiveFlow.Dequeue();
			_negativeSum -= _negativeFlow.Dequeue();
		}

		if (_positiveFlow.Count < Period)
		return null;

		if (_negativeSum == 0m)
		return 100m;

		var ratio = _positiveSum / _negativeSum;
		return 100m - 100m / (1m + ratio);
	}

	private void TrimHistory()
	{
		var maxSize = SignalBar + Math.Max(Period, SmoothingLength) + 5;
		if (_oscillatorHistory.Count <= maxSize)
		return;

		var remove = _oscillatorHistory.Count - maxSize;
		_oscillatorHistory.RemoveRange(0, remove);
	}

	private void CheckRisk(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetRisk();
				return;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(Position));
				ResetRisk();
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetRisk();
				return;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetRisk();
			}
		}
	}

	private void SetRiskLevels(ICandleMessage candle, Sides side)
	{
		_entryPrice = candle.ClosePrice;

		var step = Security?.MinPriceStep ?? 0m;
		if (step <= 0m)
		{
			_stopPrice = null;
			_takePrice = null;
			return;
		}

		_stopPrice = StopLossPoints > 0
		? side == Sides.Buy
		? _entryPrice - step * StopLossPoints
		: _entryPrice + step * StopLossPoints
		: null;

		_takePrice = TakeProfitPoints > 0
		? side == Sides.Buy
		? _entryPrice + step * TakeProfitPoints
		: _entryPrice - step * TakeProfitPoints
		: null;
	}

	private void ResetRisk()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	private decimal GetPrice(ICandleMessage candle, AppliedPrice price)
	{
		return price switch
		{
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			AppliedPrice.Simpl => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice
			: candle.ClosePrice < candle.OpenPrice ? candle.LowPrice
			: candle.ClosePrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
			? (candle.HighPrice + candle.ClosePrice) / 2m
			: candle.ClosePrice < candle.OpenPrice
			? (candle.LowPrice + candle.ClosePrice) / 2m
			: candle.ClosePrice,
			AppliedPrice.Demark =>
			{
				var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;
				if (candle.ClosePrice < candle.OpenPrice)
				res = (res + candle.LowPrice) / 2m;
				else if (candle.ClosePrice > candle.OpenPrice)
				res = (res + candle.HighPrice) / 2m;
				else
				res = (res + candle.ClosePrice) / 2m;

				return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
			},
			_ => candle.ClosePrice,
		};
	}

	private decimal GetVolume(ICandleMessage candle)
	{
		return candle.TotalVolume;
	}

	private static LengthIndicator<decimal>? CreateSmoother(SmoothingMethod method, int length)
	{
		return method switch
		{
			SmoothingMethod.None => null,
		SmoothingMethod.Sma => new SimpleMovingAverage { Length = length },
		SmoothingMethod.Ema => new ExponentialMovingAverage { Length = length },
		SmoothingMethod.Smma => new SmoothedMovingAverage { Length = length },
		SmoothingMethod.Lwma => new WeightedMovingAverage { Length = length },
		_ => new SmoothedMovingAverage { Length = length }
		};
	}
}

/// <summary>
/// Trend handling mode.
/// </summary>
public enum TrendMode
{
	/// <summary>
	/// Follow oscillator direction.
	/// </summary>
	Direct,
	/// <summary>
	/// Trade against oscillator direction.
	/// </summary>
	Counter
}

/// <summary>
/// Moving average method used for smoothing.
/// </summary>
public enum SmoothingMethod
{
	/// <summary>
	/// No smoothing.
	/// </summary>
	None,
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Sma,
	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Ema,
	/// <summary>
	/// Smoothed moving average.
	/// </summary>
	Smma,
	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma
}

/// <summary>
/// Applied price options.
/// </summary>
public enum AppliedPrice
{
	/// <summary>
	/// Close price.
	/// </summary>
	Close,
	/// <summary>
	/// Open price.
	/// </summary>
	Open,
	/// <summary>
	/// High price.
	/// </summary>
	High,
	/// <summary>
	/// Low price.
	/// </summary>
	Low,
	/// <summary>
	/// Median price (HL/2).
	/// </summary>
	Median,
	/// <summary>
	/// Typical price (HLC/3).
	/// </summary>
	Typical,
	/// <summary>
	/// Weighted close (HLCC/4).
	/// </summary>
	Weighted,
	/// <summary>
	/// Simple average of open and close.
	/// </summary>
	Simpl,
	/// <summary>
	/// Quarter price (OHLC/4).
	/// </summary>
	Quarter,
	/// <summary>
	/// Trend-following price variant.
	/// </summary>
	TrendFollow0,
	/// <summary>
	/// Alternate trend-following price.
	/// </summary>
	TrendFollow1,
	/// <summary>
	/// DeMarker price.
	/// </summary>
	Demark
}

/// <summary>
/// Volume source used for the MFI component.
/// </summary>
public enum MfiVolumeType
{
	/// <summary>
	/// Tick volume.
	/// </summary>
	Tick,
	/// <summary>
	/// Real traded volume.
	/// </summary>
	Real
}
