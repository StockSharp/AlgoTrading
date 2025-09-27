namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Utility strategy that mirrors the "Close All MT5" expert advisor.
/// It automatically monitors open positions and closes them once the
/// configured profit target (in money, points or pips) is reached.
/// In addition, a manual trigger emulates the chart button from the
/// original script so operators can instantly flatten exposure.
/// </summary>
public class CloseAllMt5Strategy : Strategy
{
	private readonly StrategyParam<PositionSelection> _positionSelection;
	private readonly StrategyParam<ProfitMeasure> _profitMeasure;
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<CloseRequestMode> _closeMode;
	private readonly StrategyParam<string> _commentFilter;
	private readonly StrategyParam<string> _currencyFilter;
	private readonly StrategyParam<long> _magicNumber;
	private readonly StrategyParam<long> _ticketNumber;
	private readonly StrategyParam<int> _maxSlippage;
	private readonly StrategyParam<bool> _manualTrigger;

	private readonly Dictionary<Security, decimal> _lastPrices = new();
	private readonly HashSet<Security> _subscriptions = new();

	/// <summary>
	/// Determines which position sides participate in the automatic profit checks.
	/// </summary>
	public PositionSelection PositionFilter
	{
		get => _positionSelection.Value;
		set => _positionSelection.Value = value;
	}

	/// <summary>
	/// Measurement unit used for the floating profit comparison.
	/// </summary>
	public ProfitMeasure ProfitMode
	{
		get => _profitMeasure.Value;
		set => _profitMeasure.Value = value;
	}

	/// <summary>
	/// Target value expressed in the selected <see cref="ProfitMode"/>.
	/// Positive numbers represent a take-profit while negative numbers create a stop-loss.
	/// </summary>
	public decimal ProfitThreshold
	{
		get => _profitThreshold.Value;
		set => _profitThreshold.Value = value;
	}

	/// <summary>
	/// Manual closing behaviour that emulates the button from the MQL version.
	/// </summary>
	public CloseRequestMode CloseMode
	{
		get => _closeMode.Value;
		set => _closeMode.Value = value;
	}

	/// <summary>
	/// Optional substring that must be present inside the originating strategy identifier.
	/// </summary>
	public string CommentFilter
	{
		get => _commentFilter.Value;
		set => _commentFilter.Value = value;
	}

	/// <summary>
	/// Symbol identifier applied when <see cref="CloseMode"/> equals <see cref="CloseRequestMode.CloseCurrency"/>.
	/// Leave empty to use the current strategy security.
	/// </summary>
	public string CurrencyFilter
	{
		get => _currencyFilter.Value;
		set => _currencyFilter.Value = value;
	}

	/// <summary>
	/// Strategy identifier (Magic number) used by <see cref="CloseRequestMode.CloseMagic"/>.
	/// </summary>
	public long MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>
	/// Specific position identifier targeted by <see cref="CloseRequestMode.CloseTicket"/>.
	/// </summary>
	public long TicketNumber
	{
		get => _ticketNumber.Value;
		set => _ticketNumber.Value = value;
	}

	/// <summary>
	/// Maximum permitted slippage in price steps when closing positions.
	/// Currently reserved for future improvements.
	/// </summary>
	public int MaxSlippage
	{
		get => _maxSlippage.Value;
		set => _maxSlippage.Value = value;
	}

	/// <summary>
	/// Manual trigger that executes the close routine once toggled to <c>true</c>.
	/// The flag resets automatically after processing the request.
	/// </summary>
	public bool TriggerClose
	{
		get => _manualTrigger.Value;
		set
		{
			if (_manualTrigger.Value == value)
				return;

			_manualTrigger.Value = value;

			if (value)
			{
				ExecuteManualClose();
				_manualTrigger.Value = false;
			}
		}
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CloseAllMt5Strategy()
	{
		_positionSelection = Param(nameof(PositionFilter), PositionSelection.Buy)
			.SetDisplay("Position Filter", "Positions included in automatic profit checks", "General");

		_profitMeasure = Param(nameof(ProfitMode), ProfitMeasure.Money)
			.SetDisplay("Profit Mode", "Unit used to measure floating profit", "General");

		_profitThreshold = Param(nameof(ProfitThreshold), 3m)
			.SetDisplay("Profit Threshold", "Target profit or loss that triggers closing", "General")
			.SetCanOptimize(true)
			.SetOptimize(-20m, 20m, 1m);

		_closeMode = Param(nameof(CloseMode), CloseRequestMode.CloseAll)
			.SetDisplay("Close Mode", "Manual close behaviour", "Controls");

		_commentFilter = Param(nameof(CommentFilter), "Bonnitta EA")
			.SetDisplay("Comment Filter", "Substring required inside the originating strategy identifier", "Filters");

		_currencyFilter = Param(nameof(CurrencyFilter), string.Empty)
			.SetDisplay("Currency Filter", "Symbol identifier used by the CloseCurrency mode", "Filters");

		_magicNumber = Param(nameof(MagicNumber), 1L)
			.SetDisplay("Magic Number", "Identifier used by the CloseMagic mode", "Filters");

		_ticketNumber = Param(nameof(TicketNumber), 1L)
			.SetDisplay("Ticket Number", "Position identifier used by the CloseTicket mode", "Filters");

		_maxSlippage = Param(nameof(MaxSlippage), 10)
			.SetDisplay("Max Slippage", "Maximum acceptable slippage in price steps", "Orders");

		_manualTrigger = Param(nameof(TriggerClose), false)
			.SetDisplay("Trigger Close", "Set to true to execute the manual close routine", "Controls");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var portfolio = Portfolio;
		if (portfolio != null)
		{
			foreach (var position in portfolio.Positions)
			{
				if (position.Security != null)
					yield return (position.Security, DataType.Ticks);
			}
		}

		if (Security != null)
			yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		InitializeSubscriptions();
		EvaluateAllPositions();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var orderSecurity = trade.Order.Security;
		if (orderSecurity != null)
			EnsureSubscription(orderSecurity);

		EvaluateAllPositions();
	}

	private void InitializeSubscriptions()
	{
		if (Security != null)
			EnsureSubscription(Security);

		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		foreach (var position in portfolio.Positions)
		{
			if (position.Security != null)
				EnsureSubscription(position.Security);
		}
	}

	private void EnsureSubscription(Security security)
	{
		if (!_subscriptions.Add(security))
			return;

		var subscription = SubscribeTrades(security);
		subscription
		.Bind(trade => ProcessTrade(security, trade))
		.Start();
	}

	private void ProcessTrade(Security security, ExecutionMessage trade)
	{
		if (trade.TradePrice is decimal price)
			_lastPrices[security] = price;

		EvaluateAllPositions();
	}

	private void EvaluateAllPositions()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var toClose = new List<Position>();

		foreach (var position in portfolio.Positions)
		{
			if (!MatchesSelection(position))
				continue;

			if (!MatchesComment(position))
				continue;

			if (!ShouldCloseByProfit(position))
				continue;

			toClose.Add(position);
		}

		foreach (var position in toClose)
			ClosePosition(position);
	}

	private bool MatchesSelection(Position position)
	{
		var volume = position.CurrentValue ?? 0m;

		return PositionFilter switch
		{
			PositionSelection.Buy => volume > 0m,
			PositionSelection.Sell => volume < 0m,
			_ => volume != 0m,
		};
	}

	private bool MatchesComment(Position position)
	{
		var filter = CommentFilter;
		if (string.IsNullOrWhiteSpace(filter))
			return true;

		var strategyId = TryGetStrategyId(position);
		if (!string.IsNullOrEmpty(strategyId) && strategyId.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
			return true;

		return false;
	}

	private bool ShouldCloseByProfit(Position position)
	{
		var threshold = ProfitThreshold;
		if (threshold == 0m)
			return false;

		if (!TryGetMetricValue(position, out var metric))
			return false;

		return threshold > 0m ? metric >= threshold : metric <= threshold;
	}

	private bool TryGetMetricValue(Position position, out decimal metric)
	{
		metric = 0m;

		var volume = position.CurrentValue ?? 0m;
		if (volume == 0m)
			return false;

		var security = position.Security;
		if (security == null)
			return false;

		var averagePrice = position.AveragePrice;

		if (!_lastPrices.TryGetValue(security, out var lastPrice) || lastPrice <= 0m)
		{
			var fallback = security.LastTick?.Price ?? security.LastPrice;
			if (fallback == null || fallback <= 0m)
				return false;

			lastPrice = fallback.Value;
			_lastPrices[security] = lastPrice;
		}

		var priceDiff = volume > 0m ? lastPrice - averagePrice : averagePrice - lastPrice;
		var absVolume = Math.Abs(volume);

		switch (ProfitMode)
		{
			case ProfitMeasure.Money:
			{
				var pnl = position.PnL;
				if (pnl.HasValue)
				{
					metric = pnl.Value;
					return true;
				}

				var step = security.PriceStep ?? 0m;
				var cost = security.PriceStepCost ?? 0m;

				if (step <= 0m || cost <= 0m)
				{
					metric = priceDiff * absVolume;
					return true;
				}

				metric = priceDiff / step * cost * absVolume;
				return true;
			}

			case ProfitMeasure.Points:
			{
				var step = security.PriceStep ?? 0m;
				if (step <= 0m)
					return false;

				metric = priceDiff / step;
				return true;
			}

			case ProfitMeasure.Pips:
			{
				var pipSize = GetPipSize(security);
				if (pipSize <= 0m)
					return false;

				metric = priceDiff / pipSize;
				return true;
			}

			default:
				return false;
		}
	}

	private static decimal GetPipSize(Security security)
	{
		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var decimals = security.Decimals;
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private void ClosePosition(Position position)
	{
		var security = position.Security;
		if (security == null)
			return;

		var volume = position.CurrentValue ?? 0m;
		if (volume > 0m)
		{
			SellMarket(volume, security);
		}
		else if (volume < 0m)
		{
			BuyMarket(-volume, security);
		}
	}

	private void ExecuteManualClose()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var positions = portfolio.Positions.ToArray();

		foreach (var position in positions)
		{
			if (!MatchesComment(position))
				continue;

			if (!MatchesManualMode(position))
				continue;

			ClosePosition(position);
		}

		if (CloseMode == CloseRequestMode.CloseAllAndPending)
		{
			var securities = positions
			.Select(p => p.Security)
			.Where(s => s != null)
			.Distinct()!
			.ToArray();

			foreach (var security in securities)
				CancelOrdersForSecurity(security!);
		}
	}

	private bool MatchesManualMode(Position position)
	{
		var volume = position.CurrentValue ?? 0m;
		var security = position.Security;

		switch (CloseMode)
		{
			case CloseRequestMode.CloseAll:
			case CloseRequestMode.CloseAllAndPending:
				return true;

			case CloseRequestMode.CloseBuy:
				return volume > 0m;

			case CloseRequestMode.CloseSell:
				return volume < 0m;

			case CloseRequestMode.CloseCurrency:
			{
				var filter = CurrencyFilter;
				if (string.IsNullOrWhiteSpace(filter))
					filter = Security?.Id ?? string.Empty;

				if (string.IsNullOrWhiteSpace(filter))
					return true;

				var symbol = security?.Id;
				return symbol != null && string.Equals(symbol, filter, StringComparison.OrdinalIgnoreCase);
			}

			case CloseRequestMode.CloseMagic:
			{
				if (MagicNumber == 0L)
					return false;

				var strategyId = TryGetStrategyId(position);
				if (string.IsNullOrEmpty(strategyId))
					return false;

				var magic = MagicNumber.ToString(CultureInfo.InvariantCulture);
				return string.Equals(strategyId, magic, StringComparison.OrdinalIgnoreCase);
			}

			case CloseRequestMode.CloseTicket:
			{
				if (TicketNumber == 0L)
					return false;

				var positionId = position.Id?.ToString();
				if (string.IsNullOrEmpty(positionId))
					return false;

				var ticket = TicketNumber.ToString(CultureInfo.InvariantCulture);
				return string.Equals(positionId, ticket, StringComparison.OrdinalIgnoreCase);
			}

			default:
				return false;
		}
	}

	private void CancelOrdersForSecurity(Security security)
	{
		var filter = CommentFilter;

		foreach (var order in Orders)
		{
			if (order.State != OrderStates.Active)
				continue;

			if (!Equals(order.Security, security))
				continue;

			if (!string.IsNullOrWhiteSpace(filter))
			{
				var comment = order.Comment ?? string.Empty;
				if (comment.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
					continue;
			}

			CancelOrder(order);
		}
	}

	private static string TryGetStrategyId(Position position)
	{
		var value = position.StrategyId;
		return value?.ToString();
	}

	/// <summary>
	/// Position side filter from the MQL version.
	/// </summary>
	public enum PositionSelection
	{
		/// <summary>Process only long positions.</summary>
		Buy = 0,
		/// <summary>Process only short positions.</summary>
		Sell = 1,
		/// <summary>Process both long and short positions.</summary>
		BuyAndSell = 2
	}

	/// <summary>
	/// Profit evaluation unit.
	/// </summary>
	public enum ProfitMeasure
	{
		/// <summary>Use monetary profit reported by the portfolio.</summary>
		Money = 0,
		/// <summary>Use price steps (points) difference between entry and current price.</summary>
		Points = 1,
		/// <summary>Use pip distance (ten price steps on fractional symbols).</summary>
		Pips = 2
	}

	/// <summary>
	/// Manual closing mode emulating the button from the original script.
	/// </summary>
	public enum CloseRequestMode
	{
		/// <summary>Close every matching position.</summary>
		CloseAll = 0,
		/// <summary>Close positions and cancel pending orders.</summary>
		CloseAllAndPending = 1,
		/// <summary>Close only long positions.</summary>
		CloseBuy = 2,
		/// <summary>Close only short positions.</summary>
		CloseSell = 3,
		/// <summary>Close positions belonging to a specific symbol.</summary>
		CloseCurrency = 4,
		/// <summary>Close positions opened by a specific strategy identifier.</summary>
		CloseMagic = 5,
		/// <summary>Close a single position by its identifier.</summary>
		CloseTicket = 6
	}
}