using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the ColorXPWMA Digit indicator with money management recounter logic.
/// </summary>
public class ColorXpWmaDigitMmRecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _indicatorPeriod;
	private readonly StrategyParam<decimal> _indicatorPower;
	private readonly StrategyParam<SmoothMethod> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _smoothingPhase;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<int> _roundingDigits;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<decimal> _normalVolume;
	private readonly StrategyParam<decimal> _reducedVolume;
	private readonly StrategyParam<int> _buyTotalTrigger;
	private readonly StrategyParam<int> _buyLossTrigger;
	private readonly StrategyParam<int> _sellTotalTrigger;
	private readonly StrategyParam<int> _sellLossTrigger;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private ColorXpWmaDigitIndicator _indicator = null!;

	private readonly List<int> _colorHistory = new();
	private readonly List<decimal> _longResults = new();
	private readonly List<decimal> _shortResults = new();

	private decimal? _longEntryPrice;
	private decimal _longEntryVolume;
	private decimal? _shortEntryPrice;
	private decimal _shortEntryVolume;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ColorXpWmaDigitMmRecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for processing", "General");

		_indicatorPeriod = Param(nameof(IndicatorPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Indicator Period", "Number of bars for PWMA", "Indicator")
			.SetCanOptimize(true);

		_indicatorPower = Param(nameof(IndicatorPower), 2.00001m)
			.SetDisplay("Power", "Exponent applied to weights", "Indicator")
			.SetCanOptimize(true);

		_smoothingMethod = Param(nameof(SmoothingMethod), SmoothMethod.Sma)
			.SetDisplay("Smoothing Method", "Moving average applied to PWMA", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Length for the smoothing average", "Indicator")
			.SetCanOptimize(true);

		_smoothingPhase = Param(nameof(SmoothingPhase), 15)
			.SetDisplay("Smoothing Phase", "Phase parameter for some smoothers", "Indicator")
			.SetCanOptimize(true);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrice.Close)
			.SetDisplay("Applied Price", "Price source for the indicator", "Indicator");

		_roundingDigits = Param(nameof(RoundingDigits), 2)
			.SetDisplay("Rounding Digits", "Digits for rounding the indicator", "Indicator")
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Shift used to read colors", "Logic")
			.SetCanOptimize(true);

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Buy Entries", "Allow opening long positions", "Permissions");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Sell Entries", "Allow opening short positions", "Permissions");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
			.SetDisplay("Enable Buy Exits", "Allow closing longs on sell signals", "Permissions");

		_enableSellExits = Param(nameof(EnableSellExits), true)
			.SetDisplay("Enable Sell Exits", "Allow closing shorts on buy signals", "Permissions");

		_normalVolume = Param(nameof(NormalVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Normal Volume", "Default trade volume", "Money Management");

		_reducedVolume = Param(nameof(ReducedVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Reduced Volume", "Volume used after a loss streak", "Money Management");

		_buyTotalTrigger = Param(nameof(BuyTotalTrigger), 5)
			.SetNotNegative()
			.SetDisplay("Buy Total Trigger", "Number of recent buys checked", "Money Management");

		_buyLossTrigger = Param(nameof(BuyLossTrigger), 3)
			.SetNotNegative()
			.SetDisplay("Buy Loss Trigger", "Loss count switching to reduced volume", "Money Management");

		_sellTotalTrigger = Param(nameof(SellTotalTrigger), 5)
			.SetNotNegative()
			.SetDisplay("Sell Total Trigger", "Number of recent sells checked", "Money Management");

		_sellLossTrigger = Param(nameof(SellLossTrigger), 3)
			.SetNotNegative()
			.SetDisplay("Sell Loss Trigger", "Loss count switching to reduced volume", "Money Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetNotNegative()
			.SetDisplay("Stop Loss Points", "Protective stop distance in points", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetNotNegative()
			.SetDisplay("Take Profit Points", "Protective take profit distance in points", "Risk Management");
	}

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Power weighted moving average period.
	/// </summary>
	public int IndicatorPeriod
	{
		get => _indicatorPeriod.Value;
		set => _indicatorPeriod.Value = value;
	}

	/// <summary>
	/// Exponent used in weights.
	/// </summary>
	public decimal IndicatorPower
	{
		get => _indicatorPower.Value;
		set => _indicatorPower.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the weighted average.
	/// </summary>
	public SmoothMethod SmoothingMethod
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
	/// Phase parameter forwarded to smoothing algorithms.
	/// </summary>
	public int SmoothingPhase
	{
		get => _smoothingPhase.Value;
		set => _smoothingPhase.Value = value;
	}

	/// <summary>
	/// Selected price source.
	/// </summary>
	public AppliedPrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Digits used to round the indicator output.
	/// </summary>
	public int RoundingDigits
	{
		get => _roundingDigits.Value;
		set => _roundingDigits.Value = value;
	}

	/// <summary>
	/// Shift applied when reading color values.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on sell signals.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on buy signals.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
	}

	/// <summary>
	/// Default volume used for entries.
	/// </summary>
	public decimal NormalVolume
	{
		get => _normalVolume.Value;
		set => _normalVolume.Value = value;
	}

	/// <summary>
	/// Reduced volume used after loss streaks.
	/// </summary>
	public decimal ReducedVolume
	{
		get => _reducedVolume.Value;
		set => _reducedVolume.Value = value;
	}

	/// <summary>
	/// Number of buy trades inspected by the money management filter.
	/// </summary>
	public int BuyTotalTrigger
	{
		get => _buyTotalTrigger.Value;
		set => _buyTotalTrigger.Value = value;
	}

	/// <summary>
	/// Loss threshold switching buys to reduced volume.
	/// </summary>
	public int BuyLossTrigger
	{
		get => _buyLossTrigger.Value;
		set => _buyLossTrigger.Value = value;
	}

	/// <summary>
	/// Number of sell trades inspected by the money management filter.
	/// </summary>
	public int SellTotalTrigger
	{
		get => _sellTotalTrigger.Value;
		set => _sellTotalTrigger.Value = value;
	}

	/// <summary>
	/// Loss threshold switching sells to reduced volume.
	/// </summary>
	public int SellLossTrigger
	{
		get => _sellLossTrigger.Value;
		set => _sellLossTrigger.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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

		_indicator = new ColorXpWmaDigitIndicator
		{
			Period = IndicatorPeriod,
			Power = IndicatorPower,
			Method = SmoothingMethod,
			SmoothingLength = SmoothingLength,
			Phase = SmoothingPhase,
			AppliedPrice = AppliedPrice,
			RoundingDigits = RoundingDigits,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_indicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawOwnTrades(area);
		}

		var step = Security?.PriceStep ?? 1m;
		Unit? stop = StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Point) : null;
		Unit? take = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null;
		if (stop != null || take != null)
			StartProtection(takeProfit: take, stopLoss: stop);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var xpValue = indicatorValue as ColorXpWmaDigitValue;
		if (xpValue == null)
			return;

		if (xpValue.Color is not int color || xpValue.Line is not decimal)
			return;

		_colorHistory.Add(color);
		TrimHistory();

		var required = SignalBar + 2;
		if (_colorHistory.Count < required)
			return;

		var olderIndex = _colorHistory.Count - (SignalBar + 2);
		var currentIndex = _colorHistory.Count - (SignalBar + 1);
		var olderColor = _colorHistory[olderIndex];
		var currentColor = _colorHistory[currentIndex];

		var closeLong = false;
		var closeShort = false;
		var openLong = false;
		var openShort = false;

		if (olderColor == 2)
		{
			if (EnableBuyEntries && currentColor != 2)
				openLong = true;
			if (EnableSellExits)
				closeShort = true;
		}

		if (olderColor == 0)
		{
			if (EnableSellEntries && currentColor != 0)
				openShort = true;
			if (EnableBuyExits)
				closeLong = true;
		}

		var shouldOpenLong = openLong && Position <= 0;
		var shouldOpenShort = openShort && Position >= 0;

		if (shouldOpenLong)
		{
			var closeShortVolume = Position < 0 ? Math.Abs(Position) : 0m;

			if (closeShortVolume > 0)
			{
				RegisterShortResult(candle.ClosePrice);
			}

			var entryVolume = GetMoneyManagementVolume(true);

			if (entryVolume > 0 || closeShortVolume > 0)
			{
				BuyMarket(entryVolume + closeShortVolume);

				if (entryVolume > 0)
				{
					_longEntryPrice = candle.ClosePrice;
					_longEntryVolume = entryVolume;
				}
				else
				{
					_longEntryPrice = null;
					_longEntryVolume = 0;
				}

				_shortEntryPrice = null;
				_shortEntryVolume = 0;
			}

			return;
		}

		if (shouldOpenShort)
		{
			var closeLongVolume = Position > 0 ? Position : 0m;

			if (closeLongVolume > 0)
			{
				RegisterLongResult(candle.ClosePrice);
			}

			var entryVolume = GetMoneyManagementVolume(false);

			if (entryVolume > 0 || closeLongVolume > 0)
			{
				SellMarket(entryVolume + closeLongVolume);

				if (entryVolume > 0)
				{
					_shortEntryPrice = candle.ClosePrice;
					_shortEntryVolume = entryVolume;
				}
				else
				{
					_shortEntryPrice = null;
					_shortEntryVolume = 0;
				}

				_longEntryPrice = null;
				_longEntryVolume = 0;
			}

			return;
		}

		if (closeLong && Position > 0)
		{
			SellMarket(Position);
			RegisterLongResult(candle.ClosePrice);
		}

		if (closeShort && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			RegisterShortResult(candle.ClosePrice);
		}
	}

	private decimal GetMoneyManagementVolume(bool isLong)
	{
		var history = isLong ? _longResults : _shortResults;
		var total = isLong ? BuyTotalTrigger : SellTotalTrigger;
		var losses = isLong ? BuyLossTrigger : SellLossTrigger;

		if (total <= 0)
			return NormalVolume;

		var count = 0;
		var lossCount = 0;
		for (var i = history.Count - 1; i >= 0 && count < total; i--)
		{
			if (history[i] < 0)
				lossCount++;

			count++;

			if (lossCount >= losses)
				return ReducedVolume;
		}

		return NormalVolume;
	}

	private void RegisterLongResult(decimal exitPrice)
	{
		if (_longEntryPrice is decimal entryPrice && _longEntryVolume > 0)
		{
			var result = (exitPrice - entryPrice) * _longEntryVolume;
			_longResults.Add(result);
			TrimResults(_longResults, Math.Max(BuyTotalTrigger, 1));
		}

		_longEntryPrice = null;
		_longEntryVolume = 0;
	}

	private void RegisterShortResult(decimal exitPrice)
	{
		if (_shortEntryPrice is decimal entryPrice && _shortEntryVolume > 0)
		{
			var result = (entryPrice - exitPrice) * _shortEntryVolume;
			_shortResults.Add(result);
			TrimResults(_shortResults, Math.Max(SellTotalTrigger, 1));
		}

		_shortEntryPrice = null;
		_shortEntryVolume = 0;
	}

	private static void TrimResults(List<decimal> results, int maxCount)
	{
		if (maxCount <= 0)
			return;

		var extra = results.Count - maxCount * 2;
		if (extra > 0)
			results.RemoveRange(0, extra);
	}

	private void TrimHistory()
	{
		var maxNeeded = Math.Max(SignalBar + 2, 10);
		var extra = _colorHistory.Count - maxNeeded;
		if (extra > 0)
			_colorHistory.RemoveRange(0, extra);
	}
}

/// <summary>
/// ColorXPWMA Digit indicator producing both value and color buffers.
/// </summary>
public class ColorXpWmaDigitIndicator : BaseIndicator<decimal>
{
	private readonly Queue<decimal> _prices = new();
	private decimal[] _weights = Array.Empty<decimal>();
	private int _weightsPeriod;
	private decimal _lastPower;
	private decimal _weightsSum;
	private IIndicator _smoother;
	private SmoothMethod _lastMethod;
	private int _lastLength;
	private decimal? _previousLine;
	private int? _previousColor;

	/// <summary>
	/// Number of bars used in the weighted average.
	/// </summary>
	public int Period { get; set; } = 14;

	/// <summary>
	/// Exponent used for weighting.
	/// </summary>
	public decimal Power { get; set; } = 2.00001m;

	/// <summary>
	/// Smoothing method applied to the weighted average.
	/// </summary>
	public SmoothMethod Method { get; set; } = SmoothMethod.Sma;

	/// <summary>
	/// Length of the smoothing moving average.
	/// </summary>
	public int SmoothingLength { get; set; } = 5;

	/// <summary>
	/// Phase parameter forwarded to smoothers supporting it.
	/// </summary>
	public int Phase { get; set; } = 15;

	/// <summary>
	/// Price source used by the indicator.
	/// </summary>
	public AppliedPrice AppliedPrice { get; set; } = AppliedPrice.Close;

	/// <summary>
	/// Digits used to round the final line.
	/// </summary>
	public int RoundingDigits { get; set; } = 2;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle)
			return new ColorXpWmaDigitValue(this, input, null, _previousColor);

		if (candle.State != CandleStates.Finished)
			return new ColorXpWmaDigitValue(this, input, null, _previousColor);

		if (Period <= 0)
			throw new InvalidOperationException("Period must be positive.");

		EnsureWeights();
		EnsureSmoother();

		var price = GetPrice(candle);
		_prices.Enqueue(price);
		if (_prices.Count > Period)
			_prices.Dequeue();

		if (_prices.Count < Period)
			return new ColorXpWmaDigitValue(this, input, null, _previousColor);

		var prices = _prices.ToArray();
		decimal sum = 0;
		for (var i = 0; i < Period; i++)
		{
			var priceIndex = prices.Length - 1 - i;
			sum += prices[priceIndex] * _weights[i];
		}

		var weighted = sum / _weightsSum;
		var smoothingInput = new DecimalIndicatorValue(_smoother!, weighted, input.Time);
		var smoothedValue = _smoother!.Process(smoothingInput);
		if (!smoothedValue.IsFinal)
			return new ColorXpWmaDigitValue(this, input, null, _previousColor);

		var line = smoothedValue.ToDecimal();
		if (RoundingDigits >= 0)
			line = Math.Round(line, RoundingDigits, MidpointRounding.AwayFromZero);

		var color = 1;
		if (_previousLine is decimal prev)
		{
			if (line > prev)
				color = 2;
			else if (line < prev)
				color = 0;
			else if (_previousColor is int existing)
				color = existing;
		}

		_previousLine = line;
		_previousColor = color;

		return new ColorXpWmaDigitValue(this, input, line, color);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_prices.Clear();
		_previousLine = null;
		_previousColor = null;
	}

	private void EnsureWeights()
	{
		if (_weightsPeriod == Period && _lastPower == Power)
			return;

		_weightsPeriod = Period;
		_lastPower = Power;
		_weights = new decimal[Period];
		decimal sum = 0;
		for (var i = 0; i < Period; i++)
		{
			var weight = (decimal)Math.Pow(Period - i, (double)Power);
			_weights[i] = weight;
			sum += weight;
		}

		_weightsSum = sum;
	}

	private void EnsureSmoother()
	{
		if (_smoother != null && _lastMethod == Method && _lastLength == SmoothingLength)
			return;

		_lastMethod = Method;
		_lastLength = SmoothingLength;

		_smoother = Method switch
		{
			SmoothMethod.Sma => new SimpleMovingAverage { Length = SmoothingLength },
			SmoothMethod.Ema => new ExponentialMovingAverage { Length = SmoothingLength },
			SmoothMethod.Smma => new SmoothedMovingAverage { Length = SmoothingLength },
			SmoothMethod.Lwma => new WeightedMovingAverage { Length = SmoothingLength },
			SmoothMethod.Jjma => new JurikMovingAverage { Length = SmoothingLength },
			SmoothMethod.JurX => new JurikMovingAverage { Length = SmoothingLength },
			SmoothMethod.ParMa => new ExponentialMovingAverage { Length = SmoothingLength },
			SmoothMethod.T3 => new TripleExponentialMovingAverage { Length = SmoothingLength },
			SmoothMethod.Vidya => new ExponentialMovingAverage { Length = SmoothingLength },
			SmoothMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = SmoothingLength },
			_ => new SimpleMovingAverage { Length = SmoothingLength },
		};
	}

	private decimal GetPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarted => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			AppliedPrice.Demark => GetDemarkPrice(candle),
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

/// <summary>
/// Indicator output for <see cref="ColorXpWmaDigitIndicator"/>.
/// </summary>
public class ColorXpWmaDigitValue : ComplexIndicatorValue
{
	/// <summary>
	/// Initializes a new instance of the indicator value.
	/// </summary>
	public ColorXpWmaDigitValue(IIndicator indicator, IIndicatorValue input, decimal? line, int? color)
		: base(indicator, input, (nameof(Line), line), (nameof(Color), color))
	{
	}

	/// <summary>
	/// Rounded PWMA value.
	/// </summary>
	public decimal? Line => GetNullableDecimal(nameof(Line));

	/// <summary>
	/// Color state: 0 for downtrend, 1 for neutral, 2 for uptrend.
	/// </summary>
	public int? Color => GetValue(nameof(Color)) as int?;

	private decimal? GetNullableDecimal(string name)
	{
		var value = GetValue(name);
		return value is decimal d ? d : null;
	}
}

/// <summary>
/// Available smoothing methods.
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
	/// Smoothed moving average.
	/// </summary>
	Smma,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma,

	/// <summary>
	/// Jurik moving average.
	/// </summary>
	Jjma,

	/// <summary>
	/// JurX moving average approximation.
	/// </summary>
	JurX,

	/// <summary>
	/// Parabolic moving average approximation.
	/// </summary>
	ParMa,

	/// <summary>
	/// Triple exponential moving average.
	/// </summary>
	T3,

	/// <summary>
	/// Variable index dynamic average approximation.
	/// </summary>
	Vidya,

	/// <summary>
	/// Kaufman adaptive moving average.
	/// </summary>
	Ama,
}

/// <summary>
/// Price sources for the indicator.
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
	/// Typical price (high + low + close) / 3.
	/// </summary>
	Typical,

	/// <summary>
	/// Weighted close price.
	/// </summary>
	Weighted,

	/// <summary>
	/// Simple (open + close) / 2.
	/// </summary>
	Simple,

	/// <summary>
	/// Quarted price (high + low + open + close) / 4.
	/// </summary>
	Quarted,

	/// <summary>
	/// Trend follow price (high or low based on candle direction).
	/// </summary>
	TrendFollow0,

	/// <summary>
	/// Trend follow price variant using half-range offsets.
	/// </summary>
	TrendFollow1,

	/// <summary>
	/// Demark price.
	/// </summary>
	Demark,
}
