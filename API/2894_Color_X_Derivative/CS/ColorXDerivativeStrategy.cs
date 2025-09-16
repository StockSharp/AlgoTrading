using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader ColorXDerivative expert advisor to StockSharp.
/// Generates entry signals from the ColorXDerivative momentum histogram encoded as five color states.
/// Buys when positive momentum accelerates or a bearish swing starts to contract, sells on the inverse conditions.
/// Applies optional stop loss and take profit measured in price steps.
/// </summary>
public class ColorXDerivativeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _derivativePeriod;
	private readonly StrategyParam<AppliedPriceOption> _appliedPrice;
	private readonly StrategyParam<SmoothingMethodOption> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;

	private ColorXDerivativeIndicator _indicator = null!;

	/// <summary>
	/// Order volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			Volume = value;
		}
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
	/// Derivative lookback period.
	/// </summary>
	public int DerivativePeriod
	{
		get => _derivativePeriod.Value;
		set => _derivativePeriod.Value = value;
	}

	/// <summary>
	/// Price source applied before derivative smoothing.
	/// </summary>
	public AppliedPriceOption AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Smoothing method matching the MQL configuration.
	/// </summary>
	public SmoothingMethodOption SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing filter applied to the derivative.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Bar shift used to read ColorXDerivative signals (1 = last finished bar).
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Enables long entries when true.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Enables short entries when true.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Enables long exits when true.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Enables short exits when true.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorXDerivativeStrategy"/>.
	/// </summary>
	public ColorXDerivativeStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume used for market orders", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for the ColorXDerivative", "General");

		_derivativePeriod = Param(nameof(DerivativePeriod), 34)
		.SetGreaterThanZero()
		.SetDisplay("Derivative Period", "Shift used in the derivative calculation", "Indicator");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceOption.Weighted)
		.SetDisplay("Applied Price", "Price source passed into the derivative", "Indicator");

		_smoothingMethod = Param(nameof(SmoothingMethod), SmoothingMethodOption.Jurik)
		.SetDisplay("Smoothing Method", "Filter used on the derivative", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 7)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "Period of the smoothing filter", "Indicator");

		_signalShift = Param(nameof(SignalShift), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Signal Shift", "Bars back used for signal evaluation", "Indicator");

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (ticks)", "Protective stop in price steps", "Risk Management");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (ticks)", "Profit target in price steps", "Risk Management");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
		.SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
		.SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
		.SetDisplay("Enable Long Exit", "Allow closing long positions", "Trading");

		_enableShortExit = Param(nameof(EnableShortExit), true)
		.SetDisplay("Enable Short Exit", "Allow closing short positions", "Trading");

		Volume = OrderVolume;
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

		Volume = OrderVolume;

		_indicator = new ColorXDerivativeIndicator
		{
			DerivativePeriod = DerivativePeriod,
			AppliedPrice = AppliedPrice,
			SmoothingMethod = SmoothingMethod,
			SmoothingLength = SmoothingLength,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_indicator, ProcessCandle)
			.Start();

		var step = Security?.Step ?? 0m;
		if (step > 0m && (StopLossTicks > 0 || TakeProfitTicks > 0))
		{
			StartProtection(
				stopLoss: StopLossTicks > 0 ? new Unit(StopLossTicks * step, UnitTypes.Point) : null,
				takeProfit: TakeProfitTicks > 0 ? new Unit(TakeProfitTicks * step, UnitTypes.Point) : null);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator, "ColorXDerivative");
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

		if (!_indicator.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var shift = SignalShift <= 0 ? 1 : SignalShift;
		var currentColor = _indicator.GetColorByShift(shift);
		var previousColor = _indicator.GetColorByShift(shift + 1);

		if (currentColor is null || previousColor is null)
			return;

		var buyOpen = EnableLongEntry &&
			((currentColor == 0 && previousColor != 0) ||
			(currentColor == 3 && (previousColor == 4 || previousColor == 2)));

		var sellOpen = EnableShortEntry &&
			((currentColor == 4 && previousColor != 4) ||
			(currentColor == 1 && (previousColor == 0 || previousColor == 2)));

		var buyClose = EnableLongExit && (currentColor == 1 || currentColor == 4);
		var sellClose = EnableShortExit && (currentColor == 0 || currentColor == 3);

		if (buyClose && Position > 0)
			SellMarket(Position);

		if (sellClose && Position < 0)
			BuyMarket(-Position);

		if (buyOpen && Position <= 0)
			BuyMarket();

		if (sellOpen && Position >= 0)
			SellMarket();
	}

	/// <summary>
	/// Smoothing methods supported by the translated indicator.
	/// </summary>
	public enum SmoothingMethodOption
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
		/// Jurik moving average approximation of JJMA.
		/// </summary>
		Jurik,
	}

	/// <summary>
	/// Price modes matching the MetaTrader implementation.
	/// </summary>
	public enum AppliedPriceOption
	{
		/// <summary>
		/// Closing price.
		/// </summary>
		Close = 1,

		/// <summary>
		/// Opening price.
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
		/// Median price (H+L)/2.
		/// </summary>
		Median,

		/// <summary>
		/// Typical price (H+L+C)/3.
		/// </summary>
		Typical,

		/// <summary>
		/// Weighted close (2*C+H+L)/4.
		/// </summary>
		Weighted,

		/// <summary>
		/// Simple price (O+C)/2.
		/// </summary>
		Simple,

		/// <summary>
		/// Quarter price (O+C+H+L)/4.
		/// </summary>
		Quarter,

		/// <summary>
		/// Trend-following price variant 0.
		/// </summary>
		TrendFollow0,

		/// <summary>
		/// Trend-following price variant 1.
		/// </summary>
		TrendFollow1,

		/// <summary>
		/// Demark price.
		/// </summary>
		Demark,
	}

	private sealed class ColorXDerivativeIndicator : Indicator<ICandleMessage>
	{
		private readonly Queue<decimal> _priceBuffer = new();
		private readonly List<int> _colors = new();
		private IIndicator? _smoothingIndicator;
		private SmoothingMethodOption _cachedMethod;
		private int _cachedLength;
		private decimal? _previousValue;

		public int DerivativePeriod { get; set; } = 34;
		public AppliedPriceOption AppliedPrice { get; set; } = AppliedPriceOption.Weighted;
		public SmoothingMethodOption SmoothingMethod { get; set; } = SmoothingMethodOption.Jurik;
		public int SmoothingLength { get; set; } = 7;

		public int LastColor { get; private set; } = 2;
		public int PreviousColor { get; private set; } = 2;

		public int? GetColorByShift(int shift)
		{
			if (shift <= 0 || shift > _colors.Count)
				return null;

			return _colors[_colors.Count - shift];
		}

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			EnsureSmoothingIndicator();

			var candle = input.GetValue<ICandleMessage>();
			var price = GetPrice(candle);

			_priceBuffer.Enqueue(price);
			if (_priceBuffer.Count > DerivativePeriod + 1)
				_priceBuffer.Dequeue();

			if (_priceBuffer.Count <= DerivativePeriod)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			var oldestPrice = _priceBuffer.Peek();
			var derivative = (price - oldestPrice) * 100m / DerivativePeriod;

			var smoothingValue = _smoothingIndicator!
				.Process(derivative, input.Time, true)
				.ToDecimal();

			var previousColor = _colors.Count > 0 ? _colors[^1] : 2;
			var color = CalculateColor(smoothingValue, _previousValue);

			_previousValue = smoothingValue;
			PreviousColor = previousColor;
			LastColor = color;

			_colors.Add(color);
			if (_colors.Count > 512)
				_colors.RemoveAt(0);

			IsFormed = _smoothingIndicator.IsFormed && _colors.Count >= 2;

			return new DecimalIndicatorValue(this, smoothingValue, input.Time);
		}

		public override void Reset()
		{
			base.Reset();

			_priceBuffer.Clear();
			_colors.Clear();
			_previousValue = null;
			LastColor = 2;
			PreviousColor = 2;
			_smoothingIndicator?.Reset();
		}

		private void EnsureSmoothingIndicator()
		{
			if (_smoothingIndicator != null && _cachedMethod == SmoothingMethod && _cachedLength == SmoothingLength)
				return;

			_cachedMethod = SmoothingMethod;
			_cachedLength = Math.Max(1, SmoothingLength);
			_smoothingIndicator = CreateSmoothingIndicator(_cachedMethod, _cachedLength);
		}

		private static IIndicator CreateSmoothingIndicator(SmoothingMethodOption method, int length)
		{
			return method switch
			{
				SmoothingMethodOption.Sma => new SMA { Length = length },
				SmoothingMethodOption.Ema => new EMA { Length = length },
				SmoothingMethodOption.Smma => new SMMA { Length = length },
				SmoothingMethodOption.Lwma => new WMA { Length = length },
				SmoothingMethodOption.Jurik => new JurikMovingAverage { Length = length },
				_ => new SMA { Length = length },
			};
		}

		private static int CalculateColor(decimal current, decimal? previous)
		{
			if (previous is null)
			{
				if (current > 0m)
					return 0;
				if (current < 0m)
					return 4;
				return 2;
			}

			if (current > 0m)
				return previous <= current ? 0 : 1;

			if (current < 0m)
				return previous >= current ? 4 : 3;

			return 2;
		}

		private decimal GetPrice(ICandleMessage candle)
		{
			return AppliedPrice switch
			{
				AppliedPriceOption.Close => candle.ClosePrice,
				AppliedPriceOption.Open => candle.OpenPrice,
				AppliedPriceOption.High => candle.HighPrice,
				AppliedPriceOption.Low => candle.LowPrice,
				AppliedPriceOption.Median => (candle.HighPrice + candle.LowPrice) / 2m,
				AppliedPriceOption.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
				AppliedPriceOption.Weighted => (candle.ClosePrice * 2m + candle.HighPrice + candle.LowPrice) / 4m,
				AppliedPriceOption.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
				AppliedPriceOption.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
				AppliedPriceOption.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
					? candle.HighPrice
					: candle.ClosePrice < candle.OpenPrice
						? candle.LowPrice
						: candle.ClosePrice,
				AppliedPriceOption.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
					? (candle.HighPrice + candle.ClosePrice) / 2m
					: candle.ClosePrice < candle.OpenPrice
						? (candle.LowPrice + candle.ClosePrice) / 2m
						: candle.ClosePrice,
				AppliedPriceOption.Demark => CalculateDemarkPrice(candle),
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
	}
}
