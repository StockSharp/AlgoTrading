namespace StockSharp.Samples.Strategies;

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

using System.Globalization;

/// <summary>
/// Dashboard strategy that mirrors the "Informative Dashboard" MetaTrader panel.
/// It refreshes the strategy comment with account statistics such as daily PnL,
/// percentage drawdown, number of active positions and orders, and the current spread.
/// </summary>
public class InformativeDashboardStrategy : Strategy
{
	private readonly StrategyParam<int> _refreshIntervalSeconds;

	private readonly HashSet<Order> _activeOrders = new();

	private DateTime _currentDay;
	private decimal _dailyPnLBase;
	private bool _hasOpenPosition;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private string _lastComment = string.Empty;

	/// <summary>
	/// Interval between forced dashboard updates, expressed in seconds.
	/// </summary>
	public int RefreshIntervalSeconds
	{
		get => _refreshIntervalSeconds.Value;
		set => _refreshIntervalSeconds.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="InformativeDashboardStrategy"/>.
	/// </summary>
	public InformativeDashboardStrategy()
	{
		_refreshIntervalSeconds = Param(nameof(RefreshIntervalSeconds), 30)
			.SetGreaterThanZero()
			.SetDisplay("Refresh Interval (seconds)", "Delay between dashboard refresh attempts", "Dashboard");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_activeOrders.Clear();
		_hasOpenPosition = false;
		_bestBid = null;
		_bestAsk = null;
		_lastComment = string.Empty;
		Comment = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentDay = time.Date;
		_dailyPnLBase = PnL;
		_hasOpenPosition = Position != 0m;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		if (RefreshIntervalSeconds > 0 && Security != null)
		{
			var candleType = TimeSpan.FromSeconds(RefreshIntervalSeconds).TimeFrame();
			SubscribeCandles(candleType)
				.Bind(OnTimerCandle)
				.Start();
		}
		else if (Security == null)
		{
			LogWarning("Security is not assigned. Timer-based refresh is disabled.");
		}

		UpdateDashboard();
	}

	private void OnTimerCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDashboard();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
		if (bid != null)
			_bestBid = bid.Value;

		var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);
		if (ask != null)
			_bestAsk = ask.Value;

		UpdateDashboard();
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order == null)
			return;

		switch (order.State)
		{
			case OrderStates.None:
			case OrderStates.Pending:
			case OrderStates.Active:
				_activeOrders.Add(order);
				break;
			default:
				_activeOrders.Remove(order);
				break;
		}

		UpdateDashboard();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		_hasOpenPosition = Position != 0m;

		UpdateDashboard();
	}

	/// <inheritdoc />
	protected override void OnPnLChanged(decimal diff)
	{
		base.OnPnLChanged(diff);

		UpdateDashboard();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		Comment = string.Empty;
	}

	private void UpdateDashboard()
	{
		var now = CurrentTime;
		if (now == default)
			now = DateTimeOffset.UtcNow;

		if (now.Date != _currentDay)
		{
			_currentDay = now.Date;
			_dailyPnLBase = PnL;
		}

		var portfolio = Portfolio;
		var balance = portfolio?.BeginValue ?? 0m;
		var equity = portfolio?.CurrentValue ?? balance;

		if (balance <= 0m && equity > 0m)
			balance = equity;

		var dailyPnL = PnL - _dailyPnLBase;

		var drawdownPercent = 0m;
		if (balance != 0m)
			drawdownPercent = (equity - balance) / balance * 100m;

		var openOrders = CountActiveOrders();
		var openPositions = _hasOpenPosition ? 1 : 0;
		var posAndOrders = openPositions + openOrders;

		var spreadText = GetSpreadText();
		var accountName = portfolio?.Name;
		if (accountName.IsEmptyOrWhiteSpace())
			accountName = "Unknown";

		var comment = string.Format(CultureInfo.InvariantCulture,
			"Account: {0} | Daily PL: {1:0.00} | Drawdown: {2:0.00}% | Pos & Orders: {3} | Spread: {4}",
			accountName,
			dailyPnL,
			drawdownPercent,
			posAndOrders,
			spreadText);

		if (comment == _lastComment)
			return;

		_lastComment = comment;
		Comment = comment;
	}

	private int CountActiveOrders()
	{
		var count = 0;
		foreach (var order in _activeOrders)
		{
			if (order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active)
				count++;
		}

		return count;
	}

	private string GetSpreadText()
	{
		if (_bestBid == null || _bestAsk == null)
			return "N/A";

		var spread = _bestAsk.Value - _bestBid.Value;
		if (spread < 0m)
			spread = 0m;

		return spread.ToString("0.#####", CultureInfo.InvariantCulture);
	}
}

