namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the "Elite eFibo Trader v2.1" expert advisor.
/// Rebuilds the Fibonacci ladder with dynamic money management and shared stops.
/// </summary>
public class EliteEfiboTraderV21Strategy : Strategy
{
	private readonly StrategyParam<int> _levelCount;

	private readonly StrategyParam<bool> _openBuy;
	private readonly StrategyParam<bool> _openSell;
	private readonly StrategyParam<bool> _tradeAgainAfterProfit;
	private readonly StrategyParam<int> _levelDistancePips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<decimal>[] _levelVolumeParams;

	private readonly List<LevelState> _levels = new();
	private readonly Dictionary<Order, LevelState> _entryOrderMap = new();
	private readonly Dictionary<Order, LevelState> _exitOrderMap = new();

	private bool _allowTrading = true;
	private decimal _pipSize;
	private decimal _priceStep;
	private decimal _stepPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="EliteEfiboTraderV21Strategy"/> class.
	/// </summary>
	public EliteEfiboTraderV21Strategy()
	{
		_levelCount = Param(nameof(LevelCount), 14)
		.SetRange(1, 14)
		.SetDisplay("Level Count", "Number of Fibonacci ladder levels to manage.", "Execution");

		_openBuy = Param(nameof(OpenBuy), false)
					.SetDisplay("Open Buy", "Allow the strategy to build long Fibonacci ladders.", "Execution");

		_openSell = Param(nameof(OpenSell), true)
			.SetDisplay("Open Sell", "Allow the strategy to build short Fibonacci ladders.", "Execution");

		_tradeAgainAfterProfit = Param(nameof(TradeAgainAfterProfit), true)
			.SetDisplay("Trade After Profit", "Resume trading after the basket hits the money take-profit.", "Risk");

		_levelDistancePips = Param(nameof(LevelDistancePips), 20)
			.SetNotNegative()
			.SetDisplay("Level Distance", "Distance between consecutive pending levels in pips.", "Execution");

		_stopLossPips = Param(nameof(StopLossPips), 10)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Initial stop-loss distance for every level in pips.", "Risk");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 2000m)
			.SetNotNegative()
			.SetDisplay("Money Take Profit", "Cash target that closes the entire basket.", "Risk");

		var defaults = new decimal[] { 1m, 1m, 2m, 3m, 5m, 8m, 13m, 21m, 34m, 55m, 89m, 144m, 233m, 377m };
		_levelVolumeParams = new StrategyParam<decimal>[defaults.Length];

		for (var i = 0; i < _levelVolumeParams.Length; i++)
		{
			var index = i + 1;
			_levelVolumeParams[i] = Param($"Level{index}Volume", defaults[i])
			.SetNotNegative()
			.SetDisplay($"Level {index} Volume", $"Volume multiplier used for Fibonacci level {index}.", "Position Sizing");
		}
	}

	/// <summary>
	/// Enable the buy ladder.
	/// </summary>
	public bool OpenBuy
	{
		get => _openBuy.Value;
		set => _openBuy.Value = value;
	}

	/// <summary>
	/// Enable the sell ladder.
	/// </summary>
	public bool OpenSell
	{
		get => _openSell.Value;
		set => _openSell.Value = value;
	}

	/// <summary>
	/// Allow the strategy to restart after taking profit.
	/// </summary>
	public bool TradeAgainAfterProfit
	{
		get => _tradeAgainAfterProfit.Value;
		set => _tradeAgainAfterProfit.Value = value;
	}

	/// <summary>
	/// Pip distance between ladder levels.
	/// </summary>
	public int LevelDistancePips
	{
		get => _levelDistancePips.Value;
		set => _levelDistancePips.Value = value;
	}

	/// <summary>
	/// Pip distance used for stop-loss placement.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Cash profit target for the active basket.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Number of Fibonacci ladder levels to trade.
	/// </summary>
	public int LevelCount
	{
		get => _levelCount.Value;
		set => _levelCount.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_allowTrading = true;
		_priceStep = Security?.PriceStep ?? 0m;
		_stepPrice = Security?.StepPrice ?? 0m;
		_pipSize = CalculatePipSize();

		StartProtection();

		var trades = SubscribeTrades();
		trades.Bind(ProcessTrade).Start();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null)
			return;

		if (_entryOrderMap.TryGetValue(order, out var entryLevel) && IsFinalState(order))
		{
			_entryOrderMap.Remove(order);
			entryLevel.EntryOrder = null;
		}

		if (_exitOrderMap.TryGetValue(order, out var exitLevel) && IsFinalState(order))
		{
			_exitOrderMap.Remove(order);
			exitLevel.ExitOrder = null;
			if (exitLevel.OpenVolume <= 0m)
				exitLevel.StopPrice = null;
		}

		CleanupLevels();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order is not Order order)
			return;

		var tradeInfo = trade.Trade;
		var tradeVolume = tradeInfo?.Volume ?? 0m;
		if (tradeVolume <= 0m)
			return;

		if (_entryOrderMap.TryGetValue(order, out var entryLevel))
		{
			var tradePrice = tradeInfo?.Price ?? order.Price ?? 0m;
			if (tradePrice > 0m)
			{
				var executed = entryLevel.ExecutedVolume;
				var newExecuted = executed + tradeVolume;
				if (executed <= 0m)
				{
					entryLevel.EntryPrice = tradePrice;
				}
				else if (entryLevel.EntryPrice is decimal avg)
				{
					entryLevel.EntryPrice = (avg * executed + tradePrice * tradeVolume) / newExecuted;
				}
				entryLevel.ExecutedVolume = newExecuted;
			}

			entryLevel.OpenVolume += tradeVolume;
			UpdateInitialStop(entryLevel);
		}
		else if (_exitOrderMap.TryGetValue(order, out var exitLevel))
		{
			exitLevel.OpenVolume -= tradeVolume;
			if (exitLevel.OpenVolume < 0m)
				exitLevel.OpenVolume = 0m;
		}

		CleanupLevels();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		if (trade.TradePrice is not decimal price)
			return;

		if (TradeAgainAfterProfit)
			_allowTrading = true;

		_priceStep = Security?.PriceStep ?? _priceStep;
		_stepPrice = Security?.StepPrice ?? _stepPrice;
		_pipSize = CalculatePipSize();

		AlignSharedStops();

		if (HasOpenVolume())
		{
			if (MoneyTakeProfit > 0m)
			{
				var profit = CalculateOpenProfit(price);
				if (profit >= MoneyTakeProfit)
				{
					LogInfo($"Money take profit reached: {profit:F2}");
					CloseAllPositions();
					CancelPendingOrders();
					if (!TradeAgainAfterProfit)
						_allowTrading = false;
					return;
				}
			}

			if (IsStopTriggered(price))
			{
				LogInfo("Shared stop triggered. Closing the basket.");
				CloseAllPositions();
				CancelPendingOrders();
				if (!TradeAgainAfterProfit)
					_allowTrading = false;
				return;
			}
		}
		else if (!HasPendingOrders())
		{
			CleanupLevels();
		}

		if (!_allowTrading)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (HasActiveExposure())
			return;

		var side = ResolveDirection();
		if (side == null)
			return;

		TryOpenLadder(side.Value, price);
	}

	private Sides? ResolveDirection()
	{
		var buy = OpenBuy && !OpenSell;
		var sell = OpenSell && !OpenBuy;

		if (buy)
			return Sides.Buy;

		if (sell)
			return Sides.Sell;

		return null;
	}

	private void TryOpenLadder(Sides side, decimal referencePrice)
	{
		var distance = LevelDistancePips * _pipSize;
		var stopOffset = StopLossPips * _pipSize;

		var activeLevels = Math.Min(LevelCount, _levelVolumeParams.Length);
		for (var i = 0; i < activeLevels; i++)
		{
			var volume = _levelVolumeParams[i].Value;
			if (volume <= 0m)
				continue;

			volume = NormalizeVolume(volume);
			if (volume <= 0m)
				continue;

			Order order;
			if (i == 0)
			{
				order = side == Sides.Buy ? BuyMarket(volume) : SellMarket(volume);
			}
			else
			{
				var steps = distance * i;
				decimal price;
				if (side == Sides.Buy)
					price = referencePrice + steps;
				else
					price = referencePrice - steps;

				price = NormalizePrice(price);
				if (price <= 0m)
					continue;

				order = side == Sides.Buy ? BuyStop(volume, price) : SellStop(volume, price);
			}

			if (order == null)
				continue;

			var level = new LevelState(i + 1, side, volume, stopOffset)
			{
				EntryOrder = order
			};

			_levels.Add(level);
			_entryOrderMap[order] = level;
		}
	}

	private void CloseAllPositions()
	{
		foreach (var level in _levels)
		{
			if (level.OpenVolume <= 0m)
				continue;

			if (level.ExitOrder != null && !IsFinalState(level.ExitOrder))
				continue;

			Order order = level.Side == Sides.Buy
				? SellMarket(level.OpenVolume)
				: BuyMarket(level.OpenVolume);

			if (order != null)
			{
				level.ExitOrder = order;
				_exitOrderMap[order] = level;
			}
		}
	}

	private void CancelPendingOrders()
	{
		foreach (var level in _levels)
		{
			var order = level.EntryOrder;
			if (order == null || IsFinalState(order))
				continue;

			CancelOrder(order);
		}
	}

	private void AlignSharedStops()
	{
		decimal? bestBuy = null;
		decimal? bestSell = null;

		foreach (var level in _levels)
		{
			if (level.OpenVolume <= 0m || level.EntryPrice is not decimal entry)
				continue;

			var stop = level.Side == Sides.Buy
				? NormalizePrice(entry - level.StopOffset)
				: NormalizePrice(entry + level.StopOffset);

			if (level.Side == Sides.Buy)
			{
				if (bestBuy is null || stop > bestBuy)
					bestBuy = stop;
			}
			else
			{
				if (bestSell is null || stop < bestSell)
					bestSell = stop;
			}

			level.StopPrice = stop;
		}

		if (bestBuy is decimal buyStop)
		{
			foreach (var level in _levels)
			{
				if (level.Side != Sides.Buy || level.OpenVolume <= 0m)
					continue;

				if (level.StopPrice is not decimal current || current < buyStop)
					level.StopPrice = buyStop;
			}
		}

		if (bestSell is decimal sellStop)
		{
			foreach (var level in _levels)
			{
				if (level.Side != Sides.Sell || level.OpenVolume <= 0m)
					continue;

				if (level.StopPrice is not decimal current || current > sellStop)
					level.StopPrice = sellStop;
			}
		}
	}

	private bool IsStopTriggered(decimal currentPrice)
	{
		foreach (var level in _levels)
		{
			if (level.OpenVolume <= 0m || level.StopPrice is not decimal stop)
				continue;

			if (level.Side == Sides.Buy)
			{
				if (currentPrice <= stop)
					return true;
			}
			else
			{
				if (currentPrice >= stop)
					return true;
			}
		}

		return false;
	}

	private void UpdateInitialStop(LevelState level)
	{
		if (level.StopOffset <= 0m || level.EntryPrice is not decimal price)
			return;

		var stop = level.Side == Sides.Buy
			? NormalizePrice(price - level.StopOffset)
			: NormalizePrice(price + level.StopOffset);

		if (level.StopPrice is not decimal current)
		{
			level.StopPrice = stop;
		}
		else if (level.Side == Sides.Buy && stop > current)
		{
			level.StopPrice = stop;
		}
		else if (level.Side == Sides.Sell && stop < current)
		{
			level.StopPrice = stop;
		}
	}

	private void CleanupLevels()
	{
		for (var i = _levels.Count - 1; i >= 0; i--)
		{
			var level = _levels[i];

			if (level.OpenVolume > 0m)
				continue;

			var hasEntry = level.EntryOrder != null && !IsFinalState(level.EntryOrder);
			var hasExit = level.ExitOrder != null && !IsFinalState(level.ExitOrder);

			if (!hasEntry && !hasExit)
				_levels.RemoveAt(i);
		}
	}

	private bool HasActiveExposure()
	{
		if (HasOpenVolume())
			return true;

		foreach (var level in _levels)
		{
			if (level.EntryOrder != null && !IsFinalState(level.EntryOrder))
				return true;

			if (level.ExitOrder != null && !IsFinalState(level.ExitOrder))
				return true;
		}

		return false;
	}

	private bool HasOpenVolume()
	{
		foreach (var level in _levels)
		{
			if (level.OpenVolume > 0m)
				return true;
		}

		return false;
	}

	private bool HasPendingOrders()
	{
		foreach (var level in _levels)
		{
			var order = level.EntryOrder;
			if (order != null && !IsFinalState(order))
				return true;
		}

		return false;
	}

	private decimal CalculateOpenProfit(decimal currentPrice)
	{
		if (_priceStep <= 0m || _stepPrice <= 0m)
			return 0m;

		decimal profit = 0m;

		foreach (var level in _levels)
		{
			if (level.OpenVolume <= 0m || level.EntryPrice is not decimal entry)
				continue;

			var difference = level.Side == Sides.Buy ? currentPrice - entry : entry - currentPrice;
			var steps = difference / _priceStep;
			profit += steps * _stepPrice * level.OpenVolume;
		}

		return profit;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
			return price;

		return Math.Round(price / _priceStep, MidpointRounding.AwayFromZero) * _priceStep;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		var multiplier = security.Decimals is 3 or 5 ? 10m : 1m;
		var pip = step * multiplier;
		return pip > 0m ? pip : 0.0001m;
	}

	private sealed class LevelState
	{
		public LevelState(int index, Sides side, decimal plannedVolume, decimal stopOffset)
		{
			Index = index;
			Side = side;
			PlannedVolume = plannedVolume;
			StopOffset = stopOffset;
		}

		public int Index { get; }
		public Sides Side { get; }
		public decimal PlannedVolume { get; }
		public decimal StopOffset { get; }
		public Order EntryOrder { get; set; }
		public Order ExitOrder { get; set; }
		public decimal ExecutedVolume { get; set; }
		public decimal OpenVolume { get; set; }
		public decimal? EntryPrice { get; set; }
		public decimal? StopPrice { get; set; }
	}
}
