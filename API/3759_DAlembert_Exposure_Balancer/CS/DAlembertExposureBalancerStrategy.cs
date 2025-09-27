using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-currency strategy that reproduces the d'Alembert exposure balancing logic from the original MQL source.
/// </summary>
public class DAlembertExposureBalancerStrategy : Strategy
{
	private readonly StrategyParam<Security> _eurUsdParam;
	private readonly StrategyParam<Security> _eurGbpParam;
	private readonly StrategyParam<Security> _gbpUsdParam;
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<bool> _invertSignalsParam;
	private readonly StrategyParam<decimal> _baseVolumeParam;
	private readonly StrategyParam<int> _lotMultiplierParam;
	private readonly StrategyParam<int> _startLevelParam;
	private readonly StrategyParam<decimal> _maxPositionParam;
	private readonly StrategyParam<decimal> _maxExposureParam;
	private readonly StrategyParam<bool> _manageTakeProfitParam;
	private readonly StrategyParam<bool> _manageLossParam;
	private readonly StrategyParam<decimal> _takeProfitPercentParam;
	private readonly StrategyParam<decimal> _lossPercentParam;
	private readonly StrategyParam<int> _cooldownSecondsParam;

	private readonly Dictionary<Security, SymbolState> _states = new();
	private readonly Dictionary<string, decimal> _currencyExposure = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the <see cref="DAlembertExposureBalancerStrategy"/> class.
	/// </summary>
	public DAlembertExposureBalancerStrategy()
	{
		_eurUsdParam = Param<Security>(nameof(EurUsd))
			.SetDisplay("EURUSD", "Primary currency pair", "Instruments");

		_eurGbpParam = Param<Security>(nameof(EurGbp))
			.SetDisplay("EURGBP", "Second currency pair", "Instruments");

		_gbpUsdParam = Param<Security>(nameof(GbpUsd))
			.SetDisplay("GBPUSD", "Third currency pair", "Instruments");

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for d'Alembert logic", "Data");

		_invertSignalsParam = Param(nameof(InvertSignals), false)
			.SetDisplay("Invert Signals", "Reverse the default hedge directions", "Trading");

		_baseVolumeParam = Param(nameof(BaseVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Minimal lot size used for new trades", "Money Management");

		_lotMultiplierParam = Param(nameof(LotMultiplier), 1)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Multiplier applied to the base volume", "Money Management");

		_startLevelParam = Param(nameof(StartLevel), 1)
			.SetGreaterOrEqual(1)
			.SetDisplay("Start Level", "Initial d'Alembert betting step", "Money Management");

		_maxPositionParam = Param(nameof(MaxPositionPerSymbol), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Position", "Maximum net volume per symbol", "Risk");

		_maxExposureParam = Param(nameof(MaxExposurePerCurrency), 1.25m)
			.SetGreaterThanZero()
			.SetDisplay("Max Exposure", "Maximum absolute exposure per currency", "Risk");

		_manageTakeProfitParam = Param(nameof(ManageTakeProfit), true)
			.SetDisplay("Manage Take Profit", "Enable automatic take profit management", "Trading");

		_manageLossParam = Param(nameof(ManageLoss), true)
			.SetDisplay("Manage Loss", "Enable loss recovery exits", "Trading");

		_takeProfitPercentParam = Param(nameof(TakeProfitPercent), 0.0095m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Target percentage applied to the entry price", "Trading");

		_lossPercentParam = Param(nameof(LossPercent), 0.0025m)
			.SetGreaterThanZero()
			.SetDisplay("Loss %", "Maximum adverse excursion before forcing an exit", "Trading");

		_cooldownSecondsParam = Param(nameof(CooldownSeconds), 9)
			.SetNotNegative()
			.SetDisplay("Cooldown", "Seconds between subsequent decisions", "Trading");
	}

	/// <summary>
	/// EURUSD trading instrument.
	/// </summary>
	public Security EurUsd
	{
		get => _eurUsdParam.Value;
		set => _eurUsdParam.Value = value;
	}

	/// <summary>
	/// EURGBP trading instrument.
	/// </summary>
	public Security EurGbp
	{
		get => _eurGbpParam.Value;
		set => _eurGbpParam.Value = value;
	}

	/// <summary>
	/// GBPUSD trading instrument.
	/// </summary>
	public Security GbpUsd
	{
		get => _gbpUsdParam.Value;
		set => _gbpUsdParam.Value = value;
	}

	/// <summary>
	/// Candle type used for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Reverse default hedge directions.
	/// </summary>
	public bool InvertSignals
	{
		get => _invertSignalsParam.Value;
		set => _invertSignalsParam.Value = value;
	}

	/// <summary>
	/// Minimal volume for a single lot.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolumeParam.Value;
		set => _baseVolumeParam.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the base volume.
	/// </summary>
	public int LotMultiplier
	{
		get => _lotMultiplierParam.Value;
		set => _lotMultiplierParam.Value = value;
	}

	/// <summary>
	/// Initial d'Alembert level.
	/// </summary>
	public int StartLevel
	{
		get => _startLevelParam.Value;
		set => _startLevelParam.Value = value;
	}

	/// <summary>
	/// Maximum net position per symbol.
	/// </summary>
	public decimal MaxPositionPerSymbol
	{
		get => _maxPositionParam.Value;
		set => _maxPositionParam.Value = value;
	}

	/// <summary>
	/// Maximum allowed absolute exposure per currency.
	/// </summary>
	public decimal MaxExposurePerCurrency
	{
		get => _maxExposureParam.Value;
		set => _maxExposureParam.Value = value;
	}

	/// <summary>
	/// Enable automatic take profit handling.
	/// </summary>
	public bool ManageTakeProfit
	{
		get => _manageTakeProfitParam.Value;
		set => _manageTakeProfitParam.Value = value;
	}

	/// <summary>
	/// Enable automatic loss exits.
	/// </summary>
	public bool ManageLoss
	{
		get => _manageLossParam.Value;
		set => _manageLossParam.Value = value;
	}

	/// <summary>
	/// Take profit expressed as a percentage of the entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercentParam.Value;
		set => _takeProfitPercentParam.Value = value;
	}

	/// <summary>
	/// Allowed loss expressed as a percentage of the entry price.
	/// </summary>
	public decimal LossPercent
	{
		get => _lossPercentParam.Value;
		set => _lossPercentParam.Value = value;
	}

	/// <summary>
	/// Cooldown between trading decisions in seconds.
	/// </summary>
	public int CooldownSeconds
	{
		get => _cooldownSecondsParam.Value;
		set => _cooldownSecondsParam.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (EurUsd != null)
			yield return (EurUsd, CandleType);

		if (EurGbp != null)
			yield return (EurGbp, CandleType);

		if (GbpUsd != null)
			yield return (GbpUsd, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_states.Clear();
		_currencyExposure.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_states.Clear();
		_currencyExposure.Clear();

		CreateState(EurUsd, InvertSignals ? SymbolBias.Sell : SymbolBias.Buy);
		CreateState(EurGbp, InvertSignals ? SymbolBias.Buy : SymbolBias.Sell);
		CreateState(GbpUsd, InvertSignals ? SymbolBias.Buy : SymbolBias.Sell);

		foreach (var state in _states.Values)
		{
			var subscription = SubscribeCandles(CandleType, state.Security);
			subscription.Bind((candle) => ProcessCandle(state, candle)).Start();
		}
	}

	private void CreateState(Security security, SymbolBias bias)
	{
		if (security == null)
		return;

		if (_states.ContainsKey(security))
		return;

		_states.Add(security, new SymbolState(security, bias, StartLevel));
	}

	private void ProcessCandle(SymbolState state, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		state.UpdateHeikinAshi(candle);

		var now = candle.CloseTime;
		var cooldown = TimeSpan.FromSeconds(CooldownSeconds);
		if (now - state.LastDecisionTime < cooldown)
		return;

		var position = state.Position;
		if (position == 0m)
		{
			TryOpen(state, candle);
		}
		else if (position > 0m)
		{
			TryManageLong(state, candle);
		}
		else
		{
			TryManageShort(state, candle);
		}
	}

	private void TryOpen(SymbolState state, ICandleMessage candle)
	{
		var bias = state.Bias;
		var bullish = state.IsBullish;
		var bearish = state.IsBearish;

		if (bias == SymbolBias.Neutral)
		return;

		var openLong = (bias == SymbolBias.Buy && bullish) || (bias == SymbolBias.Sell && bearish);
		var openShort = (bias == SymbolBias.Sell && bullish) || (bias == SymbolBias.Buy && bearish);

		var volume = CalculateVolume(state, 0m);
		if (volume <= 0m)
		return;

		if (openLong && CanOpen(state, Sides.Buy, volume))
		{
			LogInfo($"Opening long on {state.Security.Id} with volume {volume:0.##} lots.");
			BuyMarket(volume, state.Security);
			state.LastDecisionTime = candle.CloseTime;
		}
		else if (openShort && CanOpen(state, Sides.Sell, volume))
		{
			LogInfo($"Opening short on {state.Security.Id} with volume {volume:0.##} lots.");
			SellMarket(volume, state.Security);
			state.LastDecisionTime = candle.CloseTime;
		}
	}

	private void TryManageLong(SymbolState state, ICandleMessage candle)
	{
		if (state.Position <= 0m)
		return;

		var exitReason = string.Empty;

		if (ManageTakeProfit && state.AverageEntryPrice.HasValue)
		{
			var target = state.AverageEntryPrice.Value * (1m + TakeProfitPercent);
			if (candle.HighPrice >= target)
			{
			exitReason = $"Take profit reached at {target:0.#####}";
			}
		}

		if (exitReason.Length == 0 && ManageLoss && state.AverageEntryPrice.HasValue)
		{
			var stopPrice = state.AverageEntryPrice.Value * (1m - LossPercent);
			if (candle.LowPrice <= stopPrice)
			{
			exitReason = $"Loss stop triggered at {stopPrice:0.#####}";
			}
		}

		if (exitReason.Length == 0 && state.IsBearish)
		exitReason = "Heikin Ashi reversal detected";

		if (exitReason.Length == 0)
		return;

		LogInfo($"Closing long on {state.Security.Id}: {exitReason}.");
		SellMarket(state.Position, state.Security);
		state.LastDecisionTime = candle.CloseTime;
	}

	private void TryManageShort(SymbolState state, ICandleMessage candle)
	{
		if (state.Position >= 0m)
		return;

		var exitReason = string.Empty;

		if (ManageTakeProfit && state.AverageEntryPrice.HasValue)
		{
			var target = state.AverageEntryPrice.Value * (1m - TakeProfitPercent);
			if (candle.LowPrice <= target)
			{
			exitReason = $"Take profit reached at {target:0.#####}";
			}
		}

		if (exitReason.Length == 0 && ManageLoss && state.AverageEntryPrice.HasValue)
		{
			var stopPrice = state.AverageEntryPrice.Value * (1m + LossPercent);
			if (candle.HighPrice >= stopPrice)
			{
			exitReason = $"Loss stop triggered at {stopPrice:0.#####}";
			}
		}

		if (exitReason.Length == 0 && state.IsBullish)
		exitReason = "Heikin Ashi reversal detected";

		if (exitReason.Length == 0)
		return;

		LogInfo($"Closing short on {state.Security.Id}: {exitReason}.");
		BuyMarket(Math.Abs(state.Position), state.Security);
		state.LastDecisionTime = candle.CloseTime;
	}

	private decimal CalculateVolume(SymbolState state, decimal additionalSignedVolume)
	{
		var baseVolume = BaseVolume;
		if (baseVolume <= 0m)
		return 0m;

		var multiplier = Math.Max(1, LotMultiplier);
		var level = Math.Max(1, StartLevel + state.LevelOffset);
		var planned = baseVolume * multiplier * level;

		var maxPerSymbol = MaxPositionPerSymbol;
		if (maxPerSymbol > 0m)
		{
			var allowed = maxPerSymbol - Math.Abs(state.Position + additionalSignedVolume);
			planned = Math.Min(planned, Math.Max(0m, allowed));
		}

		return Math.Round(planned, 2, MidpointRounding.AwayFromZero);
	}

	private bool CanOpen(SymbolState state, Sides side, decimal volume)
	{
		if (volume <= 0m)
		return false;

		var signedVolume = side == Sides.Buy ? volume : -volume;
		var maxPerSymbol = MaxPositionPerSymbol;
		if (maxPerSymbol > 0m)
		{
			var projected = Math.Abs(state.Position + signedVolume);
			if (projected > maxPerSymbol)
			return false;
		}

		var maxExposure = MaxExposurePerCurrency;
		if (maxExposure <= 0m)
		return true;

		var snapshot = new Dictionary<string, decimal>(_currencyExposure, StringComparer.OrdinalIgnoreCase);
		ApplyExposure(snapshot, state.Security, side, volume);

		foreach (var pair in snapshot)
		{
			if (Math.Abs(pair.Value) > maxExposure)
			return false;
		}

		return true;
	}

	private void ApplyExposure(IDictionary<string, decimal> storage, Security security, Sides side, decimal volume)
	{
		var (baseCurrency, quoteCurrency) = ExtractCurrencies(security);
		if (baseCurrency == null || quoteCurrency == null)
		return;

		var baseSign = side == Sides.Buy ? 1m : -1m;
		var quoteSign = -baseSign;

		storage[baseCurrency] = storage.TryGetValue(baseCurrency, out var baseValue)
		? baseValue + baseSign * volume
		: baseSign * volume;

		storage[quoteCurrency] = storage.TryGetValue(quoteCurrency, out var quoteValue)
		? quoteValue + quoteSign * volume
		: quoteSign * volume;
	}

	private void RecalculateExposure()
	{
		_currencyExposure.Clear();

		foreach (var state in _states.Values)
		{
			var position = state.Position;
			if (position == 0m)
			continue;

			var side = position > 0m ? Sides.Buy : Sides.Sell;
			ApplyExposure(_currencyExposure, state.Security, side, Math.Abs(position));
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		var security = order?.Security;
		if (security == null)
		return;

		if (!_states.TryGetValue(security, out var state))
		return;

		var direction = order.Direction;
		var volume = trade.Trade?.Volume ?? order.Volume ?? 0m;
		if (volume <= 0m)
		return;

		var price = trade.Trade?.Price ?? order.Price ?? 0m;
		var signedVolume = direction == Sides.Buy ? volume : -volume;

		var previousPosition = state.Position;
		var newPosition = previousPosition + signedVolume;

		if (previousPosition == 0m || Math.Sign(previousPosition) == Math.Sign(signedVolume))
		{
			var existingSize = Math.Abs(previousPosition);
			var newSize = existingSize + volume;
			var avgPrice = state.AverageEntryPrice ?? price;
			avgPrice = (avgPrice * existingSize + price * volume) / newSize;
			state.AverageEntryPrice = avgPrice;
		}
		else
		{
			var closingVolume = Math.Min(volume, Math.Abs(previousPosition));
			if (previousPosition > 0m)
			{
				var entry = state.AverageEntryPrice ?? price;
				state.LastClosedPnL += (price - entry) * closingVolume;
			}
			else
			{
				var entry = state.AverageEntryPrice ?? price;
				state.LastClosedPnL += (entry - price) * closingVolume;
			}

			if (Math.Sign(previousPosition) != Math.Sign(newPosition) && newPosition != 0m)
			{
				state.AverageEntryPrice = price;
				state.LastClosedPnL = 0m;
			}
		}

		state.Position = newPosition;

		if (newPosition == 0m)
		{
			AdjustLevel(state);
			state.AverageEntryPrice = null;
			state.LastClosedPnL = 0m;
		}

		state.LastTradeTime = trade.Trade?.Time ?? CurrentTime;

		RecalculateExposure();
	}

	private void AdjustLevel(SymbolState state)
	{
		if (state.LastClosedPnL > 0m)
		{
			state.LevelOffset = Math.Max(0, state.LevelOffset - 1);
		}
		else if (state.LastClosedPnL < 0m)
		{
			state.LevelOffset++;
		}
	}

	private static (string Base, string Quote) ExtractCurrencies(Security security)
	{
		var code = security?.Code;
		if (code.IsEmpty() || code.Length < 6)
		return (null, null);

		var baseCurrency = code.Substring(0, 3);
		var quoteCurrency = code.Substring(3, Math.Min(3, code.Length - 3));
		return (baseCurrency, quoteCurrency);
	}

	private sealed class SymbolState
	{
		public SymbolState(Security security, SymbolBias bias, int startLevel)
		{
			Security = security;
			Bias = bias;
			LevelOffset = Math.Max(0, startLevel - 1);
		}

		public Security Security { get; }
		public SymbolBias Bias { get; }
		public decimal Position { get; set; }
		public decimal? AverageEntryPrice { get; set; }
		public decimal LastClosedPnL { get; set; }
		public int LevelOffset { get; set; }
		public DateTimeOffset LastTradeTime { get; set; }
		public DateTimeOffset LastDecisionTime { get; set; }
		public decimal HaOpen { get; private set; }
		public decimal HaClose { get; private set; }
		public bool HasHeikin { get; private set; }

		public bool IsBullish => HasHeikin && HaClose > HaOpen;
		public bool IsBearish => HasHeikin && HaClose < HaOpen;

		public void UpdateHeikinAshi(ICandleMessage candle)
		{
			var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;

			decimal haOpen;
			if (!HasHeikin)
			{
				haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
				HasHeikin = true;
			}
			else
			{
				haOpen = (HaOpen + HaClose) / 2m;
			}

			HaOpen = haOpen;
			HaClose = haClose;
		}
	}

	private enum SymbolBias
	{
		Neutral,
		Buy,
		Sell
	}
}
