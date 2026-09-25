namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
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
	private DateTime? _armedAt;
	private decimal? _buyStop;
	private decimal? _sellStop;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _bestPrice;

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
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_events.Clear();
		_bestBid = null;
		_bestAsk = null;
		ClearPending();
		ResetProtection();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ParseCalendar();
		SubscribeLevel1().Bind(ProcessLevel1).Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
			_bestBid = bid;
		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			_bestAsk = ask;

		if (_bestBid is null || _bestAsk is null)
			return;

		var now = message.ServerTime;

		if (Position != 0m)
		{
			ManageOpenPosition();
			return;
		}

		if (!TradeNews)
		{
			ClearPending();
			return;
		}

		if (_armedEvent is not null && IsExpired(now))
			ClearPending();

		if (_armedEvent is null)
			TryArm(now);

		if (_armedEvent is null)
			return;

		if (_buyStop is decimal buyStop && _bestAsk.Value >= buyStop)
		{
			Execute(Sides.Buy, _bestAsk.Value);
			return;
		}

		if (_sellStop is decimal sellStop && _bestBid.Value <= sellStop)
			Execute(Sides.Sell, _bestBid.Value);
	}

	private void TryArm(DateTime now)
	{
		var evt = _events
			.Where(e => !e.Consumed &&
				e.Currency.EqualsIgnoreCase(BaseCurrency) &&
				e.HighImpact &&
				now >= e.Time.AddMinutes(-LeadMinutes) &&
				now <= e.Time.AddMinutes(PostMinutes))
			.OrderBy(e => e.Time)
			.FirstOrDefault();

		if (evt is null)
			return;

		var step = GetPoint();
		_armedEvent = evt;
		_armedAt = now;
		_buyStop = _bestAsk.Value + BuyDistancePoints * step;
		_sellStop = _bestBid.Value - SellDistancePoints * step;
	}

	private bool IsExpired(DateTime now)
	{
		if (_armedEvent is null || _armedAt is null)
			return true;

		var postExpired = PostMinutes >= 0 && now > _armedEvent.Time.AddMinutes(PostMinutes);
		var lifetimeExpired = ExpiryMinutes > 0 && now > _armedAt.Value.AddMinutes(ExpiryMinutes);
		return postExpired || lifetimeExpired;
	}

	private void Execute(Sides side, decimal price)
	{
		var volume = CalculateVolume();

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_armedEvent.Consumed = true;
		ClearPending(keepConsumed: true);

		var point = GetPoint();
		_entryPrice = price;
		_bestPrice = price;
		_stopPrice = StopLossPoints > 0
			? side == Sides.Buy ? price - StopLossPoints * point : price + StopLossPoints * point
			: null;
		_takePrice = TakeProfitPoints > 0
			? side == Sides.Buy ? price + TakeProfitPoints * point : price - TakeProfitPoints * point
			: null;
	}

	private void ManageOpenPosition()
	{
		var point = GetPoint();

		if (Position > 0m)
		{
			var price = _bestBid.Value;
			_bestPrice = _bestPrice is decimal best ? Math.Max(best, price) : price;

			if (TrailingStopPoints > 0 && price - _entryPrice >= TrailingStopPoints * point)
			{
				var candidate = price - TrailingStopPoints * point;
				if (_stopPrice is null || candidate > _stopPrice)
					_stopPrice = candidate;
			}

			if ((_stopPrice is decimal stop && price <= stop) ||
				(_takePrice is decimal take && price >= take))
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
			}
		}
		else
		{
			var price = _bestAsk.Value;
			_bestPrice = _bestPrice is decimal best ? Math.Min(best, price) : price;

			if (TrailingStopPoints > 0 && _entryPrice - price >= TrailingStopPoints * point)
			{
				var candidate = price + TrailingStopPoints * point;
				if (_stopPrice is null || candidate < _stopPrice)
					_stopPrice = candidate;
			}

			if ((_stopPrice is decimal stop && price >= stop) ||
				(_takePrice is decimal take && price <= take))
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
			}
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
		if (Security?.MaxVolume is decimal max && max > 0m)
			volume = Math.Min(volume, max);
		if (Security?.MinVolume is decimal min && min > 0m)
			volume = Math.Max(volume, min);
		if (Security?.VolumeStep is decimal step && step > 0m)
			volume = Math.Floor(volume / step) * step;
		return volume > 0m ? volume : OrderVolume;
	}

	private decimal GetPoint()
	{
		var point = Security?.PriceStep ?? 1m;
		return point > 0m ? point : 1m;
	}

	private void ParseCalendar()
	{
		_events.Clear();
		var formats = new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm", "dd.MM.yyyy HH:mm" };

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
				HighImpact = importance.EqualsIgnoreCase("High") || importance.EqualsIgnoreCase("Nfp"),
				Title = string.Join(";", parts.Skip(3)).Trim(),
			});
		}
	}

	private void ClearPending(bool keepConsumed = false)
	{
		if (!keepConsumed && _armedEvent is not null)
			_armedEvent.Consumed = true;

		_armedEvent = null;
		_armedAt = null;
		_buyStop = null;
		_sellStop = null;
	}

	private void ResetProtection()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_bestPrice = null;
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
