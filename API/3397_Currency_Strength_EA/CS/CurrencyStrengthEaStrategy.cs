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

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Currency strength based strategy converted from the MetaTrader expert advisor.
/// The strategy calculates strength scores for eight major currencies using a basket of pairs.
/// It trades the configured security when the base currency is strong and the quote currency is weak.
/// </summary>
public class CurrencyStrengthEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _strengthCandleType;
	private readonly StrategyParam<string> _currencyPairs;
	private readonly StrategyParam<int> _numberOfCandles;
	private readonly StrategyParam<bool> _applySmoothing;
	private readonly StrategyParam<bool> _triangularWeighting;
	private readonly StrategyParam<decimal> _upperLimit;
	private readonly StrategyParam<decimal> _lowerLimit;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<StepModes> _stopMode;
	private readonly StrategyParam<decimal> _stopLossFactor;
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<string> _startTime;
	private readonly StrategyParam<string> _endTime;
	private readonly StrategyParam<TimeModeses> _timeMode;
	private readonly StrategyParam<string> _baseCurrency;
	private readonly StrategyParam<string> _quoteCurrency;

	private readonly Dictionary<string, CurrencyState> _currencyStates = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly List<PairContext> _pairContexts = new();

	private AverageTrueRange _atrIndicator = null!;
	private decimal? _currentAtr;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortTakeProfit;

	/// <summary>
	/// Initializes a new instance of <see cref="CurrencyStrengthEaStrategy"/>.
	/// </summary>
	public CurrencyStrengthEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Entry Candle Type", "Primary candle type used for trading decisions", "General");

		_strengthCandleType = Param(nameof(StrengthCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Strength Candle Type", "Candle type used for currency strength calculation", "General");

		_currencyPairs = Param(nameof(CurrencyPairs), "EURUSD,GBPUSD,USDJPY,USDCHF,USDCAD,AUDUSD,NZDUSD,EURJPY,EURGBP,EURCHF,EURCAD,EURAUD,EURNZD,GBPJPY,GBPCHF,GBPCAD,GBPAUD,GBPNZD,CADJPY,CHFJPY,NZDJPY,AUDJPY,AUDNZD,AUDCAD,AUDCHF,NZDCAD,NZDCHF,CADCHF")
		.SetDisplay("Currency Pairs", "Comma separated list of currency pairs used for strength", "Currency Strength");

		_numberOfCandles = Param(nameof(NumberOfCandles), 55)
		.SetGreaterThanZero()
		.SetDisplay("Lookback", "Number of candles in the strength window", "Currency Strength")
		.SetCanOptimize(true)
		.SetOptimize(20, 120, 5);

		_applySmoothing = Param(nameof(ApplySmoothing), true)
		.SetDisplay("Apply Smoothing", "Smooth ratio values before converting to strength", "Currency Strength");

		_triangularWeighting = Param(nameof(TriangularWeighting), true)
		.SetDisplay("Triangular Weighting", "Use linear weighted smoothing", "Currency Strength");

		_upperLimit = Param(nameof(UpperLimit), 7.2m)
		.SetDisplay("Upper Limit", "Strength level that defines a strong currency", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(6.0m, 8.5m, 0.1m);

		_lowerLimit = Param(nameof(LowerLimit), 3.3m)
		.SetDisplay("Lower Limit", "Strength level that defines a weak currency", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(2.5m, 4.5m, 0.1m);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR lookback used for stop and target", "Risk Management");

		_stopMode = Param(nameof(StopMode), StepModes.InAtr)
		.SetDisplay("Stop Mode", "Select whether stops are based on ATR or absolute points", "Risk Management");

		_stopLossFactor = Param(nameof(StopLossFactor), 0m)
		.SetDisplay("Stop Loss Factor", "Stop loss distance in ATR multiples or points", "Risk Management");

		_takeProfitFactor = Param(nameof(TakeProfitFactor), 1m)
		.SetDisplay("Take Profit Factor", "Take profit distance in ATR multiples or points", "Risk Management");

		_trailingStop = Param(nameof(TrailingStop), 30m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk Management");

		_trailingStep = Param(nameof(TrailingStep), 5m)
		.SetDisplay("Trailing Step", "Minimum move before trailing stop update", "Risk Management");

		_startTime = Param(nameof(StartTime), "10:00")
		.SetDisplay("Start Time", "Trading session start (HH:mm)", "Session");

		_endTime = Param(nameof(EndTime), "16:00")
		.SetDisplay("End Time", "Trading session end (HH:mm)", "Session");

		_timeMode = Param(nameof(TimeMode), TimeModeses.Server)
		.SetDisplay("Time Mode", "Select which clock is used for the session filter", "Session");

		_baseCurrency = Param(nameof(BaseCurrency), "EUR")
		.SetDisplay("Base Currency", "Three letter code of the base currency for the traded symbol", "Signals");

		_quoteCurrency = Param(nameof(QuoteCurrency), "USD")
		.SetDisplay("Quote Currency", "Three letter code of the quote currency for the traded symbol", "Signals");
	}

	/// <summary>
	/// Primary candle type used for generating trade signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used for the currency strength calculation.
	/// </summary>
	public DataType StrengthCandleType
	{
		get => _strengthCandleType.Value;
		set => _strengthCandleType.Value = value;
	}

	/// <summary>
	/// Comma separated list of currency pairs used in the strength basket.
	/// </summary>
	public string CurrencyPairs
	{
		get => _currencyPairs.Value;
		set => _currencyPairs.Value = value;
	}

	/// <summary>
	/// Number of candles considered in the strength window.
	/// </summary>
	public int NumberOfCandles
	{
		get => _numberOfCandles.Value;
		set => _numberOfCandles.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether ratio smoothing is applied.
	/// </summary>
	public bool ApplySmoothing
	{
		get => _applySmoothing.Value;
		set => _applySmoothing.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether triangular weighting is used for smoothing.
	/// </summary>
	public bool TriangularWeighting
	{
		get => _triangularWeighting.Value;
		set => _triangularWeighting.Value = value;
	}

	/// <summary>
	/// Strength threshold that defines a strong currency.
	/// </summary>
	public decimal UpperLimit
	{
		get => _upperLimit.Value;
		set => _upperLimit.Value = value;
	}

	/// <summary>
	/// Strength threshold that defines a weak currency.
	/// </summary>
	public decimal LowerLimit
	{
		get => _lowerLimit.Value;
		set => _lowerLimit.Value = value;
	}

	/// <summary>
	/// ATR period used for protective orders.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Defines whether stop distances are in ATR units or raw points.
	/// </summary>
	public StepModes StopMode
	{
		get => _stopMode.Value;
		set => _stopMode.Value = value;
	}

	/// <summary>
	/// Stop loss distance multiplier.
	/// </summary>
	public decimal StopLossFactor
	{
		get => _stopLossFactor.Value;
		set => _stopLossFactor.Value = value;
	}

	/// <summary>
	/// Take profit distance multiplier.
	/// </summary>
	public decimal TakeProfitFactor
	{
		get => _takeProfitFactor.Value;
		set => _takeProfitFactor.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Minimum move required to update the trailing stop in points.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Trading session opening time (HH:mm format).
	/// </summary>
	public string StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading session closing time (HH:mm format).
	/// </summary>
	public string EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Selects which clock is used for the session filter.
	/// </summary>
	public TimeModeses TimeMode
	{
		get => _timeMode.Value;
		set => _timeMode.Value = value;
	}

	/// <summary>
	/// Three letter base currency code of the traded symbol.
	/// </summary>
	public string BaseCurrency
	{
		get => _baseCurrency.Value;
		set => _baseCurrency.Value = value;
	}

	/// <summary>
	/// Three letter quote currency code of the traded symbol.
	/// </summary>
	public string QuoteCurrency
	{
		get => _quoteCurrency.Value;
		set => _quoteCurrency.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var pair in ParsePairs())
		{
			var security = this.GetSecurity(pair.Symbol);
			if (security != null)
			yield return (security, StrengthCandleType);
		}

		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currencyStates.Clear();
		_pairContexts.Clear();
		_currentAtr = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfit = null;
		_shortTakeProfit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeCurrencies();
		InitializePairs();

		_atrIndicator = new AverageTrueRange { Length = AtrPeriod };

		var tradeSubscription = SubscribeCandles(CandleType);
		tradeSubscription
		.Bind(_atrIndicator, ProcessTradingCandle)
		.Start();

		foreach (var context in _pairContexts)
		{
			var subscription = SubscribeCandles(StrengthCandleType, security: context.Security);
			subscription
			.Bind(context.Highest, context.Lowest, (candle, highestValue, lowestValue) => ProcessStrengthCandle(context, candle, highestValue, lowestValue))
			.Start();
		}
	}

	private void ProcessTradingCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_currentAtr = _atrIndicator.IsFormed ? atrValue : null;

		UpdateTrailingStops(candle);
		ManageActiveStops(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsWithinSession(candle.OpenTime))
		return;

		var baseStrength = TryGetStrength(BaseCurrency);
		var quoteStrength = TryGetStrength(QuoteCurrency);

		if (baseStrength is null || quoteStrength is null)
		return;

		var isLongSignal = baseStrength >= UpperLimit && quoteStrength <= LowerLimit;
		var isShortSignal = baseStrength <= LowerLimit && quoteStrength >= UpperLimit;

		if (isLongSignal && Position <= 0)
		{
			if (Position < 0)
			BuyMarket(Math.Abs(Position));

			BuyMarket(Volume);
			SetProtectiveLevels(candle.ClosePrice, true);
			LogInfo($"Entered long position. Base strength={baseStrength:F2}, Quote strength={quoteStrength:F2}");
		}
		else if (isShortSignal && Position >= 0)
		{
			if (Position > 0)
			SellMarket(Math.Abs(Position));

			SellMarket(Volume);
			SetProtectiveLevels(candle.ClosePrice, false);
			LogInfo($"Entered short position. Base strength={baseStrength:F2}, Quote strength={quoteStrength:F2}");
		}
	}

	private void ProcessStrengthCandle(PairContext context, ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (candle.HighPrice <= candle.LowPrice)
		return;

		var range = highestValue - lowestValue;
		if (range <= 0m)
		return;

		var ratio = (candle.ClosePrice - lowestValue) / range;
		ratio = Math.Min(1m, Math.Max(0m, ratio));

		var smoothed = ApplySmoothing ? context.ProcessRatio(ratio, candle.OpenTime) : ratio;
		if (smoothed is null)
		return;

		var strength = CalculateStrengthLevel(smoothed.Value);
		context.Strength = strength;

		UpdateCurrencyStrengths();
	}

	private void UpdateCurrencyStrengths()
	{
		foreach (var state in _currencyStates.Values)
		{
			state.Sum = 0m;
			state.Count = 0;
		}

		foreach (var context in _pairContexts)
		{
			if (context.Strength is not decimal strength)
			continue;

			var baseState = _currencyStates[context.BaseCurrency];
			baseState.Sum += strength;
			baseState.Count++;

			var quoteState = _currencyStates[context.QuoteCurrency];
			quoteState.Sum += 9m - strength;
			quoteState.Count++;
		}

		foreach (var state in _currencyStates.Values)
		{
			state.Average = state.Count > 0 ? state.Sum / state.Count : null;
		}
	}

	private void InitializeCurrencies()
	{
		var symbols = new[] { "USD", "EUR", "GBP", "CHF", "CAD", "AUD", "JPY", "NZD" };
		foreach (var symbol in symbols)
		{
			_currencyStates[symbol] = new CurrencyState();
		}
	}

	private void InitializePairs()
	{
		_pairContexts.Clear();

		foreach (var pair in ParsePairs())
		{
			var security = this.GetSecurity(pair.Symbol);
			if (security == null)
			{
				LogWarning($"Security '{pair.Symbol}' was not found.");
				continue;
			}

			var highest = new Highest { Length = NumberOfCandles };
			var lowest = new Lowest { Length = NumberOfCandles };

			var context = new PairContext(security, pair.BaseCurrency, pair.QuoteCurrency, highest, lowest, CreateSmoother());
			_pairContexts.Add(context);
		}
	}

	private IIndicatorValueSmoother CreateSmoother()
	{
		if (!ApplySmoothing)
		return null;

		return TriangularWeighting
		? new IndicatorSmoother(new WeightedMovingAverage { Length = NumberOfCandles })
		: new IndicatorSmoother(new SimpleMovingAverage { Length = NumberOfCandles });
	}

	private IEnumerable<(string Symbol, string BaseCurrency, string QuoteCurrency)> ParsePairs()
	{
		return CurrencyPairs
		.Split(',', StringSplitOptions.RemoveEmptyEntries)
		.Select(raw => raw.Trim())
		.Where(raw => raw.Length >= 6)
		.Select(raw => (Symbol: raw, BaseCurrency: raw[..3].ToUpperInvariant(), QuoteCurrency: raw.Substring(raw.Length - 3).ToUpperInvariant()));
	}

	private decimal? TryGetStrength(string currency)
	{
		return _currencyStates.TryGetValue(currency.ToUpperInvariant(), out var state) ? state.Average : null;
	}

	private void SetProtectiveLevels(decimal entryPrice, bool isLong)
	{
		var stopDistance = CalculateStopDistance();
		var takeDistance = CalculateTakeDistance();

		if (isLong)
		{
			_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;
			_longTakeProfit = takeDistance > 0m ? entryPrice + takeDistance : null;
			_shortStopPrice = null;
			_shortTakeProfit = null;
		}
		else
		{
			_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;
			_shortTakeProfit = takeDistance > 0m ? entryPrice - takeDistance : null;
			_longStopPrice = null;
			_longTakeProfit = null;
		}
	}

	private decimal CalculateStopDistance()
	{
		if (StopLossFactor <= 0m)
		return 0m;

		return StopMode switch
		{
			StepModes.InAtr => _currentAtr is decimal atr ? atr * StopLossFactor : 0m,
			StepModes.InPips => Security?.PriceStep is decimal step ? step * StopLossFactor : 0m,
			_ => 0m,
		};
	}

	private decimal CalculateTakeDistance()
	{
		if (TakeProfitFactor <= 0m)
		return 0m;

		return StopMode switch
		{
			StepModes.InAtr => _currentAtr is decimal atr ? atr * TakeProfitFactor : 0m,
			StepModes.InPips => Security?.PriceStep is decimal step ? step * TakeProfitFactor : 0m,
			_ => 0m,
		};
	}

	private void ManageActiveStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				_longStopPrice = null;
				_longTakeProfit = null;
				LogInfo("Long stop loss triggered.");
			}
			else if (_longTakeProfit is decimal target && candle.HighPrice >= target)
			{
				SellMarket(Math.Abs(Position));
				_longStopPrice = null;
				_longTakeProfit = null;
				LogInfo("Long take profit triggered.");
			}
		}
		else if (Position < 0)
		{
			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				_shortStopPrice = null;
				_shortTakeProfit = null;
				LogInfo("Short stop loss triggered.");
			}
			else if (_shortTakeProfit is decimal target && candle.LowPrice <= target)
			{
				BuyMarket(Math.Abs(Position));
				_shortStopPrice = null;
				_shortTakeProfit = null;
				LogInfo("Short take profit triggered.");
			}
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (Security?.PriceStep is not decimal step || TrailingStop <= 0m)
		return;

		var trailingDistance = step * TrailingStop;
		var trailingStep = step * TrailingStep;

		if (Position > 0 && _longStopPrice is decimal stop)
		{
			var newStop = candle.ClosePrice - trailingDistance;
			if (newStop - stop >= trailingStep)
			{
				_longStopPrice = newStop;
				LogInfo($"Trailing stop moved to {_longStopPrice:F5}.");
			}
		}
		else if (Position < 0 && _shortStopPrice is decimal stop)
		{
			var newStop = candle.ClosePrice + trailingDistance;
			if (stop - newStop >= trailingStep)
			{
				_shortStopPrice = newStop;
				LogInfo($"Trailing stop moved to {_shortStopPrice:F5}.");
			}
		}
	}

	private bool IsWithinSession(DateTimeOffset time)
	{
		if (!TryParseTime(StartTime, out var start) || !TryParseTime(EndTime, out var end))
		return true;

		var reference = TimeMode switch
		{
			TimeModeses.Server => time,
			TimeModeses.Gmt => time.ToUniversalTime(),
			TimeModeses.Local => time.ToLocalTime(),
			_ => time,
		};

		var timeOfDay = reference.TimeOfDay;

		return start <= end
		? timeOfDay >= start && timeOfDay < end
		: timeOfDay >= start || timeOfDay < end;
	}

	private static bool TryParseTime(string value, out TimeSpan result)
	{
		return TimeSpan.TryParseExact(value, "hh\\:mm", CultureInfo.InvariantCulture, out result);
	}

	private static decimal CalculateStrengthLevel(decimal ratio)
	{
		return ratio switch
		{
			>= 0.97m => 9m,
			>= 0.90m => 8m,
			>= 0.75m => 7m,
			>= 0.60m => 6m,
			>= 0.50m => 5m,
			>= 0.40m => 4m,
			>= 0.25m => 3m,
			>= 0.10m => 2m,
			>= 0.03m => 1m,
			_ => 0m,
		};
	}

	private sealed record CurrencyState
	{
		public decimal Sum { get; set; }
		public int Count { get; set; }
		public decimal? Average { get; set; }
	}

	private sealed class PairContext
	{
		private readonly IIndicatorValueSmoother _smoother;

		public PairContext(Security security, string baseCurrency, string quoteCurrency, Highest highest, Lowest lowest, IIndicatorValueSmoother smoother)
		{
			Security = security;
			BaseCurrency = baseCurrency;
			QuoteCurrency = quoteCurrency;
			Highest = highest;
			Lowest = lowest;
			_smoother = smoother;
		}

		public Security Security { get; }
		public string BaseCurrency { get; }
		public string QuoteCurrency { get; }
		public Highest Highest { get; }
		public Lowest Lowest { get; }
		public decimal? Strength { get; set; }

		public decimal? ProcessRatio(decimal ratio, DateTimeOffset time)
		{
			return _smoother == null ? ratio : _smoother.Process(ratio, time);
		}
	}

	private interface IIndicatorValueSmoother
	{
		decimal? Process(decimal value, DateTimeOffset time);
	}

	private sealed class IndicatorSmoother : IIndicatorValueSmoother
	{
		private readonly IIndicator _indicator;

		public IndicatorSmoother(IIndicator indicator)
		{
			_indicator = indicator;
		}

		public decimal? Process(decimal value, DateTimeOffset time)
		{
			var indicatorValue = _indicator.Process(new DecimalIndicatorValue(_indicator, value, time));
			return indicatorValue.IsFinal ? indicatorValue.ToNullableDecimal() : null;
		}
	}

	/// <summary>
	/// Session clock modes.
	/// </summary>
	public enum TimeModeses
	{
		Server,
		Gmt,
		Local,
	}

	/// <summary>
	/// Distance mode for protective orders.
	/// </summary>
	public enum StepModes
	{
		InAtr,
		InPips,
	}
}

