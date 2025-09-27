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
/// Multi-currency hedge strategy converted from the MetaTrader expert advisor HTH.mq4.
/// Opens a four-leg basket once per day based on the sign of the previous daily deviation
/// and manages the basket using aggregated profit expressed in pips.
/// </summary>
public class HthStrategy : Strategy
{
	private readonly StrategyParam<bool> _tradeEnabled;
	private readonly StrategyParam<bool> _showProfitInfo;
	private readonly StrategyParam<bool> _useProfitTarget;
	private readonly StrategyParam<bool> _useLossLimit;
	private readonly StrategyParam<bool> _allowEmergencyTrading;
	private readonly StrategyParam<int> _emergencyLossPips;
	private readonly StrategyParam<int> _profitTargetPips;
	private readonly StrategyParam<int> _lossLimitPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<Security> _symbol2Param;
	private readonly StrategyParam<Security> _symbol3Param;
	private readonly StrategyParam<Security> _symbol4Param;
	private readonly StrategyParam<DataType> _intradayCandleTypeParam;

	private readonly DataType _dailyCandleType = TimeSpan.FromDays(1).TimeFrame();

	private readonly Dictionary<Security, InstrumentState> _states = new();

	private DateTime _lastEntryDate = DateTime.MinValue;
	private bool _emergencyArmed;
	private decimal _lastTotalProfitPips;

	/// <summary>
	/// Enable automated trading for the basket.
	/// </summary>
	public bool TradeEnabled
	{
		get => _tradeEnabled.Value;
		set => _tradeEnabled.Value = value;
	}

	/// <summary>
	/// Display basket profit in pips and enable risk management rules.
	/// </summary>
	public bool ShowProfitInfo
	{
		get => _showProfitInfo.Value;
		set => _showProfitInfo.Value = value;
	}

	/// <summary>
	/// Close the basket when the aggregated profit hits the target.
	/// </summary>
	public bool UseProfitTarget
	{
		get => _useProfitTarget.Value;
		set => _useProfitTarget.Value = value;
	}

	/// <summary>
	/// Close the basket when the aggregated loss reaches the threshold.
	/// </summary>
	public bool UseLossLimit
	{
		get => _useLossLimit.Value;
		set => _useLossLimit.Value = value;
	}

	/// <summary>
	/// Allow doubling profitable legs when the basket drawdown exceeds the emergency threshold.
	/// </summary>
	public bool AllowEmergencyTrading
	{
		get => _allowEmergencyTrading.Value;
		set => _allowEmergencyTrading.Value = value;
	}

	/// <summary>
	/// Drawdown in pips that arms the emergency doubling routine.
	/// </summary>
	public int EmergencyLossPips
	{
		get => _emergencyLossPips.Value;
		set => _emergencyLossPips.Value = value;
	}

	/// <summary>
	/// Target profit in pips for the complete basket.
	/// </summary>
	public int ProfitTargetPips
	{
		get => _profitTargetPips.Value;
		set => _profitTargetPips.Value = value;
	}

	/// <summary>
	/// Maximum acceptable loss in pips for the basket.
	/// </summary>
	public int LossLimitPips
	{
		get => _lossLimitPips.Value;
		set => _lossLimitPips.Value = value;
	}

	/// <summary>
	/// Volume used when opening each hedge leg.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Second instrument in the basket (defaults to USDCHF in the original script).
	/// </summary>
	public Security Symbol2
	{
		get => _symbol2Param.Value;
		set => _symbol2Param.Value = value;
	}

	/// <summary>
	/// Third instrument in the basket (defaults to GBPUSD in the original script).
	/// </summary>
	public Security Symbol3
	{
		get => _symbol3Param.Value;
		set => _symbol3Param.Value = value;
	}

	/// <summary>
	/// Fourth instrument in the basket (defaults to AUDUSD in the original script).
	/// </summary>
	public Security Symbol4
	{
		get => _symbol4Param.Value;
		set => _symbol4Param.Value = value;
	}

	/// <summary>
	/// Intraday candle type used to monitor time of day and current prices.
	/// </summary>
	public DataType IntradayCandleType
	{
		get => _intradayCandleTypeParam.Value;
		set => _intradayCandleTypeParam.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public HthStrategy()
	{
		_tradeEnabled = Param(nameof(TradeEnabled), true)
		.SetDisplay("Enable Trading", "Allow the strategy to submit the daily hedge basket.", "General");

		_showProfitInfo = Param(nameof(ShowProfitInfo), true)
		.SetDisplay("Show Profit Info", "Log basket profit in pips and activate management rules.", "General");

		_useProfitTarget = Param(nameof(UseProfitTarget), false)
		.SetDisplay("Use Profit Target", "Close all legs when total profit exceeds the target.", "Risk");

		_useLossLimit = Param(nameof(UseLossLimit), false)
		.SetDisplay("Use Loss Limit", "Close all legs when total loss exceeds the limit.", "Risk");

		_allowEmergencyTrading = Param(nameof(AllowEmergencyTrading), true)
		.SetDisplay("Emergency Trading", "Allow doubling profitable legs on deep drawdown.", "Risk");

		_emergencyLossPips = Param(nameof(EmergencyLossPips), 60)
		.SetGreaterThanZero()
		.SetDisplay("Emergency Loss (pips)", "Basket drawdown that triggers emergency doubling.", "Risk");

		_profitTargetPips = Param(nameof(ProfitTargetPips), 80)
		.SetGreaterThanZero()
		.SetDisplay("Profit Target (pips)", "Aggregated profit level that closes the basket.", "Risk");

		_lossLimitPips = Param(nameof(LossLimitPips), 40)
		.SetGreaterThanZero()
		.SetDisplay("Loss Limit (pips)", "Aggregated loss that closes the basket.", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Order size applied to each hedge leg.", "Execution");

		_symbol2Param = Param<Security>(nameof(Symbol2))
		.SetDisplay("Symbol 2", "Second currency pair in the basket.", "Instruments");

		_symbol3Param = Param<Security>(nameof(Symbol3))
		.SetDisplay("Symbol 3", "Third currency pair in the basket.", "Instruments");

		_symbol4Param = Param<Security>(nameof(Symbol4))
		.SetDisplay("Symbol 4", "Fourth currency pair in the basket.", "Instruments");

		_intradayCandleTypeParam = Param(nameof(IntradayCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Intraday TF", "Timeframe used to evaluate the trading window.", "General");
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
		_lastEntryDate = DateTime.MinValue;
		_emergencyArmed = false;
		_lastTotalProfitPips = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		throw new InvalidOperationException("Primary security must be assigned before starting the strategy.");

		if (Symbol2 == null || Symbol3 == null || Symbol4 == null)
		throw new InvalidOperationException("All four securities must be specified to reproduce the HTH basket.");

		Volume = TradeVolume;

		_states.Clear();

	var instruments = new[] { Security, Symbol2, Symbol3, Symbol4 };

		foreach (var instrument in instruments)
		{
			var state = new InstrumentState(instrument);
			_states[instrument] = state;

			var current = instrument;

			var intradaySubscription = current == Security
			? SubscribeCandles(IntradayCandleType)
			: SubscribeCandles(IntradayCandleType, security: current);

			if (current == Security)
			{
				intradaySubscription.Bind(ProcessPrimaryIntraday).Start();
			}
			else
			{
				intradaySubscription.Bind(candle => ProcessAuxiliaryIntraday(current, candle)).Start();
			}

			var dailySubscription = current == Security
			? SubscribeCandles(_dailyCandleType)
			: SubscribeCandles(_dailyCandleType, security: current);

			dailySubscription.Bind(candle => ProcessDailyCandle(current, candle)).Start();
		}

		_emergencyArmed = AllowEmergencyTrading;

		StartProtection();
	}

	private void ProcessPrimaryIntraday(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var state = _states[Security];
		state.LastPrice = candle.ClosePrice;

		HandleBasketManagement(true);
		LogDeviationSnapshot();

		var timeOfDay = candle.OpenTime.TimeOfDay;

		if (timeOfDay.Hours >= 23)
		{
			ClosePositions();
			return;
		}

		if (HasOpenPositions())
		return;

		var windowStart = TimeSpan.FromMinutes(5);
		var windowEnd = TimeSpan.FromMinutes(12);

		if (timeOfDay < windowStart || timeOfDay > windowEnd)
		return;

		var tradeDate = candle.OpenTime.Date;

		if (tradeDate <= _lastEntryDate)
		return;

		_emergencyArmed = AllowEmergencyTrading;
		OpenDailyBasket();
		_lastEntryDate = tradeDate;
	}

	private void ProcessAuxiliaryIntraday(Security security, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_states.TryGetValue(security, out var state))
		return;

		state.LastPrice = candle.ClosePrice;

		HandleBasketManagement(false);
	}

	private void ProcessDailyCandle(Security security, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var state = _states[security];
		state.PreviousDailyClose2 = state.PreviousDailyClose1;
		state.PreviousDailyClose1 = candle.ClosePrice;
	}

	private void OpenDailyBasket()
	{
		if (!TradeEnabled || !IsFormedAndOnlineAndAllowTrading())
		return;

		var primary = _states[Security];

		if (primary.PreviousDailyClose1 <= 0m || primary.PreviousDailyClose2 <= 0m)
		{
			LogWarning("Not enough daily data to evaluate the previous deviation. Entry skipped.");
			return;
		}

		var deviation = CalculateDeviation(primary.PreviousDailyClose1, primary.PreviousDailyClose2);
		var volume = TradeVolume;

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

	private void HandleBasketManagement(bool isPrimaryUpdate)
	{
		var totalProfit = CalculateTotalProfitPips();
		_lastTotalProfitPips = totalProfit;

		if (!isPrimaryUpdate)
		return;

		if (!ShowProfitInfo)
		return;

	LogInfo($"Basket profit: {totalProfit:F2} pips");

		if (AllowEmergencyTrading && _emergencyArmed && totalProfit <= -EmergencyLossPips)
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

		_emergencyArmed = false;
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

		if (state.PriceStep is not decimal step || step <= 0m)
		return 0m;

		if (state.LastPrice <= 0m || state.AveragePrice <= 0m)
		return 0m;

		var priceDiff = state.LastPrice - state.AveragePrice;
		var signedDiff = state.PositionVolume > 0m ? priceDiff : -priceDiff;

		return signedDiff / step;
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

	private void LogDeviationSnapshot()
	{
		var c1 = _states[Security];
		var c2 = _states[Symbol2];
		var c3 = _states[Symbol3];
		var c4 = _states[Symbol4];

		if (!HasDeviationData(c1) || !HasDeviationData(c2) || !HasDeviationData(c3) || !HasDeviationData(c4))
		return;

		var d1 = CalculateDeviation(c1.LastPrice, c1.PreviousDailyClose1);
		var d2 = CalculateDeviation(c2.LastPrice, c2.PreviousDailyClose1);
		var d3 = CalculateDeviation(c3.LastPrice, c3.PreviousDailyClose1);
		var d4 = CalculateDeviation(c4.LastPrice, c4.PreviousDailyClose1);

		var prev1 = CalculateDeviation(c1.PreviousDailyClose1, c1.PreviousDailyClose2);
		var prev2 = CalculateDeviation(c2.PreviousDailyClose1, c2.PreviousDailyClose2);
		var prev3 = CalculateDeviation(c3.PreviousDailyClose1, c3.PreviousDailyClose2);
		var prev4 = CalculateDeviation(c4.PreviousDailyClose1, c4.PreviousDailyClose2);

		var message = string.Join(Environment.NewLine, new[]
		{
			"Deviation snapshot:",
$"{Security?.Id} Deviation: {d1:F2}% | Previous Deviation: {prev1:F2}%",
$"{Symbol2?.Id} Deviation: {d2:F2}% | Previous Deviation: {prev2:F2}%",
$"{Symbol3?.Id} Deviation: {d3:F2}% | Previous Deviation: {prev3:F2}%",
$"{Symbol4?.Id} Deviation: {d4:F2}% | Previous Deviation: {prev4:F2}%",
			string.Empty,
$"{Security?.Id} / {Symbol2?.Id} Pair Deviation: {(d1 + d2):F2}%",
$"{Security?.Id} / {Symbol3?.Id} Pair Deviation: {(d1 - d3):F2}%",
$"{Security?.Id} / {Symbol4?.Id} Pair Deviation: {(d1 - d4):F2}%",
$"{Symbol2?.Id} / {Symbol3?.Id} Pair Deviation: {(d2 + d3):F2}%",
$"{Symbol3?.Id} / {Symbol4?.Id} Pair Deviation: {(d3 - d4):F2}%",
$"{Symbol2?.Id} / {Symbol4?.Id} Pair Deviation: {(d2 + d4):F2}%",
			string.Empty,
$"{Security?.Id}/{Symbol2?.Id} vs. {Symbol3?.Id}/{Symbol4?.Id} Pair Deviation: {(d1 + d2 + d3 - d4):F2}%",
				$"PIP Profit: {_lastTotalProfitPips:F2}"
				});

				LogInfo(message);
			}

			private static bool HasDeviationData(InstrumentState state)
			{
				return state.LastPrice > 0m && state.PreviousDailyClose1 > 0m && state.PreviousDailyClose2 > 0m;
			}

			private static decimal CalculateDeviation(decimal current, decimal reference)
			{
				if (current <= 0m || reference <= 0m)
				return 0m;

				return (100m * current / reference) - 100m;
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
