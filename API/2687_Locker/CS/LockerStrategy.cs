using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class LockerStrategy : Strategy
{
	private readonly struct PositionEntry
	{
		public PositionEntry(Sides side, decimal price, decimal volume)
		{
			Side = side;
			Price = price;
			Volume = volume;
		}

		public Sides Side { get; }
		public decimal Price { get; }
		public decimal Volume { get; }
	}

	private readonly StrategyParam<decimal> _profitTargetPercent;
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _stepVolume;
	private readonly StrategyParam<decimal> _stepPoints;
	private readonly StrategyParam<bool> _enableAutomation;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _entries = new();

	private decimal _realizedPnL;
	private decimal _lastEntryPrice;
	private Sides? _lastEntrySide;

	private const int MaxOpenPositions = 8;

	public decimal ProfitTargetPercent { get => _profitTargetPercent.Value; set => _profitTargetPercent.Value = value; }
	public decimal StartVolume { get => _startVolume.Value; set => _startVolume.Value = value; }
	public decimal StepVolume { get => _stepVolume.Value; set => _stepVolume.Value = value; }
	public decimal StepPoints { get => _stepPoints.Value; set => _stepPoints.Value = value; }
	public bool EnableAutomation { get => _enableAutomation.Value; set => _enableAutomation.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LockerStrategy()
	{
		_profitTargetPercent = Param(nameof(ProfitTargetPercent), 0.001m)
			.SetGreaterThanZero()
			.SetDisplay("Profit %", "Target profit percent of balance", "General")
			.SetCanOptimize();

		_startVolume = Param(nameof(StartVolume), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Start Volume", "Initial trade volume", "General")
			.SetCanOptimize();

		_stepVolume = Param(nameof(StepVolume), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Step Volume", "Volume for subsequent trades", "General")
			.SetCanOptimize();

		_stepPoints = Param(nameof(StepPoints), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Step Points", "Number of price steps between new trades", "General")
			.SetCanOptimize();

		_enableAutomation = Param(nameof(EnableAutomation), true)
			.SetDisplay("Enable Automation", "Allow the strategy to place trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_entries.Clear();
		_realizedPnL = 0m;
		_lastEntryPrice = 0m;
		_lastEntrySide = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		SubscribeCandles(CandleType).Bind(Process).Start();
	}

	private void Process(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!EnableAutomation)
			return;

		var closePrice = candle.ClosePrice;
		// Use the candle close as a proxy for bid/ask because we operate on finished bars.
		var bid = closePrice;
		var ask = closePrice;

		var currentProfit = _realizedPnL + CalculateUnrealizedProfit(bid, ask);
		var openCount = _entries.Count;

		if (openCount == 0)
		{
			// Start the grid with an initial buy order.
			OpenPosition(Sides.Buy, StartVolume, ask);
			return;
		}

		if (openCount >= MaxOpenPositions && TryClosePair(bid, ask))
		{
			// Reduce exposure when too many hedged orders are active.
			return;
		}

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		var targetProfit = portfolioValue * ProfitTargetPercent;

		if (targetProfit > 0m && currentProfit >= targetProfit)
		{
			// Target reached, flatten the book.
			CloseAllPositions(bid, ask);
			return;
		}

		if (targetProfit <= 0m)
			return;

		if (currentProfit <= -targetProfit)
		{
			var lastPrice = _lastEntryPrice;
			if (lastPrice == 0m)
				return;

			var stepDistance = GetStepDistance();
			if (stepDistance <= 0m)
				return;

			// Add a hedging order whenever price travels far enough from the latest entry.
			if (ask > lastPrice + stepDistance)
				OpenPosition(Sides.Sell, StepVolume, ask);
			else if (bid < lastPrice - stepDistance)
				OpenPosition(Sides.Buy, StepVolume, bid);
		}
	}

	private decimal CalculateUnrealizedProfit(decimal bid, decimal ask)
	{
		var profit = 0m;
		for (var i = 0; i < _entries.Count; i++)
		{
			var entry = _entries[i];
			var exitPrice = entry.Side == Sides.Buy ? bid : ask;
			var direction = entry.Side == Sides.Buy ? 1m : -1m;
			profit += (exitPrice - entry.Price) * direction * entry.Volume;
		}
		return profit;
	}

	private bool TryClosePair(decimal bid, decimal ask)
	{
		var buyIndex = -1;
		var sellIndex = -1;

		for (var i = 0; i < _entries.Count; i++)
		{
			var entry = _entries[i];
			if (entry.Side == Sides.Buy && buyIndex == -1)
				buyIndex = i;
			else if (entry.Side == Sides.Sell && sellIndex == -1)
				sellIndex = i;

			if (buyIndex != -1 && sellIndex != -1)
				break;
		}

		if (buyIndex == -1 || sellIndex == -1)
			return false;

		if (buyIndex > sellIndex)
		{
			CloseEntry(buyIndex, bid, ask);
			CloseEntry(sellIndex, bid, ask);
		}
		else
		{
			CloseEntry(sellIndex, bid, ask);
			CloseEntry(buyIndex, bid, ask);
		}

		UpdateLastEntry();
		return true;
	}

	private void CloseAllPositions(decimal bid, decimal ask)
	{
		for (var i = _entries.Count - 1; i >= 0; i--)
		{
			CloseEntry(i, bid, ask);
		}

		UpdateLastEntry();
	}

	private void CloseEntry(int index, decimal bid, decimal ask)
	{
		if (index < 0 || index >= _entries.Count)
			return;

		var entry = _entries[index];
		var exitPrice = entry.Side == Sides.Buy ? bid : ask;
		var direction = entry.Side == Sides.Buy ? Sides.Sell : Sides.Buy;

		// Send the offsetting market order to neutralize the entry.
		if (direction == Sides.Sell)
			SellMarket(entry.Volume);
		else
			BuyMarket(entry.Volume);

		var pnl = (exitPrice - entry.Price) * (entry.Side == Sides.Buy ? 1m : -1m) * entry.Volume;
		_realizedPnL += pnl;

		_entries.RemoveAt(index);
	}

	private void OpenPosition(Sides side, decimal volume, decimal price)
	{
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_entries.Add(new PositionEntry(side, price, volume));
		_lastEntryPrice = price;
		_lastEntrySide = side;
	}

	private decimal GetStepDistance()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		return priceStep > 0m ? StepPoints * priceStep : StepPoints;
	}

	private void UpdateLastEntry()
	{
		if (_entries.Count == 0)
		{
			_lastEntryPrice = 0m;
			_lastEntrySide = null;
			return;
		}

		var entry = _entries[_entries.Count - 1];
		_lastEntryPrice = entry.Price;
		_lastEntrySide = entry.Side;
	}
}
