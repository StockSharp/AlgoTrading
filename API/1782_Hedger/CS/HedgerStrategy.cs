using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending-order hedging strategy with optional risk management.
/// Supports both long and short modes.
/// </summary>
public class HedgerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryPrice;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _spread;
	private readonly StrategyParam<bool> _isLong;
	private readonly StrategyParam<bool> _useRiskHedge;
	private readonly StrategyParam<bool> _useRiskSl;
	private readonly StrategyParam<int> _riskSlTicks;
	private readonly StrategyParam<bool> _useRule7550;
	
	private Order _entryOrder = null!;
	private Order _hedgeOrder = null!;
	private Order _stopOrder = null!;
	private Order _takeProfitOrder = null!;
	private bool _protectivePlaced;
	private bool _riskHedgeOpened;
	private bool _riskSlApplied;
	private bool _ruleApplied;
	
	public HedgerStrategy()
	{
		_entryPrice = Param(nameof(EntryPrice), 0m)
		.SetDisplay("Entry Price", "Price for limit order", "Trading")
		.SetCanOptimize(true);
		
		_stopLoss = Param(nameof(StopLoss), 0m)
		.SetDisplay("Stop Loss", "Protective stop price", "Trading")
		.SetCanOptimize(true);
		
		_takeProfit = Param(nameof(TakeProfit), 0m)
		.SetDisplay("Take Profit", "Target profit price", "Trading")
		.SetCanOptimize(true);
		
		_volume = Param(nameof(Volume), 1m)
		.SetDisplay("Volume", "Order volume", "Trading")
		.SetCanOptimize(true);
		
		_spread = Param(nameof(Spread), 0m)
		.SetDisplay("Spread", "Price offset for hedge", "Trading")
		.SetCanOptimize(true);
		
		_isLong = Param(nameof(IsLong), true)
		.SetDisplay("Is Long", "Trade long when true, short otherwise", "General")
		.SetCanOptimize(true);
		
		_useRiskHedge = Param(nameof(UseRiskHedge), false)
		.SetDisplay("Use Risk Hedge", "Hedge after adverse move", "Risk")
		.SetCanOptimize(true);
		
		_useRiskSl = Param(nameof(UseRiskSl), true)
		.SetDisplay("Use Risk SL", "Tighten stop after adverse move", "Risk")
		.SetCanOptimize(true);
		
		_riskSlTicks = Param(nameof(RiskSlTicks), 100)
		.SetDisplay("Risk SL Ticks", "Ticks to tighten stop", "Risk")
		.SetCanOptimize(true);
		
		_useRule7550 = Param(nameof(UseRule7550), false)
		.SetDisplay("Use 75-50 Rule", "Move stop to 50% after 75% gain", "Rules")
		.SetCanOptimize(true);
	}
	
	public decimal EntryPrice { get => _entryPrice.Value; set => _entryPrice.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public decimal Spread { get => _spread.Value; set => _spread.Value = value; }
	public bool IsLong { get => _isLong.Value; set => _isLong.Value = value; }
	public bool UseRiskHedge { get => _useRiskHedge.Value; set => _useRiskHedge.Value = value; }
	public bool UseRiskSl { get => _useRiskSl.Value; set => _useRiskSl.Value = value; }
	public int RiskSlTicks { get => _riskSlTicks.Value; set => _riskSlTicks.Value = value; }
	public bool UseRule7550 { get => _useRule7550.Value; set => _useRule7550.Value = value; }
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, DataType.Ticks)];
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		RegisterEntryOrders();
		
		SubscribeTrades().Bind(ProcessTrade).Start();
	}
	
	private void RegisterEntryOrders()
	{
		if (IsLong)
		{
		_entryOrder = BuyLimit(Volume, EntryPrice);
		_hedgeOrder = SellStop(Volume, EntryPrice - Spread);
		}
		else
		{
		_entryOrder = SellLimit(Volume, EntryPrice);
		_hedgeOrder = BuyStop(Volume, EntryPrice + Spread);
		}
	}
	
	private void PlaceProtectiveOrders()
	{
		if (_protectivePlaced || Position == 0)
		return;
		
		if (IsLong)
		{
		_stopOrder = SellStop(Position, StopLoss);
		_takeProfitOrder = SellLimit(Position, TakeProfit);
		}
		else
		{
		_stopOrder = BuyStop(-Position, StopLoss);
		_takeProfitOrder = BuyLimit(-Position, TakeProfit);
		}
		
		_protectivePlaced = true;
		}
		
	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;
		
		PlaceProtectiveOrders();
		
		if (UseRule7550 && !_ruleApplied && Position != 0)
		{
		if (IsLong)
		{
		var target = EntryPrice + 0.75m * (TakeProfit - EntryPrice);
		if (price >= target)
		{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		CancelOrder(_stopOrder);
		var newStop = EntryPrice + 0.5m * (TakeProfit - EntryPrice);
		_stopOrder = SellStop(Position, newStop);
		_ruleApplied = true;
		}
		}
		else
		{
		var target = EntryPrice - 0.75m * (EntryPrice - TakeProfit);
		if (price <= target)
		{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		CancelOrder(_stopOrder);
		var newStop = EntryPrice - 0.5m * (EntryPrice - TakeProfit);
		_stopOrder = BuyStop(-Position, newStop);
		_ruleApplied = true;
		}
		}
		}
		
		if (UseRiskHedge && !_riskHedgeOpened && Position != 0)
		{
		if (IsLong && price < EntryPrice - 3m * Spread)
		{
		SellMarket(Volume);
		_riskHedgeOpened = true;
		}
		else if (!IsLong && price > EntryPrice + 3m * Spread)
		{
		BuyMarket(Volume);
		_riskHedgeOpened = true;
		}
		}
		
		if (UseRiskSl && !_riskSlApplied && Position != 0)
		{
		if (IsLong && price < EntryPrice - RiskSlTicks * Spread)
		{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		CancelOrder(_stopOrder);
		var newStop = EntryPrice - RiskSlTicks * Spread;
		_stopOrder = SellStop(Position, newStop);
		_riskSlApplied = true;
		}
		else if (!IsLong && price > EntryPrice + RiskSlTicks * Spread)
		{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		CancelOrder(_stopOrder);
		var newStop = EntryPrice + RiskSlTicks * Spread;
		_stopOrder = BuyStop(-Position, newStop);
		_riskSlApplied = true;
		}
		}
	}
}
