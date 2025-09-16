using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-asset hedge strategy converted from the MQL HTH Trader expert advisor.
/// </summary>
public class HthTraderStrategy : Strategy
{
	private readonly StrategyParam<bool> _tradeEnabled;
	private readonly StrategyParam<bool> _showProfitInfo;
	private readonly StrategyParam<bool> _useProfitTarget;
	private readonly StrategyParam<bool> _useLossLimit;
	private readonly StrategyParam<bool> _allowEmergencyTrading;
	private readonly StrategyParam<int> _emergencyLossPips;
	private readonly StrategyParam<int> _profitTargetPips;
	private readonly StrategyParam<int> _lossLimitPips;
	private readonly StrategyParam<decimal> _volumeParam;
	private readonly StrategyParam<Security> _symbol2Param;
	private readonly StrategyParam<Security> _symbol3Param;
	private readonly StrategyParam<Security> _symbol4Param;
	private readonly StrategyParam<DataType> _intradayCandleTypeParam;

	private readonly DataType _dailyCandleType = TimeSpan.FromDays(1).TimeFrame();

	private readonly Dictionary<Security, InstrumentState> _states = new();

	private DateTime _lastTradeDate;
	private bool _emergencyReady;

	/// <summary>
	/// Enable automated trading.
	/// </summary>
	public bool TradeEnabled
	{
		get => _tradeEnabled.Value;
		set => _tradeEnabled.Value = value;
	}

	/// <summary>
	/// Log aggregated open profit in pips on each bar.
	/// </summary>
	public bool ShowProfitInfo
	{
		get => _showProfitInfo.Value;
		set => _showProfitInfo.Value = value;
	}

	/// <summary>
	/// Enable closing by reaching the daily profit target.
	/// </summary>
	public bool UseProfitTarget
	{
		get => _useProfitTarget.Value;
		set => _useProfitTarget.Value = value;
	}

	/// <summary>
	/// Enable closing by reaching the daily loss limit.
	/// </summary>
	public bool UseLossLimit
	{
		get => _useLossLimit.Value;
		set => _useLossLimit.Value = value;
	}

	/// <summary>
	/// Allow emergency doubling of profitable legs when the basket drawdown reaches the threshold.
	/// </summary>
	public bool AllowEmergencyTrading
	{
		get => _allowEmergencyTrading.Value;
		set => _allowEmergencyTrading.Value = value;
	}

	/// <summary>
	/// Basket drawdown in pips that triggers emergency doubling.
	/// </summary>
	public int EmergencyLossPips
	{
		get => _emergencyLossPips.Value;
		set => _emergencyLossPips.Value = value;
	}

	/// <summary>
	/// Daily profit target in pips.
	/// </summary>
	public int ProfitTargetPips
	{
		get => _profitTargetPips.Value;
		set => _profitTargetPips.Value = value;
	}

	/// <summary>
	/// Daily loss limit in pips.
	/// </summary>
	public int LossLimitPips
	{
		get => _lossLimitPips.Value;
		set => _lossLimitPips.Value = value;
	}

	/// <summary>
	/// Volume used when opening each hedge leg.
	/// </summary>
	public decimal TradingVolume
	{
		get => _volumeParam.Value;
		set => _volumeParam.Value = value;
	}

	/// <summary>
	/// Second asset in the basket (defaults to USDCHF).
	/// </summary>
	public Security Symbol2
	{
		get => _symbol2Param.Value;
		set => _symbol2Param.Value = value;
	}

	/// <summary>
	/// Third asset in the basket (defaults to GBPUSD).
	/// </summary>
	public Security Symbol3
	{
		get => _symbol3Param.Value;
		set => _symbol3Param.Value = value;
	}

	/// <summary>
	/// Fourth asset in the basket (defaults to AUDUSD).
	/// </summary>
	public Security Symbol4
	{
		get => _symbol4Param.Value;
		set => _symbol4Param.Value = value;
	}

	/// <summary>
	/// Intraday candle type used for monitoring time of day and price updates.
	/// </summary>
	public DataType IntradayCandleType
	{
		get => _intradayCandleTypeParam.Value;
		set => _intradayCandleTypeParam.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public HthTraderStrategy()
	{
		_tradeEnabled = Param(nameof(TradeEnabled), true)
		.SetDisplay("Trade Enabled", "Allow the strategy to submit orders", "General");

		_showProfitInfo = Param(nameof(ShowProfitInfo), true)
		.SetDisplay("Show Profit Info", "Log basket profit in pips", "General");

		_useProfitTarget = Param(nameof(UseProfitTarget), false)
		.SetDisplay("Use Profit Target", "Close all trades when daily profit is reached", "Risk");

		_useLossLimit = Param(nameof(UseLossLimit), false)
		.SetDisplay("Use Loss Limit", "Close all trades when daily loss is reached", "Risk");

		_allowEmergencyTrading = Param(nameof(AllowEmergencyTrading), true)
		.SetDisplay("Emergency Trading", "Allow doubling of profitable legs on deep drawdown", "Risk");

		_emergencyLossPips = Param(nameof(EmergencyLossPips), 60)
		.SetDisplay("Emergency Loss (pips)", "Drawdown threshold for emergency doubling", "Risk");

		_profitTargetPips = Param(nameof(ProfitTargetPips), 80)
		.SetDisplay("Profit Target (pips)", "Daily basket profit target", "Risk");

		_lossLimitPips = Param(nameof(LossLimitPips), 40)
		.SetDisplay("Loss Limit (pips)", "Daily basket loss limit", "Risk");

		_volumeParam = Param(nameof(TradingVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume used for each leg", "Execution");

		_symbol2Param = Param<Security>(nameof(Symbol2))
		.SetDisplay("Symbol 2", "Second asset in the hedge basket", "Assets");

		_symbol3Param = Param<Security>(nameof(Symbol3))
		.SetDisplay("Symbol 3", "Third asset in the hedge basket", "Assets");

		_symbol4Param = Param<Security>(nameof(Symbol4))
		.SetDisplay("Symbol 4", "Fourth asset in the hedge basket", "Assets");

		_intradayCandleTypeParam = Param(nameof(IntradayCandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Intraday TF", "Timeframe for monitoring trading sessions", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var result = new List<(Security, DataType)>();

		void Add(Security security)
		{
			if (security == null)
			return;

			result.Add((security, IntradayCandleType));
			result.Add((security, _dailyCandleType));
		}

		Add(Security);
		Add(Symbol2);
		Add(Symbol3);
		Add(Symbol4);

		return result;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_states.Clear();
		_lastTradeDate = DateTime.MinValue;
		_emergencyReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		throw new InvalidOperationException("Primary security is not specified.");

		if (Symbol2 == null || Symbol3 == null || Symbol4 == null)
		throw new InvalidOperationException("All four securities must be assigned before starting the strategy.");

		Volume = TradingVolume;

		_states.Clear();

		var securities = new[] { Security, Symbol2, Symbol3, Symbol4 };

		foreach (var sec in securities)
		{
			var state = new InstrumentState(sec);
			_states[sec] = state;

			var current = sec;

			var intradaySubscription = current == Security
			? SubscribeCandles(IntradayCandleType)
			: SubscribeCandles(IntradayCandleType, security: current);

			if (current == Security)
			{
				intradaySubscription.Bind(ProcessPrimaryCandle).Start();
			}
			else
			{
				intradaySubscription.Bind(candle => ProcessAuxiliaryCandle(current, candle)).Start();
			}

			var dailySubscription = current == Security
			? SubscribeCandles(_dailyCandleType)
			: SubscribeCandles(_dailyCandleType, security: current);

			dailySubscription.Bind(candle => ProcessDailyCandle(current, candle)).Start();
		}

		_emergencyReady = AllowEmergencyTrading;
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var state = _states[Security];
		state.LastPrice = candle.ClosePrice;

		HandleBasketManagement();

		var timeOfDay = candle.OpenTime.TimeOfDay;

		if (timeOfDay.Hours >= 23)
		{
			ClosePositions();
			return;
		}

		if (HasOpenPositions())
		return;

		if (timeOfDay >= TimeSpan.FromMinutes(5) && timeOfDay <= TimeSpan.FromMinutes(12))
		{
			var tradeDate = candle.OpenTime.Date;
			if (tradeDate > _lastTradeDate)
			{
				_emergencyReady = AllowEmergencyTrading;
				OpenDailyPositions();
				_lastTradeDate = tradeDate;
			}
		}
	}

	private void ProcessAuxiliaryCandle(Security security, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_states.TryGetValue(security, out var state))
		state.LastPrice = candle.ClosePrice;

		HandleBasketManagement();
	}

	private void ProcessDailyCandle(Security security, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var state = _states[security];
		state.PreviousDailyClose2 = state.PreviousDailyClose1;
		state.PreviousDailyClose1 = candle.ClosePrice;
	}

	private void HandleBasketManagement()
	{
		if (!HasOpenPositions())
		return;

		var totalProfit = CalculateTotalProfitPips();

		if (ShowProfitInfo)
		LogInfo($"Basket profit: {totalProfit:F2} pips");

		if (AllowEmergencyTrading && _emergencyReady && totalProfit <= -EmergencyLossPips)
		{
			DoublePositions();
		}

		if (UseProfitTarget && totalProfit >= ProfitTargetPips)
		{
			ClosePositions();
		}

		if (UseLossLimit && totalProfit <= -LossLimitPips)
		{
			ClosePositions();
		}
	}

	private void OpenDailyPositions()
	{
		if (!TradeEnabled || !IsFormedAndOnlineAndAllowTrading())
		return;

		var primary = _states[Security];

		if (primary.PreviousDailyClose1 <= 0m || primary.PreviousDailyClose2 <= 0m)
		{
			LogWarning("Not enough daily data to evaluate deviation.");
			return;
		}

		var deviation = (100m * primary.PreviousDailyClose1 / primary.PreviousDailyClose2) - 100m;
		var volume = TradingVolume;

		if (volume <= 0m)
		return;

		if (deviation > 0m)
		{
			ExecuteTrade(Security, Sides.Buy, volume);
			ExecuteTrade(Symbol2, Sides.Buy, volume);
			ExecuteTrade(Symbol3, Sides.Sell, volume);
			ExecuteTrade(Symbol4, Sides.Buy, volume);
		}
		else if (deviation < 0m)
		{
			ExecuteTrade(Security, Sides.Sell, volume);
			ExecuteTrade(Symbol2, Sides.Sell, volume);
			ExecuteTrade(Symbol3, Sides.Buy, volume);
			ExecuteTrade(Symbol4, Sides.Sell, volume);
		}
	}

	private void ExecuteTrade(Security security, Sides side, decimal volume)
	{
		if (!_states.TryGetValue(security, out var state))
		return;

		if (state.LastPrice <= 0m)
		{
			LogWarning($"Last price for {security?.Id} is not available. Trade skipped.");
			return;
		}

		if (side == Sides.Buy)
		{
			if (security == Security)
			BuyMarket(volume);
			else
			BuyMarket(volume, security);
		}
		else
		{
			if (security == Security)
			SellMarket(volume);
			else
			SellMarket(volume, security);
		}

		state.ApplyTrade(volume, state.LastPrice, side);
	}

	private void ClosePositions()
	{
		foreach (var (security, state) in _states)
		{
			if (state.PositionVolume == 0m)
			continue;

			if (security == Security)
			ClosePosition();
			else
			ClosePosition(security);

			state.ResetPosition();
		}

		_emergencyReady = AllowEmergencyTrading;
	}

	private void DoublePositions()
	{
		foreach (var (security, state) in _states)
		{
			if (state.PositionVolume == 0m)
			continue;

			var profit = CalculateProfitInPips(state);
			if (profit <= 0m)
			continue;

			var volume = Math.Abs(state.PositionVolume);
			if (volume <= 0m || state.LastPrice <= 0m)
			continue;

			if (state.PositionVolume > 0m)
			{
				if (security == Security)
				BuyMarket(volume);
				else
				BuyMarket(volume, security);

				state.ApplyTrade(volume, state.LastPrice, Sides.Buy);
			}
			else
			{
				if (security == Security)
				SellMarket(volume);
				else
				SellMarket(volume, security);

				state.ApplyTrade(volume, state.LastPrice, Sides.Sell);
			}
		}

		_emergencyReady = false;
	}

	private decimal CalculateTotalProfitPips()
	{
		var total = 0m;

		foreach (var state in _states.Values)
		total += CalculateProfitInPips(state);

		return total;
	}

	private static decimal CalculateProfitInPips(InstrumentState state)
	{
		if (state.PositionVolume == 0m)
		return 0m;

		if (!state.PriceStep.HasValue || state.PriceStep.Value <= 0m)
		return 0m;

		if (state.LastPrice <= 0m || state.AveragePrice <= 0m)
		return 0m;

		var priceDiff = state.LastPrice - state.AveragePrice;
		var signed = state.PositionVolume > 0m ? priceDiff : -priceDiff;

		return signed / state.PriceStep.Value;
	}

	private bool HasOpenPositions()
	{
		foreach (var state in _states.Values)
		{
			if (state.PositionVolume != 0m)
			return true;
		}

		return false;
	}

	private sealed class InstrumentState
	{
		public InstrumentState(Security security)
		{
			Security = security;
			PriceStep = security?.PriceStep ?? security?.MinPriceStep;
		}

		public Security Security { get; }
		public decimal? PriceStep { get; }
		public decimal LastPrice { get; set; }
		public decimal PreviousDailyClose1 { get; set; }
		public decimal PreviousDailyClose2 { get; set; }
		public decimal PositionVolume { get; private set; }
		public decimal AveragePrice { get; private set; }

		public void ApplyTrade(decimal volume, decimal price, Sides side)
		{
			var signedVolume = side == Sides.Buy ? volume : -volume;
			var newVolume = PositionVolume + signedVolume;

			if (PositionVolume == 0m)
			{
				AveragePrice = price;
			}
			else if (Math.Sign(PositionVolume) == Math.Sign(signedVolume))
			{
				var existingAbs = Math.Abs(PositionVolume);
				AveragePrice = (AveragePrice * existingAbs + price * volume) / (existingAbs + volume);
			}
			else if (Math.Sign(newVolume) != Math.Sign(PositionVolume) && newVolume != 0m)
			{
				AveragePrice = price;
			}

			PositionVolume = newVolume;

			if (PositionVolume == 0m)
			AveragePrice = 0m;
		}

		public void ResetPosition()
		{
			PositionVolume = 0m;
			AveragePrice = 0m;
		}
	}
}
