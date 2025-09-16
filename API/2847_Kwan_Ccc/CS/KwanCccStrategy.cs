using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the KWAN_CCC expert advisor.
/// Trades on transitions of the smoothed Chaikin*CCI/Momentum oscillator.
/// Opens a long position when the oscillator stops rising and a short position when it stops falling.
/// </summary>
public class KwanCccStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<ChaikinMovingAverageMethod> _chaikinMethod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<KwanCccSmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _smoothingPhase;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private AccumulationDistributionLine _adLine = null!;
	private CommodityChannelIndex _cci = null!;
	private Momentum _momentum = null!;
	private IIndicator _fastMa = null!;
	private IIndicator _slowMa = null!;
	private IIndicator? _smoothingIndicator;
	private ChandeMomentumOscillator? _vidyaCmo;
	private decimal? _vidyaValue;
	private decimal? _previousSmoothed;
	private readonly List<int> _colorHistory = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="KwanCccStrategy"/> class.
	/// </summary>
	public KwanCccStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Base order size used for entries", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_fastPeriod = Param(nameof(FastPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Chaikin Fast Period", "Fast moving average length for Chaikin oscillator", "Chaikin")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Chaikin Slow Period", "Slow moving average length for Chaikin oscillator", "Chaikin")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_chaikinMethod = Param(nameof(ChaikinMethod), ChaikinMovingAverageMethod.LinearWeighted)
		.SetDisplay("Chaikin MA Method", "Moving average type used inside the Chaikin oscillator", "Chaikin");

		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Commodity Channel Index length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 2);

		_momentumPeriod = Param(nameof(MomentumPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Momentum indicator length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_smoothingMethod = Param(nameof(SmoothingMethod), KwanCccSmoothingMethod.Jurik)
		.SetDisplay("Smoothing Method", "Type of smoothing applied to the raw oscillator", "Smoothing");

		_smoothingLength = Param(nameof(SmoothingLength), 7)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "Number of bars used by the smoothing filter", "Smoothing")
		.SetCanOptimize(true)
		.SetOptimize(3, 30, 1);

		_smoothingPhase = Param(nameof(SmoothingPhase), 100)
		.SetDisplay("Smoothing Phase", "Additional phase/curvature parameter for specific methods", "Smoothing");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetRange(0, 5)
		.SetDisplay("Signal Bar", "Offset in bars for signal evaluation (0=current, 1=previous)", "Signals");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
		.SetDisplay("Allow Long Entries", "Enable or disable opening long positions", "Trading");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
		.SetDisplay("Allow Short Entries", "Enable or disable opening short positions", "Trading");

		_enableLongExits = Param(nameof(EnableLongExits), true)
		.SetDisplay("Allow Long Exits", "Enable or disable indicator-based long exits", "Trading");

		_enableShortExits = Param(nameof(EnableShortExits), true)
		.SetDisplay("Allow Short Exits", "Enable or disable indicator-based short exits", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetDisplay("Stop Loss Points", "Protective stop in price steps (0 disables)", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 2000m, 100m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetDisplay("Take Profit Points", "Protective target in price steps (0 disables)", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 4000m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for calculations", "General");
	}

	/// <summary>
	/// Base order volume used when creating new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Fast moving average period for the Chaikin oscillator.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period for the Chaikin oscillator.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type used inside the Chaikin oscillator.
	/// </summary>
	public ChaikinMovingAverageMethod ChaikinMethod
	{
		get => _chaikinMethod.Value;
		set => _chaikinMethod.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Momentum indicator period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing algorithm used for the raw oscillator.
	/// </summary>
	public KwanCccSmoothingMethod SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Length parameter passed into the smoothing filter.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Phase or curvature parameter for smoothing methods that require it.
	/// </summary>
	public int SmoothingPhase
	{
		get => _smoothingPhase.Value;
		set => _smoothingPhase.Value = value;
	}

	/// <summary>
	/// Offset in bars for signal detection.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Toggle for opening long positions.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	/// <summary>
	/// Toggle for opening short positions.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	/// <summary>
	/// Toggle for indicator-based closing of long positions.
	/// </summary>
	public bool EnableLongExits
	{
		get => _enableLongExits.Value;
		set => _enableLongExits.Value = value;
	}

	/// <summary>
	/// Toggle for indicator-based closing of short positions.
	/// </summary>
	public bool EnableShortExits
	{
		get => _enableShortExits.Value;
		set => _enableShortExits.Value = value;
	}

	/// <summary>
	/// Stop-loss expressed in instrument price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit expressed in instrument price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_previousSmoothed = null;
		_vidyaValue = null;
		_colorHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize core indicators mirrored from the original expert.
		_adLine = new AccumulationDistributionLine();
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_fastMa = CreateChaikinMa(FastPeriod);
		_slowMa = CreateChaikinMa(SlowPeriod);
		(_smoothingIndicator, _vidyaCmo, _vidyaValue) = CreateSmoother();

		Volume = OrderVolume;

		// Subscribe to the selected timeframe and feed indicators through Bind.
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_adLine, _cci, _momentum, ProcessCandle)
		.Start();

		var takeProfit = CreateProtectionUnit(TakeProfitPoints);
		var stopLoss = CreateProtectionUnit(StopLossPoints);
		StartProtection(takeProfit: takeProfit, stopLoss: stopLoss, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _momentum);
			DrawOwnTrades(area);
		}
	}

	// Process each finished candle and replicate the KWAN_CCC signal extraction.
	private void ProcessCandle(ICandleMessage candle, decimal adValue, decimal cciValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Run the Chaikin oscillator by smoothing accumulation/distribution with the selected MAs.
		var fastResult = _fastMa.Process(new DecimalIndicatorValue(_fastMa, adValue, candle.OpenTime));
		var slowResult = _slowMa.Process(new DecimalIndicatorValue(_slowMa, adValue, candle.OpenTime));

		if (!fastResult.IsFinal || !slowResult.IsFinal || !_cci.IsFormed || !_momentum.IsFormed)
		return;

		var fastMaValue = fastResult.GetValue<decimal>();
		var slowMaValue = slowResult.GetValue<decimal>();
		var chaikinValue = fastMaValue - slowMaValue;
		// Combine Chaikin with CCI and Momentum exactly like the MetaTrader indicator.
		decimal rawValue;
		if (momentumValue == 0m)
		{
			rawValue = 100m;
		}
		else
		{
			rawValue = chaikinValue * cciValue / momentumValue;
		}

		// Apply the chosen smoothing method (mapped from XMA options).
		var smoothed = ProcessSmoothing(rawValue, candle.OpenTime);
		if (smoothed is null)
		return;

		var color = 1;
		if (_previousSmoothed is decimal prevSmoothed)
		{
			if (smoothed.Value > prevSmoothed)
			color = 0;
			else if (smoothed.Value < prevSmoothed)
			color = 2;
		}

		// Track color changes based on smoothed slope just like the color buffer in MQL.
		_previousSmoothed = smoothed.Value;
		_colorHistory.Add(color);

		var requiredHistory = Math.Max(2, SignalBar + 2);
		if (_colorHistory.Count > requiredHistory)
		{
			var removeCount = _colorHistory.Count - requiredHistory;
			_colorHistory.RemoveRange(0, removeCount);
		}

		var indexCurrent = _colorHistory.Count - 1 - SignalBar;
		if (indexCurrent <= 0)
		return;

		var indexPrevious = indexCurrent - 1;
		if (indexPrevious < 0)
		return;

		var colorCurrent = _colorHistory[indexCurrent];
		var colorPrevious = _colorHistory[indexPrevious];

		var sellClose = EnableShortExits && colorPrevious == 0 && colorCurrent != 0;
		var buyOpen = EnableLongEntries && colorPrevious == 0 && colorCurrent != 0;
		var buyClose = EnableLongExits && colorPrevious == 2 && colorCurrent != 2;
		var sellOpen = EnableShortEntries && colorPrevious == 2 && colorCurrent != 2;

		// Execute entry/exit permissions using translated boolean flags.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (sellClose && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (buyClose && Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}

		if (buyOpen && Position <= 0 && Volume > 0)
		{
			if (Position < 0)
			BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		else if (sellOpen && Position >= 0 && Volume > 0)
		{
			if (Position > 0)
			SellMarket(Math.Abs(Position));
			SellMarket(Volume);
		}
	}

	private (IIndicator? indicator, ChandeMomentumOscillator? vidyaCmo, decimal? vidyaValue) CreateSmoother()
	{
		switch (SmoothingMethod)
		{
			case KwanCccSmoothingMethod.Simple:
			return (new SimpleMovingAverage { Length = SmoothingLength }, null, null);
			case KwanCccSmoothingMethod.Exponential:
			return (new ExponentialMovingAverage { Length = SmoothingLength }, null, null);
			case KwanCccSmoothingMethod.Smoothed:
			return (new SmoothedMovingAverage { Length = SmoothingLength }, null, null);
			case KwanCccSmoothingMethod.LinearWeighted:
			return (new WeightedMovingAverage { Length = SmoothingLength }, null, null);
			case KwanCccSmoothingMethod.Jurik:
			case KwanCccSmoothingMethod.JurX:
			case KwanCccSmoothingMethod.Parabolic:
			case KwanCccSmoothingMethod.T3:
			return (new JurikMovingAverage { Length = SmoothingLength }, null, null);
			case KwanCccSmoothingMethod.Vidya:
			{
				var cmoLength = Math.Max(1, SmoothingPhase);
				return (null, new ChandeMomentumOscillator { Length = cmoLength }, null);
			}
			case KwanCccSmoothingMethod.Adaptive:
			{
				var slow = Math.Max(2, SmoothingPhase);
				return (new KaufmanAdaptiveMovingAverage { Length = SmoothingLength, FastSCPeriod = 2, SlowSCPeriod = slow }, null, null);
			}
			default:
			return (new JurikMovingAverage { Length = SmoothingLength }, null, null);
		}
	}

	private decimal? ProcessSmoothing(decimal value, DateTimeOffset time)
	{
		if (_smoothingIndicator != null)
		{
			var result = _smoothingIndicator.Process(new DecimalIndicatorValue(_smoothingIndicator, value, time));
			return result.IsFinal ? result.GetValue<decimal>() : null;
		}

		if (_vidyaCmo != null)
		{
			var cmoResult = _vidyaCmo.Process(new DecimalIndicatorValue(_vidyaCmo, value, time));
			if (!cmoResult.IsFinal)
			return null;

			var cmo = Math.Abs(cmoResult.GetValue<decimal>()) / 100m;
			var alpha = cmo * (2m / (SmoothingLength + 1m));

			if (_vidyaValue is null)
			{
				_vidyaValue = value;
			}
			else
			{
				_vidyaValue = alpha * value + (1 - alpha) * _vidyaValue.Value;
			}

			return _vidyaValue;
		}

		return value;
	}

	private Unit? CreateProtectionUnit(decimal points)
	{
		if (points <= 0m)
		return null;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
		step = 1m;

		var priceOffset = points * step;
		return new Unit(priceOffset, UnitTypes.Absolute);
	}

	private IIndicator CreateChaikinMa(int length)
	{
		return ChaikinMethod switch
		{
			ChaikinMovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			ChaikinMovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			ChaikinMovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			ChaikinMovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Smoothing method options inherited from the original expert advisor.
	/// </summary>
	public enum KwanCccSmoothingMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted,
		Jurik,
		JurX,
		Parabolic,
		T3,
		Vidya,
		Adaptive
	}

	/// <summary>
	/// Moving average options for the Chaikin oscillator.
	/// </summary>
	public enum ChaikinMovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}
}
