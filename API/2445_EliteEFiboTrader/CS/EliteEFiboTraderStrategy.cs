using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Elite eFibo Trader grid strategy converted from MQL5.
/// Builds a Fibonacci-based sequence of buy or sell stop orders and manages trailing stops.
/// </summary>
public class EliteEFiboTraderStrategy : Strategy
{
	private const int LevelsCount = 14;

	private readonly StrategyParam<bool> _openBuy;
	private readonly StrategyParam<bool> _openSell;
	private readonly StrategyParam<bool> _tradeAgainAfterProfit;
	private readonly StrategyParam<decimal> _levelDistance;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<decimal>[] _levelVolumes;

	private decimal _prevPosition;
	private decimal _prevPositionPrice;
	private bool _tradeEnabled;
	private bool _cycleActive;
	private int _activeDirection;
	private decimal _stopDistance;
	private decimal _levelStep;
	private decimal? _bestBuyStop;
	private decimal? _bestSellStop;

	private readonly List<GridEntry> _entries = new();

	/// <summary>
	/// Enable buy-only mode.
	/// </summary>
	public bool OpenBuy
	{
		get => _openBuy.Value;
		set => _openBuy.Value = value;
	}

	/// <summary>
	/// Enable sell-only mode.
	/// </summary>
	public bool OpenSell
	{
		get => _openSell.Value;
		set => _openSell.Value = value;
	}

	/// <summary>
	/// Allow restarting the trading cycle after profit target is reached.
	/// </summary>
	public bool TradeAgainAfterProfit
	{
		get => _tradeAgainAfterProfit.Value;
		set => _tradeAgainAfterProfit.Value = value;
	}

	/// <summary>
	/// Distance between successive pending levels in price steps.
	/// </summary>
	public decimal LevelDistance
	{
		get => _levelDistance.Value;
		set => _levelDistance.Value = value;
	}

	/// <summary>
	/// Stop-loss size in price steps for each grid order.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit target in currency.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Volume for the first grid level.
	/// </summary>
	public decimal LotsLevel1
	{
		get => _levelVolumes[0].Value;
		set => _levelVolumes[0].Value = value;
	}

	/// <summary>
	/// Volume for the second grid level.
	/// </summary>
	public decimal LotsLevel2
	{
		get => _levelVolumes[1].Value;
		set => _levelVolumes[1].Value = value;
	}

	/// <summary>
	/// Volume for the third grid level.
	/// </summary>
	public decimal LotsLevel3
	{
		get => _levelVolumes[2].Value;
		set => _levelVolumes[2].Value = value;
	}

	/// <summary>
	/// Volume for the fourth grid level.
	/// </summary>
	public decimal LotsLevel4
	{
		get => _levelVolumes[3].Value;
		set => _levelVolumes[3].Value = value;
	}

	/// <summary>
	/// Volume for the fifth grid level.
	/// </summary>
	public decimal LotsLevel5
	{
		get => _levelVolumes[4].Value;
		set => _levelVolumes[4].Value = value;
	}

	/// <summary>
	/// Volume for the sixth grid level.
	/// </summary>
	public decimal LotsLevel6
	{
		get => _levelVolumes[5].Value;
		set => _levelVolumes[5].Value = value;
	}

	/// <summary>
	/// Volume for the seventh grid level.
	/// </summary>
	public decimal LotsLevel7
	{
		get => _levelVolumes[6].Value;
		set => _levelVolumes[6].Value = value;
	}

	/// <summary>
	/// Volume for the eighth grid level.
	/// </summary>
	public decimal LotsLevel8
	{
		get => _levelVolumes[7].Value;
		set => _levelVolumes[7].Value = value;
	}

	/// <summary>
	/// Volume for the ninth grid level.
	/// </summary>
	public decimal LotsLevel9
	{
		get => _levelVolumes[8].Value;
		set => _levelVolumes[8].Value = value;
	}

	/// <summary>
	/// Volume for the tenth grid level.
	/// </summary>
	public decimal LotsLevel10
	{
		get => _levelVolumes[9].Value;
		set => _levelVolumes[9].Value = value;
	}

	/// <summary>
	/// Volume for the eleventh grid level.
	/// </summary>
	public decimal LotsLevel11
	{
		get => _levelVolumes[10].Value;
		set => _levelVolumes[10].Value = value;
	}

	/// <summary>
	/// Volume for the twelfth grid level.
	/// </summary>
	public decimal LotsLevel12
	{
		get => _levelVolumes[11].Value;
		set => _levelVolumes[11].Value = value;
	}

	/// <summary>
	/// Volume for the thirteenth grid level.
	/// </summary>
	public decimal LotsLevel13
	{
		get => _levelVolumes[12].Value;
		set => _levelVolumes[12].Value = value;
	}

	/// <summary>
	/// Volume for the fourteenth grid level.
	/// </summary>
	public decimal LotsLevel14
	{
		get => _levelVolumes[13].Value;
		set => _levelVolumes[13].Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public EliteEFiboTraderStrategy()
	{
		_openBuy = Param(nameof(OpenBuy), false)
		.SetDisplay("Open Buy", "Enable Fibonacci grid in the long direction", "General");

		_openSell = Param(nameof(OpenSell), true)
		.SetDisplay("Open Sell", "Enable Fibonacci grid in the short direction", "General");

		_tradeAgainAfterProfit = Param(nameof(TradeAgainAfterProfit), true)
		.SetDisplay("Trade Again", "Restart grid after profit target", "General");

		_levelDistance = Param(nameof(LevelDistance), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Level Distance", "Distance between pending orders in points", "Grid");

		_stopLossPoints = Param(nameof(StopLossPoints), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop-loss size in points", "Risk");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 2000m)
		.SetGreaterThanZero()
		.SetDisplay("Money Take Profit", "Unrealized profit target in account currency", "Risk");

		_levelVolumes = new StrategyParam<decimal>[LevelsCount];

		_levelVolumes[0] = Param(nameof(LotsLevel1), 1m)
		.SetDisplay("Level 1 Volume", "Volume for the first order", "Volumes");
		_levelVolumes[1] = Param(nameof(LotsLevel2), 1m)
		.SetDisplay("Level 2 Volume", "Volume for the second order", "Volumes");
		_levelVolumes[2] = Param(nameof(LotsLevel3), 2m)
		.SetDisplay("Level 3 Volume", "Volume for the third order", "Volumes");
		_levelVolumes[3] = Param(nameof(LotsLevel4), 3m)
		.SetDisplay("Level 4 Volume", "Volume for the fourth order", "Volumes");
		_levelVolumes[4] = Param(nameof(LotsLevel5), 5m)
		.SetDisplay("Level 5 Volume", "Volume for the fifth order", "Volumes");
		_levelVolumes[5] = Param(nameof(LotsLevel6), 8m)
		.SetDisplay("Level 6 Volume", "Volume for the sixth order", "Volumes");
		_levelVolumes[6] = Param(nameof(LotsLevel7), 13m)
		.SetDisplay("Level 7 Volume", "Volume for the seventh order", "Volumes");
		_levelVolumes[7] = Param(nameof(LotsLevel8), 21m)
		.SetDisplay("Level 8 Volume", "Volume for the eighth order", "Volumes");
		_levelVolumes[8] = Param(nameof(LotsLevel9), 34m)
		.SetDisplay("Level 9 Volume", "Volume for the ninth order", "Volumes");
		_levelVolumes[9] = Param(nameof(LotsLevel10), 55m)
		.SetDisplay("Level 10 Volume", "Volume for the tenth order", "Volumes");
		_levelVolumes[10] = Param(nameof(LotsLevel11), 89m)
		.SetDisplay("Level 11 Volume", "Volume for the eleventh order", "Volumes");
		_levelVolumes[11] = Param(nameof(LotsLevel12), 144m)
		.SetDisplay("Level 12 Volume", "Volume for the twelfth order", "Volumes");
		_levelVolumes[12] = Param(nameof(LotsLevel13), 233m)
		.SetDisplay("Level 13 Volume", "Volume for the thirteenth order", "Volumes");
		_levelVolumes[13] = Param(nameof(LotsLevel14), 377m)
		.SetDisplay("Level 14 Volume", "Volume for the fourteenth order", "Volumes");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_tradeEnabled = true;
		_cycleActive = false;
		_activeDirection = 0;
		_prevPosition = 0m;
		_prevPositionPrice = 0m;
		_stopDistance = 0m;
		_levelStep = 0m;
		_bestBuyStop = null;
		_bestSellStop = null;
		_entries.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tradeEnabled = true;
		_prevPosition = Position;
		_prevPositionPrice = PositionPrice;
		UpdateStepSizes();

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		if (trade.TradePrice is not decimal price)
		return;

		if (TradeAgainAfterProfit)
		_tradeEnabled = true;

		UpdateStepSizes();
		UpdateExecutedEntries();

		if (!_cycleActive && _tradeEnabled && HasValidDirection() && IsFormedAndOnlineAndAllowTrading())
		TryStartCycle(price);

		if (Position != 0m)
		{
			ApplyTrailing(price);
			CheckTakeProfit(price);
		}
		else if (_cycleActive && AreActiveOrdersEmpty())
		{
			FinishCycle(false);
		}

		_prevPosition = Position;
		_prevPositionPrice = PositionPrice;
	}

	private void UpdateExecutedEntries()
	{
		var currentPosition = Position;
		var executedVolumeDelta = currentPosition - _prevPosition;

		if (executedVolumeDelta == 0m)
		return;

		var prevValue = _prevPosition * _prevPositionPrice;
		var currentValue = currentPosition * PositionPrice;
		decimal? executedPrice = null;

		if (executedVolumeDelta != 0m)
		executedPrice = (currentValue - prevValue) / executedVolumeDelta;

		if (executedPrice is null)
		return;

		if (_activeDirection == 1)
		{
			if (executedVolumeDelta > 0m)
			{
				RegisterEntry(Math.Abs(executedVolumeDelta), executedPrice.Value);
			}
			else
			{
				HandleReduction(Math.Abs(executedVolumeDelta));
			}
		}
		else if (_activeDirection == -1)
		{
			if (executedVolumeDelta < 0m)
			{
				RegisterEntry(Math.Abs(executedVolumeDelta), executedPrice.Value);
			}
			else
			{
				HandleReduction(Math.Abs(executedVolumeDelta));
			}
		}
		else if (Position == 0m)
		{
			ClearEntries();
		}
	}

	private void RegisterEntry(decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		_entries.Add(new GridEntry(volume, price));

		if (_activeDirection == 1)
		{
			var stopPrice = price - _stopDistance;
			if (_bestBuyStop is null || stopPrice > _bestBuyStop)
			_bestBuyStop = stopPrice;
		}
		else if (_activeDirection == -1)
		{
			var stopPrice = price + _stopDistance;
			if (_bestSellStop is null || stopPrice < _bestSellStop)
			_bestSellStop = stopPrice;
		}
	}

	private void HandleReduction(decimal volume)
	{
		if (volume <= 0m)
		return;

		var remaining = volume;

		for (var i = _entries.Count - 1; i >= 0 && remaining > 0m; i--)
		{
			var entry = _entries[i];
			if (entry.Volume <= remaining)
			{
				remaining -= entry.Volume;
				_entries.RemoveAt(i);
			}
			else
			{
				_entries[i] = entry with { Volume = entry.Volume - remaining };
				remaining = 0m;
			}
		}

		if (_entries.Count == 0)
		{
			_bestBuyStop = null;
			_bestSellStop = null;
			return;
		}

		RecalculateBestStops();
	}

	private void RecalculateBestStops()
	{
		if (_activeDirection == 1)
		{
			decimal? best = null;
			foreach (var entry in _entries)
			{
				var stopPrice = entry.EntryPrice - _stopDistance;
				if (best is null || stopPrice > best)
				best = stopPrice;
			}
			_bestBuyStop = best;
		}
		else if (_activeDirection == -1)
		{
			decimal? best = null;
			foreach (var entry in _entries)
			{
				var stopPrice = entry.EntryPrice + _stopDistance;
				if (best is null || stopPrice < best)
				best = stopPrice;
			}
			_bestSellStop = best;
		}
	}

	private void TryStartCycle(decimal price)
	{
		UpdateStepSizes();

		if (_stopDistance <= 0m || _levelStep <= 0m)
		return;

		_cycleActive = true;
		_entries.Clear();
		_bestBuyStop = null;
		_bestSellStop = null;

		if (OpenBuy && !OpenSell)
		{
			_activeDirection = 1;
			PlaceInitialOrders(price, true);
		}
		else if (OpenSell && !OpenBuy)
		{
			_activeDirection = -1;
			PlaceInitialOrders(price, false);
		}
	}

	private void PlaceInitialOrders(decimal price, bool isBuy)
	{
		var firstVolume = _levelVolumes[0].Value;
		if (firstVolume > 0m)
		{
			if (isBuy)
			BuyMarket(firstVolume);
			else
			SellMarket(firstVolume);
		}

		for (var i = 1; i < LevelsCount; i++)
		{
			var levelVolume = _levelVolumes[i].Value;
			if (levelVolume <= 0m)
			continue;

			var levelOffset = _levelStep * i;
			if (isBuy)
			{
				var levelPrice = price + levelOffset;
				BuyStop(levelPrice, levelVolume);
			}
			else
			{
				var levelPrice = price - levelOffset;
				SellStop(levelPrice, levelVolume);
			}
		}
	}

	private void ApplyTrailing(decimal price)
	{
		if (_activeDirection == 1 && _bestBuyStop is decimal longStop && price <= longStop)
		{
			CancelActiveOrders();
			ClosePosition();
			FinishCycle(false);
		}
		else if (_activeDirection == -1 && _bestSellStop is decimal shortStop && price >= shortStop)
		{
			CancelActiveOrders();
			ClosePosition();
			FinishCycle(false);
		}
	}

	private void CheckTakeProfit(decimal price)
	{
		if (MoneyTakeProfit <= 0m)
		return;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		return;

		decimal pnl = 0m;

		if (Position > 0m)
		{
			pnl = (price - PositionPrice) / priceStep * stepPrice * Position;
		}
		else if (Position < 0m)
		{
			pnl = (PositionPrice - price) / priceStep * stepPrice * Math.Abs(Position);
		}

		if (pnl >= MoneyTakeProfit)
		{
			CancelActiveOrders();
			ClosePosition();
			FinishCycle(!TradeAgainAfterProfit);
		}
	}

	private void ClosePosition()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (Position > 0m)
		SellMarket(volume);
		else
		BuyMarket(volume);
	}

	private void FinishCycle(bool disableTrading)
	{
		ClearEntries();
		_cycleActive = false;
		_activeDirection = 0;

		if (disableTrading)
		_tradeEnabled = false;
	}

	private void ClearEntries()
	{
		_entries.Clear();
		_bestBuyStop = null;
		_bestSellStop = null;
	}

	private bool HasValidDirection()
	{
		return OpenBuy ^ OpenSell;
	}

	private void UpdateStepSizes()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return;

		_stopDistance = StopLossPoints * step;
		_levelStep = LevelDistance * step;
	}

	private bool AreActiveOrdersEmpty()
	{
		return ActiveOrders == null || ActiveOrders.Count == 0;
	}

	private readonly record struct GridEntry(decimal Volume, decimal EntryPrice);
}
