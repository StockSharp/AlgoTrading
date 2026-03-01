namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Dashboard strategy that mirrors the "Informative Dashboard" MetaTrader panel.
/// It refreshes the strategy comment with account statistics such as daily PnL,
/// percentage drawdown, number of active positions and orders, and the current spread.
/// </summary>
public class InformativeDashboardStrategy : Strategy
{
	private readonly StrategyParam<int> _refreshIntervalSeconds;

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

		_hasOpenPosition = false;
		_bestBid = null;
		_bestAsk = null;
		_lastComment = string.Empty;
		_lastComment = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentDay = time.Date;
		_dailyPnLBase = PnL;
		_hasOpenPosition = Position != 0m;

		// Subscribe to candles for periodic dashboard refresh.
		if (RefreshIntervalSeconds > 0 && Security != null)
		{
			var candleType = TimeSpan.FromSeconds(RefreshIntervalSeconds).TimeFrame();
			SubscribeCandles(candleType)
				.Bind(OnTimerCandle)
				.Start();
		}

		UpdateDashboard();
	}

	private void OnTimerCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_hasOpenPosition = Position != 0m;
		UpdateDashboard();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		_lastComment = string.Empty;
	}

	private void UpdateDashboard()
	{
		var now = CurrentTime;

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

		var openPositions = _hasOpenPosition ? 1 : 0;

		var spreadText = GetSpreadText();
		var accountName = portfolio?.Name;
		if (accountName.IsEmptyOrWhiteSpace())
			accountName = "Unknown";

		var comment = string.Format(CultureInfo.InvariantCulture,
			"Account: {0} | Daily PL: {1:0.00} | Drawdown: {2:0.00}% | Pos: {3} | Spread: {4}",
			accountName,
			dailyPnL,
			drawdownPercent,
			openPositions,
			spreadText);

		if (comment == _lastComment)
			return;

		_lastComment = comment;
		LogInfo(comment);
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
