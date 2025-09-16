using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Blau Ergodic Market Directional Indicator strategy converted from MetaTrader.
/// Uses a triple-smoothed momentum histogram with configurable entry confirmation modes.
/// </summary>
public class BlauErgodicMdiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _allowLongExits;
	private readonly StrategyParam<bool> _allowShortExits;
	private readonly StrategyParam<EntryMode> _entryMode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _primaryLength;
	private readonly StrategyParam<int> _firstSmoothingLength;
	private readonly StrategyParam<int> _secondSmoothingLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<int> _signalBarShift;
	private readonly StrategyParam<int> _phase;

	private IIndicator _priceAverage = null!;
	private IIndicator _firstSmoothing = null!;
	private IIndicator _secondSmoothing = null!;
	private IIndicator _signalSmoothing = null!;

	private decimal[] _histogramBuffer = Array.Empty<decimal>();
	private decimal[] _signalBuffer = Array.Empty<decimal>();
	private int _bufferIndex;
	private int _bufferFilled;
	private decimal _pointValue = 1m;

	/// <summary>
	/// Initializes a new instance of <see cref="BlauErgodicMdiStrategy"/>.
	/// </summary>
	public BlauErgodicMdiStrategy()
		{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_slippagePoints = Param(nameof(SlippagePoints), 10)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Slippage", "Maximum slippage in points", "Risk");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
			.SetDisplay("Allow Long Entries", "Enable opening long positions", "Permissions");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
			.SetDisplay("Allow Short Entries", "Enable opening short positions", "Permissions");

		_allowLongExits = Param(nameof(AllowLongExits), true)
			.SetDisplay("Allow Long Exits", "Enable closing long positions", "Permissions");

		_allowShortExits = Param(nameof(AllowShortExits), true)
			.SetDisplay("Allow Short Exits", "Enable closing short positions", "Permissions");

		_entryMode = Param(nameof(Mode), EntryMode.Twist)
			.SetDisplay("Entry Mode", "Signal interpretation mode", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Indicator Timeframe", "Timeframe used for calculations", "Data");

		_smoothingMethod = Param(nameof(SmoothingMethod), SmoothingMethod.Exponential)
			.SetDisplay("Smoothing Method", "Type of moving average", "Indicator");

		_primaryLength = Param(nameof(PrimaryLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Primary Length", "Base smoothing length", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_firstSmoothingLength = Param(nameof(FirstSmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Smoothing", "First smoothing length", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_secondSmoothingLength = Param(nameof(SecondSmoothingLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Histogram Smoothing", "Second smoothing length", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_signalLength = Param(nameof(SignalLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal line smoothing", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 30, 1);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrice.Close)
			.SetDisplay("Applied Price", "Price source for calculations", "Indicator");

		_signalBarShift = Param(nameof(SignalBarShift), 1)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Signal Bar", "Shift of the bar used for signals", "Strategy");

		_phase = Param(nameof(Phase), 15)
			.SetDisplay("Phase", "Reserved smoothing phase parameter", "Indicator");
	}

	/// <summary>
	/// Trading volume.
	/// </summary>
	public decimal Volume
		{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in instrument points.
	/// </summary>
	public int StopLossPoints
		{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in instrument points.
	/// </summary>
	public int TakeProfitPoints
		{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allowed price slippage in points.
	/// </summary>
	public int SlippagePoints
		{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Enables opening long positions.
	/// </summary>
	public bool AllowLongEntries
		{
		get => _allowLongEntries.Value;
		set => _allowLongEntries.Value = value;
	}

	/// <summary>
	/// Enables opening short positions.
	/// </summary>
	public bool AllowShortEntries
		{
		get => _allowShortEntries.Value;
		set => _allowShortEntries.Value = value;
	}

	/// <summary>
	/// Enables closing existing long positions on opposite signals.
	/// </summary>
	public bool AllowLongExits
		{
		get => _allowLongExits.Value;
		set => _allowLongExits.Value = value;
	}

	/// <summary>
	/// Enables closing existing short positions on opposite signals.
	/// </summary>
	public bool AllowShortExits
		{
		get => _allowShortExits.Value;
		set => _allowShortExits.Value = value;
	}

	/// <summary>
	/// Selected entry confirmation mode.
	/// </summary>
	public EntryMode Mode
		{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
		{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average family used for smoothing steps.
	/// </summary>
	public SmoothingMethod SmoothingMethod
		{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Length for the initial smoothing of price.
	/// </summary>
	public int PrimaryLength
		{
		get => _primaryLength.Value;
		set => _primaryLength.Value = value;
	}

	/// <summary>
	/// Length of the first smoothing applied to momentum.
	/// </summary>
	public int FirstSmoothingLength
		{
		get => _firstSmoothingLength.Value;
		set => _firstSmoothingLength.Value = value;
	}

	/// <summary>
	/// Length of the second smoothing forming the histogram.
	/// </summary>
	public int SecondSmoothingLength
		{
		get => _secondSmoothingLength.Value;
		set => _secondSmoothingLength.Value = value;
	}

	/// <summary>
	/// Length of the signal line smoothing.
	/// </summary>
	public int SignalLength
		{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Applied price selection for calculations.
	/// </summary>
	public AppliedPrice AppliedPrice
		{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Offset of the bar used for signal confirmation.
	/// </summary>
	public int SignalBarShift
		{
		get => _signalBarShift.Value;
		set => _signalBarShift.Value = value;
	}

	/// <summary>
	/// Reserved phase parameter kept for compatibility with the original script.
	/// </summary>
	public int Phase
		{
		get => _phase.Value;
		set => _phase.Value = value;
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

		_histogramBuffer = Array.Empty<decimal>();
		_signalBuffer = Array.Empty<decimal>();
		_bufferIndex = 0;
		_bufferFilled = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
		{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 1m;
		if (_pointValue <= 0m)
			_pointValue = 1m;

		_priceAverage = CreateMovingAverage(SmoothingMethod, PrimaryLength);
		_firstSmoothing = CreateMovingAverage(SmoothingMethod, FirstSmoothingLength);
		_secondSmoothing = CreateMovingAverage(SmoothingMethod, SecondSmoothingLength);
		_signalSmoothing = CreateMovingAverage(SmoothingMethod, SignalLength);

		InitializeBuffers();

		Slippage = SlippagePoints * _pointValue;

		StartProtection(
			TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * _pointValue) : default,
			StopLossPoints > 0 ? new Unit(StopLossPoints * _pointValue) : default);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
		{
		if (candle.State != CandleStates.Finished)
			return;

		var price = SelectPrice(candle);
		var time = candle.CloseTime ?? candle.OpenTime;

		// Smooth the selected price to match the indicator baseline.
		var baseValue = _priceAverage.Process(new DecimalIndicatorValue(_priceAverage, price, time));
		if (!baseValue.IsFormed)
			return;

		var basePrice = baseValue.ToDecimal();
		var momentum = _pointValue != 0m ? (price - basePrice) / _pointValue : 0m;

		// Apply the first momentum smoothing stage.
		var firstValue = _firstSmoothing.Process(new DecimalIndicatorValue(_firstSmoothing, momentum, time));
		if (!firstValue.IsFormed)
			return;

		var first = firstValue.ToDecimal();

		// Build the histogram with the second smoothing stage.
		var secondValue = _secondSmoothing.Process(new DecimalIndicatorValue(_secondSmoothing, first, time));
		if (!secondValue.IsFormed)
			return;

		var histogram = secondValue.ToDecimal();

		// Smooth the histogram to generate the signal line.
		var signalValue = _signalSmoothing.Process(new DecimalIndicatorValue(_signalSmoothing, histogram, time));
		if (!signalValue.IsFormed)
			return;

		var signal = signalValue.ToDecimal();

		// Store values so that shifted comparisons work like in the MQL version.
		AddToBuffer(histogram, signal);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!TryGetHist(SignalBarShift, out var latestHist) || !TryGetHist(SignalBarShift + 1, out var previousHist))
			return;

		var currentPosition = Position;
		var buySignal = false;
		var sellSignal = false;

		switch (Mode)
		{
			case EntryMode.Breakdown:
			{
				buySignal = latestHist > 0m && previousHist <= 0m;
				sellSignal = latestHist < 0m && previousHist >= 0m;
				break;
			}

			case EntryMode.Twist:
			{
				if (!TryGetHist(SignalBarShift + 2, out var olderHist))
					return;

				buySignal = previousHist < latestHist && olderHist > previousHist;
				sellSignal = previousHist > latestHist && olderHist < previousHist;
				break;
			}

			case EntryMode.CloudTwist:
			{
				if (!TryGetSignal(SignalBarShift, out var latestSignal) || !TryGetSignal(SignalBarShift + 1, out var previousSignal))
					return;

				buySignal = latestHist > latestSignal && previousHist <= previousSignal;
				sellSignal = latestHist < latestSignal && previousHist >= previousSignal;
				break;
			}
		}

		if (buySignal)
		{
			ExecuteBuy(currentPosition);
		}
		else if (sellSignal)
		{
			ExecuteSell(currentPosition);
		}
	}

	private void ExecuteBuy(decimal currentPosition)
		{
		var volume = 0m;

		if (AllowShortExits && currentPosition < 0m)
			volume += Math.Abs(currentPosition);

		if (AllowLongEntries && (currentPosition <= 0m || (AllowShortExits && currentPosition < 0m)))
			volume += Volume;

		if (volume > 0m)
			BuyMarket(volume);
	}

	private void ExecuteSell(decimal currentPosition)
		{
		var volume = 0m;

		if (AllowLongExits && currentPosition > 0m)
			volume += Math.Abs(currentPosition);

		if (AllowShortEntries && (currentPosition >= 0m || (AllowLongExits && currentPosition > 0m)))
			volume += Volume;

		if (volume > 0m)
			SellMarket(volume);
	}

	private void InitializeBuffers()
		{
		var size = Math.Max(3, SignalBarShift + 3);
		_histogramBuffer = new decimal[size];
		_signalBuffer = new decimal[size];
		_bufferIndex = 0;
		_bufferFilled = 0;
	}

	private void AddToBuffer(decimal histogram, decimal signal)
		{
		if (_histogramBuffer.Length == 0)
			return;

		_histogramBuffer[_bufferIndex] = histogram;
		_signalBuffer[_bufferIndex] = signal;
		_bufferIndex = (_bufferIndex + 1) % _histogramBuffer.Length;
		if (_bufferFilled < _histogramBuffer.Length)
			_bufferFilled++;
	}

	private bool TryGetHist(int shift, out decimal value)
		{
		return TryGetBufferedValue(_histogramBuffer, shift, out value);
	}

	private bool TryGetSignal(int shift, out decimal value)
		{
		return TryGetBufferedValue(_signalBuffer, shift, out value);
	}

	private bool TryGetBufferedValue(decimal[] buffer, int shift, out decimal value)
		{
		value = default;

		if (shift < 0 || shift >= _bufferFilled)
			return false;

		var index = _bufferIndex - 1 - shift;
		if (index < 0)
			index += buffer.Length;

		value = buffer[index];
		return true;
	}

	private decimal SelectPrice(ICandleMessage candle)
		{
		return AppliedPrice switch
		{
	AppliedPrice.Open => candle.OpenPrice,
	AppliedPrice.High => candle.HighPrice,
	AppliedPrice.Low => candle.LowPrice,
	AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
	AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
	AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
	AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
	AppliedPrice.Quarter => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
	AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
	AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
	_ => candle.ClosePrice,
		};
	}

	private static IIndicator CreateMovingAverage(SmoothingMethod method, int length)
		{
		return method switch
		{
	SmoothingMethod.Simple => new SimpleMovingAverage { Length = length },
	SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = length },
	SmoothingMethod.Weighted => new WeightedMovingAverage { Length = length },
	_ => new ExponentialMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Entry confirmation modes replicated from the original expert advisor.
	/// </summary>
	public enum EntryMode
		{
	/// <summary>
	/// Histogram breaks above or below the zero line.
	/// </summary>
	Breakdown,

	/// <summary>
	/// Histogram changes slope direction.
	/// </summary>
	Twist,

	/// <summary>
	/// Histogram crosses the signal line.
	/// </summary>
	CloudTwist
	}

	/// <summary>
	/// Supported smoothing families.
	/// </summary>
	public enum SmoothingMethod
		{
	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Exponential,

	/// <summary>
	/// Simple moving average.
	/// </summary>
	Simple,

	/// <summary>
	/// Smoothed (RMA) moving average.
	/// </summary>
	Smoothed,

	/// <summary>
	/// Weighted moving average.
	/// </summary>
	Weighted
	}

	/// <summary>
	/// Applied price sources identical to the MetaTrader version.
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
	/// Median price (high + low) / 2.
	/// </summary>
	Median,

	/// <summary>
	/// Typical price (close + high + low) / 3.
	/// </summary>
	Typical,

	/// <summary>
	/// Weighted price (2 * close + high + low) / 4.
	/// </summary>
	Weighted,

	/// <summary>
	/// Simple price (open + close) / 2.
	/// </summary>
	Simple,

	/// <summary>
	/// Quarted price (open + high + low + close) / 4.
	/// </summary>
	Quarter,

	/// <summary>
	/// Trend-following price using candle extremes.
	/// </summary>
	TrendFollow0,

	/// <summary>
	/// Half-trend-following price.
	/// </summary>
	TrendFollow1
	}
}
