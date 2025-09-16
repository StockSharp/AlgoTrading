using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price impulse strategy that trades on rapid bid/ask moves.
/// </summary>
public class PriceImpulseStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _impulsePoints;
	private readonly StrategyParam<int> _historyGap;
	private readonly StrategyParam<int> _extraHistory;
	private readonly StrategyParam<int> _cooldownSeconds;

	private readonly List<decimal> _askHistory = [];
	private readonly List<decimal> _bidHistory = [];

	private decimal _tickSize;
	private DateTimeOffset? _lastTradeTime;

	/// <summary>
	/// Volume used for each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum impulse measured in price points to trigger a trade.
	/// </summary>
	public int ImpulsePoints
	{
		get => _impulsePoints.Value;
		set => _impulsePoints.Value = value;
	}

	/// <summary>
	/// Number of Level1 updates between price comparisons.
	/// </summary>
	public int HistoryGap
	{
		get => _historyGap.Value;
		set => _historyGap.Value = value;
	}

	/// <summary>
	/// Additional Level1 samples kept in the rolling buffer.
	/// </summary>
	public int ExtraHistory
	{
		get => _extraHistory.Value;
		set => _extraHistory.Value = value;
	}

	/// <summary>
	/// Minimum number of seconds between two trades.
	/// </summary>
	public int CooldownSeconds
	{
		get => _cooldownSeconds.Value;
		set => _cooldownSeconds.Value = value;
	}

	private int HistoryCapacity => Math.Max(HistoryGap + ExtraHistory + 1, HistoryGap + 1);

	/// <summary>
	/// Initializes strategy parameters with sensible defaults.
	/// </summary>
	public PriceImpulseStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Volume used for each market order", "Trading")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 150)
			.SetDisplay("Stop Loss Points", "Stop loss distance expressed in price points", "Risk")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetDisplay("Take Profit Points", "Take profit distance expressed in price points", "Risk")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_impulsePoints = Param(nameof(ImpulsePoints), 15)
			.SetDisplay("Impulse Points", "Minimum ask/bid impulse required to trade", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_historyGap = Param(nameof(HistoryGap), 15)
			.SetDisplay("Gap Ticks", "Number of Level1 updates between comparison points", "Signals")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_extraHistory = Param(nameof(ExtraHistory), 15)
			.SetDisplay("Extra History", "Additional Level1 samples kept to absorb bursts", "Signals")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true)
			.SetOptimize(0, 30, 5);

		_cooldownSeconds = Param(nameof(CooldownSeconds), 100)
			.SetDisplay("Cooldown Seconds", "Minimum number of seconds between trades", "Risk")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true)
			.SetOptimize(0, 300, 20);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_askHistory.Clear();
		_bidHistory.Clear();
		_lastTradeTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Derive point value from the security metadata.
		_tickSize = Security?.PriceStep ?? Security?.MinPriceStep ?? 1m;
		if (_tickSize <= 0)
			_tickSize = 1m;

		var takeProfit = TakeProfitPoints > 0
			? new Unit(TakeProfitPoints * _tickSize, UnitTypes.Absolute)
			: null;

		var stopLoss = StopLossPoints > 0
			? new Unit(StopLossPoints * _tickSize, UnitTypes.Absolute)
			: null;

		StartProtection(
			takeProfit: takeProfit,
			stopLoss: stopLoss);

		// Subscribe to Level1 data to monitor best bid/ask changes.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var askUpdated = false;
		var bidUpdated = false;

		if (level1.TryGetDecimal(Level1Fields.BestAskPrice) is decimal askPrice && askPrice > 0)
		{
			UpdateHistory(_askHistory, askPrice);
			askUpdated = true;
		}

		if (level1.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bidPrice && bidPrice > 0)
		{
			UpdateHistory(_bidHistory, bidPrice);
			bidUpdated = true;
		}

		// Skip processing if nothing new arrived.
		if (!askUpdated && !bidUpdated)
			return;

		// Ensure the strategy is allowed to submit orders.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = level1.ServerTime != default ? level1.ServerTime : CurrentTime;
		var impulseThreshold = ImpulsePoints * _tickSize;
		var traded = false;

		// Check for bullish impulse when the ask jumps upward.
		if (askUpdated && _askHistory.Count > HistoryGap)
		{
			var lastIndex = _askHistory.Count - 1;
			var compareIndex = lastIndex - HistoryGap;

			if (compareIndex >= 0)
			{
				var currentAsk = _askHistory[lastIndex];
				var comparisonAsk = _askHistory[compareIndex];
				var askImpulse = currentAsk - comparisonAsk;

				if (askImpulse > impulseThreshold && Position <= 0 && IsCooldownPassed(time))
				{
					BuyMarket(OrderVolume);
					LogInfo($"Buy signal: ask impulse {askImpulse}, ask={currentAsk}, baseline={comparisonAsk}");
					traded = true;
				}
			}
		}

		if (traded)
		{
			_lastTradeTime = time;
			return;
		}

		// Check for bearish impulse when the bid collapses downward.
		if (bidUpdated && _bidHistory.Count > HistoryGap)
		{
			var lastIndex = _bidHistory.Count - 1;
			var compareIndex = lastIndex - HistoryGap;

			if (compareIndex >= 0)
			{
				var currentBid = _bidHistory[lastIndex];
				var comparisonBid = _bidHistory[compareIndex];
				var bidImpulse = comparisonBid - currentBid;

				if (bidImpulse > impulseThreshold && Position >= 0 && IsCooldownPassed(time))
				{
					SellMarket(OrderVolume);
					LogInfo($"Sell signal: bid impulse {bidImpulse}, bid={currentBid}, baseline={comparisonBid}");
					traded = true;
				}
			}
		}

		if (traded)
			_lastTradeTime = time;
	}

	private void UpdateHistory(List<decimal> history, decimal value)
	{
		history.Add(value);

		var capacity = HistoryCapacity;
		while (history.Count > capacity)
			history.RemoveAt(0);
	}

	private bool IsCooldownPassed(DateTimeOffset time)
	{
		if (_lastTradeTime is null)
			return true;

		var cooldownSeconds = CooldownSeconds;
		if (cooldownSeconds <= 0)
			return true;

		return time - _lastTradeTime.Value >= TimeSpan.FromSeconds(cooldownSeconds);
	}
}
