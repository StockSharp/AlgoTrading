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
/// Currencies strength strategy inspired by the classic CCFp expert advisor.
/// It compares relative currency strength values to find bullish and bearish combinations.
/// </summary>
public class CcfpCurrencyStrengthStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _stepThreshold;
	private readonly StrategyParam<bool> _closeOppositePositions;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _eurUsd;
	private readonly StrategyParam<Security> _gbpUsd;
	private readonly StrategyParam<Security> _audUsd;
	private readonly StrategyParam<Security> _nzdUsd;
	private readonly StrategyParam<Security> _usdCad;
	private readonly StrategyParam<Security> _usdChf;
	private readonly StrategyParam<Security> _usdJpy;

	private readonly Dictionary<Currencies, PairState> _pairStates = new();
	private readonly decimal[] _currentStrengths = new decimal[8];
	private readonly decimal[] _previousStrengths = new decimal[8];
	private bool _hasPreviousStrengths;
	private DateTimeOffset _lastSignalTime;

	private static readonly string[] CurrencyNames = { "USD", "EUR", "GBP", "CHF", "JPY", "AUD", "CAD", "NZD" };

	private enum Currencies
	{
		USD = 0,
		EUR = 1,
		GBP = 2,
		CHF = 3,
		JPY = 4,
		AUD = 5,
		CAD = 6,
		NZD = 7
	}

	private sealed class PairState
	{
		public Security Security { get; init; }
		public SimpleMovingAverage Fast { get; init; }
		public SimpleMovingAverage Slow { get; init; }
		public bool InvertRatio { get; init; }
		public Currencies BaseCurrency { get; init; }
		public Currencies QuoteCurrency { get; init; }
		public decimal Ratio { get; set; }
		public DateTimeOffset LastTime { get; set; }
		public bool IsReady { get; set; }
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

	/// <summary>
	/// Fast moving average period used inside the strength calculation.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period used inside the strength calculation.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Minimal difference between currencies that triggers a trade.
	/// </summary>
	public decimal StepThreshold
	{
		get => _stepThreshold.Value;
		set => _stepThreshold.Value = value;
	}

	/// <summary>
	/// If true opposite positions are closed before opening a new one.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOppositePositions.Value;
		set => _closeOppositePositions.Value = value;
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
	/// EUR/USD security reference.
	/// </summary>
	public Security EurUsd
	{
		get => _eurUsd.Value;
		set => _eurUsd.Value = value;
	}

	/// <summary>
	/// GBP/USD security reference.
	/// </summary>
	public Security GbpUsd
	{
		get => _gbpUsd.Value;
		set => _gbpUsd.Value = value;
	}

	/// <summary>
	/// AUD/USD security reference.
	/// </summary>
	public Security AudUsd
	{
		get => _audUsd.Value;
		set => _audUsd.Value = value;
	}

	/// <summary>
	/// NZD/USD security reference.
	/// </summary>
	public Security NzdUsd
	{
		get => _nzdUsd.Value;
		set => _nzdUsd.Value = value;
	}

	/// <summary>
	/// USD/CAD security reference.
	/// </summary>
	public Security UsdCad
	{
		get => _usdCad.Value;
		set => _usdCad.Value = value;
	}

	/// <summary>
	/// USD/CHF security reference.
	/// </summary>
	public Security UsdChf
	{
		get => _usdChf.Value;
		set => _usdChf.Value = value;
	}

	/// <summary>
	/// USD/JPY security reference.
	/// </summary>
	public Security UsdJpy
	{
		get => _usdJpy.Value;
		set => _usdJpy.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CcfpCurrencyStrengthStrategy()
	{
		Volume = 1m;

		_fastMaPeriod = Param(nameof(FastMaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average length", "Indicators")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average length", "Indicators")
			.SetCanOptimize(true);

		_stepThreshold = Param(nameof(StepThreshold), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Strength Step", "Minimal strength difference", "Signals")
			.SetCanOptimize(true);

		_closeOppositePositions = Param(nameof(CloseOppositePositions), true)
			.SetDisplay("Close Opposite", "Close opposite positions before entry", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "Data");

		_eurUsd = Param<Security>(nameof(EurUsd))
			.SetDisplay("EUR/USD", "EURUSD instrument", "Data")
			.SetRequired();

		_gbpUsd = Param<Security>(nameof(GbpUsd))
			.SetDisplay("GBP/USD", "GBPUSD instrument", "Data")
			.SetRequired();

		_audUsd = Param<Security>(nameof(AudUsd))
			.SetDisplay("AUD/USD", "AUDUSD instrument", "Data")
			.SetRequired();

		_nzdUsd = Param<Security>(nameof(NzdUsd))
			.SetDisplay("NZD/USD", "NZDUSD instrument", "Data")
			.SetRequired();

		_usdCad = Param<Security>(nameof(UsdCad))
			.SetDisplay("USD/CAD", "USDCAD instrument", "Data")
			.SetRequired();

		_usdChf = Param<Security>(nameof(UsdChf))
			.SetDisplay("USD/CHF", "USDCHF instrument", "Data")
			.SetRequired();

		_usdJpy = Param<Security>(nameof(UsdJpy))
			.SetDisplay("USD/JPY", "USDJPY instrument", "Data")
			.SetRequired();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (EurUsd != null)
			yield return (EurUsd, CandleType);
		if (GbpUsd != null)
			yield return (GbpUsd, CandleType);
		if (AudUsd != null)
			yield return (AudUsd, CandleType);
		if (NzdUsd != null)
			yield return (NzdUsd, CandleType);
		if (UsdCad != null)
			yield return (UsdCad, CandleType);
		if (UsdChf != null)
			yield return (UsdChf, CandleType);
		if (UsdJpy != null)
			yield return (UsdJpy, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();
		EnsurePairsConfigured();

		CreatePairState(Currencies.EUR, EurUsd, false, Currencies.EUR, Currencies.USD);
		CreatePairState(Currencies.GBP, GbpUsd, false, Currencies.GBP, Currencies.USD);
		CreatePairState(Currencies.AUD, AudUsd, false, Currencies.AUD, Currencies.USD);
		CreatePairState(Currencies.NZD, NzdUsd, false, Currencies.NZD, Currencies.USD);
		CreatePairState(Currencies.CAD, UsdCad, true, Currencies.USD, Currencies.CAD);
		CreatePairState(Currencies.CHF, UsdChf, true, Currencies.USD, Currencies.CHF);
		CreatePairState(Currencies.JPY, UsdJpy, true, Currencies.USD, Currencies.JPY);
	}

	private void ResetState()
	{
		_pairStates.Clear();
		_hasPreviousStrengths = false;
		_lastSignalTime = default;

		for (var i = 0; i < _currentStrengths.Length; i++)
		{
			_currentStrengths[i] = 0m;
			_previousStrengths[i] = 0m;
		}
	}

	private void EnsurePairsConfigured()
	{
		if (EurUsd == null)
			throw new InvalidOperationException("EUR/USD security is required.");
		if (GbpUsd == null)
			throw new InvalidOperationException("GBP/USD security is required.");
		if (AudUsd == null)
			throw new InvalidOperationException("AUD/USD security is required.");
		if (NzdUsd == null)
			throw new InvalidOperationException("NZD/USD security is required.");
		if (UsdCad == null)
			throw new InvalidOperationException("USD/CAD security is required.");
		if (UsdChf == null)
			throw new InvalidOperationException("USD/CHF security is required.");
		if (UsdJpy == null)
			throw new InvalidOperationException("USD/JPY security is required.");
	}

	private void CreatePairState(Currencies key, Security security, bool invert, Currencies baseCurrency, Currencies quoteCurrency)
	{
		var state = new PairState
		{
			Security = security,
			Fast = new SimpleMovingAverage { Length = FastMaPeriod },
			Slow = new SimpleMovingAverage { Length = SlowMaPeriod },
			InvertRatio = invert,
			BaseCurrency = baseCurrency,
			QuoteCurrency = quoteCurrency,
			Ratio = 0m,
			LastTime = default,
			IsReady = false
		};

		_pairStates[key] = state;

		var subscription = SubscribeCandles(CandleType, security: security);
		subscription
			.Bind(state.Fast, state.Slow, (candle, fast, slow) => ProcessPairCandle(key, candle, fast, slow))
			.Start();
	}

	private void ProcessPairCandle(Currencies key, ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Retrieve the latest state holder for the processed currency.
		if (!_pairStates.TryGetValue(key, out var state))
			return;

		// Wait until both moving averages are formed before producing signals.
		if (!state.Fast.IsFormed || !state.Slow.IsFormed)
			return;

		// Guard against invalid values coming from the indicators.
		if (fastValue == 0m || slowValue == 0m)
			return;

		// Convert fast/slow values into the ratio used by the original indicator.
		var ratio = state.InvertRatio ? fastValue / slowValue : slowValue / fastValue;
		if (ratio == 0m)
			return;

		state.Ratio = ratio;
		state.LastTime = candle.OpenTime;
		state.IsReady = true;

		// Recalculate the combined strengths after every update.
		TryEvaluateSignals();
	}

	private void TryEvaluateSignals()
	{
		if (!TryUpdateStrengths())
			return;

		var minTime = GetMinimumTime();
		if (minTime <= _lastSignalTime)
			return;

		if (!_hasPreviousStrengths)
		{
			CopyStrengths();
			_lastSignalTime = minTime;
			_hasPreviousStrengths = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			CopyStrengths();
			_lastSignalTime = minTime;
			return;
		}

		var step = StepThreshold;

		for (var topIndex = 0; topIndex < CurrencyNames.Length; topIndex++)
		{
			// Evaluate every ordered currency pair to spot new breakouts.
			for (var downIndex = 0; downIndex < CurrencyNames.Length; downIndex++)
			{
				if (topIndex == downIndex)
					continue;

				var diff = _currentStrengths[topIndex] - _currentStrengths[downIndex];
				var prevDiff = _previousStrengths[topIndex] - _previousStrengths[downIndex];

				if (diff > step && prevDiff <= step &&
					_currentStrengths[topIndex] > _previousStrengths[topIndex] &&
					_currentStrengths[downIndex] < _previousStrengths[downIndex])
				{
					GenerateTrades((Currencies)topIndex, (Currencies)downIndex);
				}
			}
		}

		CopyStrengths();
		_lastSignalTime = minTime;
	}

	private bool TryUpdateStrengths()
	{
		if (_pairStates.Count != 7)
			return false;

		foreach (var pair in _pairStates.Values)
		{
			if (!pair.IsReady)
				return false;
		}

		var eur = _pairStates[Currencies.EUR].Ratio;
		var gbp = _pairStates[Currencies.GBP].Ratio;
		var aud = _pairStates[Currencies.AUD].Ratio;
		var nzd = _pairStates[Currencies.NZD].Ratio;
		var cad = _pairStates[Currencies.CAD].Ratio;
		var chf = _pairStates[Currencies.CHF].Ratio;
		var jpy = _pairStates[Currencies.JPY].Ratio;

		if (eur == 0m || gbp == 0m || aud == 0m || nzd == 0m || cad == 0m || chf == 0m || jpy == 0m)
			return false;

		var sum = eur + gbp + aud + nzd + cad + chf + jpy;
		// Apply the same aggregation formula used by the MQL CCFp indicator.
		_currentStrengths[(int)Currencies.USD] = sum - 7m;
		_currentStrengths[(int)Currencies.EUR] = (sum - eur + 1m) / eur - 7m;
		_currentStrengths[(int)Currencies.GBP] = (sum - gbp + 1m) / gbp - 7m;
		_currentStrengths[(int)Currencies.CHF] = (sum - chf + 1m) / chf - 7m;
		_currentStrengths[(int)Currencies.JPY] = (sum - jpy + 1m) / jpy - 7m;
		_currentStrengths[(int)Currencies.AUD] = (sum - aud + 1m) / aud - 7m;
		_currentStrengths[(int)Currencies.CAD] = (sum - cad + 1m) / cad - 7m;
		_currentStrengths[(int)Currencies.NZD] = (sum - nzd + 1m) / nzd - 7m;

		return true;
	}

	private DateTimeOffset GetMinimumTime()
	{
		var minTime = DateTimeOffset.MaxValue;

		foreach (var pair in _pairStates.Values)
		{
			if (pair.LastTime < minTime)
				minTime = pair.LastTime;
		}

		return minTime;
	}

	private void CopyStrengths()
	{
		for (var i = 0; i < _currentStrengths.Length; i++)
			_previousStrengths[i] = _currentStrengths[i];
	}

	private void GenerateTrades(Currencies top, Currencies down)
	{
		var comment = $"({CurrencyNames[(int)top]}{CurrencyNames[(int)down]})";
		// Keep the comment format to match historical MQL trade annotations.
		var actions = BuildActions(top, down);

		for (var i = 0; i < actions.Count; i++)
			ExecuteAction(actions[i], comment);
	}

	private List<TradeAction> BuildActions(Currencies top, Currencies down)
	{
		var actions = new List<TradeAction>();

		if (top == Currencies.USD)
		{
			if (TryGetCurrencyAction(down, false, out var action))
				actions.Add(action);
			return actions;
		}

		if (down == Currencies.USD)
		{
			if (TryGetCurrencyAction(top, true, out var action))
				actions.Add(action);
			return actions;
		}

		if (TryGetCurrencyAction(top, true, out var longAction))
			actions.Add(longAction);

		if (TryGetCurrencyAction(down, false, out var shortAction))
			actions.Add(shortAction);

		return actions;
	}

	private bool TryGetCurrencyAction(Currencies currency, bool longCurrency, out TradeAction action)
	{
		action = default;

		if (currency == Currencies.USD)
			return false;

		if (!_pairStates.TryGetValue(currency, out var state))
			return false;

		if (state.Security == null)
			return false;

		var side = DetermineSide(state, currency, longCurrency);
		action = new TradeAction(state.Security, side);
		return true;
	}

	private static Sides DetermineSide(PairState state, Currencies currency, bool longCurrency)
	{
		return state.BaseCurrency == currency
			? (longCurrency ? Sides.Buy : Sides.Sell)
			: (longCurrency ? Sides.Sell : Sides.Buy);
	}

	private void ExecuteAction(TradeAction action, string comment)
	{
		var security = action.Security;
		if (security == null)
			return;

		var positionValue = GetPositionValue(security, Portfolio) ?? 0m;

		if (positionValue > 0m)
		{
			if (action.Side == Sides.Buy)
				return;

			if (CloseOppositePositions)
			{
				// Close opposite exposure before placing a fresh order.
				RegisterOrder(new Order
				{
					Security = security,
					Portfolio = Portfolio,
					Side = Sides.Sell,
					Volume = Math.Abs(positionValue),
					Type = OrderTypes.Market,
					Comment = comment + " close"
				});
			}

			return;
		}

		if (positionValue < 0m)
		{
			if (action.Side == Sides.Sell)
				return;

			if (CloseOppositePositions)
			{
				// Close opposite exposure before placing a fresh order.
				RegisterOrder(new Order
				{
					Security = security,
					Portfolio = Portfolio,
					Side = Sides.Buy,
					Volume = Math.Abs(positionValue),
					Type = OrderTypes.Market,
					Comment = comment + " close"
				});
			}

			return;
		}

		var volume = Volume;
		if (volume <= 0m)
			return;

		// Send a market order with the desired direction and size.
		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = action.Side,
			Volume = volume,
			Type = OrderTypes.Market,
			Comment = comment
		});
	}
}