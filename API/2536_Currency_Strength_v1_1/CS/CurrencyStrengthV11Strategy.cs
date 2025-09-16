using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades multiple FX pairs based on relative currency strength.
/// It compares percentage changes of the major currencies calculated from daily candles.
/// </summary>
public class CurrencyStrengthV11Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _differenceThreshold;
	private readonly StrategyParam<bool> _tradeOncePerDay;
	private readonly StrategyParam<bool> _useSlTp;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;

	// Currency pair parameters used to recreate the original basket.
	private readonly StrategyParam<Security> _usdJpy;
	private readonly StrategyParam<Security> _usdCad;
	private readonly StrategyParam<Security> _audUsd;
	private readonly StrategyParam<Security> _usdChf;
	private readonly StrategyParam<Security> _gbpUsd;
	private readonly StrategyParam<Security> _eurUsd;
	private readonly StrategyParam<Security> _nzdUsd;
	private readonly StrategyParam<Security> _eurJpy;
	private readonly StrategyParam<Security> _eurCad;
	private readonly StrategyParam<Security> _eurGbp;
	private readonly StrategyParam<Security> _eurChf;
	private readonly StrategyParam<Security> _eurAud;
	private readonly StrategyParam<Security> _eurNzd;
	private readonly StrategyParam<Security> _audNzd;
	private readonly StrategyParam<Security> _audCad;
	private readonly StrategyParam<Security> _audChf;
	private readonly StrategyParam<Security> _audJpy;
	private readonly StrategyParam<Security> _chfJpy;
	private readonly StrategyParam<Security> _gbpChf;
	private readonly StrategyParam<Security> _gbpAud;
	private readonly StrategyParam<Security> _gbpCad;
	private readonly StrategyParam<Security> _gbpJpy;
	private readonly StrategyParam<Security> _cadJpy;
	private readonly StrategyParam<Security> _nzdJpy;
	private readonly StrategyParam<Security> _gbpNzd;
	private readonly StrategyParam<Security> _cadChf;

	private readonly List<PairDefinition> _pairDefinitions = new();
	private readonly Dictionary<string, PairState> _pairStates = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<(Security security, Sides side), DateTime> _lastTradeDate = new();
	private readonly Dictionary<Security, PositionState> _positionStates = new();

	// Tracks the last trading date that was evaluated to avoid duplicate processing.
	private DateTime _lastProcessedDate = DateTime.MinValue;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CurrencyStrengthV11Strategy()
	{
		// Configure the timeframe used for strength calculations.
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strength calculation", "General");

		_differenceThreshold = Param(nameof(DifferenceThreshold), 0.5m)
		.SetDisplay("Strength Threshold", "Minimum difference between currencies", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 2m, 0.1m);

		_tradeOncePerDay = Param(nameof(TradeOncePerDay), true)
		.SetDisplay("Trade Once", "Allow only one trade per direction per day", "Risk Management");

		_useSlTp = Param(nameof(UseSlTp), false)
		.SetDisplay("Use SL/TP", "Enable daily stop-loss and take-profit checks", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
		.SetDisplay("Take Profit (pips)", "Target in pips for closing trades", "Risk Management");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
		.SetDisplay("Stop Loss (pips)", "Protective stop in pips", "Risk Management");

		// Register the 26 pairs exactly as in the MetaTrader implementation.
		_usdJpy = AddPair(nameof(UsdJpy), "USD/JPY pair");
		_usdCad = AddPair(nameof(UsdCad), "USD/CAD pair");
		_audUsd = AddPair(nameof(AudUsd), "AUD/USD pair");
		_usdChf = AddPair(nameof(UsdChf), "USD/CHF pair");
		_gbpUsd = AddPair(nameof(GbpUsd), "GBP/USD pair");
		_eurUsd = AddPair(nameof(EurUsd), "EUR/USD pair");
		_nzdUsd = AddPair(nameof(NzdUsd), "NZD/USD pair");
		_eurJpy = AddPair(nameof(EurJpy), "EUR/JPY pair");
		_eurCad = AddPair(nameof(EurCad), "EUR/CAD pair");
		_eurGbp = AddPair(nameof(EurGbp), "EUR/GBP pair");
		_eurChf = AddPair(nameof(EurChf), "EUR/CHF pair");
		_eurAud = AddPair(nameof(EurAud), "EUR/AUD pair");
		_eurNzd = AddPair(nameof(EurNzd), "EUR/NZD pair");
		_audNzd = AddPair(nameof(AudNzd), "AUD/NZD pair");
		_audCad = AddPair(nameof(AudCad), "AUD/CAD pair");
		_audChf = AddPair(nameof(AudChf), "AUD/CHF pair");
		_audJpy = AddPair(nameof(AudJpy), "AUD/JPY pair");
		_chfJpy = AddPair(nameof(ChfJpy), "CHF/JPY pair");
		_gbpChf = AddPair(nameof(GbpChf), "GBP/CHF pair");
		_gbpAud = AddPair(nameof(GbpAud), "GBP/AUD pair");
		_gbpCad = AddPair(nameof(GbpCad), "GBP/CAD pair");
		_gbpJpy = AddPair(nameof(GbpJpy), "GBP/JPY pair");
		_cadJpy = AddPair(nameof(CadJpy), "CAD/JPY pair");
		_nzdJpy = AddPair(nameof(NzdJpy), "NZD/JPY pair");
		_gbpNzd = AddPair(nameof(GbpNzd), "GBP/NZD pair");
		_cadChf = AddPair(nameof(CadChf), "CAD/CHF pair");

		Volume = 0.01m;
	}

	/// <summary>
	/// Timeframe used to compute percentage change for each pair.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum difference between two currency strengths to trigger trades.
	/// </summary>
	public decimal DifferenceThreshold
	{
		get => _differenceThreshold.Value;
		set => _differenceThreshold.Value = value;
	}

	/// <summary>
	/// Enables the one-trade-per-direction-per-day limit.
	/// </summary>
	public bool TradeOncePerDay
	{
		get => _tradeOncePerDay.Value;
		set => _tradeOncePerDay.Value = value;
	}

	/// <summary>
	/// Enables use of stop-loss and take-profit evaluated on daily candles.
	/// </summary>
	public bool UseSlTp
	{
		get => _useSlTp.Value;
		set => _useSlTp.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// USD/JPY security.
	/// </summary>
	public Security UsdJpy
	{
		get => _usdJpy.Value;
		set => _usdJpy.Value = value;
	}

	/// <summary>
	/// USD/CAD security.
	/// </summary>
	public Security UsdCad
	{
		get => _usdCad.Value;
		set => _usdCad.Value = value;
	}

	/// <summary>
	/// AUD/USD security.
	/// </summary>
	public Security AudUsd
	{
		get => _audUsd.Value;
		set => _audUsd.Value = value;
	}

	/// <summary>
	/// USD/CHF security.
	/// </summary>
	public Security UsdChf
	{
		get => _usdChf.Value;
		set => _usdChf.Value = value;
	}

	/// <summary>
	/// GBP/USD security.
	/// </summary>
	public Security GbpUsd
	{
		get => _gbpUsd.Value;
		set => _gbpUsd.Value = value;
	}

	/// <summary>
	/// EUR/USD security.
	/// </summary>
	public Security EurUsd
	{
		get => _eurUsd.Value;
		set => _eurUsd.Value = value;
	}

	/// <summary>
	/// NZD/USD security.
	/// </summary>
	public Security NzdUsd
	{
		get => _nzdUsd.Value;
		set => _nzdUsd.Value = value;
	}

	/// <summary>
	/// EUR/JPY security.
	/// </summary>
	public Security EurJpy
	{
		get => _eurJpy.Value;
		set => _eurJpy.Value = value;
	}

	/// <summary>
	/// EUR/CAD security.
	/// </summary>
	public Security EurCad
	{
		get => _eurCad.Value;
		set => _eurCad.Value = value;
	}

	/// <summary>
	/// EUR/GBP security.
	/// </summary>
	public Security EurGbp
	{
		get => _eurGbp.Value;
		set => _eurGbp.Value = value;
	}

	/// <summary>
	/// EUR/CHF security.
	/// </summary>
	public Security EurChf
	{
		get => _eurChf.Value;
		set => _eurChf.Value = value;
	}

	/// <summary>
	/// EUR/AUD security.
	/// </summary>
	public Security EurAud
	{
		get => _eurAud.Value;
		set => _eurAud.Value = value;
	}

	/// <summary>
	/// EUR/NZD security.
	/// </summary>
	public Security EurNzd
	{
		get => _eurNzd.Value;
		set => _eurNzd.Value = value;
	}

	/// <summary>
	/// AUD/NZD security.
	/// </summary>
	public Security AudNzd
	{
		get => _audNzd.Value;
		set => _audNzd.Value = value;
	}

	/// <summary>
	/// AUD/CAD security.
	/// </summary>
	public Security AudCad
	{
		get => _audCad.Value;
		set => _audCad.Value = value;
	}

	/// <summary>
	/// AUD/CHF security.
	/// </summary>
	public Security AudChf
	{
		get => _audChf.Value;
		set => _audChf.Value = value;
	}

	/// <summary>
	/// AUD/JPY security.
	/// </summary>
	public Security AudJpy
	{
		get => _audJpy.Value;
		set => _audJpy.Value = value;
	}

	/// <summary>
	/// CHF/JPY security.
	/// </summary>
	public Security ChfJpy
	{
		get => _chfJpy.Value;
		set => _chfJpy.Value = value;
	}

	/// <summary>
	/// GBP/CHF security.
	/// </summary>
	public Security GbpChf
	{
		get => _gbpChf.Value;
		set => _gbpChf.Value = value;
	}

	/// <summary>
	/// GBP/AUD security.
	/// </summary>
	public Security GbpAud
	{
		get => _gbpAud.Value;
		set => _gbpAud.Value = value;
	}

	/// <summary>
	/// GBP/CAD security.
	/// </summary>
	public Security GbpCad
	{
		get => _gbpCad.Value;
		set => _gbpCad.Value = value;
	}

	/// <summary>
	/// GBP/JPY security.
	/// </summary>
	public Security GbpJpy
	{
		get => _gbpJpy.Value;
		set => _gbpJpy.Value = value;
	}

	/// <summary>
	/// CAD/JPY security.
	/// </summary>
	public Security CadJpy
	{
		get => _cadJpy.Value;
		set => _cadJpy.Value = value;
	}

	/// <summary>
	/// NZD/JPY security.
	/// </summary>
	public Security NzdJpy
	{
		get => _nzdJpy.Value;
		set => _nzdJpy.Value = value;
	}

	/// <summary>
	/// GBP/NZD security.
	/// </summary>
	public Security GbpNzd
	{
		get => _gbpNzd.Value;
		set => _gbpNzd.Value = value;
	}

	/// <summary>
	/// CAD/CHF security.
	/// </summary>
	public Security CadChf
	{
		get => _cadChf.Value;
		set => _cadChf.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return _pairDefinitions
		.Where(p => p.Param.Value != null)
		.Select(p => (p.Param.Value, CandleType));
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pairStates.Clear();
		_lastTradeDate.Clear();
		_positionStates.Clear();
		_lastProcessedDate = DateTime.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pairStates.Clear();

		foreach (var definition in _pairDefinitions)
		{
			var security = definition.Param.Value ?? throw new InvalidOperationException($"Security parameter {definition.Key} is not set.");

			var state = new PairState(security);
			_pairStates[definition.Key] = state;

			// Subscribe to daily candles for each pair and process updates with the shared handler.
			SubscribeCandles(CandleType, security: security)
			.Bind(candle => ProcessPair(definition.Key, candle))
			.Start();
		}
	}

	private StrategyParam<Security> AddPair(string propertyName, string description)
	{
		var param = Param<Security>(propertyName)
		.SetDisplay(propertyName.ToUpperInvariant(), description, "Currency Pairs")
		.SetRequired();

		_pairDefinitions.Add(new PairDefinition(propertyName.ToUpperInvariant(), param));
		return param;
	}

	private void ProcessPair(string key, ICandleMessage candle)
	{
		// Skip unfinished candles to avoid using incomplete information.
		if (candle.State != CandleStates.Finished)
		return;

		var state = _pairStates[key];

		// Evaluate protective exits before refreshing the percentage change.
		TryHandleProtection(state, candle);

		state.Update(candle);

		// Attempt to run the currency strength engine once all candles share the same date.
		TryEvaluateSignals(candle.OpenTime.Date);
	}

	private void TryEvaluateSignals(DateTime date)
	{
		// Ensure that every pair already produced a candle for the same session.
		if (date <= _lastProcessedDate)
		return;

		if (_pairStates.Values.Any(p => !p.HasData || p.Date != date))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		_lastProcessedDate = date;

		EvaluateSignals(date);
	}

	private void EvaluateSignals(DateTime date)
	{
		// Recreate the original basket-based strength measures for every currency.
		var eur = Average("EURJPY", "EURCAD", "EURGBP", "EURCHF", "EURAUD", "EURUSD", "EURNZD");
		var usd = (GetChange("USDJPY") + GetChange("USDCAD") - GetChange("AUDUSD") + GetChange("USDCHF") - GetChange("GBPUSD") - GetChange("EURUSD") - GetChange("NZDUSD")) / 7m;
		var jpy = -(GetChange("USDJPY") + GetChange("EURJPY") + GetChange("AUDJPY") + GetChange("CHFJPY") + GetChange("GBPJPY") + GetChange("CADJPY") + GetChange("NZDJPY")) / 7m;
		var cad = (GetChange("CADCHF") + GetChange("CADJPY") - (GetChange("GBPCAD") + GetChange("AUDCAD") + GetChange("EURCAD") + GetChange("USDCAD"))) / 6m;
		var aud = (GetChange("AUDUSD") + GetChange("AUDNZD") + GetChange("AUDCAD") + GetChange("AUDCHF") + GetChange("AUDJPY") - (GetChange("EURAUD") + GetChange("GBPAUD"))) / 7m;
		var nzd = (GetChange("NZDUSD") + GetChange("NZDJPY") - (GetChange("EURNZD") + GetChange("AUDNZD") + GetChange("GBPNZD"))) / 5m;
		var gbp = (GetChange("GBPUSD") - GetChange("EURGBP") + GetChange("GBPCHF") + GetChange("GBPAUD") + GetChange("GBPCAD") + GetChange("GBPJPY") + GetChange("GBPNZD")) / 7m;
		var chf = (GetChange("CHFJPY") - (GetChange("USDCHF") + GetChange("EURCHF") + GetChange("AUDCHF") + GetChange("GBPCHF") + GetChange("CADCHF"))) / 6m;

		// Evaluate each pair independently using the calculated currency strengths.
		ProcessSignal(date, "USDJPY", usd, jpy);
		ProcessSignal(date, "USDCAD", usd, cad);
		ProcessSignal(date, "AUDUSD", aud, usd);
		ProcessSignal(date, "USDCHF", usd, chf);
		ProcessSignal(date, "GBPUSD", gbp, usd);
		ProcessSignal(date, "EURUSD", eur, usd);
		ProcessSignal(date, "NZDUSD", nzd, usd);
		ProcessSignal(date, "EURJPY", eur, jpy);
		ProcessSignal(date, "EURCAD", eur, cad);
		ProcessSignal(date, "EURGBP", eur, gbp);
		ProcessSignal(date, "EURCHF", eur, chf);
		ProcessSignal(date, "EURAUD", eur, aud);
		ProcessSignal(date, "EURNZD", eur, nzd);
		ProcessSignal(date, "AUDNZD", aud, nzd);
		ProcessSignal(date, "AUDCAD", aud, cad);
		ProcessSignal(date, "AUDCHF", aud, chf);
		ProcessSignal(date, "AUDJPY", aud, jpy);
		ProcessSignal(date, "CHFJPY", chf, jpy);
		ProcessSignal(date, "GBPCHF", gbp, chf);
		ProcessSignal(date, "GBPAUD", gbp, aud);
		ProcessSignal(date, "GBPCAD", gbp, cad);
		ProcessSignal(date, "GBPJPY", gbp, jpy);
		ProcessSignal(date, "CADJPY", cad, jpy);
		ProcessSignal(date, "NZDJPY", nzd, jpy);
		ProcessSignal(date, "GBPNZD", gbp, nzd);
		ProcessSignal(date, "CADCHF", cad, chf);
	}

	private decimal Average(params string[] keys)
	{
		var sum = keys.Sum(GetChange);
		return sum / keys.Length;
	}

	private decimal GetChange(string key)
	{
		return _pairStates[key].PercentChange;
	}

	private void ProcessSignal(DateTime date, string pairKey, decimal baseCurrency, decimal quoteCurrency)
	{
		// Compare the two currency strengths and react only when the spread exceeds the threshold.
		var difference = baseCurrency - quoteCurrency;
		if (Math.Abs(difference) <= DifferenceThreshold)
		return;

		var state = _pairStates[pairKey];
		var desiredSide = difference > 0m ? Sides.Buy : Sides.Sell;

		TryEnterPosition(date, state, desiredSide);
	}

	private void TryEnterPosition(DateTime date, PairState pair, Sides side)
	{
		var security = pair.Security;
		var position = PositionBy(security);

		if (side == Sides.Buy)
		{
			// Only buy when the candle confirms the bullish bias.
			if (!pair.IsBullish)
			return;

			// Skip if a long position already exists.
			if (position > 0m)
			return;

			// Enforce the one-trade-per-day limit.
			if (TradeOncePerDay && _lastTradeDate.TryGetValue((security, Sides.Buy), out var lastDate) && lastDate >= date)
			return;

			var volume = Volume;
			if (volume <= 0m)
			return;

			// If we are short, close the short and flip the position size.
			if (position < 0m)
			{
				volume += Math.Abs(position);
				_positionStates.Remove(security);
			}

			BuyMarket(security, volume);
			_lastTradeDate[(security, Sides.Buy)] = date;

			if (UseSlTp)
			{
				_positionStates[security] = new PositionState
				{
					Side = Sides.Buy,
					EntryPrice = pair.ClosePrice,
					EntryDate = date
				};
			}
		}
		else
		{
			// Only sell when the candle confirms the bearish bias.
			if (!pair.IsBearish)
			return;

			if (position < 0m)
			return;

			if (TradeOncePerDay && _lastTradeDate.TryGetValue((security, Sides.Sell), out var lastDate) && lastDate >= date)
			return;

			var volume = Volume;
			if (volume <= 0m)
			return;

			// If we are long, liquidate and reverse in one order.
			if (position > 0m)
			{
				volume += position;
				_positionStates.Remove(security);
			}

			SellMarket(security, volume);
			_lastTradeDate[(security, Sides.Sell)] = date;

			if (UseSlTp)
			{
				_positionStates[security] = new PositionState
				{
					Side = Sides.Sell,
					EntryPrice = pair.ClosePrice,
					EntryDate = date
				};
			}
		}
	}

	private void TryHandleProtection(PairState pair, ICandleMessage candle)
	{
		// Exit management is optional and only runs when explicitly enabled.
		if (!UseSlTp)
		return;

		if (!_positionStates.TryGetValue(pair.Security, out var positionState))
		return;

		var position = PositionBy(pair.Security);
		if (position == 0m)
		{
			_positionStates.Remove(pair.Security);
			return;
		}

		var pipSize = GetPipSize(pair.Security);
		if (pipSize <= 0m)
		return;

		if (positionState.Side == Sides.Buy)
		{
			var exitVolume = Math.Abs(position);

			if (TakeProfitPips > 0m)
			{
				var target = positionState.EntryPrice + pipSize * TakeProfitPips;
				if (candle.HighPrice >= target)
				{
					SellMarket(pair.Security, exitVolume);
					_positionStates.Remove(pair.Security);
					return;
				}
			}

			if (StopLossPips > 0m)
			{
				var stop = positionState.EntryPrice - pipSize * StopLossPips;
				if (candle.LowPrice <= stop)
				{
					SellMarket(pair.Security, exitVolume);
					_positionStates.Remove(pair.Security);
				}
			}
		}
		else
		{
			var exitVolume = Math.Abs(position);

			if (TakeProfitPips > 0m)
			{
				var target = positionState.EntryPrice - pipSize * TakeProfitPips;
				if (candle.LowPrice <= target)
				{
					BuyMarket(pair.Security, exitVolume);
					_positionStates.Remove(pair.Security);
					return;
				}
			}

			if (StopLossPips > 0m)
			{
				var stop = positionState.EntryPrice + pipSize * StopLossPips;
				if (candle.HighPrice >= stop)
				{
					BuyMarket(pair.Security, exitVolume);
					_positionStates.Remove(pair.Security);
				}
			}
		}
	}

	private decimal GetPipSize(Security security)
	{
		// Convert the exchange-specific price step into an approximate pip size.
		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		step = security.MinPriceStep ?? 0m;

		if (step <= 0m)
		return 0.0001m;

		if (step >= 0.01m)
		return step;

		return step * 10m;
	}

	private decimal PositionBy(Security security)
	{
		// Helper that returns the current net position for the requested security.
		return GetPositionValue(security, Portfolio) ?? 0m;
	}

	private sealed class PairDefinition
	{
		public PairDefinition(string key, StrategyParam<Security> param)
		{
			Key = key;
			Param = param;
		}

		public string Key { get; }
		public StrategyParam<Security> Param { get; }
	}

	private sealed class PairState
	{
		public PairState(Security security)
		{
			Security = security;
		}

		public Security Security { get; }
		public decimal PercentChange { get; private set; }
		public DateTime Date { get; private set; }
		public decimal OpenPrice { get; private set; }
		public decimal ClosePrice { get; private set; }
		public decimal HighPrice { get; private set; }
		public decimal LowPrice { get; private set; }
		public bool HasData { get; private set; }
		public bool IsBullish => ClosePrice > OpenPrice;
		public bool IsBearish => ClosePrice < OpenPrice;

		public void Update(ICandleMessage candle)
		{
			// Store the latest daily metrics that feed the strength calculations.
			OpenPrice = candle.OpenPrice;
			ClosePrice = candle.ClosePrice;
			HighPrice = candle.HighPrice;
			LowPrice = candle.LowPrice;
			PercentChange = candle.OpenPrice != 0m ? (candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice * 100m : 0m;
			Date = candle.OpenTime.Date;
			HasData = true;
		}
	}

	private sealed class PositionState
	{
		public decimal EntryPrice { get; set; }
		public Sides Side { get; set; }
		public DateTime EntryDate { get; set; }
	}
}
