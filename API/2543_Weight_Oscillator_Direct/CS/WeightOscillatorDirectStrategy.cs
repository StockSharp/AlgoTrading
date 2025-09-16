using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading strategy that combines several oscillators into a weighted composite signal.
/// </summary>
public class WeightOscillatorDirectStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<WeightOscillatorTrendMode> _trendMode;
	private readonly StrategyParam<int> _signalBar;

	private readonly StrategyParam<decimal> _rsiWeight;
	private readonly StrategyParam<int> _rsiPeriod;

	private readonly StrategyParam<decimal> _mfiWeight;
	private readonly StrategyParam<int> _mfiPeriod;

	private readonly StrategyParam<decimal> _wprWeight;
	private readonly StrategyParam<int> _wprPeriod;

	private readonly StrategyParam<decimal> _deMarkerWeight;
	private readonly StrategyParam<int> _deMarkerPeriod;

	private readonly StrategyParam<WeightOscillatorSmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;

	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private readonly StrategyParam<bool> _buyOpenEnabled;
	private readonly StrategyParam<bool> _sellOpenEnabled;
	private readonly StrategyParam<bool> _buyCloseEnabled;
	private readonly StrategyParam<bool> _sellCloseEnabled;

	private RelativeStrengthIndex _rsi = null!;
	private MoneyFlowIndex _mfi = null!;
	private WilliamsR _wpr = null!;
	private DeMarker _deMarker = null!;
	private IIndicator _smoothing = null!;

	private readonly List<decimal> _oscillatorHistory = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="WeightOscillatorDirectStrategy"/> class.
	/// </summary>
	public WeightOscillatorDirectStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General");

		_trendMode = Param(nameof(TrendMode), WeightOscillatorTrendMode.Direct)
		.SetDisplay("Trend Mode", "Trade with the oscillator slope or against it", "Trading");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Number of closed bars to skip before evaluating signals", "Trading")
		.SetRange(1, 5)
		.SetCanOptimize(true);

		_rsiWeight = Param(nameof(RsiWeight), 1m)
		.SetDisplay("RSI Weight", "Weight of RSI in the composite score", "Oscillator")
		.SetRange(0m, 5m)
		.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "Number of bars used for RSI", "Oscillator")
		.SetRange(2, 200)
		.SetCanOptimize(true);

		_mfiWeight = Param(nameof(MfiWeight), 1m)
		.SetDisplay("MFI Weight", "Weight of Money Flow Index", "Oscillator")
		.SetRange(0m, 5m)
		.SetCanOptimize(true);

		_mfiPeriod = Param(nameof(MfiPeriod), 14)
		.SetDisplay("MFI Period", "Number of bars used for MFI", "Oscillator")
		.SetRange(2, 200)
		.SetCanOptimize(true);

		_wprWeight = Param(nameof(WprWeight), 1m)
		.SetDisplay("WPR Weight", "Weight of Williams %R", "Oscillator")
		.SetRange(0m, 5m)
		.SetCanOptimize(true);

		_wprPeriod = Param(nameof(WprPeriod), 14)
		.SetDisplay("WPR Period", "Number of bars used for Williams %R", "Oscillator")
		.SetRange(2, 200)
		.SetCanOptimize(true);

		_deMarkerWeight = Param(nameof(DeMarkerWeight), 1m)
		.SetDisplay("DeMarker Weight", "Weight of DeMarker oscillator", "Oscillator")
		.SetRange(0m, 5m)
		.SetCanOptimize(true);

		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
		.SetDisplay("DeMarker Period", "Number of bars used for DeMarker", "Oscillator")
		.SetRange(2, 200)
		.SetCanOptimize(true);

		_smoothingMethod = Param(nameof(SmoothingMethod), WeightOscillatorSmoothingMethod.Jurik)
		.SetDisplay("Smoothing Method", "Moving average applied to the blended oscillator", "Oscillator");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
		.SetDisplay("Smoothing Length", "Length of the smoothing moving average", "Oscillator")
		.SetRange(1, 200)
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss Points", "Protective stop in price steps (0 disables)", "Risk Management")
		.SetRange(0, 10000)
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit Points", "Profit target in price steps (0 disables)", "Risk Management")
		.SetRange(0, 20000)
		.SetCanOptimize(true);

		_buyOpenEnabled = Param(nameof(BuyOpenEnabled), true)
		.SetDisplay("Allow Long Entries", "Enable opening long positions", "Trading");

		_sellOpenEnabled = Param(nameof(SellOpenEnabled), true)
		.SetDisplay("Allow Short Entries", "Enable opening short positions", "Trading");

		_buyCloseEnabled = Param(nameof(BuyCloseEnabled), true)
		.SetDisplay("Close Shorts on Long Signal", "Allow closing shorts when a long signal appears", "Trading");

		_sellCloseEnabled = Param(nameof(SellCloseEnabled), true)
		.SetDisplay("Close Longs on Short Signal", "Allow closing longs when a short signal appears", "Trading");
	}

	/// <summary>
	/// Candle type used for the calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Defines whether the strategy trades with or against the oscillator direction.
	/// </summary>
	public WeightOscillatorTrendMode TrendMode
	{
		get => _trendMode.Value;
		set => _trendMode.Value = value;
	}

	/// <summary>
	/// Number of closed bars to skip when evaluating the composite oscillator.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Weight assigned to RSI.
	/// </summary>
	public decimal RsiWeight
	{
		get => _rsiWeight.Value;
		set => _rsiWeight.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Weight assigned to MFI.
	/// </summary>
	public decimal MfiWeight
	{
		get => _mfiWeight.Value;
		set => _mfiWeight.Value = value;
	}

	/// <summary>
	/// MFI lookback period.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Weight assigned to Williams %R.
	/// </summary>
	public decimal WprWeight
	{
		get => _wprWeight.Value;
		set => _wprWeight.Value = value;
	}

	/// <summary>
	/// Williams %R lookback period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Weight assigned to DeMarker oscillator.
	/// </summary>
	public decimal DeMarkerWeight
	{
		get => _deMarkerWeight.Value;
		set => _deMarkerWeight.Value = value;
	}

	/// <summary>
	/// DeMarker lookback period.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the blended oscillator.
	/// </summary>
	public WeightOscillatorSmoothingMethod SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing moving average.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables opening long positions.
	/// </summary>
	public bool BuyOpenEnabled
	{
		get => _buyOpenEnabled.Value;
		set => _buyOpenEnabled.Value = value;
	}

	/// <summary>
	/// Enables opening short positions.
	/// </summary>
	public bool SellOpenEnabled
	{
		get => _sellOpenEnabled.Value;
		set => _sellOpenEnabled.Value = value;
	}

	/// <summary>
	/// Enables closing short positions on a long signal.
	/// </summary>
	public bool BuyCloseEnabled
	{
		get => _buyCloseEnabled.Value;
		set => _buyCloseEnabled.Value = value;
	}

	/// <summary>
	/// Enables closing long positions on a short signal.
	/// </summary>
	public bool SellCloseEnabled
	{
		get => _sellCloseEnabled.Value;
		set => _sellCloseEnabled.Value = value;
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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_mfi = new MoneyFlowIndex { Length = MfiPeriod };
		_wpr = new WilliamsR { Length = WprPeriod };
		_deMarker = new DeMarker { Length = DeMarkerPeriod };
		_smoothing = CreateSmoothingIndicator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _mfi, _wpr, _deMarker, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		var step = Security?.PriceStep ?? 1m;
		var takeProfit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null;
		var stopLoss = StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Point) : null;

		StartProtection(takeProfit, stopLoss);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal mfiValue, decimal wprValue, decimal deMarkerValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var totalWeight = RsiWeight + MfiWeight + WprWeight + DeMarkerWeight;
		if (totalWeight <= 0)
		{
		LogWarning("Total oscillator weight must be positive to generate signals.");
		return;
		}

		// Williams %R is negative in StockSharp, so shift it into the 0..100 range.
		var normalizedWpr = wprValue + 100m;
		// DeMarker returns 0..1; scale to match other oscillators.
		var normalizedDeMarker = deMarkerValue * 100m;

		var blended = (RsiWeight * rsiValue + MfiWeight * mfiValue + WprWeight * normalizedWpr + DeMarkerWeight * normalizedDeMarker) / totalWeight;

		var smoothedValue = _smoothing.Process(blended, candle.OpenTime, true);
		if (!smoothedValue.IsFinal)
		return;

		var oscillator = smoothedValue.ToDecimal();

		_oscillatorHistory.Add(oscillator);
		if (_oscillatorHistory.Count > 512)
		_oscillatorHistory.RemoveAt(0);

		var requiredCount = SignalBar + 2;
		if (_oscillatorHistory.Count < requiredCount)
		return;

		var current = GetHistoryValue(SignalBar);
		var previous = GetHistoryValue(SignalBar + 1);
		var prior = GetHistoryValue(SignalBar + 2);

		// Rising when slope turns up over the last two steps.
		var rising = previous < prior && current > previous;
		// Falling when slope turns down over the last two steps.
		var falling = previous > prior && current < previous;

		bool longSignal;
		bool shortSignal;

		if (TrendMode == WeightOscillatorTrendMode.Direct)
		{
		longSignal = rising;
		shortSignal = falling;
		}
		else
		{
		longSignal = falling;
		shortSignal = rising;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (longSignal)
		{
		if (BuyCloseEnabled && Position < 0)
		{
		// Close existing short exposure before flipping long.
		BuyMarket(Math.Abs(Position));
		}

		if (BuyOpenEnabled && Position <= 0)
		{
		// Use Volume + |Position| to reverse and build the new long position in a single order.
		BuyMarket(Volume + Math.Abs(Position));
		}
		}

		if (shortSignal)
		{
		if (SellCloseEnabled && Position > 0)
		{
		// Close existing long exposure before flipping short.
		SellMarket(Math.Abs(Position));
		}

		if (SellOpenEnabled && Position >= 0)
		{
		// Use Volume + |Position| to reverse and build the new short position in a single order.
		SellMarket(Volume + Math.Abs(Position));
		}
		}
	}

	private IIndicator CreateSmoothingIndicator()
	{
		return SmoothingMethod switch
		{
			WeightOscillatorSmoothingMethod.Simple => new SimpleMovingAverage { Length = SmoothingLength },
			WeightOscillatorSmoothingMethod.Exponential => new ExponentialMovingAverage { Length = SmoothingLength },
			WeightOscillatorSmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = SmoothingLength },
			WeightOscillatorSmoothingMethod.Weighted => new WeightedMovingAverage { Length = SmoothingLength },
			WeightOscillatorSmoothingMethod.Kaufman => new KaufmanAdaptiveMovingAverage { Length = SmoothingLength },
			_ => new JurikMovingAverage { Length = SmoothingLength },
		};
	}

	private decimal GetHistoryValue(int shift)
	{
		return _oscillatorHistory[_oscillatorHistory.Count - shift];
	}
}

/// <summary>
/// Defines how the strategy reacts to the oscillator slope.
/// </summary>
public enum WeightOscillatorTrendMode
{
	/// <summary>
	/// Trade in the direction of the oscillator slope.
	/// </summary>
	Direct,

	/// <summary>
	/// Trade against the oscillator slope.
	/// </summary>
	Against,
}

/// <summary>
/// Available smoothing methods for the blended oscillator.
/// </summary>
public enum WeightOscillatorSmoothingMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed (RMA) moving average.
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Weighted,

	/// <summary>
	/// Jurik moving average.
	/// </summary>
	Jurik,

	/// <summary>
	/// Kaufman adaptive moving average.
	/// </summary>
	Kaufman,
}
