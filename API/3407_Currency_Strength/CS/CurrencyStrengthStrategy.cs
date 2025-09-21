using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Currency strength strategy converted from the "Currency Strength" MQL4 expert.
/// The strategy aggregates the relative performance of the eight major currencies
/// across twenty-eight FX pairs and trades the current symbol when its base
/// currency is the strongest and the quote currency is the weakest.
/// </summary>
public class CurrencyStrengthStrategy : Strategy
{
	private sealed class PairStrengthState
	{
		public PairStrengthState(Security security)
		{
			Security = security;
		}

		public Security Security { get; }

		public decimal? PreviousClose { get; private set; }

		public decimal? LastClose { get; private set; }

		public decimal? PercentChange { get; private set; }

		public void Update(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (candle.ClosePrice <= 0m)
				return;

			if (LastClose is decimal last)
				PreviousClose = last;

			LastClose = candle.ClosePrice;

			if (PreviousClose is decimal prev && prev != 0m)
			{
				PercentChange = (LastClose.Value - prev) / prev * 100m;
			}
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0} Prev={1} Last={2} Change={3}", Security.Id, PreviousClose, LastClose, PercentChange);
		}
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentumIndicator = null!;
	private MovingAverageConvergenceDivergence _macdIndicator = null!;

	private readonly Dictionary<string, PairStrengthState> _pairStates = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, decimal> _currencyStrengths = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<Security> _additionalSecurities = new();

	private DateTimeOffset _lastStrengthLogTime;

	private static readonly string[] _strengthPairs =
	{
		"EURGBP", "EURAUD", "EURNZD", "EURUSD", "EURCAD", "EURCHF", "EURJPY",
		"GBPAUD", "GBPNZD", "GBPUSD", "GBPCAD", "GBPCHF", "GBPJPY",
		"AUDNZD", "AUDUSD", "AUDCAD", "AUDCHF", "AUDJPY",
		"NZDUSD", "NZDCAD", "NZDCHF", "NZDJPY",
		"USDCAD", "USDCHF", "USDJPY",
		"CADCHF", "CADJPY",
		"CHFJPY"
	};

	/// <summary>
	/// Initializes a new instance of the <see cref="CurrencyStrengthStrategy"/> class.
	/// </summary>
	public CurrencyStrengthStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for every candle subscription.", "Data");

		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(4, 20, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 150, 5);

		_momentumLength = Param(nameof(MomentumLength), 14)
			.SetDisplay("Momentum Length", "Number of candles used for the momentum calculation.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetDisplay("Momentum Threshold", "Absolute momentum level required to open a trade.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1.5m, 0.1m);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD Fast", "Fast exponential average for the MACD filter.", "Indicators");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD Slow", "Slow exponential average for the MACD filter.", "Indicators");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("MACD Signal", "Signal average for the MACD filter.", "Indicators");
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the fast linear weighted moving average.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow linear weighted moving average.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum period used to assess volatility strength.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Minimum momentum absolute value required to open new trades.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Fast period parameter of the MACD filter.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow period parameter of the MACD filter.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal period parameter of the MACD filter.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		foreach (var security in _additionalSecurities)
			yield return (security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMa = null!;
		_slowMa = null!;
		_momentumIndicator = null!;
		_macdIndicator = null!;
		_pairStates.Clear();
		_currencyStrengths.Clear();
		_additionalSecurities.Clear();
		_lastStrengthLogTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaLength };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaLength };
		_momentumIndicator = new Momentum { Length = MomentumLength };
		_macdIndicator = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastLength,
			LongPeriod = MacdSlowLength,
			SignalPeriod = MacdSignalLength
		};

		InitializeStrengthSubscriptions();

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(ProcessMainCandle).Start();

		StartProtection();
	}

	private void InitializeStrengthSubscriptions()
	{
		_pairStates.Clear();
		_currencyStrengths.Clear();
		_additionalSecurities.Clear();

		foreach (var pairCode in _strengthPairs)
		{
			var security = this.GetSecurity(pairCode);
			if (security == null)
			{
				LogWarning($"Security '{pairCode}' is not available for strength calculation.");
				continue;
			}

			var state = new PairStrengthState(security);
			_pairStates[security.Code ?? security.Id] = state;

			if (Security == null || security.Id != Security.Id)
				_additionalSecurities.Add(security);

			SubscribeCandles(CandleType, security: security)
				.Bind(candle => ProcessStrengthCandle(state, candle))
				.Start();
		}
	}

	private void ProcessStrengthCandle(PairStrengthState state, ICandleMessage candle)
	{
		state.Update(candle);
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		var fastValue = _fastMa.Process(typicalPrice, candle.OpenTime, true);
		var slowValue = _slowMa.Process(typicalPrice, candle.OpenTime, true);
		var momentumValue = _momentumIndicator.Process(candle.ClosePrice, candle.OpenTime, true);
		var macdRaw = _macdIndicator.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!fastValue.IsFinal || !slowValue.IsFinal || !momentumValue.IsFinal)
			return;

		if (macdRaw is not MovingAverageConvergenceDivergenceValue macdValue)
			return;

		if (macdValue.Macd is not decimal macdLine || macdValue.Signal is not decimal signalLine)
			return;

		var fastMa = fastValue.ToDecimal();
		var slowMa = slowValue.ToDecimal();
		var momentum = momentumValue.ToDecimal();

		var (strongest, weakest) = RecalculateCurrencyStrengths();

		if (Security?.Code.IsEmpty() != false)
			return;

		var (baseCurrency, quoteCurrency) = SplitCurrencyPair(Security.Code);
		if (baseCurrency.IsEmpty() || quoteCurrency.IsEmpty())
			return;

		var longBias = string.Equals(strongest, baseCurrency, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(weakest, quoteCurrency, StringComparison.OrdinalIgnoreCase);
		var shortBias = string.Equals(strongest, quoteCurrency, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(weakest, baseCurrency, StringComparison.OrdinalIgnoreCase);

		var momentumReady = Math.Abs(momentum) >= MomentumThreshold;
		var macdBullish = macdLine > 0m && macdLine > signalLine;
		var macdBearish = macdLine < 0m && macdLine < signalLine;

		if (longBias && fastMa > slowMa && macdBullish && momentumReady)
		{
			if (Position < 0)
				ClosePosition();

			if (Position <= 0)
			{
				BuyMarket();
				LogInfo($"Opened long position. Strongest={strongest} Weakest={weakest} Momentum={momentum:F2} MACD={macdLine:F4}");
			}
		}
		else if (shortBias && fastMa < slowMa && macdBearish && momentumReady)
		{
			if (Position > 0)
				ClosePosition();

			if (Position >= 0)
			{
				SellMarket();
				LogInfo($"Opened short position. Strongest={strongest} Weakest={weakest} Momentum={momentum:F2} MACD={macdLine:F4}");
			}
		}
		else
		{
			if (Position > 0 && (!longBias || fastMa <= slowMa || !macdBullish))
			{
				ClosePosition();
				LogInfo("Closed long position because bullish conditions were lost.");
			}
			else if (Position < 0 && (!shortBias || fastMa >= slowMa || !macdBearish))
			{
				ClosePosition();
				LogInfo("Closed short position because bearish conditions were lost.");
			}
		}

		TryLogStrengthSnapshot(candle.OpenTime);
	}

	private (string? Strongest, string? Weakest) RecalculateCurrencyStrengths()
	{
		_currencyStrengths.Clear();

		foreach (var currency in new[] { "EUR", "GBP", "AUD", "NZD", "USD", "CAD", "CHF", "JPY" })
		{
			_currencyStrengths[currency] = 0m;
		}

		foreach (var state in _pairStates.Values)
		{
			if (state.PercentChange is not decimal change)
				continue;

			var (baseCurrency, quoteCurrency) = SplitCurrencyPair(state.Security.Code);
			if (baseCurrency.IsEmpty() || quoteCurrency.IsEmpty())
				continue;

			_currencyStrengths[baseCurrency] += change;
			_currencyStrengths[quoteCurrency] -= change;
		}

		if (_currencyStrengths.Count == 0)
			return (null, null);

		var strongest = _currencyStrengths.Aggregate((l, r) => r.Value > l.Value ? r : l).Key;
		var weakest = _currencyStrengths.Aggregate((l, r) => r.Value < l.Value ? r : l).Key;

		return (strongest, weakest);
	}

	private void TryLogStrengthSnapshot(DateTimeOffset time)
	{
		if (time - _lastStrengthLogTime < TimeSpan.FromMinutes(30))
			return;

		_lastStrengthLogTime = time;

		if (_currencyStrengths.Count == 0)
			return;

		var ordered = _currencyStrengths
			.OrderByDescending(p => p.Value)
			.Select(p => string.Format(CultureInfo.InvariantCulture, "{0}:{1:F2}", p.Key, p.Value))
			.Join(" | ");

		LogInfo($"Currency strengths => {ordered}");
	}

	private static (string Base, string Quote) SplitCurrencyPair(string? code)
	{
		if (code.IsEmpty())
			return (string.Empty, string.Empty);

		var normalized = code!.Replace("/", string.Empty, StringComparison.Ordinal);
		if (normalized.Length < 6)
			return (string.Empty, string.Empty);

		var baseCurrency = normalized.Substring(0, 3).ToUpperInvariant();
		var quoteCurrency = normalized.Substring(3, 3).ToUpperInvariant();

		return (baseCurrency, quoteCurrency);
	}
}
