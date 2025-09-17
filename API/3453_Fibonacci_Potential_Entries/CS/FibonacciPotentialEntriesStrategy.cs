using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reproduces the Fibonacci Potential Entries expert advisor.
/// </summary>
public class FibonacciPotentialEntriesStrategy : Strategy
{
	private const decimal FirstTradeRiskShare = 0.7m;
	private const decimal SpreadMultiplier = 3m;
	
	private readonly StrategyParam<decimal> _priceOn50Level;
	private readonly StrategyParam<decimal> _priceOn61Level;
	private readonly StrategyParam<decimal> _priceOn100Level;
	private readonly StrategyParam<decimal> _targetPrice;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _isBullish;
	
	private readonly TradeSlot _firstTrade = new();
	private readonly TradeSlot _secondTrade = new();
	
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private bool _ordersPlaced;
	private bool _targetHandled;
	
	/// <summary>
	/// Price of the 50% Fibonacci retracement level.
	/// </summary>
	public decimal PriceOn50Level
	{
		get => _priceOn50Level.Value;
		set => _priceOn50Level.Value = value;
	}
	
	/// <summary>
	/// Price of the 61% Fibonacci retracement level.
	/// </summary>
	public decimal PriceOn61Level
	{
		get => _priceOn61Level.Value;
		set => _priceOn61Level.Value = value;
	}
	
	/// <summary>
	/// Price of the 100% Fibonacci retracement level.
	/// </summary>
	public decimal PriceOn100Level
	{
		get => _priceOn100Level.Value;
		set => _priceOn100Level.Value = value;
	}
	
	/// <summary>
	/// Common profit target for both trades.
	/// </summary>
	public decimal TargetPrice
	{
		get => _targetPrice.Value;
		set => _targetPrice.Value = value;
	}
	
	/// <summary>
	/// Total risk per cycle expressed as a portfolio percentage.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}
	
	/// <summary>
	/// Chooses between bullish (long) and bearish (short) setups.
	/// </summary>
	public bool IsBullish
	{
		get => _isBullish.Value;
		set => _isBullish.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FibonacciPotentialEntriesStrategy"/> class.
	/// </summary>
	public FibonacciPotentialEntriesStrategy()
	{
		_priceOn50Level = Param(nameof(PriceOn50Level), 1.08261m)
			.SetDisplay("50% Level", "Price on the 50% retracement level", "General");
		
		_priceOn61Level = Param(nameof(PriceOn61Level), 1.07811m)
			.SetDisplay("61% Level", "Price on the 61% retracement level", "General");
		
		_priceOn100Level = Param(nameof(PriceOn100Level), 1.06370m)
			.SetDisplay("100% Level", "Price on the 100% retracement level", "General");
		
		_targetPrice = Param(nameof(TargetPrice), 1.10178m)
			.SetDisplay("Target", "Profit target shared by both entries", "Risk Management");
		
		_riskPercent = Param(nameof(RiskPercent), 2m)
			.SetDisplay("Risk %", "Total risk per cycle expressed in percent", "Risk Management")
			.SetGreaterThanZero();
		
		_isBullish = Param(nameof(IsBullish), true)
			.SetDisplay("Bullish Setup", "Set to true for buy limits, false for sell limits", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_firstTrade.Reset();
		_secondTrade.Reset();
		_bestBid = null;
		_bestAsk = null;
		_ordersPlaced = false;
		_targetHandled = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_firstTrade.EntryPrice = PriceOn50Level;
		_secondTrade.EntryPrice = PriceOn61Level;
		_firstTrade.EntrySide = IsBullish ? Sides.Buy : Sides.Sell;
		_secondTrade.EntrySide = IsBullish ? Sides.Buy : Sides.Sell;
		
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}
	
	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);
		
		ProcessTradeSlot(_firstTrade, trade);
		ProcessTradeSlot(_secondTrade, trade);
	}
	
	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
			_bestBid = bid;
		
		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
			_bestAsk = ask;
		
		TryPlaceInitialOrders();
		TryHandleTargets();
		TryHandleBreakEven();
	}
	
	private void TryPlaceInitialOrders()
	{
		if (_ordersPlaced)
			return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
			return;
		
		var spread = ask - bid;
		if (spread <= 0m)
			spread = Security.PriceStep ?? 0m;
		
		var firstStop = PriceOn61Level - SpreadMultiplier * spread;
		var secondStop = (PriceOn61Level + PriceOn100Level) / 2m - SpreadMultiplier * spread;
		
		if (!IsBullish)
		{
			firstStop = PriceOn61Level + SpreadMultiplier * spread;
			secondStop = (PriceOn61Level + PriceOn100Level) / 2m + SpreadMultiplier * spread;
		}
		
		var firstRiskShare = Math.Min(RiskPercent, FirstTradeRiskShare);
		var secondRiskShare = Math.Max(0m, RiskPercent - FirstTradeRiskShare);
		
		var firstVolume = CalculateVolume(PriceOn50Level, firstStop, firstRiskShare);
		var secondVolume = CalculateVolume(PriceOn61Level, secondStop, secondRiskShare);
		
		if (firstVolume > 0m)
		{
			_firstTrade.ExpectedVolume = firstVolume;
			
			if (IsBullish)
				BuyLimit(NormalizePrice(PriceOn50Level), firstVolume);
			else
				SellLimit(NormalizePrice(PriceOn50Level), firstVolume);
		}
		
		if (secondVolume > 0m)
		{
			_secondTrade.ExpectedVolume = secondVolume;
			
			if (IsBullish)
				BuyLimit(NormalizePrice(PriceOn61Level), secondVolume);
			else
				SellLimit(NormalizePrice(PriceOn61Level), secondVolume);
		}
		
		_ordersPlaced = _firstTrade.ExpectedVolume > 0m || _secondTrade.ExpectedVolume > 0m;
	}
	
	private void TryHandleTargets()
	{
		if (_targetHandled)
			return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		var target = TargetPrice;
		if (target <= 0m)
			return;
		
		var targetHit = IsBullish
			? GetBestAsk(target) >= target
			: GetBestBid(target) <= target;
		
		if (!targetHit)
			return;
		
		var anyExecuted = CloseHalf(_firstTrade) | CloseHalf(_secondTrade);
		
		if (anyExecuted)
			_targetHandled = true;
	}
	
	private void TryHandleBreakEven()
	{
		HandleBreakEven(_firstTrade);
		HandleBreakEven(_secondTrade);
	}
	
	private bool CloseHalf(TradeSlot slot)
	{
		if (slot.FilledVolume <= 0m || slot.PartialClosed || slot.RemainingVolume <= 0m)
			return false;
		
		var halfVolume = NormalizeVolume(slot.RemainingVolume / 2m);
		if (halfVolume <= 0m)
			return false;
		
		if (slot.EntrySide == Sides.Buy)
			SellMarket(halfVolume);
		else
			BuyMarket(halfVolume);
		
		slot.PartialClosed = true;
		slot.BreakEvenPrice = slot.EntryPrice;
		
		return true;
	}
	
	private void HandleBreakEven(TradeSlot slot)
	{
		if (!slot.PartialClosed || slot.RemainingVolume <= 0m || slot.BreakEvenPrice is not decimal breakEven)
			return;
		
		if (slot.EntrySide == Sides.Buy)
		{
			if (GetBestBid(breakEven) <= breakEven)
			{
				SellMarket(NormalizeVolume(slot.RemainingVolume));
				slot.BreakEvenPrice = null;
			}
		}
		else
		{
			if (GetBestAsk(breakEven) >= breakEven)
			{
				BuyMarket(NormalizeVolume(slot.RemainingVolume));
				slot.BreakEvenPrice = null;
			}
		}
	}
	
	private void ProcessTradeSlot(TradeSlot slot, MyTrade trade)
	{
		if (slot.EntryPrice <= 0m)
			return;
		
		var tolerance = Security.PriceStep ?? 0.0001m;
		var volume = trade.Trade.Volume;
		
		if (trade.Order.Direction == slot.EntrySide)
		{
			if (IsNear(trade.Order.Price, slot.EntryPrice, tolerance))
			{
				slot.FilledVolume += volume;
				slot.RemainingVolume += volume;
			}
		}
		else if (slot.RemainingVolume > 0m)
		{
			var toReduce = Math.Min(slot.RemainingVolume, volume);
			slot.RemainingVolume = Math.Max(0m, slot.RemainingVolume - toReduce);
			
			if (slot.RemainingVolume <= 0m)
			{
				slot.BreakEvenPrice = null;
			}
		}
	}
	
	private decimal CalculateVolume(decimal entryPrice, decimal stopPrice, decimal riskShare)
	{
		if (riskShare <= 0m)
			return 0m;
		
		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
		if (balance is null or <= 0m)
			return 0m;
		
		var priceStep = Security.PriceStep;
		var stepPrice = Security.StepPrice;
		
		if (priceStep is null or <= 0m || stepPrice is null or <= 0m)
			return 0m;
		
		var distance = Math.Abs(entryPrice - stopPrice);
		if (distance <= 0m)
			return 0m;
		
		var pipValue = stepPrice.Value / priceStep.Value;
		if (pipValue <= 0m)
			return 0m;
		
		var riskAmount = balance.Value * riskShare / 100m;
		if (riskAmount <= 0m)
			return 0m;
		
		var rawVolume = riskAmount / (distance * pipValue);
		return NormalizeVolume(rawVolume);
	}
	
	private decimal NormalizePrice(decimal price)
	{
		var step = Security.PriceStep;
		if (step is null or <= 0m)
			return price;
		
		return Math.Round(price / step.Value) * step.Value;
	}
	
	private decimal NormalizeVolume(decimal volume)
	{
		var step = Security.VolumeStep;
		var minVolume = Security.MinVolume ?? step;
		
		if (step is null or <= 0m)
			return volume;
		
		var normalized = Math.Floor(volume / step.Value) * step.Value;
		
		if (minVolume is not null && normalized < minVolume)
			return 0m;
		
		return normalized;
	}
	
	private decimal GetBestBid(decimal fallback)
	{
		return _bestBid ?? fallback;
	}
	
	private decimal GetBestAsk(decimal fallback)
	{
		return _bestAsk ?? fallback;
	}
	
	private static bool IsNear(decimal value, decimal reference, decimal tolerance)
	{
		return Math.Abs(value - reference) <= tolerance;
	}
	
	private sealed class TradeSlot
	{
		public decimal EntryPrice { get; set; }
		public Sides EntrySide { get; set; }
		public decimal ExpectedVolume { get; set; }
		public decimal FilledVolume { get; set; }
		public decimal RemainingVolume { get; set; }
		public bool PartialClosed { get; set; }
		public decimal? BreakEvenPrice { get; set; }
		
		public void Reset()
		{
			EntryPrice = 0m;
			EntrySide = Sides.Buy;
			ExpectedVolume = 0m;
			FilledVolume = 0m;
			RemainingVolume = 0m;
			PartialClosed = false;
			BreakEvenPrice = null;
		}
	}
}
