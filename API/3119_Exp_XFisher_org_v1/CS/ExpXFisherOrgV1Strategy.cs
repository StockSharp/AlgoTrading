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

using System.Reflection;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 5 expert Exp_XFisher_org_v1.
/// Detects turning points of the Fisher transform smoothed with a configurable average.
/// Opens counter-trend trades when the Fisher value reverses direction between consecutive bars.
/// </summary>
public class ExpXFisherOrgV1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _phase;
	private readonly StrategyParam<XfisherSmoothingMethods> _smoothingMethod;
	private readonly StrategyParam<XfisherAppliedPrices> _priceType;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxHistory;

	private XFisherOrgIndicator _indicator = null!;
	private readonly List<decimal> _fisherHistory = new();

	/// <summary>
	/// Trading volume per order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpenAllowed
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpenAllowed
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyCloseAllowed
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellCloseAllowed
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Number of closed bars between the current moment and the signal candle.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Period for calculating the highest and lowest prices.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Smoothing length applied to the Fisher transform output.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Phase parameter forwarded to Jurik smoothing (ignored by other methods).
	/// </summary>
	public int Phase
	{
		get => _phase.Value;
		set => _phase.Value = value;
	}

	/// <summary>
	/// Smoothing method used for the Fisher output.
	/// </summary>
	public XfisherSmoothingMethods SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Price source used in the Fisher transform.
	/// </summary>
	public XfisherAppliedPrices PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
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
	/// Maximum number of Fisher readings cached for reversal detection.
	/// </summary>
	public int MaxHistory
	{
		get => _maxHistory.Value;
		set => _maxHistory.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ExpXFisherOrgV1Strategy"/>.
	/// </summary>
	public ExpXFisherOrgV1Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume sent with each market order", "Trading");

		_buyOpen = Param(nameof(BuyOpenAllowed), true)
			.SetDisplay("Allow Long Entries", "Enable opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpenAllowed), true)
			.SetDisplay("Allow Short Entries", "Enable opening short positions", "Trading");

		_buyClose = Param(nameof(BuyCloseAllowed), true)
			.SetDisplay("Allow Long Exits", "Enable closing existing long positions", "Trading");

		_sellClose = Param(nameof(SellCloseAllowed), true)
			.SetDisplay("Allow Short Exits", "Enable closing existing short positions", "Trading");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Lookback shift for the signal candle", "Parameters");

		_length = Param(nameof(Length), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fisher Length", "Period used to search highs and lows", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 25, 2);

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Period of the smoothing average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_phase = Param(nameof(Phase), 15)
			.SetDisplay("Phase", "Phase argument for Jurik smoothing", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(-50, 50, 5);

		_smoothingMethod = Param(nameof(SmoothingMethod), XfisherSmoothingMethods.Jjma)
			.SetDisplay("Smoothing Method", "Moving average applied to Fisher", "Indicators");

		_priceType = Param(nameof(PriceType), XfisherAppliedPrices.Close)
			.SetDisplay("Applied Price", "Price source forwarded to the indicator", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");

		_maxHistory = Param(nameof(MaxHistory), 1024)
			.SetGreaterThanZero()
			.SetDisplay("History Size", "Maximum number of cached Fisher values", "Advanced");
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
		_fisherHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Apply the configured order volume to StockSharp base class.
		Volume = OrderVolume;

		// Recreate the helper indicator with the latest parameter values.
		_indicator = new XFisherOrgIndicator
		{
			Length = Length,
			SmoothingLength = SmoothingLength,
			Phase = Phase,
			SmoothingMethod = SmoothingMethod,
			PriceType = PriceType,
		};

		var subscription = SubscribeCandles(CandleType);
		// Attach the indicator to the candle subscription and process updates on completion.
		subscription.BindEx(_indicator, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (indicatorValue is not XFisherOrgValue fisherValue || !fisherValue.HasValue)
			return;

		// Keep a compact rolling history to emulate the MT5 buffer shifts.
		_fisherHistory.Add(fisherValue.Fisher);
		if (_fisherHistory.Count > MaxHistory)
			_fisherHistory.RemoveAt(0);

		// The MT5 expert requires SignalBar, SignalBar+1 and SignalBar+2, hence +3 values in total.
		var required = SignalBar + 3;
		if (_fisherHistory.Count < required)
			return;

		var ind0 = _fisherHistory[^ (SignalBar + 1)];
		var ind1 = _fisherHistory[^ (SignalBar + 2)];
		var sign0 = ind1;
		var sign1 = _fisherHistory[^ (SignalBar + 3)];

		// Detect turning points exactly as the original buffers comparison.
		var buySignal = ind1 > sign1 && ind0 <= sign0;
		var sellSignal = ind1 < sign1 && ind0 >= sign0;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (buySignal)
		{
			// Close shorts first to stay in sync with the MT5 helper functions.
			if (SellCloseAllowed && Position < 0)
				BuyMarket(Math.Abs(Position));

			// Then open or flip into a long position.
			if (BuyOpenAllowed && Position <= 0)
				BuyMarket(CalculateEntryVolume(1));
		}

		if (sellSignal)
		{
			// Close longs before considering a new short position.
			if (BuyCloseAllowed && Position > 0)
				SellMarket(Position);

			// Then open or flip into a short position.
			if (SellOpenAllowed && Position >= 0)
				SellMarket(CalculateEntryVolume(-1));
		}
	}

	private decimal CalculateEntryVolume(int direction)
	{
		var volume = Volume;

		// When flipping the position add the absolute value of the existing exposure.
		if (direction > 0 && Position < 0)
			volume += Math.Abs(Position);
		else if (direction < 0 && Position > 0)
			volume += Position;

		return volume;
	}

	public enum XfisherSmoothingMethods
	{
		Sma,
		Ema,
		Smma,
		Lwma,
		Jjma,
		Jurx,
		Parabolic,
		T3,
		Vidya,
		Ama,
	}

	public enum XfisherAppliedPrices
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simple,
		Quarter,
		TrendFollow0,
		TrendFollow1,
		Demark,
	}
}

/// <summary>
/// Fisher transform with configurable smoothing and price source.
/// Returns both the smoothed value and its one-bar delayed signal line.
/// </summary>
public sealed class XFisherOrgIndicator : Indicator<ICandleMessage>
{
	private readonly Highest _highest = new();
	private readonly Lowest _lowest = new();

	private IIndicator _smoother;
	private decimal _valuePrev;
	private decimal _fishPrev;
	private decimal? _previousSmoothed;
	private bool _initialized;

	/// <summary>
	/// Range length for high/low calculations.
	/// </summary>
	public int Length { get; set; } = 7;

	/// <summary>
	/// Length of the smoothing moving average.
	/// </summary>
	public int SmoothingLength { get; set; } = 5;

	/// <summary>
	/// Phase parameter used by Jurik smoothing.
	/// </summary>
	public int Phase { get; set; } = 15;

	/// <summary>
	/// Type of moving average applied to the Fisher output.
	/// </summary>
	public XfisherSmoothingMethods SmoothingMethod { get; set; } = XfisherSmoothingMethods.Jjma;

	/// <summary>
	/// Price selection mode.
	/// </summary>
	public XfisherAppliedPrices PriceType { get; set; } = XfisherAppliedPrices.Close;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not CandleIndicatorValue candleValue)
			throw new ArgumentException("XFisherOrgIndicator expects candle input.", nameof(input));

		var candle = candleValue.Value;

		// Initialise smoothing helpers on the very first candle.
		if (!_initialized)
		{
			_highest.Length = Math.Max(1, Length);
			_lowest.Length = Math.Max(1, Length);
			_smoother = CreateSmoother();
			_initialized = true;
		}
		else
		{
			_highest.Length = Math.Max(1, Length);
			_lowest.Length = Math.Max(1, Length);
		}

		// Feed the candle highs and lows into rolling extremum indicators.
		var highValue = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, candle.OpenTime));
		var lowValue = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, candle.OpenTime));

		if (!highValue.IsFinal || !lowValue.IsFinal)
			// Not enough history yet, propagate an empty value.
			return new XFisherOrgValue(this, input, null, null, false);

		var max = highValue.ToDecimal();
		var min = lowValue.ToDecimal();
		var range = max - min;
		if (range == 0m)
			range = 0.0000001m;

		// Recreate the MT5 recursion on the chosen price series.
		var price = SelectPrice(candle);
		var wpr = (price - min) / range;

		var value = (wpr - 0.5m) + 0.67m * _valuePrev;
		value = Math.Min(Math.Max(value, -0.999999m), 0.999999m);

		var denominator = 1m - value;
		if (denominator == 0m)
			denominator = 1m;

		var ratio = (1m + value) / denominator;
		if (ratio < 0.0000001m)
			ratio = 1m;

		var fish = 0.5m * (decimal)Math.Log((double)ratio) + 0.5m * _fishPrev;

		_valuePrev = value;
		_fishPrev = fish;

		// Pass the Fisher transform through the selected smoother.
		var smoothValue = _smoother!.Process(new DecimalIndicatorValue(_smoother, fish, candle.OpenTime));
		if (!smoothValue.IsFinal)
			return new XFisherOrgValue(this, input, null, null, false);

		var fisher = smoothValue.ToDecimal();
		var signal = _previousSmoothed ?? 0m;
		_previousSmoothed = fisher;

		// Notify the base class once the smoothing window is fully populated.
		if (_smoother.IsFormed)
			SetFormed();

		return new XFisherOrgValue(this, input, fisher, signal, _smoother.IsFormed);
	}

	/// <summary>
	/// Creates the smoothing indicator mapped from the original MQL options.
	/// </summary>
	private IIndicator CreateSmoother()
	{
		var length = Math.Max(1, SmoothingLength);

		// Map the original smoothing options to StockSharp equivalents.
		return SmoothingMethod switch
		{
			XfisherSmoothingMethods.Sma => new SimpleMovingAverage { Length = length },
			XfisherSmoothingMethods.Ema => new ExponentialMovingAverage { Length = length },
			XfisherSmoothingMethods.Smma => new SmoothedMovingAverage { Length = length },
			XfisherSmoothingMethods.Lwma => new WeightedMovingAverage { Length = length },
			XfisherSmoothingMethods.Jjma or XfisherSmoothingMethods.Jurx or XfisherSmoothingMethods.T3 => CreateJurik(length),
			XfisherSmoothingMethods.Vidya or XfisherSmoothingMethods.Ama => new KaufmanAdaptiveMovingAverage { Length = length },
			XfisherSmoothingMethods.Parabolic => new ExponentialMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private IIndicator CreateJurik(int length)
	{
		var jurik = new JurikMovingAverage
		{
			Length = length,
			Phase = Phase,
		};

		return jurik;
	}

	private decimal SelectPrice(ICandleMessage candle)
	{
		// Match the PriceSeries helper from SmoothAlgorithms.mqh.
		return PriceType switch
		{
			XfisherAppliedPrices.Close => candle.ClosePrice,
			XfisherAppliedPrices.Open => candle.OpenPrice,
			XfisherAppliedPrices.High => candle.HighPrice,
			XfisherAppliedPrices.Low => candle.LowPrice,
			XfisherAppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			XfisherAppliedPrices.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			XfisherAppliedPrices.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			XfisherAppliedPrices.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			XfisherAppliedPrices.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			XfisherAppliedPrices.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice :
				candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			XfisherAppliedPrices.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m :
				candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			XfisherAppliedPrices.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var sum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
			sum = (sum + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			sum = (sum + candle.HighPrice) / 2m;
		else
			sum = (sum + candle.ClosePrice) / 2m;

		return ((sum - candle.LowPrice) + (sum - candle.HighPrice)) / 2m;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_highest.Reset();
		_lowest.Reset();
		_smoother = null;
		_valuePrev = 0m;
		_fishPrev = 0m;
		_previousSmoothed = null;
		_initialized = false;
	}
}

/// <summary>
/// Container for the Fisher value and its delayed signal line.
/// </summary>
public sealed class XFisherOrgValue : ComplexIndicatorValue
{
	public XFisherOrgValue(IIndicator indicator, IIndicatorValue input, decimal? fisher, decimal? signal, bool formed)
		: base(indicator, input,
			(nameof(Fisher), fisher ?? 0m),
			(nameof(Signal), signal ?? 0m))
	{
		HasValue = fisher.HasValue && signal.HasValue;
		IsIndicatorFormed = formed && HasValue;
	}

	/// <summary>
	/// Indicates whether the value is ready for trading logic.
	/// </summary>
	public bool HasValue { get; }

	/// <summary>
	/// Shows if the internal smoother is fully formed.
	/// </summary>
	public bool IsIndicatorFormed { get; }

	/// <summary>
	/// Current Fisher transform value.
	/// </summary>
	public decimal Fisher => (decimal)GetValue(nameof(Fisher));

	/// <summary>
	/// One-bar delayed Fisher value acting as a signal line.
	/// </summary>
	public decimal Signal => (decimal)GetValue(nameof(Signal));
}
