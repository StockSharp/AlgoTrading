using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual sell management strategy converted from the MetaTrader expert advisor "iTrade".
/// </summary>
public class ITradeStrategy : Strategy
{
	private readonly StrategyParam<int> _profitResetAfterWins;
	private readonly StrategyParam<int> _historyLimit;

	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<decimal> _averageProfitTarget;
	private readonly StrategyParam<decimal> _extendedAverageProfitTarget;
	private readonly StrategyParam<int> _baseTradeCount;
	private readonly StrategyParam<TimeSpan> _controlInterval;

	private readonly List<OpenTrade> _openTrades = new();
	private readonly Queue<OpenTrade> _closingQueue = new();
	private readonly List<decimal> _closedProfits = new();

	private decimal _lastBid;
	private decimal _lastAsk;
	private decimal _lastTradePrice;
	private decimal _priceStep;
	private decimal _stepPrice;

	private int _pendingSellRequests;

	/// <summary>
	/// Initializes a new instance of <see cref="ITradeStrategy"/>.
	/// </summary>
	public ITradeStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 0.02m)
			.SetDisplay("Initial Volume", "Base lot size used for the first sell order.", "Money Management")
			.SetGreaterThanZero();

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 1.4m)
			.SetDisplay("Martingale Multiplier", "Volume multiplier applied after each losing trade.", "Money Management")
			.SetGreaterThanZero();

		_averageProfitTarget = Param(nameof(AverageProfitTarget), 3.2m)
			.SetDisplay("Average Profit Target", "Average floating profit per trade required to start closing the initial batch.", "Profit Control");

		_extendedAverageProfitTarget = Param(nameof(ExtendedAverageProfitTarget), 4.9m)
			.SetDisplay("Extended Profit Target", "Average floating profit per trade required when more than the base batch is active.", "Profit Control");

		_baseTradeCount = Param(nameof(BaseTradeCount), 7)
			.SetDisplay("Base Trade Count", "Number of trades considered part of the initial batch.", "Profit Control")
			.SetGreaterThanZero();

		_profitResetAfterWins = Param(nameof(ProfitResetAfterWins), 1)
			.SetDisplay("Profit Reset Wins", "Number of consecutive winning cycles before profit counters reset.", "Profit Control")
			.SetGreaterThanOrEqualZero();

		_historyLimit = Param(nameof(HistoryLimit), 200)
			.SetDisplay("History Limit", "Maximum number of closed profit samples retained.", "Profit Control")
			.SetGreaterThanZero();

		_controlInterval = Param(nameof(ControlInterval), TimeSpan.FromSeconds(1))
			.SetDisplay("Control Interval", "Frequency of profit checks and order dispatch.", "General")
			.SetGreaterThan(TimeSpan.Zero);
	}

	/// <summary>
	/// Initial volume used for the first sell order in a martingale sequence.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the volume after each losing trade.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Average floating profit threshold for the first batch of trades.
	/// </summary>
	public decimal AverageProfitTarget
	{
		get => _averageProfitTarget.Value;
		set => _averageProfitTarget.Value = value;
	}

	/// <summary>
	/// Average floating profit threshold used once the trade count exceeds <see cref="BaseTradeCount"/>.
	/// </summary>
	public decimal ExtendedAverageProfitTarget
	{
		get => _extendedAverageProfitTarget.Value;
		set => _extendedAverageProfitTarget.Value = value;
	}

	/// <summary>
	/// Maximum number of trades considered part of the initial batch.
	/// </summary>
	public int BaseTradeCount
	{
		get => _baseTradeCount.Value;
		set => _baseTradeCount.Value = value;
	}

	/// <summary>
	/// Interval used to poll trading conditions.
	/// </summary>
	public TimeSpan ControlInterval
	{
		get => _controlInterval.Value;
		set => _controlInterval.Value = value;
	}

	/// <summary>
	/// Number of consecutive profitable cycles before the profit history resets.
	/// </summary>
	public int ProfitResetAfterWins
	{
		get => _profitResetAfterWins.Value;
		set => _profitResetAfterWins.Value = value;
	}

	/// <summary>
	/// Maximum number of closed profit samples preserved for averaging.
	/// </summary>
	public int HistoryLimit
	{
		get => _historyLimit.Value;
		set => _historyLimit.Value = value;
	}

	/// <summary>
	/// Gets the current number of open sell trades managed by the strategy.
	/// </summary>
	public int OpenSellCount => _openTrades.Count;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, DataType.Ticks);
		yield return (Security, DataType.OrderBook);
	}

	/// <summary>
	/// Enqueue a manual sell request, emulating the original MetaTrader chart button.
	/// </summary>
	public void QueueSellRequest()
	{
		_pendingSellRequests++;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openTrades.Clear();
		_closingQueue.Clear();
		_closedProfits.Clear();
		_pendingSellRequests = 0;
		_lastBid = 0m;
		_lastAsk = 0m;
		_lastTradePrice = 0m;
		_priceStep = 0m;
		_stepPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException(LocalizedStrings.Str3239);

		_priceStep = Security.PriceStep ?? 0m;
		_stepPrice = Security.StepPrice ?? 0m;

		if (_priceStep <= 0m || _stepPrice <= 0m)
			throw new InvalidOperationException("Security must provide valid PriceStep and StepPrice values.");

		SubscribeTicks()
			.Bind(ProcessTrade)
			.Start();

		SubscribeOrderBook()
			.Bind(ProcessOrderBook)
			.Start();

		Timer.Start(ControlInterval, OnTimerTick);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		Timer.Stop();
		base.OnStop();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order == null || trade.Trade == null)
			return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;

		if (volume <= 0m)
			return;

		_lastTradePrice = price;

		if (trade.Order.Side == Sides.Sell)
		{
			RegisterOpenedTrade(volume, price);
		}
		else if (trade.Order.Side == Sides.Buy)
		{
			RegisterClosedVolume(volume, price);
		}
	}

	private void ProcessTrade(ITickTradeMessage trade)
	{
		var price = trade.Price;

		if (price > 0m)
			_lastTradePrice = price;
	}

	private void ProcessOrderBook(IOrderBookMessage depth)
	{
		if (depth.BestBid != null)
			_lastBid = depth.BestBid.Value.Price;

		if (depth.BestAsk != null)
			_lastAsk = depth.BestAsk.Value.Price;
	}

	private void OnTimerTick()
	{
		ProcessPendingSellRequest();
		TryCloseByAverageProfit();
	}

	private void ProcessPendingSellRequest()
	{
		if (_pendingSellRequests <= 0)
			return;

		var volume = NormalizeVolume(GetNextVolume());

		if (volume <= 0m)
			return;

		SellMarket(volume);
		_pendingSellRequests--;
	}

	private decimal GetNextVolume()
	{
		var volume = InitialVolume;

		if (_closedProfits.Count == 0)
			return volume;

		var consecutiveLosses = 0;
		var consecutiveWins = 0;

		foreach (var profit in _closedProfits.AsEnumerable().Reverse())
		{
			if (profit > 0m)
			{
				if (consecutiveLosses > 0)
					break;

				consecutiveWins++;

				if (ProfitResetAfterWins > 0 && consecutiveWins >= ProfitResetAfterWins)
					break;
			}
			else if (profit < 0m)
			{
				if (consecutiveWins > 0)
					break;

				consecutiveLosses++;
				volume *= MartingaleMultiplier;
			}
			else
			{
				break;
			}
		}

		return volume;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security == null)
			return volume;

		var step = Security.VolumeStep;
		var min = Security.VolumeMin;
		var max = Security.VolumeMax;

		if (step is { } s && s > 0m)
			volume = Math.Round(volume / s, MidpointRounding.AwayFromZero) * s;

		if (min is { } minVol && minVol > 0m && volume < minVol)
			volume = minVol;

		if (max is { } maxVol && maxVol > 0m && volume > maxVol)
			volume = maxVol;

		return volume;
	}

	private void RegisterOpenedTrade(decimal volume, decimal price)
	{
		var trade = new OpenTrade(volume, price);
		_openTrades.Add(trade);
	}

	private void RegisterClosedVolume(decimal volume, decimal price)
	{
		var remaining = volume;

		while (remaining > 0m)
		{
			var target = _closingQueue.Count > 0 ? _closingQueue.Peek() : _openTrades.FirstOrDefault();

			if (target == null)
				break;

			var closing = Math.Min(remaining, target.RemainingVolume);
			var profit = CalculateProfit(target.EntryPrice, price, closing);

			_closedProfits.Add(profit);
			TrimHistory();

			target.RemainingVolume -= closing;
			remaining -= closing;

			if (target.RemainingVolume <= 0m)
			{
				_openTrades.Remove(target);

				if (_closingQueue.Count > 0 && ReferenceEquals(_closingQueue.Peek(), target))
					_closingQueue.Dequeue();
			}
		}
	}

	private void TrimHistory()
	{
		if (_closedProfits.Count <= HistoryLimit)
			return;

		var excess = _closedProfits.Count - HistoryLimit;
		_closedProfits.RemoveRange(0, excess);
	}

	private void TryCloseByAverageProfit()
	{
		var count = _openTrades.Count;

		if (count == 0)
			return;

		var currentPrice = GetCurrentAskPrice();

		if (currentPrice <= 0m)
			return;

		var profits = new List<(OpenTrade trade, decimal profit)>(count);
		var totalProfit = 0m;

		foreach (var trade in _openTrades)
		{
			var profit = CalculateProfit(trade.EntryPrice, currentPrice, trade.RemainingVolume);
			profits.Add((trade, profit));
			totalProfit += profit;
		}

		var averageProfit = totalProfit / count;
		var threshold = count <= BaseTradeCount ? AverageProfitTarget : ExtendedAverageProfitTarget;

		if (averageProfit < threshold)
			return;

		var best = profits.OrderByDescending(p => p.profit).First().trade;
		var worst = profits.OrderBy(p => p.profit).First().trade;

		RequestClose(best);

		if (!ReferenceEquals(best, worst))
			RequestClose(worst);
	}

	private void RequestClose(OpenTrade trade)
	{
		if (trade.IsClosing)
			return;

		trade.IsClosing = true;
		_closingQueue.Enqueue(trade);

		if (trade.RemainingVolume > 0m)
			BuyMarket(trade.RemainingVolume);
	}

	private decimal CalculateProfit(decimal entryPrice, decimal exitPrice, decimal volume)
	{
		if (_priceStep <= 0m || _stepPrice <= 0m || volume <= 0m)
			return 0m;

		var steps = (entryPrice - exitPrice) / _priceStep;
		return steps * _stepPrice * volume;
	}

	private decimal GetCurrentAskPrice()
	{
		if (_lastAsk > 0m)
			return _lastAsk;

		if (_lastTradePrice > 0m)
			return _lastTradePrice;

		return 0m;
	}

	private sealed class OpenTrade
	{
		public OpenTrade(decimal volume, decimal price)
		{
			RemainingVolume = volume;
			EntryPrice = price;
		}

		public decimal RemainingVolume { get; set; }
		public decimal EntryPrice { get; }
		public bool IsClosing { get; set; }
	}
}
