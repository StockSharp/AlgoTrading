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
using StockSharp.Algo;
using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader expert Exp_i-KlPrice_Vol.
/// It multiplies the KlPrice oscillator by volume and reacts to colour
/// transitions to emulate two independent position slots per direction.
/// </summary>
public class ExpIKlPriceVolStrategy : Strategy
{
	private readonly StrategyParam<decimal> _primaryVolume;
	private readonly StrategyParam<decimal> _secondaryVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SmoothMethods> _priceMaMethod;
	private readonly StrategyParam<int> _priceMaLength;
	private readonly StrategyParam<int> _priceMaPhase;
	private readonly StrategyParam<SmoothMethods> _rangeMaMethod;
	private readonly StrategyParam<int> _rangeMaLength;
	private readonly StrategyParam<int> _rangeMaPhase;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<AppliedPrices> _appliedPrice;
	private readonly StrategyParam<AppliedVolumes> _volumeType;
	private readonly StrategyParam<int> _highLevel2;
	private readonly StrategyParam<int> _highLevel1;
	private readonly StrategyParam<int> _lowLevel1;
	private readonly StrategyParam<int> _lowLevel2;
	private readonly StrategyParam<int> _signalBar;

	private KlPriceVolCalculator _calculator;

	private readonly List<ColorSample> _colorHistory = new();

	private DateTimeOffset? _lastLongPrimarySignalTime;
	private DateTimeOffset? _lastLongSecondarySignalTime;
	private DateTimeOffset? _lastShortPrimarySignalTime;
	private DateTimeOffset? _lastShortSecondarySignalTime;

	private bool _isLongPrimaryOpen;
	private bool _isLongSecondaryOpen;
	private bool _isShortPrimaryOpen;
	private bool _isShortSecondaryOpen;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpIKlPriceVolStrategy"/> class.
	/// </summary>
	public ExpIKlPriceVolStrategy()
	{
		_primaryVolume = Param(nameof(PrimaryVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Primary Volume", "Order volume dispatched by the first slot", "Trading");

		_secondaryVolume = Param(nameof(SecondaryVolume), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Secondary Volume", "Order volume dispatched by the second slot", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Protective stop in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Target distance in price steps", "Risk");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Enable closing long positions on bearish colours", "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Enable closing short positions on bullish colours", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used by the indicator", "General");

		_priceMaMethod = Param(nameof(PriceMaMethod), SmoothMethods.Sma)
			.SetDisplay("Price MA Method", "Moving average type used to smooth price", "Indicator");

		_priceMaLength = Param(nameof(PriceMaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Price MA Length", "Period of the price moving average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);

		_priceMaPhase = Param(nameof(PriceMaPhase), 15)
			.SetDisplay("Price MA Phase", "Phase parameter for Jurik style filters", "Indicator");

		_rangeMaMethod = Param(nameof(RangeMaMethod), SmoothMethods.Jjma)
			.SetDisplay("Range MA Method", "Moving average type used to smooth the candle range", "Indicator");

		_rangeMaLength = Param(nameof(RangeMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Range MA Length", "Period applied to the price range", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_rangeMaPhase = Param(nameof(RangeMaPhase), 100)
			.SetDisplay("Range MA Phase", "Phase parameter for the range smoother", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Length of the Jurik smoother applied to the oscillator", "Indicator");

		_appliedPrice = Param(nameof(AppliedPrices), AppliedPrices.Close)
			.SetDisplay("Applied Price", "Price input used in oscillator calculations", "Indicator");

		_volumeType = Param(nameof(VolumeType), AppliedVolumes.Tick)
			.SetDisplay("Volume Type", "Volume source multiplied by the oscillator", "Indicator");

		_highLevel2 = Param(nameof(HighLevel2), 150)
			.SetDisplay("High Level 2", "Upper extreme multiplied by smoothed volume", "Indicator");

		_highLevel1 = Param(nameof(HighLevel1), 20)
			.SetDisplay("High Level 1", "Upper moderate threshold", "Indicator");

		_lowLevel1 = Param(nameof(LowLevel1), -20)
			.SetDisplay("Low Level 1", "Lower moderate threshold", "Indicator");

		_lowLevel2 = Param(nameof(LowLevel2), -150)
			.SetDisplay("Low Level 2", "Lower extreme multiplied by smoothed volume", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Shift applied before evaluating colour transitions", "Trading");
	}

	/// <summary>
	/// Volume dispatched by the first slot.
	/// </summary>
	public decimal PrimaryVolume
	{
		get => _primaryVolume.Value;
		set => _primaryVolume.Value = value;
	}

	/// <summary>
	/// Volume dispatched by the second slot.
	/// </summary>
	public decimal SecondaryVolume
	{
		get => _secondaryVolume.Value;
		set => _secondaryVolume.Value = value;
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
	/// Enable or disable opening long positions.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Enable or disable opening short positions.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Enable or disable closing long positions on bearish colours.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Enable or disable closing short positions on bullish colours.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
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
	/// Moving average method used for price smoothing.
	/// </summary>
	public SmoothMethods PriceMaMethod
	{
		get => _priceMaMethod.Value;
		set => _priceMaMethod.Value = value;
	}

	/// <summary>
	/// Period of the price moving average.
	/// </summary>
	public int PriceMaLength
	{
		get => _priceMaLength.Value;
		set => _priceMaLength.Value = value;
	}

	/// <summary>
	/// Phase parameter applied to the price moving average.
	/// </summary>
	public int PriceMaPhase
	{
		get => _priceMaPhase.Value;
		set => _priceMaPhase.Value = value;
	}

	/// <summary>
	/// Moving average method applied to the candle range.
	/// </summary>
	public SmoothMethods RangeMaMethod
	{
		get => _rangeMaMethod.Value;
		set => _rangeMaMethod.Value = value;
	}

	/// <summary>
	/// Period applied to the candle range.
	/// </summary>
	public int RangeMaLength
	{
		get => _rangeMaLength.Value;
		set => _rangeMaLength.Value = value;
	}

	/// <summary>
	/// Phase parameter for the range smoother.
	/// </summary>
	public int RangeMaPhase
	{
		get => _rangeMaPhase.Value;
		set => _rangeMaPhase.Value = value;
	}

	/// <summary>
	/// Length of the Jurik smoother applied to oscillator values.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Price input used by the oscillator.
	/// </summary>
	public AppliedPrices AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Volume source multiplied by the oscillator output.
	/// </summary>
	public AppliedVolumes VolumeType
	{
		get => _volumeType.Value;
		set => _volumeType.Value = value;
	}

	/// <summary>
	/// Upper extreme level expressed in volume units.
	/// </summary>
	public int HighLevel2
	{
		get => _highLevel2.Value;
		set => _highLevel2.Value = value;
	}

	/// <summary>
	/// Upper moderate level expressed in volume units.
	/// </summary>
	public int HighLevel1
	{
		get => _highLevel1.Value;
		set => _highLevel1.Value = value;
	}

	/// <summary>
	/// Lower moderate level expressed in volume units.
	/// </summary>
	public int LowLevel1
	{
		get => _lowLevel1.Value;
		set => _lowLevel1.Value = value;
	}

	/// <summary>
	/// Lower extreme level expressed in volume units.
	/// </summary>
	public int LowLevel2
	{
		get => _lowLevel2.Value;
		set => _lowLevel2.Value = value;
	}

	/// <summary>
	/// Shift applied before reading colour transitions.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
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

		_calculator?.Reset();
		_colorHistory.Clear();
		_lastLongPrimarySignalTime = null;
		_lastLongSecondarySignalTime = null;
		_lastShortPrimarySignalTime = null;
		_lastShortSecondarySignalTime = null;
		_isLongPrimaryOpen = false;
		_isLongSecondaryOpen = false;
		_isShortPrimaryOpen = false;
		_isShortSecondaryOpen = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_calculator = new KlPriceVolCalculator(
			PriceMaMethod,
			PriceMaLength,
			PriceMaPhase,
			RangeMaMethod,
			RangeMaLength,
			RangeMaPhase,
			SmoothingLength,
			AppliedPrice,
			VolumeType,
			HighLevel2,
			HighLevel1,
			LowLevel1,
			LowLevel2);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		var stopLoss = StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Absolute) : null;
		var takeProfit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * step, UnitTypes.Absolute) : null;

		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(stopLoss: stopLoss, takeProfit: takeProfit, useMarketOrders: true);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_calculator is null)
			return;

		var result = _calculator.Process(candle);
		if (result is null)
			return;

		var signalTime = GetSignalTime(candle);
		AddColorSample(new ColorSample(signalTime, result.Value, result.Volume, result.Color));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var (currentColor, previousColor, colorTime) = GetSignalContext();
		if (currentColor is null || previousColor is null || colorTime is null)
			return;

		var openLongPrimary = AllowLongEntry && previousColor == 4 && currentColor < 4;
		var openLongSecondary = AllowLongEntry && previousColor == 3 && currentColor < 3;
		var openShortPrimary = AllowShortEntry && previousColor == 0 && currentColor > 0;
		var openShortSecondary = AllowShortEntry && previousColor == 1 && currentColor > 1;
		var closeLong = AllowLongExit && (previousColor == 0 || previousColor == 1);
		var closeShort = AllowShortExit && (previousColor == 4 || previousColor == 3);

		if (closeLong && Position > 0)
		{
			ClosePosition();
			_isLongPrimaryOpen = false;
			_isLongSecondaryOpen = false;
			_lastLongPrimarySignalTime = null;
			_lastLongSecondarySignalTime = null;
		}

		if (closeShort && Position < 0)
		{
			ClosePosition();
			_isShortPrimaryOpen = false;
			_isShortSecondaryOpen = false;
			_lastShortPrimarySignalTime = null;
			_lastShortSecondarySignalTime = null;
		}

		if (openLongPrimary && !_isLongPrimaryOpen && _lastLongPrimarySignalTime != colorTime)
		{
			var volume = PrimaryVolume;
			if (volume > 0m)
			{
				BuyMarket(volume);
				_isLongPrimaryOpen = true;
				_lastLongPrimarySignalTime = colorTime;
			}
		}

		if (openLongSecondary && !_isLongSecondaryOpen && _lastLongSecondarySignalTime != colorTime)
		{
			var volume = SecondaryVolume;
			if (volume > 0m)
			{
				BuyMarket(volume);
				_isLongSecondaryOpen = true;
				_lastLongSecondarySignalTime = colorTime;
			}
		}

		if (openShortPrimary && !_isShortPrimaryOpen && _lastShortPrimarySignalTime != colorTime)
		{
			var volume = PrimaryVolume;
			if (volume > 0m)
			{
				SellMarket(volume);
				_isShortPrimaryOpen = true;
				_lastShortPrimarySignalTime = colorTime;
			}
		}

		if (openShortSecondary && !_isShortSecondaryOpen && _lastShortSecondarySignalTime != colorTime)
		{
			var volume = SecondaryVolume;
			if (volume > 0m)
			{
				SellMarket(volume);
				_isShortSecondaryOpen = true;
				_lastShortSecondarySignalTime = colorTime;
			}
		}
	}

	private DateTimeOffset GetSignalTime(ICandleMessage candle)
	{
		var timeFrame = CandleType.Arg is TimeSpan span ? span : TimeSpan.Zero;
		var closeTime = candle.CloseTime ?? candle.OpenTime + timeFrame;
		return closeTime;
	}

	private (int? current, int? previous, DateTimeOffset? time) GetSignalContext()
	{
		if (SignalBar < 0)
			return (null, null, null);

		var index = _colorHistory.Count - 1 - SignalBar;
		if (index < 0 || index >= _colorHistory.Count)
			return (null, null, null);

		var previousIndex = index - 1;
		if (previousIndex < 0)
			return (null, null, null);

		var currentSample = _colorHistory[index];
		var previousSample = _colorHistory[previousIndex];

		return (currentSample.Color, previousSample.Color, currentSample.Time);
	}

	private void AddColorSample(ColorSample sample)
	{
		_colorHistory.Add(sample);

		const int maxItems = 2048;
		if (_colorHistory.Count > maxItems)
			_colorHistory.RemoveRange(0, _colorHistory.Count - maxItems);
	}

	private readonly struct ColorSample
	{
		public ColorSample(DateTimeOffset time, decimal value, decimal volume, int color)
		{
			Time = time;
			Value = value;
			Volume = volume;
			Color = color;
		}

		public DateTimeOffset Time { get; }

		public decimal Value { get; }

		public decimal Volume { get; }

		public int Color { get; }
	}

	/// <summary>
	/// Volume source applied to the oscillator.
	/// </summary>
	public enum AppliedVolumes
	{
		/// <summary>
		/// Multiply by tick volume.
		/// </summary>
		Tick,

		/// <summary>
		/// Multiply by real (exchange) volume.
		/// </summary>
		Real,
	}

	/// <summary>
	/// Price source used in calculations.
	/// </summary>
	public enum AppliedPrices
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
		/// Simple price ((Open + Close)/2).
		/// </summary>
		Simple,

		/// <summary>
		/// Quarted price (HLOC/4).
		/// </summary>
		Quarter,

		/// <summary>
		/// TrendFollow 0 price.
		/// </summary>
		TrendFollow0,

		/// <summary>
		/// TrendFollow 1 price.
		/// </summary>
		TrendFollow1,

		/// <summary>
		/// Demark price.
		/// </summary>
		Demark,
	}

	/// <summary>
	/// Moving average methods supported by the calculator.
	/// </summary>
	public enum SmoothMethods
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
		/// Jurik moving average (JJMA).
		/// </summary>
		Jjma,

		/// <summary>
		/// Jurik moving average (JurX variant).
		/// </summary>
		JurX,

		/// <summary>
		/// Parabolic moving average approximation.
		/// </summary>
		ParMa,

		/// <summary>
		/// Triple exponential moving average (T3).
		/// </summary>
		T3,

		/// <summary>
		/// VIDYA adaptive moving average.
		/// </summary>
		Vidya,

		/// <summary>
		/// Kaufman adaptive moving average.
		/// </summary>
		Ama,
	}

	private sealed class KlPriceVolCalculator
	{
		private readonly LengthIndicator<decimal> _priceMa;
		private readonly LengthIndicator<decimal> _rangeMa;
		private readonly LengthIndicator<decimal> _valueSmoother;
		private readonly LengthIndicator<decimal> _volumeSmoother;
		private readonly AppliedPrices _appliedPrice;
		private readonly AppliedVolumes _volumeType;
		private readonly decimal _highLevel2;
		private readonly decimal _highLevel1;
		private readonly decimal _lowLevel1;
		private readonly decimal _lowLevel2;

		public KlPriceVolCalculator(
			SmoothMethods priceMethod,
			int priceLength,
			int pricePhase,
			SmoothMethods rangeMethod,
			int rangeLength,
			int rangePhase,
			int smoothingLength,
			AppliedPrices appliedPrice,
			AppliedVolumes volumeType,
			int highLevel2,
			int highLevel1,
			int lowLevel1,
			int lowLevel2)
		{
			var priceLen = Math.Max(1, priceLength);
			var rangeLen = Math.Max(1, rangeLength);
			var smoothLen = Math.Max(1, smoothingLength);

			_priceMa = CreateSmoother(priceMethod, priceLen, pricePhase);
			_rangeMa = CreateSmoother(rangeMethod, rangeLen, rangePhase);
			_valueSmoother = CreateSmoother(SmoothMethods.Jjma, smoothLen, 100);
			_volumeSmoother = CreateSmoother(SmoothMethods.Jjma, smoothLen, 100);
			_appliedPrice = appliedPrice;
			_volumeType = volumeType;
			_highLevel2 = highLevel2;
			_highLevel1 = highLevel1;
			_lowLevel1 = lowLevel1;
			_lowLevel2 = lowLevel2;
		}

		public void Reset()
		{
			_priceMa.Reset();
			_rangeMa.Reset();
			_valueSmoother.Reset();
			_volumeSmoother.Reset();
		}

		public KlPriceVolResult? Process(ICandleMessage candle)
		{
			var time = candle.CloseTime ?? candle.OpenTime;
			var price = GetAppliedPrice(candle, _appliedPrice);
			var priceValue = _priceMa.Process(price, time, true).ToNullableDecimal();
			if (priceValue is null)
				return null;

			var range = candle.HighPrice - candle.LowPrice;
			var rangeValue = _rangeMa.Process(range, time, true).ToNullableDecimal();
			if (rangeValue is null || rangeValue.Value == 0m)
				return null;

			var dwband = priceValue.Value - rangeValue.Value;
			var oscillator = 100m * (price - dwband) / (2m * rangeValue.Value) - 50m;

			var volume = GetVolume(candle);
			var scaled = oscillator * volume;

			var smoothedValue = _valueSmoother.Process(scaled, time, true).ToNullableDecimal();
			var smoothedVolume = _volumeSmoother.Process(volume, time, true).ToNullableDecimal();

			if (smoothedValue is null || smoothedVolume is null)
				return null;

			var color = DetermineColor(smoothedValue.Value, smoothedVolume.Value);
			return new KlPriceVolResult(smoothedValue.Value, smoothedVolume.Value, color);
		}

		private int DetermineColor(decimal value, decimal volume)
		{
			var maxLevel = _highLevel2 * volume;
			var upLevel = _highLevel1 * volume;
			var downLevel = _lowLevel1 * volume;
			var minLevel = _lowLevel2 * volume;

			if (value > maxLevel)
				return 4;

			if (value > upLevel)
				return 3;

			if (value < minLevel)
				return 0;

			if (value < downLevel)
				return 1;

			return 2;
		}

		private decimal GetVolume(ICandleMessage candle)
		{
			return _volumeType switch
			{
				AppliedVolumes.Tick => candle.TotalTicks.HasValue ? (decimal)candle.TotalTicks.Value : candle.TotalVolume ?? 0m,
				AppliedVolumes.Real => candle.TotalVolume ?? (candle.TotalTicks.HasValue ? (decimal)candle.TotalTicks.Value : 0m),
				_ => candle.TotalVolume ?? 0m,
			};
		}

		private static LengthIndicator<decimal> CreateSmoother(SmoothMethods method, int length, int phase)
		{
			switch (method)
			{
				case SmoothMethods.Sma:
					return new SimpleMovingAverage { Length = length };
				case SmoothMethods.Ema:
					return new ExponentialMovingAverage { Length = length };
				case SmoothMethods.Smma:
					return new SmoothedMovingAverage { Length = length };
				case SmoothMethods.Lwma:
					return new WeightedMovingAverage { Length = length };
				case SmoothMethods.Jjma:
					return CreateJurik(length, phase);
				case SmoothMethods.JurX:
					return CreateJurik(length, phase);
				case SmoothMethods.ParMa:
					return new ExponentialMovingAverage { Length = length };
				case SmoothMethods.T3:
					return new TripleExponentialMovingAverage { Length = length };
				case SmoothMethods.Vidya:
					return new ExponentialMovingAverage { Length = length };
				case SmoothMethods.Ama:
					return new KaufmanAdaptiveMovingAverage { Length = length };
				default:
					return new SimpleMovingAverage { Length = length };
			}
		}

		private static LengthIndicator<decimal> CreateJurik(int length, int phase)
		{
			var jurik = new JurikMovingAverage { Length = length };
			var property = jurik.GetType().GetProperty("Phase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				var value = Math.Max(-100, Math.Min(100, phase));
				property.SetValue(jurik, value);
			}

			return jurik;
		}

		private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrices price)
		{
			return price switch
			{
				AppliedPrices.Close => candle.ClosePrice,
				AppliedPrices.Open => candle.OpenPrice,
				AppliedPrices.High => candle.HighPrice,
				AppliedPrices.Low => candle.LowPrice,
				AppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
				AppliedPrices.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
				AppliedPrices.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
				AppliedPrices.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
				AppliedPrices.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
				AppliedPrices.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
					? candle.HighPrice
					: candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
				AppliedPrices.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
					? (candle.HighPrice + candle.ClosePrice) / 2m
					: candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
				AppliedPrices.Demark => GetDemarkPrice(candle),
				_ => candle.ClosePrice,
			};
		}

		private static decimal GetDemarkPrice(ICandleMessage candle)
		{
			var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;
			if (candle.ClosePrice < candle.OpenPrice)
				res = (res + candle.LowPrice) / 2m;
			else if (candle.ClosePrice > candle.OpenPrice)
				res = (res + candle.HighPrice) / 2m;
			else
				res = (res + candle.ClosePrice) / 2m;

			return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
		}
	}

	private readonly record struct KlPriceVolResult(decimal Value, decimal Volume, int Color);
}

