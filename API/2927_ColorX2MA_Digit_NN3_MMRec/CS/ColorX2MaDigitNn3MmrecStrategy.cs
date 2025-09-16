using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple timeframe strategy based on the ColorX2MA Digit indicator with money management style position sizing.
/// </summary>
public class ColorX2MaDigitNn3MmrecStrategy : Strategy
{
	private readonly TimeframeContext _aContext;
	private readonly TimeframeContext _bContext;
	private readonly TimeframeContext _cContext;

	private bool _needsSync;

	/// <summary>
	/// Initializes <see cref="ColorX2MaDigitNn3MmrecStrategy"/>.
	/// </summary>
	public ColorX2MaDigitNn3MmrecStrategy()
	{
		_aContext = new TimeframeContext(this, "A", TimeSpan.FromHours(12), ColorX2MaAppliedPrice.Close, ColorX2MaSmoothMethod.Simple,
		ColorX2MaSmoothMethod.Jurik, 12, 5, 1, 2);
		_bContext = new TimeframeContext(this, "B", TimeSpan.FromHours(6), ColorX2MaAppliedPrice.Close, ColorX2MaSmoothMethod.Simple,
		ColorX2MaSmoothMethod.Jurik, 12, 5, 1, 2);
		_cContext = new TimeframeContext(this, "C", TimeSpan.FromHours(3), ColorX2MaAppliedPrice.Close, ColorX2MaSmoothMethod.Simple,
		ColorX2MaSmoothMethod.Jurik, 12, 5, 1, 1);
	}

	/// <summary>
	/// Candle type for set A.
	/// </summary>
	public DataType ACandleType { get => _aContext.CandleType; set => _aContext.CandleType = value; }

	/// <summary>
	/// Candle type for set B.
	/// </summary>
	public DataType BCandleType { get => _bContext.CandleType; set => _bContext.CandleType = value; }

	/// <summary>
	/// Candle type for set C.
	/// </summary>
	public DataType CCandleType { get => _cContext.CandleType; set => _cContext.CandleType = value; }

	/// <summary>
	/// Trading volume for set A.
	/// </summary>
	public decimal AVolume { get => _aContext.Volume; set => _aContext.Volume = value; }

	/// <summary>
	/// Trading volume for set B.
	/// </summary>
	public decimal BVolume { get => _bContext.Volume; set => _bContext.Volume = value; }

	/// <summary>
	/// Trading volume for set C.
	/// </summary>
	public decimal CVolume { get => _cContext.Volume; set => _cContext.Volume = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to candles for each timeframe and bind indicators.
		_aContext.Start();
		_bContext.Start();
		_cContext.Start();

		_needsSync = true;
		TrySyncPosition();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Reset state for every timeframe before the next run.
		_aContext.ResetState();
		_bContext.ResetState();
		_cContext.ResetState();

		_needsSync = true;
		TrySyncPosition();
	}

	private void ProcessContextCandle(TimeframeContext context, ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (context.HandleIndicatorValue(indicatorValue))
		{
			_needsSync = true;
		}

		TrySyncPosition();
	}

	private void TrySyncPosition()
	{
		if (!_needsSync)
		return;

		// Wait until the strategy is ready to send orders.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var desired = _aContext.CurrentTarget + _bContext.CurrentTarget + _cContext.CurrentTarget;
		var diff = desired - Position;

		if (diff > 0m)
		{
			BuyMarket(diff);
		}
		else if (diff < 0m)
		{
			SellMarket(-diff);
		}

		_needsSync = false;
	}

	private sealed class TimeframeContext
	{
		private readonly ColorX2MaDigitNn3MmrecStrategy _parent;
		private readonly string _label;

		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<ColorX2MaSmoothMethod> _fastMethod;
		private readonly StrategyParam<ColorX2MaSmoothMethod> _slowMethod;
		private readonly StrategyParam<int> _fastLength;
		private readonly StrategyParam<int> _slowLength;
		private readonly StrategyParam<int> _signalBars;
		private readonly StrategyParam<int> _digits;
		private readonly StrategyParam<ColorX2MaAppliedPrice> _priceType;
		private readonly StrategyParam<bool> _allowLongEntry;
		private readonly StrategyParam<bool> _allowLongExit;
		private readonly StrategyParam<bool> _allowShortEntry;
		private readonly StrategyParam<bool> _allowShortExit;
		private readonly StrategyParam<decimal> _volume;

		private Subscription? _subscription;
		private ColorX2MaDigitIndicator? _indicator;
		private TrendDirection _pendingDirection = TrendDirection.None;
		private int _pendingCount;

		public TimeframeContext(ColorX2MaDigitNn3MmrecStrategy parent, string label, TimeSpan timeframe, ColorX2MaAppliedPrice price,
		ColorX2MaSmoothMethod fastMethod, ColorX2MaSmoothMethod slowMethod, int fastLength, int slowLength, int signalBars, int digits)
		{
			_parent = parent;
			_label = label;
			var group = $"{label} Settings";

			_candleType = parent.Param($"{label}CandleType", timeframe.TimeFrame())
			.SetDisplay($"{label} Candle Type", "Timeframe used for this signal", group);

			_fastMethod = parent.Param($"{label}FastMethod", fastMethod)
			.SetDisplay($"{label} Fast Method", "Smoothing method for the first average", group);

			_slowMethod = parent.Param($"{label}SlowMethod", slowMethod)
			.SetDisplay($"{label} Slow Method", "Smoothing method for the second average", group);

			_fastLength = parent.Param($"{label}FastLength", fastLength)
			.SetGreaterThanZero()
			.SetDisplay($"{label} Fast Length", "Period for the first moving average", group)
			.SetCanOptimize(true);

			_slowLength = parent.Param($"{label}SlowLength", slowLength)
			.SetGreaterThanZero()
			.SetDisplay($"{label} Slow Length", "Period for the second moving average", group)
			.SetCanOptimize(true);

			_signalBars = parent.Param($"{label}SignalBars", signalBars)
			.SetGreaterThanZero()
			.SetDisplay($"{label} Confirmation", "Number of bars required to confirm a signal", group);

			_digits = parent.Param($"{label}Digits", digits)
			.SetDisplay($"{label} Digits", "Decimal precision used for rounding", group);

			_priceType = parent.Param($"{label}PriceType", price)
			.SetDisplay($"{label} Price", "Price source for the indicator", group);

			_allowLongEntry = parent.Param($"{label}AllowLongEntry", true)
			.SetDisplay($"{label} Long Entry", "Allow opening long positions", group);

			_allowLongExit = parent.Param($"{label}AllowLongExit", true)
			.SetDisplay($"{label} Long Exit", "Allow closing long positions", group);

			_allowShortEntry = parent.Param($"{label}AllowShortEntry", true)
			.SetDisplay($"{label} Short Entry", "Allow opening short positions", group);

			_allowShortExit = parent.Param($"{label}AllowShortExit", true)
			.SetDisplay($"{label} Short Exit", "Allow closing short positions", group);

			_volume = parent.Param($"{label}Volume", 1m)
			.SetGreaterThanZero()
			.SetDisplay($"{label} Volume", "Volume traded when this signal turns active", group)
			.SetCanOptimize(true);
		}

		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		public decimal Volume
		{
			get => _volume.Value;
			set => _volume.Value = value;
		}

		public decimal CurrentTarget { get; private set; }

		public void Start()
		{
			_indicator = new ColorX2MaDigitIndicator
			{
				AppliedPrice = _priceType.Value,
				FastMethod = _fastMethod.Value,
				SlowMethod = _slowMethod.Value,
				FastLength = _fastLength.Value,
				SlowLength = _slowLength.Value,
				Digits = _digits.Value
			};

			_subscription = _parent.SubscribeCandles(_candleType.Value);
			_subscription
			.BindEx(_indicator, (candle, indicatorValue) => _parent.ProcessContextCandle(this, candle, indicatorValue))
			.Start();
		}

		public void ResetState()
		{
			_subscription?.Dispose();
			_subscription = null;

			_indicator?.Reset();
			_indicator = null;

			CurrentTarget = 0m;
			_pendingDirection = TrendDirection.None;
			_pendingCount = 0;
		}

		public bool HandleIndicatorValue(IIndicatorValue indicatorValue)
		{
			if (_indicator == null)
				return false;

			if (!_indicator.IsFormed)
				return false;

			if (indicatorValue is not ColorX2MaDigitValue colorValue)
				return false;

			var direction = colorValue.Direction;
			if (direction == TrendDirection.None)
				return false;

			if (direction != _pendingDirection)
			{
				_pendingDirection = direction;
				_pendingCount = 1;
			}
			else
			{
				_pendingCount++;
			}

			if (_pendingCount < _signalBars.Value)
				return false;

			var target = CurrentTarget;

			if (direction == TrendDirection.Up)
			{
				// Close short exposure when the direction flips to bullish.
				if (target < 0m)
				{
					if (_allowShortExit.Value)
					{
						target = 0m;
					}
					else
					{
						return false;
					}
				}

				// Open long exposure if it is permitted by the parameters.
				if (_allowLongEntry.Value)
				{
					target = _volume.Value;
				}
			}
			else if (direction == TrendDirection.Down)
			{
				// Close long exposure when the direction flips to bearish.
				if (target > 0m)
				{
					if (_allowLongExit.Value)
					{
						target = 0m;
					}
					else
					{
						return false;
					}
				}

				// Open short exposure if the configuration allows it.
				if (_allowShortEntry.Value)
				{
					target = -_volume.Value;
				}
			}

			if (target == CurrentTarget)
				return false;

			CurrentTarget = target;
			return true;
		}
	}
}

/// <summary>
/// Available price sources for the custom indicator.
/// </summary>
public enum ColorX2MaAppliedPrice
{
	/// <summary>
	/// Close price.
	/// </summary>
	Close = 1,
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
	/// Typical price (high + low + close) / 3.
	/// </summary>
	Typical,
	/// <summary>
	/// Weighted close price (2 * close + high + low) / 4.
	/// </summary>
	Weighted,
	/// <summary>
	/// (open + close) / 2.
	/// </summary>
	Simpl,
	/// <summary>
	/// (open + high + low + close) / 4.
	/// </summary>
	Quarter,
	/// <summary>
	/// Trend follow price using extreme values when candles are directional.
	/// </summary>
	TrendFollow0,
	/// <summary>
	/// Trend follow price averaged with the close when candles are directional.
	/// </summary>
	TrendFollow1,
	/// <summary>
	/// DeMark price calculation.
	/// </summary>
	Demark
}

/// <summary>
/// Smoothing method used by the custom double moving average.
/// </summary>
public enum ColorX2MaSmoothMethod
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
	/// Smoothed moving average.
	/// </summary>
	Smoothed,
	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	LinearWeighted,
	/// <summary>
	/// Jurik moving average approximation.
	/// </summary>
	Jurik,
	/// <summary>
	/// Kaufman adaptive moving average approximation.
	/// </summary>
	Adaptive
}

internal enum TrendDirection
{
	None,
	Up,
	Down
}

internal sealed class ColorX2MaDigitIndicator : Indicator<ICandleMessage>
{
	public ColorX2MaAppliedPrice AppliedPrice { get; set; } = ColorX2MaAppliedPrice.Close;
	public ColorX2MaSmoothMethod FastMethod { get; set; } = ColorX2MaSmoothMethod.Simple;
	public ColorX2MaSmoothMethod SlowMethod { get; set; } = ColorX2MaSmoothMethod.Jurik;
	public int FastLength { get; set; } = 12;
	public int SlowLength { get; set; } = 5;
	public int Digits { get; set; } = 2;

	private IIndicator? _fastMa;
	private IIndicator? _slowMa;
	private decimal? _previousValue;
	private TrendDirection _previousDirection = TrendDirection.None;

	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		return new ColorX2MaDigitValue(this, input, 0m, TrendDirection.None);

		_fastMa ??= CreateAverage(FastMethod, FastLength);
		_slowMa ??= CreateAverage(SlowMethod, SlowLength);

		var price = GetPrice(candle);
		var fastValue = _fastMa.Process(new DecimalIndicatorValue(_fastMa, price, input.Time));
		var fast = fastValue.ToDecimal();
		var slowValue = _slowMa.Process(new DecimalIndicatorValue(_slowMa, fast, input.Time));
		var current = Round(slowValue.ToDecimal());

		if (!_slowMa.IsFormed)
		{
			_previousValue = current;
			_previousDirection = TrendDirection.None;
			IsFormed = false;
			return new ColorX2MaDigitValue(this, input, current, TrendDirection.None);
		}

		var direction = TrendDirection.None;
		if (_previousValue is decimal prev)
		{
			var diff = current - prev;
			direction = diff > 0m
			? TrendDirection.Up
			: diff < 0m
			? TrendDirection.Down
			: _previousDirection;
		}

		_previousValue = current;
		_previousDirection = direction;
		IsFormed = true;

		return new ColorX2MaDigitValue(this, input, current, direction);
	}

	public override void Reset()
	{
		base.Reset();

		_fastMa?.Reset();
		_slowMa?.Reset();
		_fastMa = null;
		_slowMa = null;
		_previousValue = null;
		_previousDirection = TrendDirection.None;
		IsFormed = false;
	}

	private decimal GetPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			ColorX2MaAppliedPrice.Open => candle.OpenPrice,
			ColorX2MaAppliedPrice.High => candle.HighPrice,
			ColorX2MaAppliedPrice.Low => candle.LowPrice,
			ColorX2MaAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			ColorX2MaAppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			ColorX2MaAppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			ColorX2MaAppliedPrice.Simpl => (candle.OpenPrice + candle.ClosePrice) / 2m,
			ColorX2MaAppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			ColorX2MaAppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
			? candle.HighPrice
			: candle.ClosePrice < candle.OpenPrice
			? candle.LowPrice
			: candle.ClosePrice,
			ColorX2MaAppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
			? (candle.HighPrice + candle.ClosePrice) / 2m
			: candle.ClosePrice < candle.OpenPrice
			? (candle.LowPrice + candle.ClosePrice) / 2m
			: candle.ClosePrice,
			ColorX2MaAppliedPrice.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var baseSum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
		baseSum = (baseSum + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
		baseSum = (baseSum + candle.HighPrice) / 2m;
		else
		baseSum = (baseSum + candle.ClosePrice) / 2m;

		return ((baseSum - candle.LowPrice) + (baseSum - candle.HighPrice)) / 2m;
	}

	private decimal Round(decimal value)
	{
		return Digits < 0 ? value : Math.Round(value, Digits, MidpointRounding.AwayFromZero);
	}

	private static IIndicator CreateAverage(ColorX2MaSmoothMethod method, int length)
	{
		return method switch
		{
			ColorX2MaSmoothMethod.Exponential => new ExponentialMovingAverage { Length = length },
			ColorX2MaSmoothMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			ColorX2MaSmoothMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			ColorX2MaSmoothMethod.Jurik => new JurikMovingAverage { Length = length },
			ColorX2MaSmoothMethod.Adaptive => new KaufmanAdaptiveMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}
}

internal sealed class ColorX2MaDigitValue : ComplexIndicatorValue
{
	public ColorX2MaDigitValue(IIndicator indicator, IIndicatorValue input, decimal value, TrendDirection direction)
	: base(indicator, input, (nameof(Value), value), (nameof(Direction), direction))
	{
	}

	public decimal Value => (decimal)GetValue(nameof(Value));

	public TrendDirection Direction => (TrendDirection)GetValue(nameof(Direction));
}
