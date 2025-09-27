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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the Exp RSIOMA v2 MetaTrader expert advisor to StockSharp high level strategy API.
/// Uses a smoothed RSI momentum oscillator to detect trend shifts or level breakdowns.
/// </summary>
public class ExpRsiomaV2Strategy : Strategy
{
	/// <summary>
	/// Available smoothing algorithms for RSIOMA calculation.
	/// </summary>
	public enum RsiomaSmoothingMethods
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
		Weighted
	}

	/// <summary>
	/// Signal logic of the RSIOMA expert advisor.
	/// </summary>
	public enum RsiomaSignalModes
	{
		/// <summary>
		/// React to oscillator leaving the main trend levels.
		/// </summary>
		Breakdown,

		/// <summary>
		/// React to direction changes of the oscillator slope.
		/// </summary>
		Twist,

		/// <summary>
		/// React to RSIOMA returning from extreme zones.
		/// </summary>
		CloudTwist
	}

	/// <summary>
	/// Price source used by the RSIOMA computation.
	/// </summary>
	public enum RsiomaAppliedPrices
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
		/// Weighted close (2 * close + high + low) / 4.
		/// </summary>
		Weighted,

		/// <summary>
		/// Simple price (open + close) / 2.
		/// </summary>
		Simple,

		/// <summary>
		/// Quarter price (open + high + low + close) / 4.
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
		/// DeMark price calculation.
		/// </summary>
		Demark
	}
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<RsiomaSignalModes> _mode;
	private readonly StrategyParam<RsiomaSmoothingMethods> _priceSmoothing;
	private readonly StrategyParam<int> _rsiomaLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<RsiomaAppliedPrices> _appliedPrice;
	private readonly StrategyParam<decimal> _mainTrendLong;
	private readonly StrategyParam<decimal> _mainTrendShort;
	private readonly StrategyParam<int> _signalBar;

	private IIndicator _priceSmoother = null!;
	private Momentum _momentum = null!;
	private readonly List<decimal> _momentumSeed = new();
	private readonly List<decimal> _rsiHistory = new();
	private decimal _averagePositive;
	private decimal _averageNegative;
	private bool _isRsiInitialized;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpRsiomaV2Strategy"/> class.
	/// </summary>
	public ExpRsiomaV2Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle timeframe", "Data");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_enableLongExits = Param(nameof(EnableLongExits), true)
		.SetDisplay("Enable Long Exits", "Allow closing long positions", "Trading");

		_enableShortExits = Param(nameof(EnableShortExits), true)
		.SetDisplay("Enable Short Exits", "Allow closing short positions", "Trading");

		_mode = Param(nameof(Mode), RsiomaSignalModes.Breakdown)
		.SetDisplay("Signal Mode", "RSIOMA event that triggers trades", "Logic");

		_priceSmoothing = Param(nameof(PriceSmoothing), RsiomaSmoothingMethods.Exponential)
		.SetDisplay("Price Smoothing", "Moving average applied to the price", "Indicator");

		_rsiomaLength = Param(nameof(RsiomaLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSIOMA averaging period", "Indicator");

		_momentumPeriod = Param(nameof(MomentumPeriod), 1)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Lag used for momentum calculation", "Indicator");

		_appliedPrice = Param(nameof(AppliedPrice), RsiomaAppliedPrices.Close)
		.SetDisplay("Applied Price", "Source price for RSIOMA", "Indicator");

		_mainTrendLong = Param(nameof(MainTrendLong), 60m)
		.SetDisplay("Upper Threshold", "Level that marks an overbought trend", "Levels");

		_mainTrendShort = Param(nameof(MainTrendShort), 40m)
		.SetDisplay("Lower Threshold", "Level that marks an oversold trend", "Levels");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Signal Bar", "Number of closed bars to inspect", "Logic");
	}

	/// <summary>
	/// Gets or sets the default order volume.
	/// </summary>
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }

	/// <summary>
	/// Gets or sets the candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Gets or sets a value indicating whether long entries are allowed.
	/// </summary>
	public bool EnableLongEntries { get => _enableLongEntries.Value; set => _enableLongEntries.Value = value; }

	/// <summary>
	/// Gets or sets a value indicating whether short entries are allowed.
	/// </summary>
	public bool EnableShortEntries { get => _enableShortEntries.Value; set => _enableShortEntries.Value = value; }

	/// <summary>
	/// Gets or sets a value indicating whether the strategy may close long positions.
	/// </summary>
	public bool EnableLongExits { get => _enableLongExits.Value; set => _enableLongExits.Value = value; }

	/// <summary>
	/// Gets or sets a value indicating whether the strategy may close short positions.
	/// </summary>
	public bool EnableShortExits { get => _enableShortExits.Value; set => _enableShortExits.Value = value; }

	/// <summary>
	/// Gets or sets the entry logic that should be used.
	/// </summary>
	public RsiomaSignalModes Mode { get => _mode.Value; set => _mode.Value = value; }

	/// <summary>
	/// Gets or sets the smoothing method applied to the price stream.
	/// </summary>
	public RsiomaSmoothingMethods PriceSmoothing { get => _priceSmoothing.Value; set => _priceSmoothing.Value = value; }

	/// <summary>
	/// Gets or sets the RSI averaging length.
	/// </summary>
	public int RsiomaLength { get => _rsiomaLength.Value; set => _rsiomaLength.Value = value; }

	/// <summary>
	/// Gets or sets the momentum lag.
	/// </summary>
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }

	/// <summary>
	/// Gets or sets which candle price is used.
	/// </summary>
	public RsiomaAppliedPrices AppliedPrice { get => _appliedPrice.Value; set => _appliedPrice.Value = value; }

	/// <summary>
	/// Gets or sets the upper RSI threshold.
	/// </summary>
	public decimal MainTrendLong { get => _mainTrendLong.Value; set => _mainTrendLong.Value = value; }

	/// <summary>
	/// Gets or sets the lower RSI threshold.
	/// </summary>
	public decimal MainTrendShort { get => _mainTrendShort.Value; set => _mainTrendShort.Value = value; }

	/// <summary>
	/// Gets or sets the bar shift used to evaluate signals.
	/// </summary>
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_priceSmoother = CreateSmoother(PriceSmoothing, RsiomaLength);
		_momentum = new Momentum
		{
			Length = MomentumPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = GetAppliedPrice(candle, AppliedPrice);

		var priceValue = _priceSmoother.Process(new DecimalIndicatorValue(_priceSmoother, price, candle.OpenTime));
		if (!priceValue.IsFinal)
		return;

		var smoothedPrice = priceValue.ToDecimal();

		var momentumValue = _momentum.Process(new DecimalIndicatorValue(_momentum, smoothedPrice, candle.OpenTime));
		if (!momentumValue.IsFinal)
		return;

		var momentum = momentumValue.ToDecimal();
		var rsi = UpdateAndGetRsi(momentum);
		if (rsi is null)
		return;

		_rsiHistory.Add(rsi.Value);
		TrimHistory();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		ProcessSignals();
	}

	private decimal? UpdateAndGetRsi(decimal momentum)
	{
		var positive = Math.Max(momentum, 0m);
		var negative = Math.Max(-momentum, 0m);

		if (!_isRsiInitialized)
		{
			_momentumSeed.Add(momentum);

			if (_momentumSeed.Count < RsiomaLength)
			return null;

			decimal gain = 0m;
			decimal loss = 0m;

			foreach (var seed in _momentumSeed)
			{
				if (seed > 0m)
				gain += seed;
				else
				loss -= seed;
			}

			_averagePositive = gain / RsiomaLength;
			_averageNegative = loss / RsiomaLength;
			_momentumSeed.Clear();
			_isRsiInitialized = true;
		}
		else
		{
			_averagePositive = (_averagePositive * (RsiomaLength - 1) + positive) / RsiomaLength;
			_averageNegative = (_averageNegative * (RsiomaLength - 1) + negative) / RsiomaLength;
		}

		if (_averageNegative == 0m)
		return _averagePositive == 0m ? 50m : 100m;

		if (_averagePositive == 0m)
		return 0m;

		var rs = _averagePositive / _averageNegative;
		return 100m - 100m / (1m + rs);
	}

	private void ProcessSignals()
	{
		if (!TryGetRsi(SignalBar, out var current))
		return;

		if (!TryGetRsi(SignalBar + 1, out var previous))
		return;

		bool buyOpen;
		bool sellOpen;
		bool buyClose;
		bool sellClose;

		switch (Mode)
		{
		case RsiomaSignalModes.Breakdown:
			{
				buyOpen = previous > MainTrendLong && current <= MainTrendLong;
				sellClose = previous > MainTrendLong;

				sellOpen = previous < MainTrendShort && current >= MainTrendShort;
				buyClose = previous < MainTrendShort;
				break;
			}
		case RsiomaSignalModes.Twist:
			{
				if (!TryGetRsi(SignalBar + 2, out var older))
				return;

				buyOpen = previous < older && current > previous;
				sellClose = previous < older;

				sellOpen = previous > older && current < previous;
				buyClose = previous > older;
				break;
			}
		case RsiomaSignalModes.CloudTwist:
			{
				buyOpen = previous < MainTrendShort && current >= MainTrendShort;
				sellClose = previous < MainTrendShort;

				sellOpen = previous > MainTrendLong && current <= MainTrendLong;
				buyClose = previous > MainTrendLong;
				break;
			}
		default:
			return;
		}

		if (!EnableLongEntries)
		buyOpen = false;

		if (!EnableShortEntries)
		sellOpen = false;

		if (!EnableLongExits)
		buyClose = false;

		if (!EnableShortExits)
		sellClose = false;

		if (buyClose && Position > 0m)
		SellMarket();

		if (sellClose && Position < 0m)
		BuyMarket();

		if (buyOpen && Position <= 0m)
		BuyMarket();

		if (sellOpen && Position >= 0m)
		SellMarket();
	}

	private void ResetState()
	{
		_momentumSeed.Clear();
		_rsiHistory.Clear();
		_averagePositive = 0m;
		_averageNegative = 0m;
		_isRsiInitialized = false;
	}

	private void TrimHistory()
	{
		var maxSize = Math.Max(SignalBar + 3, 4);

		while (_rsiHistory.Count > maxSize)
		_rsiHistory.RemoveAt(0);
	}

	private bool TryGetRsi(int shift, out decimal value)
	{
		var index = _rsiHistory.Count - 1 - shift;

		if (index < 0)
		{
			value = 0m;
			return false;
		}

		value = _rsiHistory[index];
		return true;
	}

	private static IIndicator CreateSmoother(RsiomaSmoothingMethods method, int length)
	{
		return method switch
		{
			RsiomaSmoothingMethods.Simple => new SimpleMovingAverage { Length = length },
			RsiomaSmoothingMethods.Exponential => new ExponentialMovingAverage { Length = length },
			RsiomaSmoothingMethods.Smoothed => new SmoothedMovingAverage { Length = length },
			RsiomaSmoothingMethods.Weighted => new WeightedMovingAverage { Length = length },
			_ => throw new NotSupportedException($"Smoothing method '{method}' is not supported."),
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, RsiomaAppliedPrices priceType)
	{
		return priceType switch
		{
			RsiomaAppliedPrices.Close => candle.ClosePrice,
			RsiomaAppliedPrices.Open => candle.OpenPrice,
			RsiomaAppliedPrices.High => candle.HighPrice,
			RsiomaAppliedPrices.Low => candle.LowPrice,
			RsiomaAppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			RsiomaAppliedPrices.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			RsiomaAppliedPrices.Weighted => (candle.ClosePrice * 2m + candle.HighPrice + candle.LowPrice) / 4m,
			RsiomaAppliedPrices.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			RsiomaAppliedPrices.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			RsiomaAppliedPrices.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			RsiomaAppliedPrices.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			RsiomaAppliedPrices.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var baseValue = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
		baseValue = (baseValue + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
		baseValue = (baseValue + candle.HighPrice) / 2m;
		else
		baseValue = (baseValue + candle.ClosePrice) / 2m;

		return ((baseValue - candle.LowPrice) + (baseValue - candle.HighPrice)) / 2m;
	}
}
