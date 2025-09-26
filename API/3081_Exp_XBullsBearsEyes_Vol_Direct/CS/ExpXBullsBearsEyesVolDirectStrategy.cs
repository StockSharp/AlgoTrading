using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader expert Exp_XBullsBearsEyes_Vol_Direct.
/// It recreates the smoothed Bulls/Bears Power oscillator multiplied by volume
/// and reacts to direction flips detected by the original indicator.
/// </summary>
public class ExpXBullsBearsEyesVolDirectStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _gamma;
	private readonly StrategyParam<VolumeSource> _volumeSource;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _smoothingPhase;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private ExponentialMovingAverage _ema;
	private IIndicator _histogramSmoother;
	private IIndicator _volumeSmoother;

	private readonly List<int> _directionHistory = new();
	private decimal _l0;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;
	private decimal? _previousSmoothedValue;
	private int _previousDirection;

	/// <summary>
	/// Candle type used for calculations and trading signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period of Bulls/Bears Power.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Smoothing factor applied to the adaptive filter (0..1).
	/// </summary>
	public decimal Gamma
	{
		get => _gamma.Value;
		set => _gamma.Value = value;
	}

	/// <summary>
	/// Volume source used to weight the oscillator.
	/// </summary>
	public VolumeSource VolumeMode
	{
		get => _volumeSource.Value;
		set => _volumeSource.Value = value;
	}

	/// <summary>
	/// Moving average type for smoothing histogram and volume.
	/// </summary>
	public SmoothingMethod Method
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing windows.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Jurik phase parameter kept for compatibility.
	/// </summary>
	public int SmoothingPhase
	{
		get => _smoothingPhase.Value;
		set => _smoothingPhase.Value = value;
	}

	/// <summary>
	/// Bar shift used when reading direction buffers.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on bearish signals.
	/// </summary>
	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on bullish signals.
	/// </summary>
	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	/// <summary>
	/// Default order size.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ExpXBullsBearsEyesVolDirectStrategy"/>.
	/// </summary>
	public ExpXBullsBearsEyesVolDirectStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used by the indicator", "General");

		_period = Param(nameof(Period), 13)
		.SetGreaterThanZero()
		.SetDisplay("Bulls/Bears Period", "Lookback window of Bulls/Bears Power", "Indicator");

		_gamma = Param(nameof(Gamma), 0.6m)
		.SetDisplay("Gamma", "Adaptive filter smoothing factor", "Indicator");

		_volumeSource = Param(nameof(VolumeMode), VolumeSource.Tick)
		.SetDisplay("Volume Source", "Volume applied to the histogram", "Indicator");

		_smoothingMethod = Param(nameof(Method), SmoothingMethod.Sma)
		.SetDisplay("Smoothing Method", "Moving average type for histogram and volume", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "Length of the smoothing moving averages", "Indicator");

		_smoothingPhase = Param(nameof(SmoothingPhase), 15)
		.SetDisplay("Smoothing Phase", "Phase parameter used by Jurik averaging", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Signal Bar", "Shift applied when evaluating direction", "Trading");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
		.SetDisplay("Allow Buy Open", "Enable opening long positions", "Trading");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
		.SetDisplay("Allow Sell Open", "Enable opening short positions", "Trading");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
		.SetDisplay("Allow Buy Close", "Enable closing longs on bearish flips", "Trading");

		_allowSellClose = Param(nameof(AllowSellClose), true)
		.SetDisplay("Allow Sell Close", "Enable closing shorts on bullish flips", "Trading");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Default market order size", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Protective stop in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Protective target in price steps", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage
		{
			Length = Math.Max(1, Period)
		};

		_histogramSmoother = CreateMovingAverage(Method, SmoothingLength, SmoothingPhase);
		_volumeSmoother = CreateMovingAverage(Method, SmoothingLength, SmoothingPhase);

		_directionHistory.Clear();
		_previousSmoothedValue = null;
		_previousDirection = 0;
		_l0 = _l1 = _l2 = _l3 = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		Volume = Math.Max(0m, OrderVolume);

		var priceStep = Security?.PriceStep ?? 0m;
		Unit stopLoss = null;
		Unit takeProfit = null;

		if (StopLossPoints > 0 && priceStep > 0m)
		{
			stopLoss = new Unit(StopLossPoints * priceStep, UnitTypes.Absolute);
		}

		if (TakeProfitPoints > 0 && priceStep > 0m)
		{
			takeProfit = new Unit(TakeProfitPoints * priceStep, UnitTypes.Absolute);
		}

		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema?.Reset();
		_histogramSmoother?.Reset();
		_volumeSmoother?.Reset();
		_directionHistory.Clear();
		_previousSmoothedValue = null;
		_previousDirection = 0;
		_l0 = _l1 = _l2 = _l3 = 0m;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_ema is null || _histogramSmoother is null || _volumeSmoother is null)
		return;

		// Feed the EMA with the candle close to emulate iBullsPower/iBearsPower internals.
		var emaValue = _ema.Process(new DecimalIndicatorValue(_ema, candle.ClosePrice, candle.OpenTime, true));
		if (!emaValue.IsFinal)
		return;

		var ema = emaValue.ToDecimal();

		// Rebuild Bulls and Bears Power values.
		var bulls = candle.HighPrice - ema;
		var bears = candle.LowPrice - ema;

		// Run the four-stage adaptive smoothing described in the MQL version.
		var l0Prev = _l0;
		var l1Prev = _l1;
		var l2Prev = _l2;
		var l3Prev = _l3;

		var sum = bulls + bears;
		var gamma = Gamma;

		_l0 = ((1m - gamma) * sum) + (gamma * l0Prev);
		_l1 = (-gamma * _l0) + l0Prev + (gamma * l1Prev);
		_l2 = (-gamma * _l1) + l1Prev + (gamma * l2Prev);
		_l3 = (-gamma * _l2) + l2Prev + (gamma * l3Prev);

		var cu = 0m;
		var cd = 0m;

		if (_l0 >= _l1)
		{
			cu = _l0 - _l1;
		}
		else
		{
			cd = _l1 - _l0;
		}

		if (_l1 >= _l2)
		{
			cu += _l1 - _l2;
		}
		else
		{
			cd += _l2 - _l1;
		}

		if (_l2 >= _l3)
		{
			cu += _l2 - _l3;
		}
		else
		{
			cd += _l3 - _l2;
		}

		var denom = cu + cd;
		var result = denom != 0m ? cu / denom : 0m;
		var histogram = (result * 100m) - 50m;

		// Apply the requested volume type to the histogram.
		var volume = GetVolume(candle);
		var scaledHistogram = histogram * volume;

		var histogramValue = _histogramSmoother.Process(new DecimalIndicatorValue(_histogramSmoother, scaledHistogram, candle.OpenTime, true));
		var volumeValue = _volumeSmoother.Process(new DecimalIndicatorValue(_volumeSmoother, volume, candle.OpenTime, true));

		if (histogramValue is not DecimalIndicatorValue { IsFinal: true } histogramResult)
		return;

		if (volumeValue is not DecimalIndicatorValue { IsFinal: true })
		return;

		var smoothedHistogram = histogramResult.Value;
		var direction = CalculateDirection(smoothedHistogram);

		UpdateHistory(direction);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!TryGetColors(out var olderColor, out var currentColor))
		return;

		HandleSignals(olderColor, currentColor);
	}

	private int CalculateDirection(decimal currentValue)
	{
		if (_previousSmoothedValue is not decimal previous)
		{
			_previousSmoothedValue = currentValue;
			_previousDirection = 0;
			return _previousDirection;
		}

		int direction;
		if (currentValue > previous)
		{
			direction = 0;
		}
		else if (currentValue < previous)
		{
			direction = 1;
		}
		else
		{
			direction = _previousDirection;
		}

		_previousSmoothedValue = currentValue;
		_previousDirection = direction;
		return direction;
	}

	private void UpdateHistory(int direction)
	{
		_directionHistory.Add(direction);
		var maxHistory = Math.Max(4, SignalBar + 3);
		if (_directionHistory.Count > maxHistory)
		{
			_directionHistory.RemoveAt(0);
		}
	}

	private bool TryGetColors(out int olderColor, out int currentColor)
	{
		olderColor = 0;
		currentColor = 0;

		var shift = Math.Max(0, SignalBar);
		var currentIndex = _directionHistory.Count - 1 - shift;
		var olderIndex = currentIndex - 1;

		if (currentIndex < 0 || olderIndex < 0)
		{
			return false;
		}

		currentColor = _directionHistory[currentIndex];
		olderColor = _directionHistory[olderIndex];
		return true;
	}

	private void HandleSignals(int olderColor, int currentColor)
	{
		// Color 0 is produced when the smoothed histogram rises, color 1 when it declines.
		if (olderColor == 0)
		{
			if (AllowSellClose && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}

			if (AllowBuyOpen && currentColor == 1 && Position <= 0)
			{
				BuyMarket();
			}
		}
		else if (olderColor == 1)
		{
			if (AllowBuyClose && Position > 0)
			{
				SellMarket(Position);
			}

			if (AllowSellOpen && currentColor == 0 && Position >= 0)
			{
				SellMarket();
			}
		}
	}

	private decimal GetVolume(ICandleMessage candle)
	{
		return VolumeMode switch
		{
			VolumeSource.Tick => candle.TotalTicks.HasValue ? (decimal)candle.TotalTicks.Value : candle.TotalVolume ?? candle.Volume ?? 0m,
			VolumeSource.Real => candle.TotalVolume ?? candle.Volume ?? 0m,
			_ => candle.TotalVolume ?? candle.Volume ?? 0m,
		};
	}

	private static IIndicator CreateMovingAverage(SmoothingMethod method, int length, int phase)
	{
		var effectiveLength = Math.Max(1, length);
		return method switch
		{
			SmoothingMethod.Sma => new SimpleMovingAverage { Length = effectiveLength },
			SmoothingMethod.Ema => new ExponentialMovingAverage { Length = effectiveLength },
			SmoothingMethod.Smma => new SmoothedMovingAverage { Length = effectiveLength },
			SmoothingMethod.Lwma => new LinearWeightedMovingAverage { Length = effectiveLength },
			SmoothingMethod.Jurik => CreateJurik(effectiveLength, phase),
			_ => new SimpleMovingAverage { Length = effectiveLength },
		};
	}

	private static IIndicator CreateJurik(int length, int phase)
	{
		var jurik = new JurikMovingAverage
		{
			Length = Math.Max(1, length)
		};

		var property = jurik.GetType().GetProperty("Phase");
		if (property != null)
		{
			var value = Math.Max(-100, Math.Min(100, phase));
			property.SetValue(jurik, value);
		}

		return jurik;
	}

	/// <summary>
	/// Supported volume sources.
	/// </summary>
	public enum VolumeSource
	{
		/// <summary>
		/// Use tick count when available.
		/// </summary>
		Tick,

		/// <summary>
		/// Use traded volume (lots/contracts).
		/// </summary>
		Real
	}

	/// <summary>
	/// Supported smoothing methods.
	/// </summary>
	public enum SmoothingMethod
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Sma,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Ema,

		/// <summary>
		/// Smoothed moving average (RMA).
		/// </summary>
		Smma,

		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		Lwma,

		/// <summary>
		/// Jurik moving average.
		/// </summary>
		Jurik,

		/// <summary>
		/// Parabolic, T3, VIDYA and AMA are not available in StockSharp by default.
		/// The strategy falls back to SMA for these legacy options.
		/// </summary>
		Parabolic,

		/// <summary>
		/// Tillson T3 smoother.
		/// </summary>
		T3,

		/// <summary>
		/// Variable index dynamic average.
		/// </summary>
		Vidya,

		/// <summary>
		/// Adaptive moving average.
		/// </summary>
		Ama
	}
}
