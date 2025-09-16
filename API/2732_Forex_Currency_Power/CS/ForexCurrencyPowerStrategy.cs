
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ecng.ComponentModel;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the FOREX currency power dashboard and trades the strongest currency against the weakest.
/// The approach follows the MQL implementation by aggregating four major pairs per currency and normalizing their momentum.
/// </summary>
public class ForexCurrencyPowerStrategy : Strategy
{
	/// <summary>
	/// Holds indicator instances and the latest normalized power for a single FX pair.
	/// </summary>
	private sealed class PairState
	{
		public PairState(Security security, int lookback)
		{
			Security = security;
			HighIndicator = new Highest
			{
				Length = lookback,
				CandlePrice = CandlePrice.High
			};
			LowIndicator = new Lowest
			{
				Length = lookback,
				CandlePrice = CandlePrice.Low
			};
		}

		public Security Security { get; }

		public Highest HighIndicator { get; }

		public Lowest LowIndicator { get; }

		public decimal? Power { get; set; }
	}

	/// <summary>
	/// Aggregates all contributing pairs for a single currency and stores the blended strength value.
	/// </summary>
	private sealed class CurrencyState
	{
		public CurrencyState(string code)
		{
			Code = code;
			Contributions = new List<(PairState Pair, bool Inverse)>();
		}

		public string Code { get; }

		public List<(PairState Pair, bool Inverse)> Contributions { get; }

		public decimal? Power { get; set; }
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<string> _baseCurrency;
	private readonly StrategyParam<string> _quoteCurrency;
	private readonly StrategyParam<Security> _eurUsd;
	private readonly StrategyParam<Security> _eurGbp;
	private readonly StrategyParam<Security> _eurChf;
	private readonly StrategyParam<Security> _eurJpy;
	private readonly StrategyParam<Security> _gbpUsd;
	private readonly StrategyParam<Security> _usdChf;
	private readonly StrategyParam<Security> _usdJpy;
	private readonly StrategyParam<Security> _gbpChf;
	private readonly StrategyParam<Security> _gbpJpy;
	private readonly StrategyParam<Security> _chfJpy;

	private readonly Dictionary<string, PairState> _pairStates = new();
	private readonly Dictionary<string, CurrencyState> _currencyStates = new(StringComparer.OrdinalIgnoreCase);

	private DateTimeOffset _lastLogTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="ForexCurrencyPowerStrategy"/> class.
	/// </summary>
	public ForexCurrencyPowerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for candle subscriptions", "Data");

		_lookback = Param(nameof(Lookback), 5)
		.SetDisplay("Lookback", "Number of candles used for high/low range", "Currency Power")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_entryThreshold = Param(nameof(EntryThreshold), 15m)
		.SetDisplay("Entry Threshold", "Minimum power difference to enter a position", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(5m, 30m, 5m);

		_exitThreshold = Param(nameof(ExitThreshold), 5m)
		.SetDisplay("Exit Threshold", "Difference below which the position is closed", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(2m, 15m, 1m);

		_baseCurrency = Param(nameof(BaseCurrency), "EUR")
		.SetDisplay("Base Currency", "Currency bought when going long", "Trading");

		_quoteCurrency = Param(nameof(QuoteCurrency), "USD")
		.SetDisplay("Quote Currency", "Currency sold when going long", "Trading");

		_eurUsd = Param<Security>(nameof(EurUsd))
		.SetDisplay("EUR/USD", "EURUSD pair for power calculation", "Pairs")
		.SetRequired();

		_eurGbp = Param<Security>(nameof(EurGbp))
		.SetDisplay("EUR/GBP", "EURGBP pair for power calculation", "Pairs")
		.SetRequired();

		_eurChf = Param<Security>(nameof(EurChf))
		.SetDisplay("EUR/CHF", "EURCHF pair for power calculation", "Pairs")
		.SetRequired();

		_eurJpy = Param<Security>(nameof(EurJpy))
		.SetDisplay("EUR/JPY", "EURJPY pair for power calculation", "Pairs")
		.SetRequired();

		_gbpUsd = Param<Security>(nameof(GbpUsd))
		.SetDisplay("GBP/USD", "GBPUSD pair for power calculation", "Pairs")
		.SetRequired();

		_usdChf = Param<Security>(nameof(UsdChf))
		.SetDisplay("USD/CHF", "USDCHF pair for power calculation", "Pairs")
		.SetRequired();

		_usdJpy = Param<Security>(nameof(UsdJpy))
		.SetDisplay("USD/JPY", "USDJPY pair for power calculation", "Pairs")
		.SetRequired();

		_gbpChf = Param<Security>(nameof(GbpChf))
		.SetDisplay("GBP/CHF", "GBPCHF pair for power calculation", "Pairs")
		.SetRequired();

		_gbpJpy = Param<Security>(nameof(GbpJpy))
		.SetDisplay("GBP/JPY", "GBPJPY pair for power calculation", "Pairs")
		.SetRequired();

		_chfJpy = Param<Security>(nameof(ChfJpy))
		.SetDisplay("CHF/JPY", "CHFJPY pair for power calculation", "Pairs")
		.SetRequired();
	}

	/// <summary>
	/// Candle type used for every subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles used to compute the high/low range for each pair.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Minimum difference between base and quote powers required to open a trade.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Difference below which any open trade is closed.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Currency that represents the long side of the traded pair.
	/// </summary>
	public string BaseCurrency
	{
		get => _baseCurrency.Value;
		set => _baseCurrency.Value = value;
	}

	/// <summary>
	/// Currency that represents the short side of the traded pair.
	/// </summary>
	public string QuoteCurrency
	{
		get => _quoteCurrency.Value;
		set => _quoteCurrency.Value = value;
	}

	/// <summary>
	/// EUR/USD pair used in calculations.
	/// </summary>
	public Security EurUsd
	{
		get => _eurUsd.Value;
		set => _eurUsd.Value = value;
	}

	/// <summary>
	/// EUR/GBP pair used in calculations.
	/// </summary>
	public Security EurGbp
	{
		get => _eurGbp.Value;
		set => _eurGbp.Value = value;
	}

	/// <summary>
	/// EUR/CHF pair used in calculations.
	/// </summary>
	public Security EurChf
	{
		get => _eurChf.Value;
		set => _eurChf.Value = value;
	}

	/// <summary>
	/// EUR/JPY pair used in calculations.
	/// </summary>
	public Security EurJpy
	{
		get => _eurJpy.Value;
		set => _eurJpy.Value = value;
	}

	/// <summary>
	/// GBP/USD pair used in calculations.
	/// </summary>
	public Security GbpUsd
	{
		get => _gbpUsd.Value;
		set => _gbpUsd.Value = value;
	}

	/// <summary>
	/// USD/CHF pair used in calculations.
	/// </summary>
	public Security UsdChf
	{
		get => _usdChf.Value;
		set => _usdChf.Value = value;
	}

	/// <summary>
	/// USD/JPY pair used in calculations.
	/// </summary>
	public Security UsdJpy
	{
		get => _usdJpy.Value;
		set => _usdJpy.Value = value;
	}

	/// <summary>
	/// GBP/CHF pair used in calculations.
	/// </summary>
	public Security GbpChf
	{
		get => _gbpChf.Value;
		set => _gbpChf.Value = value;
	}

	/// <summary>
	/// GBP/JPY pair used in calculations.
	/// </summary>
	public Security GbpJpy
	{
		get => _gbpJpy.Value;
		set => _gbpJpy.Value = value;
	}

	/// <summary>
	/// CHF/JPY pair used in calculations.
	/// </summary>
	public Security ChfJpy
	{
		get => _chfJpy.Value;
		set => _chfJpy.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var seen = new HashSet<string>();
		var dt = CandleType;

		if (Security != null && seen.Add(Security.Id))
		yield return (Security, dt);

		foreach (var pair in EnumeratePairs())
		{
			if (pair == null)
			continue;

			if (seen.Add(pair.Id))
			yield return (pair, dt);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pairStates.Clear();
		_currencyStates.Clear();
		_lastLogTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		InitializeCurrencyStates();

		foreach (var state in _pairStates.Values)
		{
			var subscription = SubscribeCandles(CandleType, security: state.Security);
			subscription
			.BindEx(new IIndicator[] { state.HighIndicator, state.LowIndicator }, (candle, values) => ProcessPairCandle(state, candle, values))
			.Start();
		}

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.Bind(ProcessMainCandle)
		.Start();
	}

	private IEnumerable<Security> EnumeratePairs()
	{
		yield return EurUsd;
		yield return EurGbp;
		yield return EurChf;
		yield return EurJpy;
		yield return GbpUsd;
		yield return UsdChf;
		yield return UsdJpy;
		yield return GbpChf;
		yield return GbpJpy;
		yield return ChfJpy;
	}

	private void InitializeCurrencyStates()
	{
		_currencyStates.Clear();
		_pairStates.Clear();

		var eurUsd = GetOrCreatePairState(EurUsd);
		var eurGbp = GetOrCreatePairState(EurGbp);
		var eurChf = GetOrCreatePairState(EurChf);
		var eurJpy = GetOrCreatePairState(EurJpy);
		var gbpUsd = GetOrCreatePairState(GbpUsd);
		var usdChf = GetOrCreatePairState(UsdChf);
		var usdJpy = GetOrCreatePairState(UsdJpy);
		var gbpChf = GetOrCreatePairState(GbpChf);
		var gbpJpy = GetOrCreatePairState(GbpJpy);
		var chfJpy = GetOrCreatePairState(ChfJpy);

		var eur = CreateCurrency("EUR");
		eur.Contributions.Add((eurUsd, false));
		eur.Contributions.Add((eurGbp, false));
		eur.Contributions.Add((eurChf, false));
		eur.Contributions.Add((eurJpy, false));

		var usd = CreateCurrency("USD");
		usd.Contributions.Add((eurUsd, true));
		usd.Contributions.Add((gbpUsd, true));
		usd.Contributions.Add((usdChf, false));
		usd.Contributions.Add((usdJpy, false));

		var gbp = CreateCurrency("GBP");
		gbp.Contributions.Add((eurGbp, true));
		gbp.Contributions.Add((gbpUsd, false));
		gbp.Contributions.Add((gbpChf, false));
		gbp.Contributions.Add((gbpJpy, false));

		var chf = CreateCurrency("CHF");
		chf.Contributions.Add((eurChf, true));
		chf.Contributions.Add((usdChf, true));
		chf.Contributions.Add((gbpChf, true));
		chf.Contributions.Add((chfJpy, false));

		var jpy = CreateCurrency("JPY");
		jpy.Contributions.Add((eurJpy, true));
		jpy.Contributions.Add((usdJpy, true));
		jpy.Contributions.Add((gbpJpy, true));
		jpy.Contributions.Add((chfJpy, true));
	}

	private CurrencyState CreateCurrency(string code)
	{
		var state = new CurrencyState(code);
		_currencyStates[code] = state;
		return state;
	}

	private PairState GetOrCreatePairState(Security security)
	{
		if (security == null)
		throw new InvalidOperationException("All FX pair parameters must be assigned before the strategy starts.");

		var key = security.Id;

		if (_pairStates.TryGetValue(key, out var state))
		return state;

		state = new PairState(security, Lookback);
		_pairStates.Add(key, state);
		return state;
	}

	private void ProcessPairCandle(PairState state, ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!values[0].IsFinal || !values[1].IsFinal)
		return;

		var highest = values[0].ToDecimal();
		var lowest = values[1].ToDecimal();
		var range = highest - lowest;

		if (range <= 0m)
		return;

		var normalized = (candle.ClosePrice - lowest) / range;
		if (normalized < 0m)
		normalized = 0m;
		else if (normalized > 1m)
		normalized = 1m;

		state.Power = normalized * 100m;

		UpdateCurrencyPowers();
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		TryLogCurrencyPowers(candle.CloseTime);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var basePower = GetCurrencyPower(BaseCurrency);
		var quotePower = GetCurrencyPower(QuoteCurrency);

		if (basePower is null || quotePower is null)
		return;

		var difference = basePower.Value - quotePower.Value;

		if (difference > EntryThreshold)
		{
			if (Position < 0)
			{
				CancelActiveOrders();
				ClosePosition();
				return;
			}

			if (Position <= 0)
			{
				CancelActiveOrders();
				BuyMarket();
			}
		}
		else if (difference < -EntryThreshold)
		{
			if (Position > 0)
			{
				CancelActiveOrders();
				ClosePosition();
				return;
			}

			if (Position >= 0)
			{
				CancelActiveOrders();
				SellMarket();
			}
		}
		else if (Math.Abs(difference) < ExitThreshold && Position != 0)
		{
			CancelActiveOrders();
			ClosePosition();
		}
	}

	private void UpdateCurrencyPowers()
	{
		foreach (var currency in _currencyStates.Values)
		{
			var sum = 0m;
			var count = 0;
			var missing = false;

			foreach (var (pair, inverse) in currency.Contributions)
			{
				if (pair.Power is not decimal value)
				{
					missing = true;
					break;
				}

				sum += inverse ? 100m - value : value;
				count++;
			}

			currency.Power = !missing && count > 0 ? sum / count : null;
		}
	}

	private decimal? GetCurrencyPower(string code)
	{
		if (string.IsNullOrWhiteSpace(code))
		return null;

		return _currencyStates.TryGetValue(code, out var state) ? state.Power : null;
	}

	private void TryLogCurrencyPowers(DateTimeOffset time)
	{
		if (_currencyStates.Count == 0)
		return;

		if (time == _lastLogTime)
		return;

		_lastLogTime = time;

		var summary = string.Join(", ", _currencyStates.Values.Select(c => $"{c.Code}:{FormatPower(c.Power)}"));
		LogInfo($"Currency powers at {time:O} -> {summary}");
	}

	private static string FormatPower(decimal? value)
	{
		return value?.ToString("F2", CultureInfo.InvariantCulture) ?? "n/a";
	}
}
