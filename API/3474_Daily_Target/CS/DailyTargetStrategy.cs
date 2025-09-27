using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monitors the accumulated profit and loss for the current day and stops trading when the configured
/// daily target or loss threshold is reached.
/// </summary>
public class DailyTargetStrategy : Strategy
{
	private readonly StrategyParam<decimal> _dailyTarget;
	private readonly StrategyParam<decimal> _dailyMaxLoss;

	private DateTime _currentDate;
	private decimal _dailyPnLBase;
	private bool _dailyStopTriggered;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _lastTradePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="DailyTargetStrategy"/> class with defaults matching the MetaTrader script.
	/// </summary>
	public DailyTargetStrategy()
	{
		_dailyTarget = Param(nameof(DailyTarget), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Daily Target", "Net profit that stops trading for the rest of the day", "Risk Management")
			.SetCanOptimize(true);

		_dailyMaxLoss = Param(nameof(DailyMaxLoss), 0m)
			.SetNotNegative()
			.SetDisplay("Daily Max Loss", "Maximum drawdown tolerated before trading is halted", "Risk Management")
			.SetCanOptimize(true);

		_currentDate = DateTime.MinValue;
		_dailyPnLBase = 0m;
		_dailyStopTriggered = false;
		_bestBid = null;
		_bestAsk = null;
		_lastTradePrice = null;
	}

	/// <summary>
	/// Profit target expressed in portfolio currency that blocks further trading once reached.
	/// </summary>
	public decimal DailyTarget
	{
		get => _dailyTarget.Value;
		set => _dailyTarget.Value = value;
	}

	/// <summary>
	/// Maximum allowed loss (in portfolio currency) before all positions are flattened for the day.
	/// </summary>
	public decimal DailyMaxLoss
	{
		get => _dailyMaxLoss.Value;
		set => _dailyMaxLoss.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1), (Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentDate = DateTime.MinValue;
		_dailyPnLBase = 0m;
		_dailyStopTriggered = false;
		_bestBid = null;
		_bestAsk = null;
		_lastTradePrice = null;

		Timer.Stop();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetDailySnapshot(time);

		// Subscribe to bid/ask updates to evaluate the floating profit without scanning order history.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		// Subscribe to trades to refresh the last dealt price when bid/ask quotes are missing.
		SubscribeTicks()
			.Bind(ProcessTrade)
			.Start();

		// Re-evaluate the daily thresholds periodically to catch date changes when the market is idle.
		Timer.Start(TimeSpan.FromMinutes(1), EvaluateDailyThresholds);
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
		base.OnStopped(time);
		Timer.Stop();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);
		// Realized profit changes immediately after each fill.
		EvaluateDailyThresholds();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
			_bestBid = bidPrice;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
			_bestAsk = askPrice;

		if (message.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last) && last is decimal lastPrice)
			_lastTradePrice = lastPrice;

		EvaluateDailyThresholds();
	}

	private void ProcessTrade(ITickTradeMessage trade)
	{
		var price = trade.Price;
		_lastTradePrice = price;

		EvaluateDailyThresholds();
	}

	private void EvaluateDailyThresholds()
	{
		var now = CurrentTime;
		if (_currentDate != now.Date)
			ResetDailySnapshot(now);

		if (_dailyStopTriggered)
			return;

		var realizedPnL = PnL - _dailyPnLBase;
		var floatingPnL = CalculateFloatingPnL();
		if (floatingPnL is null)
			return;

		var totalPnL = realizedPnL + floatingPnL.Value;

		if (DailyTarget > 0m && totalPnL >= DailyTarget)
		{
			TriggerDailyStop($"Daily profit target reached at {totalPnL:0.##}.");
			return;
		}

		if (DailyMaxLoss > 0m && totalPnL <= -DailyMaxLoss)
			TriggerDailyStop($"Daily loss limit reached at {totalPnL:0.##}.");
	}

	private void ResetDailySnapshot(DateTimeOffset time)
	{
		_currentDate = time.Date;
		_dailyPnLBase = PnL;
		_dailyStopTriggered = false;
	}

	private decimal? CalculateFloatingPnL()
	{
		if (Position == 0m)
			return 0m;

		var averagePrice = Position.AveragePrice;
		if (averagePrice == 0m)
			return 0m;

		var referencePrice = GetReferencePrice();
		if (referencePrice is null)
			return null;

		return Position * (referencePrice.Value - averagePrice);
	}

	private decimal? GetReferencePrice()
	{
		if (Position > 0m)
			return _bestBid ?? _lastTradePrice;

		if (Position < 0m)
			return _bestAsk ?? _lastTradePrice;

		return _lastTradePrice;
	}

	private void TriggerDailyStop(string reason)
	{
		_dailyStopTriggered = true;
		LogInfo(reason);
		CancelActiveOrders();

		if (Position > 0m)
		{
			// Close any remaining long exposure with a market sell order.
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			// Close any remaining short exposure with a market buy order.
			BuyMarket(-Position);
		}
	}
}
