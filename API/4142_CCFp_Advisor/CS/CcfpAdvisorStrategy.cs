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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor CCFp (Currency Comparative Force) portfolio trader.
/// Determines the strongest and weakest currencies via cross-rate moving averages and opens trades on the matching majors.
/// </summary>
public class CcfpAdvisorStrategy : Strategy
{
	private readonly StrategyParam<MovingAverageModes> _maType;
	private readonly StrategyParam<CandlePrices> _priceMode;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _eurUsdSecurity;
	private readonly StrategyParam<Security> _gbpUsdSecurity;
	private readonly StrategyParam<Security> _audUsdSecurity;
	private readonly StrategyParam<Security> _nzdUsdSecurity;
	private readonly StrategyParam<Security> _usdChfSecurity;
	private readonly StrategyParam<Security> _usdJpySecurity;
	private readonly StrategyParam<Security> _usdCadSecurity;

	private readonly Dictionary<string, PairState> _pairBySymbol = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<Security, PairState> _pairBySecurity = new();
	private readonly SortedDictionary<DateTimeOffset, AggregationState> _pending = new();
	private readonly List<CurrencyStrengthSnapshot> _snapshots = new();

	private ActiveTrade? _activeStrongTrade;
	private ActiveTrade? _activeWeakTrade;
	private int _expectedPairs;

	/// <summary>
	/// Moving-average calculation modes supported by the strategy.
	/// </summary>
	public enum MovingAverageModes
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
	}

	public enum CandlePrices
	{
		Open,
		High,
		Low,
		Close,
		Median,
		Typical,
		Weighted
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CcfpAdvisorStrategy"/>.
	/// </summary>
	public CcfpAdvisorStrategy()
	{
		_maType = Param(nameof(MaType), MovingAverageModes.Exponential)
			.SetDisplay("MA Type", "Moving-average algorithm used for the currency strength calculation", "General")
			.SetCanOptimize(true);

		_priceMode = Param(nameof(PriceMode), CandlePrices.Close)
			.SetDisplay("Applied Price", "Candle price used as the input for all moving averages", "General")
			.SetCanOptimize(true);

		_fastPeriod = Param(nameof(FastPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Base length of the fast composite moving average", "Parameters")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Base length of the slow composite moving average", "Parameters")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 200m)
			.SetDisplay("Stop Loss (pips)", "Protective stop expressed in pips from the entry price", "Risk")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume (lots) submitted for every market order", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate currency strength", "General");

		_eurUsdSecurity = Param<Security>(nameof(EurUsdSecurity), null)
			.SetDisplay("EURUSD", "Security representing EUR/USD", "Securities");

		_gbpUsdSecurity = Param<Security>(nameof(GbpUsdSecurity), null)
			.SetDisplay("GBPUSD", "Security representing GBP/USD", "Securities");

		_audUsdSecurity = Param<Security>(nameof(AudUsdSecurity), null)
			.SetDisplay("AUDUSD", "Security representing AUD/USD", "Securities");

		_nzdUsdSecurity = Param<Security>(nameof(NzdUsdSecurity), null)
			.SetDisplay("NZDUSD", "Security representing NZD/USD", "Securities");

		_usdChfSecurity = Param<Security>(nameof(UsdChfSecurity), null)
			.SetDisplay("USDCHF", "Security representing USD/CHF", "Securities");

		_usdJpySecurity = Param<Security>(nameof(UsdJpySecurity), null)
			.SetDisplay("USDJPY", "Security representing USD/JPY", "Securities");

		_usdCadSecurity = Param<Security>(nameof(UsdCadSecurity), null)
			.SetDisplay("USDCAD", "Security representing USD/CAD", "Securities");
	}

	/// <summary>
	/// Moving-average method applied to every composite MA calculation.
	/// </summary>
	public MovingAverageModes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Candle price mode used as the moving average input.
	/// </summary>
	public CandlePrices PriceMode
	{
		get => _priceMode.Value;
		set => _priceMode.Value = value;
	}

	/// <summary>
	/// Base period of the fast composite moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Base period of the slow composite moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Protective stop distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Market order volume used for every entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used to build the strength indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Security representing EUR/USD.
	/// </summary>
	public Security EurUsdSecurity
	{
		get => _eurUsdSecurity.Value;
		set => _eurUsdSecurity.Value = value;
	}

	/// <summary>
	/// Security representing GBP/USD.
	/// </summary>
	public Security GbpUsdSecurity
	{
		get => _gbpUsdSecurity.Value;
		set => _gbpUsdSecurity.Value = value;
	}

	/// <summary>
	/// Security representing AUD/USD.
	/// </summary>
	public Security AudUsdSecurity
	{
		get => _audUsdSecurity.Value;
		set => _audUsdSecurity.Value = value;
	}

	/// <summary>
	/// Security representing NZD/USD.
	/// </summary>
	public Security NzdUsdSecurity
	{
		get => _nzdUsdSecurity.Value;
		set => _nzdUsdSecurity.Value = value;
	}

	/// <summary>
	/// Security representing USD/CHF.
	/// </summary>
	public Security UsdChfSecurity
	{
		get => _usdChfSecurity.Value;
		set => _usdChfSecurity.Value = value;
	}

	/// <summary>
	/// Security representing USD/JPY.
	/// </summary>
	public Security UsdJpySecurity
	{
		get => _usdJpySecurity.Value;
		set => _usdJpySecurity.Value = value;
	}

	/// <summary>
	/// Security representing USD/CAD.
	/// </summary>
	public Security UsdCadSecurity
	{
		get => _usdCadSecurity.Value;
		set => _usdCadSecurity.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pairBySymbol.Clear();
		_pairBySecurity.Clear();
		_pending.Clear();
		_snapshots.Clear();
		_activeStrongTrade = null;
		_activeWeakTrade = null;
		_expectedPairs = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not assigned.");

		var eurUsd = EurUsdSecurity ?? throw new InvalidOperationException("EURUSD security is not configured.");
		var gbpUsd = GbpUsdSecurity ?? throw new InvalidOperationException("GBPUSD security is not configured.");
		var audUsd = AudUsdSecurity ?? throw new InvalidOperationException("AUDUSD security is not configured.");
		var nzdUsd = NzdUsdSecurity ?? throw new InvalidOperationException("NZDUSD security is not configured.");
		var usdChf = UsdChfSecurity ?? throw new InvalidOperationException("USDCHF security is not configured.");
		var usdJpy = UsdJpySecurity ?? throw new InvalidOperationException("USDJPY security is not configured.");
		var usdCad = UsdCadSecurity ?? throw new InvalidOperationException("USDCAD security is not configured.");

		var timeFrame = GetTimeFrame();
		var multipliers = GetPeriodMultipliers(timeFrame);

		_pairBySymbol.Clear();
		_pairBySecurity.Clear();
		_pending.Clear();
		_snapshots.Clear();
		_activeStrongTrade = null;
		_activeWeakTrade = null;

		AddPair("EURUSD", eurUsd, multipliers);
		AddPair("GBPUSD", gbpUsd, multipliers);
		AddPair("AUDUSD", audUsd, multipliers);
		AddPair("NZDUSD", nzdUsd, multipliers);
		AddPair("USDCHF", usdChf, multipliers);
		AddPair("USDJPY", usdJpy, multipliers);
		AddPair("USDCAD", usdCad, multipliers);

		_expectedPairs = _pairBySymbol.Count;

		foreach (var pair in _pairBySymbol.Values)
		{
			var subscription = SubscribeCandles(CandleType, true, pair.Security);
			subscription.Bind(c => ProcessPair(pair.Symbol, c)).Start();
		}

		StartProtection();
	}

	private void ProcessPair(string symbol, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_pairBySymbol.TryGetValue(symbol, out var pair))
			return;

		pair.LastClosePrice = candle.ClosePrice;

		CheckStops(pair.Security, candle);

		var price = GetPrice(candle);
		var time = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;

		var fastValue = pair.FastAverage.Process(price, time);
		if (fastValue is not decimal fast)
			return;

		var slowValue = pair.SlowAverage.Process(price, time);
		if (slowValue is not decimal slow)
			return;

		if (!_pending.TryGetValue(candle.OpenTime, out var aggregation))
		{
			aggregation = new AggregationState(_expectedPairs);
			_pending[candle.OpenTime] = aggregation;
		}

		aggregation.Set(symbol, new PairValue(fast, slow));

		if (!aggregation.IsComplete)
			return;

		_pending.Remove(candle.OpenTime);

		var snapshot = BuildSnapshot(candle.OpenTime, aggregation.Values);
		if (snapshot == null)
			return;

		_snapshots.Add(snapshot);
		while (_snapshots.Count > 4)
			_snapshots.RemoveAt(0);

		EvaluateSignals();
	}

	private void EvaluateSignals()
	{
		if (_snapshots.Count < 2)
			return;

		var latest = _snapshots[_snapshots.Count - 1];
		var previous = _snapshots[_snapshots.Count - 2];

		var (maxCurrency, _) = FindExtremum(latest.Values, true);
		var (minCurrency, _) = FindExtremum(latest.Values, false);

		var previousValues = previous.Values;
		var previousMaxValue = previousValues[(int)maxCurrency];
		var previousMinValue = previousValues[(int)minCurrency];

		var highestPrevious = GetExtreme(previousValues, true);
		var lowestPrevious = GetExtreme(previousValues, false);

		var isNewMax = previousMaxValue < highestPrevious;
		var isNewMin = previousMinValue > lowestPrevious;

		if (isNewMax && TryGetTradeAction(maxCurrency, true, out var maxAction))
		{
			OpenTrade(ref _activeStrongTrade, maxCurrency, maxAction, "CCFp-Strong");
		}

		if (isNewMin && TryGetTradeAction(minCurrency, false, out var minAction))
		{
			OpenTrade(ref _activeWeakTrade, minCurrency, minAction, "CCFp-Weak");
		}

		if (_activeStrongTrade is { } strong && strong.Currency != maxCurrency)
		{
			CloseTrade(ref _activeStrongTrade);
		}

		if (_activeWeakTrade is { } weak && weak.Currency != minCurrency)
		{
			CloseTrade(ref _activeWeakTrade);
		}
	}

	private void OpenTrade(ref ActiveTrade? slot, CurrencyIndexes currency, TradeAction action, string tag)
	{
		if (slot is { Currency: var existing } && existing == currency)
			return;

		if (slot != null)
		{
			CloseTrade(ref slot);
		}

		var volume = TradeVolume;
		if (volume <= 0m)
			throw new InvalidOperationException("Trade volume must be greater than zero.");

		ExecuteMarketOrder(action.Security, action.Side, volume, tag);

		var stop = CalculateStopPrice(action.Security, action.Side);

		slot = new ActiveTrade
		{
			Currency = currency,
			Security = action.Security,
			Side = action.Side,
			Volume = volume,
			StopPrice = stop
		};
	}

	private void CloseTrade(ref ActiveTrade? slot)
	{
		if (slot == null)
			return;

		ClosePosition(slot.Value.Security);
		slot = null;
	}

	private void ClosePosition(Security security)
	{
		var position = GetPositionValue(security, Portfolio) ?? 0m;
		if (position == 0m)
			return;

		var side = position > 0m ? Sides.Sell : Sides.Buy;
		ExecuteMarketOrder(security, side, Math.Abs(position), "CCFp-Exit");
	}

	private void ExecuteMarketOrder(Security security, Sides side, decimal volume, string tag)
	{
		var order = new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = side,
			Volume = volume,
			Type = OrderTypes.Market,
			Comment = tag
		};

		RegisterOrder(order);
	}

	private decimal? CalculateStopPrice(Security security, Sides side)
	{
		if (StopLossPips <= 0m)
			return null;

		if (!_pairBySecurity.TryGetValue(security, out var pair) || pair.LastClosePrice is not decimal closePrice)
			return null;

		var pip = GetPipSize(security);
		var offset = StopLossPips * pip;

		return side == Sides.Buy ? closePrice - offset : closePrice + offset;
	}

	private void CheckStops(Security security, ICandleMessage candle)
	{
		if (_activeStrongTrade is { } strong && strong.Security == security && strong.StopPrice is decimal strongStop)
		{
			if (IsStopHit(strong.Side, candle, strongStop))
			{
				ClosePosition(security);
				_activeStrongTrade = null;
			}
		}

		if (_activeWeakTrade is { } weak && weak.Security == security && weak.StopPrice is decimal weakStop)
		{
			if (IsStopHit(weak.Side, candle, weakStop))
			{
				ClosePosition(security);
				_activeWeakTrade = null;
			}
		}
	}

	private static bool IsStopHit(Sides side, ICandleMessage candle, decimal stop)
	{
		return side == Sides.Buy ? candle.LowPrice <= stop : candle.HighPrice >= stop;
	}

	private CurrencyStrengthSnapshot BuildSnapshot(DateTimeOffset time, IReadOnlyDictionary<string, PairValue> values)
	{
		if (!TryGetPairValue(values, "EURUSD", out var eurUsd) ||
		!TryGetPairValue(values, "GBPUSD", out var gbpUsd) ||
		!TryGetPairValue(values, "AUDUSD", out var audUsd) ||
		!TryGetPairValue(values, "NZDUSD", out var nzdUsd) ||
		!TryGetPairValue(values, "USDCHF", out var usdChf) ||
		!TryGetPairValue(values, "USDJPY", out var usdJpy) ||
		!TryGetPairValue(values, "USDCAD", out var usdCad))
		{
			return null;
		}

		if (!AreValid(eurUsd) || !AreValid(gbpUsd) || !AreValid(audUsd) || !AreValid(nzdUsd) ||
		!AreValid(usdChf) || !AreValid(usdJpy) || !AreValid(usdCad))
		{
			return null;
		}

		var strength = new decimal[8];

		// USD strength.
		strength[(int)CurrencyIndexes.USD] =
		(SafeDiv(eurUsd.Slow, eurUsd.Fast) - 1m) +
		(SafeDiv(gbpUsd.Slow, gbpUsd.Fast) - 1m) +
		(SafeDiv(audUsd.Slow, audUsd.Fast) - 1m) +
		(SafeDiv(nzdUsd.Slow, nzdUsd.Fast) - 1m) +
		(SafeDiv(usdChf.Fast, usdChf.Slow) - 1m) +
		(SafeDiv(usdCad.Fast, usdCad.Slow) - 1m) +
		(SafeDiv(usdJpy.Fast, usdJpy.Slow) - 1m);

		// EUR strength.
		strength[(int)CurrencyIndexes.EUR] =
		(SafeDiv(eurUsd.Fast, eurUsd.Slow) - 1m) +
		(SafeDiv(SafeDiv(eurUsd.Fast, gbpUsd.Fast), SafeDiv(eurUsd.Slow, gbpUsd.Slow)) - 1m) +
		(SafeDiv(SafeDiv(eurUsd.Fast, audUsd.Fast), SafeDiv(eurUsd.Slow, audUsd.Slow)) - 1m) +
		(SafeDiv(SafeDiv(eurUsd.Fast, nzdUsd.Fast), SafeDiv(eurUsd.Slow, nzdUsd.Slow)) - 1m) +
		(SafeDiv(eurUsd.Fast * usdChf.Fast, eurUsd.Slow * usdChf.Slow) - 1m) +
		(SafeDiv(eurUsd.Fast * usdCad.Fast, eurUsd.Slow * usdCad.Slow) - 1m) +
		(SafeDiv(eurUsd.Fast * usdJpy.Fast, eurUsd.Slow * usdJpy.Slow) - 1m);

		// GBP strength.
		strength[(int)CurrencyIndexes.GBP] =
		(SafeDiv(gbpUsd.Fast, gbpUsd.Slow) - 1m) +
		(SafeDiv(SafeDiv(eurUsd.Slow, gbpUsd.Slow), SafeDiv(eurUsd.Fast, gbpUsd.Fast)) - 1m) +
		(SafeDiv(SafeDiv(gbpUsd.Fast, audUsd.Fast), SafeDiv(gbpUsd.Slow, audUsd.Slow)) - 1m) +
		(SafeDiv(SafeDiv(gbpUsd.Fast, nzdUsd.Fast), SafeDiv(gbpUsd.Slow, nzdUsd.Slow)) - 1m) +
		(SafeDiv(gbpUsd.Fast * usdChf.Fast, gbpUsd.Slow * usdChf.Slow) - 1m) +
		(SafeDiv(gbpUsd.Fast * usdCad.Fast, gbpUsd.Slow * usdCad.Slow) - 1m) +
		(SafeDiv(gbpUsd.Fast * usdJpy.Fast, gbpUsd.Slow * usdJpy.Slow) - 1m);

		// CHF strength.
		strength[(int)CurrencyIndexes.CHF] =
		(SafeDiv(usdChf.Slow, usdChf.Fast) - 1m) +
		(SafeDiv(eurUsd.Slow * usdChf.Slow, eurUsd.Fast * usdChf.Fast) - 1m) +
		(SafeDiv(gbpUsd.Slow * usdChf.Slow, gbpUsd.Fast * usdChf.Fast) - 1m) +
		(SafeDiv(audUsd.Slow * usdChf.Slow, audUsd.Fast * usdChf.Fast) - 1m) +
		(SafeDiv(nzdUsd.Slow * usdChf.Slow, nzdUsd.Fast * usdChf.Fast) - 1m) +
		(SafeDiv(SafeDiv(usdChf.Slow, usdCad.Slow), SafeDiv(usdChf.Fast, usdCad.Fast)) - 1m) +
		(SafeDiv(SafeDiv(usdChf.Slow, usdJpy.Slow), SafeDiv(usdChf.Fast, usdJpy.Fast)) - 1m);

		// JPY strength.
		strength[(int)CurrencyIndexes.JPY] =
		(SafeDiv(usdJpy.Slow, usdJpy.Fast) - 1m) +
		(SafeDiv(eurUsd.Slow * usdJpy.Slow, eurUsd.Fast * usdJpy.Fast) - 1m) +
		(SafeDiv(gbpUsd.Slow * usdJpy.Slow, gbpUsd.Fast * usdJpy.Fast) - 1m) +
		(SafeDiv(audUsd.Slow * usdJpy.Slow, audUsd.Fast * usdJpy.Fast) - 1m) +
		(SafeDiv(nzdUsd.Slow * usdJpy.Slow, nzdUsd.Fast * usdJpy.Fast) - 1m) +
		(SafeDiv(SafeDiv(usdJpy.Slow, usdCad.Slow), SafeDiv(usdJpy.Fast, usdCad.Fast)) - 1m) +
		(SafeDiv(SafeDiv(usdJpy.Slow, usdChf.Slow), SafeDiv(usdJpy.Fast, usdChf.Fast)) - 1m);

		// AUD strength.
		strength[(int)CurrencyIndexes.AUD] =
		(SafeDiv(audUsd.Fast, audUsd.Slow) - 1m) +
		(SafeDiv(SafeDiv(eurUsd.Slow, audUsd.Slow), SafeDiv(eurUsd.Fast, audUsd.Fast)) - 1m) +
		(SafeDiv(SafeDiv(gbpUsd.Slow, audUsd.Slow), SafeDiv(gbpUsd.Fast, audUsd.Fast)) - 1m) +
		(SafeDiv(SafeDiv(audUsd.Fast, nzdUsd.Fast), SafeDiv(audUsd.Slow, nzdUsd.Slow)) - 1m) +
		(SafeDiv(audUsd.Fast * usdChf.Fast, audUsd.Slow * usdChf.Slow) - 1m) +
		(SafeDiv(audUsd.Fast * usdCad.Fast, audUsd.Slow * usdCad.Slow) - 1m) +
		(SafeDiv(audUsd.Fast * usdJpy.Fast, audUsd.Slow * usdJpy.Slow) - 1m);

		// CAD strength.
		strength[(int)CurrencyIndexes.CAD] =
		(SafeDiv(usdCad.Slow, usdCad.Fast) - 1m) +
		(SafeDiv(eurUsd.Slow * usdCad.Slow, eurUsd.Fast * usdCad.Fast) - 1m) +
		(SafeDiv(gbpUsd.Slow * usdCad.Slow, gbpUsd.Fast * usdCad.Fast) - 1m) +
		(SafeDiv(audUsd.Slow * usdCad.Slow, audUsd.Fast * usdCad.Fast) - 1m) +
		(SafeDiv(nzdUsd.Slow * usdCad.Slow, nzdUsd.Fast * usdCad.Fast) - 1m) +
		(SafeDiv(SafeDiv(usdChf.Fast, usdCad.Fast), SafeDiv(usdChf.Slow, usdCad.Slow)) - 1m) +
		(SafeDiv(SafeDiv(usdJpy.Fast, usdCad.Fast), SafeDiv(usdJpy.Slow, usdCad.Slow)) - 1m);

		// NZD strength.
		strength[(int)CurrencyIndexes.NZD] =
		(SafeDiv(nzdUsd.Fast, nzdUsd.Slow) - 1m) +
		(SafeDiv(SafeDiv(eurUsd.Slow, nzdUsd.Slow), SafeDiv(eurUsd.Fast, nzdUsd.Fast)) - 1m) +
		(SafeDiv(SafeDiv(gbpUsd.Slow, nzdUsd.Slow), SafeDiv(gbpUsd.Fast, nzdUsd.Fast)) - 1m) +
		(SafeDiv(SafeDiv(audUsd.Slow, nzdUsd.Slow), SafeDiv(audUsd.Fast, nzdUsd.Fast)) - 1m) +
		(SafeDiv(nzdUsd.Fast * usdChf.Fast, nzdUsd.Slow * usdChf.Slow) - 1m) +
		(SafeDiv(nzdUsd.Fast * usdCad.Fast, nzdUsd.Slow * usdCad.Slow) - 1m) +
		(SafeDiv(nzdUsd.Fast * usdJpy.Fast, nzdUsd.Slow * usdJpy.Slow) - 1m);

		return new CurrencyStrengthSnapshot(time, strength);
	}

	private static bool TryGetPairValue(IReadOnlyDictionary<string, PairValue> values, string symbol, out PairValue value)
	{
		return values.TryGetValue(symbol, out value);
	}

	private static bool AreValid(PairValue value)
	{
		return value.Fast != 0m && value.Slow != 0m;
	}

	private decimal GetPrice(ICandleMessage candle)
	{
		return PriceMode switch
		{
			CandlePrices.Open => candle.OpenPrice,
			CandlePrices.High => candle.HighPrice,
			CandlePrices.Low => candle.LowPrice,
			CandlePrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrices.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrices.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private decimal GetPipSize(Security security)
	{
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep;
		if (step == null || step <= 0m)
			step = security.MinStep;

		var value = step ?? 0.0001m;

		var digits = GetDecimalDigits(value);
		return digits == 3 || digits == 5 ? value * 10m : value;
	}

	private static int GetDecimalDigits(decimal value)
	{
		value = Math.Abs(value);
		var digits = 0;

		while (value != Math.Truncate(value) && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		return digits;
	}

	private static (CurrencyIndexes currency, decimal value) FindExtremum(IReadOnlyList<decimal> values, bool isMaximum)
	{
		var best = values.Count > 0 ? values[0] : 0m;
		var index = 0;

		for (var i = 1; i < values.Count; i++)
		{
			var current = values[i];
			if (isMaximum)
			{
				if (current > best)
				{
					best = current;
					index = i;
				}
			}
			else
			{
				if (current < best)
				{
					best = current;
					index = i;
				}
			}
		}

		return ((CurrencyIndexes)index, best);
	}

	private static decimal GetExtreme(IReadOnlyList<decimal> values, bool isMaximum)
	{
		if (values.Count == 0)
			return 0m;

		var result = values[0];
		for (var i = 1; i < values.Count; i++)
		{
			var value = values[i];
			if (isMaximum)
			{
				if (value > result)
					result = value;
			}
			else
			{
				if (value < result)
					result = value;
			}
		}

		return result;
	}

	private bool TryGetTradeAction(CurrencyIndexes currency, bool isStrong, out TradeAction action)
	{
		action = default;
		return currency switch
		{
			CurrencyIndexes.EUR => CreateAction(EurUsdSecurity, isStrong ? Sides.Buy : Sides.Sell, out action),
			CurrencyIndexes.GBP => CreateAction(GbpUsdSecurity, isStrong ? Sides.Buy : Sides.Sell, out action),
			CurrencyIndexes.CHF => CreateAction(UsdChfSecurity, isStrong ? Sides.Sell : Sides.Buy, out action),
			CurrencyIndexes.JPY => CreateAction(UsdJpySecurity, isStrong ? Sides.Sell : Sides.Buy, out action),
			CurrencyIndexes.AUD => CreateAction(AudUsdSecurity, isStrong ? Sides.Buy : Sides.Sell, out action),
			CurrencyIndexes.CAD => CreateAction(UsdCadSecurity, isStrong ? Sides.Sell : Sides.Buy, out action),
			CurrencyIndexes.NZD => CreateAction(NzdUsdSecurity, isStrong ? Sides.Buy : Sides.Sell, out action),
			_ => false,
		};
	}

	private bool CreateAction(Security security, Sides side, out TradeAction action)
	{
		action = default;
		if (security == null)
			return false;

		action = new TradeAction(security, side);
		return true;
	}

	private void AddPair(string symbol, Security security, IReadOnlyList<int> multipliers)
	{
		var fast = CreateAggregatedAverage(MaType, FastPeriod, multipliers);
		var slow = CreateAggregatedAverage(MaType, SlowPeriod, multipliers);
		var pair = new PairState(symbol, security, fast, slow);

		_pairBySymbol[symbol] = pair;
		_pairBySecurity[security] = pair;
	}

	private static AggregatedMovingAverage CreateAggregatedAverage(MovingAverageModes type, int period, IReadOnlyList<int> multipliers)
	{
		var indicators = new List<LengthIndicator<decimal>>(multipliers.Count);
		for (var i = 0; i < multipliers.Count; i++)
		{
			var length = period * multipliers[i];
			indicators.Add(CreateMovingAverage(type, length));
		}

		return new AggregatedMovingAverage(indicators);
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageModes type, int length)
	{
		LengthIndicator<decimal> indicator = type switch
		{
			MovingAverageModes.Exponential => new ExponentialMovingAverage(),
			MovingAverageModes.Smoothed => new SmoothedMovingAverage(),
			MovingAverageModes.Weighted => new WeightedMovingAverage(),
			_ => new SimpleMovingAverage(),
		};

		indicator.Length = length;
		return indicator;
	}

	private TimeSpan? GetTimeFrame()
	{
		return CandleType.Arg is TimeSpan span ? span : null;
	}

	private static IReadOnlyList<int> GetPeriodMultipliers(TimeSpan? timeFrame)
	{
		if (timeFrame == null)
			return new[] { 1, 5, 11, 15, 19 };

		var minutes = (int)Math.Round(timeFrame.Value.TotalMinutes);

		return minutes switch
		{
			1 => new[] { 1, 6, 9, 11, 13, 17, 23, 27, 31 },
			5 => new[] { 1, 4, 6, 8, 12, 18, 22, 26 },
			15 => new[] { 1, 3, 5, 9, 15, 19, 23 },
			30 => new[] { 1, 3, 7, 13, 17, 21 },
			60 => new[] { 1, 5, 11, 15, 19 },
			240 => new[] { 1, 7, 11, 15 },
			1440 => new[] { 1, 5, 9 },
			10080 => new[] { 1, 5 },
			43200 => new[] { 1 },
			_ => new[] { 1, 5, 11, 15, 19 },
		};
	}

	private static decimal SafeDiv(decimal numerator, decimal denominator)
	{
		if (denominator == 0m)
			return 0m;

		return numerator / denominator;
	}

	private readonly struct PairValue
	{
		public PairValue(decimal fast, decimal slow)
		{
			Fast = fast;
			Slow = slow;
		}

		public decimal Fast { get; }
		public decimal Slow { get; }
	}

	private sealed class AggregatedMovingAverage
	{
		private readonly List<LengthIndicator<decimal>> _indicators;

		public AggregatedMovingAverage(List<LengthIndicator<decimal>> indicators)
		{
			_indicators = indicators;
		}

		public decimal? Process(decimal value, DateTimeOffset time)
		{
			decimal sum = 0m;
			for (var i = 0; i < _indicators.Count; i++)
			{
				var indicatorValue = _indicators[i].Process(value, time, true);
				if (!indicatorValue.IsFinal)
					return null;

				if (indicatorValue.GetValue<decimal>() is not decimal ma)
					return null;

				sum += ma;
			}

			return sum;
		}
	}

	private sealed class PairState
	{
		public PairState(string symbol, Security security, AggregatedMovingAverage fastAverage, AggregatedMovingAverage slowAverage)
		{
			Symbol = symbol;
			Security = security;
			FastAverage = fastAverage;
			SlowAverage = slowAverage;
		}

		public string Symbol { get; }
		public Security Security { get; }
		public AggregatedMovingAverage FastAverage { get; }
		public AggregatedMovingAverage SlowAverage { get; }
		public decimal? LastClosePrice { get; set; }
	}

	private sealed class AggregationState
	{
		private readonly int _expectedCount;
		private readonly Dictionary<string, PairValue> _values = new(StringComparer.OrdinalIgnoreCase);

		public AggregationState(int expectedCount)
		{
			_expectedCount = expectedCount;
		}

		public IReadOnlyDictionary<string, PairValue> Values => _values;

		public bool IsComplete => _values.Count >= _expectedCount;

		public void Set(string symbol, PairValue value)
		{
			_values[symbol] = value;
		}
	}

	private sealed class CurrencyStrengthSnapshot
	{
		public CurrencyStrengthSnapshot(DateTimeOffset time, decimal[] values)
		{
			Time = time;
			Values = values;
		}

		public DateTimeOffset Time { get; }
		public decimal[] Values { get; }
	}

	private readonly struct TradeAction
	{
		public TradeAction(Security security, Sides side)
		{
			Security = security;
			Side = side;
		}

		public Security Security { get; }
		public Sides Side { get; }
	}

	private struct ActiveTrade
	{
		public CurrencyIndexes Currency { get; set; }
		public Security Security { get; set; }
		public Sides Side { get; set; }
		public decimal Volume { get; set; }
		public decimal? StopPrice { get; set; }
	}

	private enum CurrencyIndexes
	{
		USD,
		EUR,
		GBP,
		CHF,
		JPY,
		AUD,
		CAD,
		NZD,
	}
}