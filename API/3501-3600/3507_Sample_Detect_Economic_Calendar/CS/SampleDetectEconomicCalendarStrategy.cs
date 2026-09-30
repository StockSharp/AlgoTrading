namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.MatchingEngine;
using StockSharp.Messages;

/// <summary>
/// Economic-calendar breakout strategy driven by manually supplied events and Level1 quotes.
/// </summary>
public class SampleDetectEconomicCalendarStrategy : Strategy
{
	private readonly StrategyParam<bool> _tradeNews;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<int> _expiryMinutes;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _buyDistancePoints;
	private readonly StrategyParam<int> _sellDistancePoints;
	private readonly StrategyParam<int> _leadMinutes;
	private readonly StrategyParam<int> _postMinutes;
	private readonly StrategyParam<string> _baseCurrency;
	private readonly StrategyParam<string> _calendarDefinition;

	private readonly List<CalendarEvent> _events = [];
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private CalendarEvent _armedEvent;
	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _winner;
	private Order _exitOrder;
	private bool _cancelPair;
	private readonly HashSet<long> _cancelRequested = [];
	private ITimerHandler _pendingTimer;
	private decimal _filledPosition;
	private decimal _entryValue;
	private decimal? _trailingPrice;

	public bool TradeNews { get => _tradeNews.Value; set => _tradeNews.Value = value; }
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public int TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public int TrailingStopPoints { get => _trailingStopPoints.Value; set => _trailingStopPoints.Value = value; }
	public int ExpiryMinutes { get => _expiryMinutes.Value; set => _expiryMinutes.Value = value; }
	public bool UseMoneyManagement { get => _useMoneyManagement.Value; set => _useMoneyManagement.Value = value; }
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }
	public int BuyDistancePoints { get => _buyDistancePoints.Value; set => _buyDistancePoints.Value = value; }
	public int SellDistancePoints { get => _sellDistancePoints.Value; set => _sellDistancePoints.Value = value; }
	public int LeadMinutes { get => _leadMinutes.Value; set => _leadMinutes.Value = value; }
	public int PostMinutes { get => _postMinutes.Value; set => _postMinutes.Value = value; }
	public string BaseCurrency { get => _baseCurrency.Value; set => _baseCurrency.Value = value; }
	public string CalendarDefinition { get => _calendarDefinition.Value; set => _calendarDefinition.Value = value; }

	public SampleDetectEconomicCalendarStrategy()
	{
		_tradeNews = Param(nameof(TradeNews), true);
		_orderVolume = Param(nameof(OrderVolume), 0.1m).SetGreaterThanZero();
		_stopLossPoints = Param(nameof(StopLossPoints), 100).SetNotNegative();
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200).SetNotNegative();
		_trailingStopPoints = Param(nameof(TrailingStopPoints), 50).SetNotNegative();
		_expiryMinutes = Param(nameof(ExpiryMinutes), 120).SetNotNegative();
		_useMoneyManagement = Param(nameof(UseMoneyManagement), false);
		_riskPercent = Param(nameof(RiskPercent), 1m).SetGreaterThanZero();
		_buyDistancePoints = Param(nameof(BuyDistancePoints), 10).SetGreaterThanZero();
		_sellDistancePoints = Param(nameof(SellDistancePoints), 10).SetGreaterThanZero();
		_leadMinutes = Param(nameof(LeadMinutes), 15).SetNotNegative();
		_postMinutes = Param(nameof(PostMinutes), 30).SetNotNegative();
		_baseCurrency = Param(nameof(BaseCurrency), "USD");
		_calendarDefinition = Param(nameof(CalendarDefinition), string.Empty);
		Trades.TradeAdded += ProcessTrade;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1), (Security, DataType.Ticks)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_events.Clear();
		_bestBid = null;
		_bestAsk = null;
		_pendingTimer?.Dispose();
		_pendingTimer = null;
		_armedEvent = null;
		_buyStopOrder = _sellStopOrder = _winner = _exitOrder = null;
		_cancelPair = false;
		_cancelRequested.Clear();
		_filledPosition = _entryValue = 0m;
		_trailingPrice = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (Security?.PriceStep is not decimal point || point <= 0m)
			throw new InvalidOperationException("A positive security price step is required for news stops.");
		ParseCalendar();
		// Native stop matching needs real trades, not synthetic OHLC prints or bid/ask-only messages.
		Subscribe(new Subscription(DataType.Ticks, Security));
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ProcessLevel1).Start();
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
			_bestBid = bid;
		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			_bestAsk = ask;

		var now = message.ServerTime;
		ProcessPending(now);
		if (_bestBid is null || _bestAsk is null)
			return;

		if (Position != 0m)
		{
			ManageOpenPosition();
			return;
		}

		if (TradeNews && _armedEvent is null && _exitOrder is not { State: OrderStates.Active or OrderStates.Pending }
			&& IsFormedAndOnlineAndAllowTrading())
			TryArm(now);
	}

	private void TryArm(DateTime now)
	{
		var evt = _events
			.Where(e => !e.Consumed &&
				e.Currency.EqualsIgnoreCase(BaseCurrency) &&
				e.HighImpact &&
				now >= e.Time.AddMinutes(-LeadMinutes) &&
				now < e.Time.AddMinutes(PostMinutes) &&
				(ExpiryMinutes == 0 || now < e.Time.AddMinutes(ExpiryMinutes)))
			.OrderBy(e => e.Time)
			.FirstOrDefault();

		if (evt is null)
			return;

		var step = GetPoint();
		var volume = CalculateVolume();
		if (volume <= 0m)
			throw new InvalidOperationException("The configured volume limits admit no positive news order volume.");
		_armedEvent = evt;
		evt.Consumed = true;
		_cancelPair = false;
		_winner = null;
		_cancelRequested.Clear();
		_buyStopOrder = CreateStop(Sides.Buy, _bestAsk.Value + BuyDistancePoints * step, volume);
		_sellStopOrder = CreateStop(Sides.Sell, _bestBid.Value - SellDistancePoints * step, volume);
		_pendingTimer = StartTimer(TimeSpan.FromSeconds(1), () => ProcessPending(CurrentTime));
		RegisterOrder(_buyStopOrder);
		RegisterOrder(_sellStopOrder);
	}

	private Order CreateStop(Sides side, decimal activation, decimal volume)
		=> new()
		{
			Security = Security,
			Portfolio = Portfolio,
			Side = side,
			Volume = volume,
			Type = OrderTypes.Conditional,
			Condition = new StopOrderCondition { ActivationPrice = activation },
			Comment = "Calendar " + _armedEvent.Title,
		};

	private bool IsExpired(DateTime now)
		=> now >= _armedEvent.Time.AddMinutes(PostMinutes)
			|| (ExpiryMinutes > 0 && now >= _armedEvent.Time.AddMinutes(ExpiryMinutes));

	private void ProcessPending(DateTime now)
	{
		if (_armedEvent is null)
			return;
		_cancelPair |= !TradeNews || IsExpired(now)
			|| _buyStopOrder.State == OrderStates.Failed || _sellStopOrder.State == OrderStates.Failed;
		if (_cancelPair || (_winner is not null && _winner != _buyStopOrder)) RequestCancel(_buyStopOrder);
		if (_cancelPair || (_winner is not null && _winner != _sellStopOrder)) RequestCancel(_sellStopOrder);
		// Keep both references until cancellation/execution is acknowledged, including delayed registrations.
		if (IsTerminal(_buyStopOrder) && IsTerminal(_sellStopOrder))
		{
			_armedEvent = null;
			_buyStopOrder = _sellStopOrder = _winner = null;
			_pendingTimer?.Dispose();
			_pendingTimer = null;
		}
	}

	private static bool IsTerminal(Order order) => order.State is OrderStates.Done or OrderStates.Failed;

	protected override void OnOrderRegistered(Order order)
	{
		base.OnOrderRegistered(order);
		if (_armedEvent is not null && (order == _buyStopOrder || order == _sellStopOrder))
			ProcessPending(CurrentTime);
	}

	private void RequestCancel(Order order)
	{
		if (order.State == OrderStates.Active && _cancelRequested.Add(order.TransactionId))
			CancelOrder(order);
	}

	private void ProcessTrade(MyTrade trade)
	{
		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;
		var signed = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previous = _filledPosition;
		if (previous == 0m || Math.Sign(previous) == Math.Sign(signed))
		{
			_entryValue += price * volume;
			_filledPosition += signed;
		}
		else
		{
			_entryValue -= _entryValue / Math.Abs(previous) * Math.Min(volume, Math.Abs(previous));
			_filledPosition += signed;
			if (_filledPosition != 0m && Math.Sign(_filledPosition) != Math.Sign(previous))
				_entryValue = price * Math.Abs(_filledPosition);
		}
		if (_filledPosition == 0m) _entryValue = 0m;
		if (Math.Sign(previous) != Math.Sign(_filledPosition)) _trailingPrice = null;
		if (trade.Order == _buyStopOrder || trade.Order == _sellStopOrder)
		{
			_winner ??= trade.Order;
			RequestCancel(trade.Order == _buyStopOrder ? _sellStopOrder : _buyStopOrder);
		}
	}

	private void ManageOpenPosition()
	{
		if (_exitOrder is { State: OrderStates.Pending or OrderStates.Active } || _filledPosition == 0m)
			return;
		var longPosition = Position > 0m;
		var direction = longPosition ? 1m : -1m;
		var price = longPosition ? _bestBid.Value : _bestAsk.Value;
		var entry = _entryValue / Math.Abs(_filledPosition);
		var point = GetPoint();
		decimal? stop = StopLossPoints > 0 ? entry - direction * StopLossPoints * point : null;
		if (TrailingStopPoints > 0 && direction * (price - entry) >= TrailingStopPoints * point)
		{
			var candidate = price - direction * TrailingStopPoints * point;
			if (_trailingPrice is null || direction * (candidate - _trailingPrice.Value) > 0m)
				_trailingPrice = candidate;
		}
		if (_trailingPrice is decimal trailing && (stop is null || direction * (trailing - stop.Value) > 0m))
			stop = trailing;
		var stopHit = stop is decimal level && direction * (price - level) <= 0m;
		var takeHit = TakeProfitPoints > 0 && direction * (price - entry) >= TakeProfitPoints * point;
		if (stopHit || takeHit)
		{
			_cancelPair = true;
			ProcessPending(CurrentTime);
			_exitOrder = longPosition ? SellMarket(Math.Abs(Position)) : BuyMarket(Math.Abs(Position));
		}
	}

	private decimal CalculateVolume()
	{
		if (!UseMoneyManagement || StopLossPoints <= 0)
			return NormalizeVolume(OrderVolume);

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (balance <= 0m)
			return NormalizeVolume(OrderVolume);

		var riskMoney = balance * RiskPercent / 100m;
		var stepPrice = Security?.StepPrice ?? 0m;
		var point = GetPoint();
		var lossPerUnit = stepPrice > 0m
			? StopLossPoints * stepPrice
			: StopLossPoints * point * (Security?.Multiplier ?? 1m);

		if (lossPerUnit <= 0m)
			return NormalizeVolume(OrderVolume);

		return NormalizeVolume(riskMoney / lossPerUnit);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var minimum = Math.Max(0m, Security.MinVolume ?? 0m);
		var maximum = Security.MaxVolume is decimal max && max > 0m ? max : decimal.MaxValue;
		if (Security?.VolumeStep is decimal step && step > 0m)
		{
			minimum = Math.Ceiling(minimum / step) * step;
			if (maximum != decimal.MaxValue) maximum = Math.Floor(maximum / step) * step;
			volume = Math.Min(volume, maximum);
			volume = Math.Floor(volume / step) * step;
		}
		if (minimum > maximum)
			throw new InvalidOperationException("The security volume grid has no quantity within its min/max limits.");
		return Math.Max(minimum, Math.Min(volume, maximum));
	}

	private decimal GetPoint()
		=> Security.PriceStep.Value;

	private void ParseCalendar()
	{
		_events.Clear();
		var formats = new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm", "yyyy/MM/dd HH:mm:ss", "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss" };

		foreach (var raw in (CalendarDefinition ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = raw.Split(';');
			if (parts.Length < 4)
				continue;

			if (!DateTime.TryParseExact(parts[0].Trim(), formats, CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var when))
				continue;

			var importance = parts[2].Trim();
			_events.Add(new CalendarEvent
			{
				Time = when,
				Currency = parts[1].Trim(),
				HighImpact = importance.EqualsIgnoreCase("High"),
				Title = string.Join(";", parts.Skip(3)).Trim(),
			});
		}
	}

	private sealed class CalendarEvent
	{
		public DateTime Time { get; init; }
		public string Currency { get; init; }
		public bool HighImpact { get; init; }
		public string Title { get; init; }
		public bool Consumed { get; set; }
	}
}
