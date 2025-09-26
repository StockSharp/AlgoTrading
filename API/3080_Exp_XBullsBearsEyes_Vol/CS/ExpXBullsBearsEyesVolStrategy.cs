using System;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader expert Exp_XBullsBearsEyes_Vol.
/// It recreates the Bulls/Bears pressure indicator that multiplies trend
/// strength by the candle volume and uses the colour transitions to drive
/// entries and exits while supporting two independent position slots per side.
	/// </summary>
public class ExpXBullsBearsEyesVolStrategy : Strategy
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

	private readonly StrategyParam<int> _indicatorPeriod;
	private readonly StrategyParam<decimal> _gamma;
	private readonly StrategyParam<AppliedVolume> _volumeType;
	private readonly StrategyParam<int> _highLevel2;
	private readonly StrategyParam<int> _highLevel1;
	private readonly StrategyParam<int> _lowLevel1;
	private readonly StrategyParam<int> _lowLevel2;
	private readonly StrategyParam<SmoothMethod> _smoothMethod;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<int> _smoothPhase;
	private readonly StrategyParam<int> _signalBar;

	private XBullsBearsEyesVolCalculator _indicator;

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
	/// Initializes a new instance of the <see cref="ExpXBullsBearsEyesVolStrategy"/> class.
	/// </summary>
	public ExpXBullsBearsEyesVolStrategy()
	{
		_primaryVolume = Param(nameof(PrimaryVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading");

		_secondaryVolume = Param(nameof(SecondaryVolume), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("Secondary Volume", "Order volume used by the second long/short slot", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Take Profit (points)", "Target distance expressed in price steps", "Risk");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
		.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
		.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
		.SetDisplay("Allow Long Exit", "Enable closing long positions on bearish colours", "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
		.SetDisplay("Allow Short Exit", "Enable closing short positions on bullish colours", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used by the indicator and trading signals", "General");

		_indicatorPeriod = Param(nameof(IndicatorPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Indicator Period", "EMA period used by Bulls/Bears power", "Indicator");

		_gamma = Param(nameof(Gamma), 0.6m)
		.SetDisplay("Gamma", "Adaptive smoothing factor used by the four-stage filter", "Indicator");

		_volumeType = Param(nameof(VolumeType), AppliedVolume.Tick)
		.SetDisplay("Volume Type", "Volume source multiplied by the indicator", "Indicator");

		_highLevel2 = Param(nameof(HighLevel2), 25)
		.SetDisplay("High Level 2", "Upper level that marks strong bullish pressure", "Indicator");

		_highLevel1 = Param(nameof(HighLevel1), 10)
		.SetDisplay("High Level 1", "Upper level that marks moderate bullish pressure", "Indicator");

		_lowLevel1 = Param(nameof(LowLevel1), -10)
		.SetDisplay("Low Level 1", "Lower level that marks moderate bearish pressure", "Indicator");

		_lowLevel2 = Param(nameof(LowLevel2), -25)
		.SetDisplay("Low Level 2", "Lower level that marks strong bearish pressure", "Indicator");

		_smoothMethod = Param(nameof(SmoothingMethod), SmoothMethod.Sma)
		.SetDisplay("Smoothing Method", "Moving average used for indicator smoothing", "Indicator");

		_smoothLength = Param(nameof(SmoothingLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "Length of the smoothing filter", "Indicator");

		_smoothPhase = Param(nameof(SmoothingPhase), 15)
		.SetDisplay("Smoothing Phase", "Phase parameter for Jurik based smoothing", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Signal Bar", "Shift applied before evaluating colour transitions", "Trading");
	}

	/// <summary>
	/// Volume used by the first long/short slot.
	/// </summary>
	public decimal PrimaryVolume
	{
		get => _primaryVolume.Value;
		set => _primaryVolume.Value = value;
	}

	/// <summary>
	/// Volume used by the second long/short slot.
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
	/// EMA period used by Bulls/Bears power calculations.
	/// </summary>
	public int IndicatorPeriod
	{
		get => _indicatorPeriod.Value;
		set => _indicatorPeriod.Value = value;
	}

	/// <summary>
	/// Adaptive smoothing factor used by the internal filter.
	/// </summary>
	public decimal Gamma
	{
		get => _gamma.Value;
		set => _gamma.Value = value;
	}

	/// <summary>
	/// Volume source multiplied by the indicator output.
	/// </summary>
	public AppliedVolume VolumeType
	{
		get => _volumeType.Value;
		set => _volumeType.Value = value;
	}

	/// <summary>
	/// Upper level that marks strong bullish pressure.
	/// </summary>
	public int HighLevel2
	{
		get => _highLevel2.Value;
		set => _highLevel2.Value = value;
	}

	/// <summary>
	/// Upper level that marks moderate bullish pressure.
	/// </summary>
	public int HighLevel1
	{
		get => _highLevel1.Value;
		set => _highLevel1.Value = value;
	}

	/// <summary>
	/// Lower level that marks moderate bearish pressure.
	/// </summary>
	public int LowLevel1
	{
		get => _lowLevel1.Value;
		set => _lowLevel1.Value = value;
	}

	/// <summary>
	/// Lower level that marks strong bearish pressure.
	/// </summary>
	public int LowLevel2
	{
		get => _lowLevel2.Value;
		set => _lowLevel2.Value = value;
	}

	/// <summary>
	/// Moving average used for indicator smoothing.
	/// </summary>
	public SmoothMethod SmoothingMethod
	{
		get => _smoothMethod.Value;
		set => _smoothMethod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing filter.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Phase parameter for Jurik based smoothing.
	/// </summary>
	public int SmoothingPhase
	{
		get => _smoothPhase.Value;
		set => _smoothPhase.Value = value;
	}

	/// <summary>
	/// Shift applied before evaluating colour transitions.
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

		_indicator?.Reset();
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

		_indicator = new XBullsBearsEyesVolCalculator(
			IndicatorPeriod,
			Gamma,
			VolumeType,
			HighLevel2,
			HighLevel1,
			LowLevel1,
			LowLevel2,
			SmoothingMethod,
			SmoothingLength,
			SmoothingPhase);

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

		if (_indicator is null)
			return;

		var result = _indicator.Process(candle);
		if (result is null)
			return;

		var signalTime = GetSignalTime(candle);
		AddColorSample(new ColorSample(signalTime, result.Value, result.Volume, result.Color));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var (currentColor, previousColor, colorTime) = GetSignalContext();
		if (currentColor is null || previousColor is null || colorTime is null)
			return;

		var openLongPrimary = false;
		var openLongSecondary = false;
		var openShortPrimary = false;
		var openShortSecondary = false;
		var closeLong = false;
		var closeShort = false;

		if (currentColor == 1)
		{
			if (AllowLongEntry && previousColor > 1)
				openLongPrimary = true;

			if (AllowShortExit)
				closeShort = true;
		}

		if (currentColor == 0)
		{
			if (AllowLongEntry && previousColor > 0)
				openLongSecondary = true;

			if (AllowShortExit)
				closeShort = true;
		}

		if (currentColor == 3)
		{
			if (AllowShortEntry && previousColor < 3)
				openShortPrimary = true;

			if (AllowLongExit)
				closeLong = true;
		}

		if (currentColor == 4)
		{
			if (AllowShortEntry && previousColor < 4)
				openShortSecondary = true;

			if (AllowLongExit)
				closeLong = true;
		}

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

		const int maxItems = 1024;
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
	/// Volume source applied to the indicator output.
	/// </summary>
	public enum AppliedVolume
{
	/// <summary>
	/// Multiply the indicator by tick volume.
	/// </summary>
			Tick,

	/// <summary>
	/// Multiply the indicator by real volume.
	/// </summary>
			Real,
}

	/// <summary>
	/// Moving average methods supported by the indicator.
	/// </summary>
	public enum SmoothMethod
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
	/// VIDYA adaptive moving average (approximated by EMA).
	/// </summary>
			Vidya,

	/// <summary>
	/// Kaufman adaptive moving average.
	/// </summary>
			Ama,
}

	private sealed class XBullsBearsEyesVolCalculator
{
	private readonly ExponentialMovingAverage _ema;
	private readonly LengthIndicator<decimal> _valueSmoother;
	private readonly LengthIndicator<decimal> _volumeSmoother;
	private readonly AppliedVolume _volumeType;
	private readonly decimal _gamma;
	private readonly decimal _highLevel2;
	private readonly decimal _highLevel1;
	private readonly decimal _lowLevel1;
	private readonly decimal _lowLevel2;

	private decimal _l0;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;

	public XBullsBearsEyesVolCalculator(
		int emaPeriod,
		decimal gamma,
		AppliedVolume volumeType,
		int highLevel2,
		int highLevel1,
		int lowLevel1,
		int lowLevel2,
		SmoothMethod method,
		int smoothLength,
		int smoothPhase)
		{
			var period = Math.Max(1, emaPeriod);
			_ema = new ExponentialMovingAverage { Length = period };
			_gamma = Math.Min(0.999m, Math.Max(0m, gamma));
			_volumeType = volumeType;
			_highLevel2 = highLevel2;
			_highLevel1 = highLevel1;
			_lowLevel1 = lowLevel1;
			_lowLevel2 = lowLevel2;
			_valueSmoother = CreateSmoother(method, smoothLength, smoothPhase);
			_volumeSmoother = CreateSmoother(method, smoothLength, smoothPhase);
		}

		public void Reset()
		{
			_ema.Reset();
			_valueSmoother.Reset();
			_volumeSmoother.Reset();
			_l0 = 0m;
			_l1 = 0m;
			_l2 = 0m;
			_l3 = 0m;
		}

		public XBullsBearsEyesVolResult? Process(ICandleMessage candle)
		{
			var time = candle.CloseTime ?? candle.OpenTime;
			var emaValue = _ema.Process(candle.ClosePrice, time, true).ToNullableDecimal();
			if (emaValue is null)
			return null;

			var bulls = candle.HighPrice - emaValue.Value;
			var bears = candle.LowPrice - emaValue.Value;
			var combined = bulls + bears;

			var l0 = (1m - _gamma) * combined + _gamma * _l0;
			var l1 = -_gamma * l0 + _l0 + _gamma * _l1;
			var l2 = -_gamma * l1 + _l1 + _gamma * _l2;
			var l3 = -_gamma * l2 + _l2 + _gamma * _l3;

			_l0 = l0;
			_l1 = l1;
			_l2 = l2;
			_l3 = l3;

			var cu = 0m;
			var cd = 0m;

			if (l0 >= l1)
			cu += l0 - l1;
			else
			cd += l1 - l0;

			if (l1 >= l2)
			cu += l1 - l2;
			else
			cd += l2 - l1;

			if (l2 >= l3)
			cu += l2 - l3;
			else
			cd += l3 - l2;

			var sum = cu + cd;
			var ratio = sum <= 0m ? 0m : cu / sum;
			var baseValue = ratio * 100m - 50m;

			var volume = GetVolume(candle);
			var scaled = baseValue * volume;

			var smoothedValue = _valueSmoother.Process(scaled, time, true).ToNullableDecimal();
			var smoothedVolume = _volumeSmoother.Process(volume, time, true).ToNullableDecimal();

			if (smoothedValue is null || smoothedVolume is null)
			return null;

			var color = DetermineColor(smoothedValue.Value, smoothedVolume.Value);
			return new XBullsBearsEyesVolResult(smoothedValue.Value, smoothedVolume.Value, color);
		}

		private int DetermineColor(decimal value, decimal volume)
		{
			var maxLevel = _highLevel2 * volume;
			var upLevel = _highLevel1 * volume;
			var downLevel = _lowLevel1 * volume;
			var minLevel = _lowLevel2 * volume;

			if (value > maxLevel)
			return 0;

			if (value > upLevel)
			return 1;

			if (value < minLevel)
			return 4;

			if (value < downLevel)
			return 3;

			return 2;
		}

		private decimal GetVolume(ICandleMessage candle)
		{
			return _volumeType switch
			{
				AppliedVolume.Tick => candle.TotalTicks.HasValue ? (decimal)candle.TotalTicks.Value : candle.TotalVolume ?? 0m,
				AppliedVolume.Real => candle.TotalVolume ?? (candle.TotalTicks.HasValue ? (decimal)candle.TotalTicks.Value : 0m),
				_ => candle.TotalVolume ?? 0m,
			};
		}

		private static LengthIndicator<decimal> CreateSmoother(SmoothMethod method, int length, int phase)
		{
			var normalizedLength = Math.Max(1, length);

			return method switch
			{
				SmoothMethod.Sma => new SimpleMovingAverage { Length = normalizedLength },
				SmoothMethod.Ema => new ExponentialMovingAverage { Length = normalizedLength },
				SmoothMethod.Smma => new SmoothedMovingAverage { Length = normalizedLength },
				SmoothMethod.Lwma => new WeightedMovingAverage { Length = normalizedLength },
				SmoothMethod.Jjma => CreateJurik(normalizedLength, phase),
				SmoothMethod.JurX => CreateJurik(normalizedLength, phase),
				SmoothMethod.ParMa => new ExponentialMovingAverage { Length = normalizedLength },
				SmoothMethod.T3 => new TripleExponentialMovingAverage { Length = normalizedLength },
				SmoothMethod.Vidya => new ExponentialMovingAverage { Length = normalizedLength },
				SmoothMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = normalizedLength },
				_ => new SimpleMovingAverage { Length = normalizedLength },
			};
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
	}

	private readonly record struct XBullsBearsEyesVolResult(decimal Value, decimal Volume, int Color);
}
