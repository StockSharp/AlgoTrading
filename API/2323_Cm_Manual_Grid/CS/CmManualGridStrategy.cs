namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Simplified manual grid strategy.
/// </summary>
public class CmManualGridStrategy : Strategy
{
	private readonly StrategyParam<int> _buyStop, _sellStop, _buyLimit, _sellLimit, _first,
	_stepBuyStop, _stepSellStop, _stepBuyLimit, _stepSellLimit;
	private readonly StrategyParam<decimal> _lot, _lotPlus, _closeB, _closeS, _closeAll, _trailStart, _trailStep;
	
	private decimal _trailHigh;
	
	public int OrdersBuyStop { get => _buyStop.Value; set => _buyStop.Value = value; }
	public int OrdersSellStop { get => _sellStop.Value; set => _sellStop.Value = value; }
	public int OrdersBuyLimit { get => _buyLimit.Value; set => _buyLimit.Value = value; }
	public int OrdersSellLimit { get => _sellLimit.Value; set => _sellLimit.Value = value; }
	public int FirstLevel { get => _first.Value; set => _first.Value = value; }
	public int StepBuyStop { get => _stepBuyStop.Value; set => _stepBuyStop.Value = value; }
	public int StepSellStop { get => _stepSellStop.Value; set => _stepSellStop.Value = value; }
	public int StepBuyLimit { get => _stepBuyLimit.Value; set => _stepBuyLimit.Value = value; }
	public int StepSellLimit { get => _stepSellLimit.Value; set => _stepSellLimit.Value = value; }
	public decimal Lot { get => _lot.Value; set => _lot.Value = value; }
	public decimal LotPlus { get => _lotPlus.Value; set => _lotPlus.Value = value; }
	public decimal CloseProfitB { get => _closeB.Value; set => _closeB.Value = value; }
	public decimal CloseProfitS { get => _closeS.Value; set => _closeS.Value = value; }
	public decimal ProfitClose { get => _closeAll.Value; set => _closeAll.Value = value; }
	public decimal TralStart { get => _trailStart.Value; set => _trailStart.Value = value; }
	public decimal TralClose { get => _trailStep.Value; set => _trailStep.Value = value; }

	public CmManualGridStrategy()
{
	_buyStop = Param(nameof(OrdersBuyStop),5).SetDisplay("BuyStop","Count","Grid");
	_sellStop = Param(nameof(OrdersSellStop),5).SetDisplay("SellStop","Count","Grid");
	_buyLimit = Param(nameof(OrdersBuyLimit),5).SetDisplay("BuyLimit","Count","Grid");
	_sellLimit = Param(nameof(OrdersSellLimit),5).SetDisplay("SellLimit","Count","Grid");
	_first = Param(nameof(FirstLevel),5).SetDisplay("First Level","Distance","Grid");
	_stepBuyStop = Param(nameof(StepBuyStop),10).SetDisplay("Step BuyStop","Step","Grid");
	_stepSellStop = Param(nameof(StepSellStop),10).SetDisplay("Step SellStop","Step","Grid");
	_stepBuyLimit = Param(nameof(StepBuyLimit),10).SetDisplay("Step BuyLimit","Step","Grid");
	_stepSellLimit = Param(nameof(StepSellLimit),10).SetDisplay("Step SellLimit","Step","Grid");
	_lot = Param(nameof(Lot),0.1m).SetDisplay("Lot","Volume","Trading");
	_lotPlus = Param(nameof(LotPlus),0.1m).SetDisplay("Lot Plus","Increment","Trading");
	_closeB = Param(nameof(CloseProfitB),10m).SetDisplay("Close Buy","Profit","Profit");
	_closeS = Param(nameof(CloseProfitS),10m).SetDisplay("Close Sell","Profit","Profit");
	_closeAll = Param(nameof(ProfitClose),10m).SetDisplay("Close All","Profit","Profit");
	_trailStart = Param(nameof(TralStart),10m).SetDisplay("Trail Start","Profit","Profit");
	_trailStep = Param(nameof(TralClose),5m).SetDisplay("Trail Step","Distance","Profit");
}

	protected override void OnReseted()
{
	base.OnReseted();
	_trailHigh = 0m;
}

	protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);
	StartProtection();
	SubscribeTrades().Bind(ProcessTrade).Start();
}

	private void ProcessTrade(ITradeMessage t)
{
	int bs=0,ss=0,bl=0,sl=0; decimal pbs=0,pss=0,pbl=0,psl=0,vbs=0,vss=0,vbl=0,vsl=0;
	foreach (var o in ActiveOrders)
	{
	if (o.Type==OrderTypes.BuyStop){bs++; if(o.Price>pbs){pbs=o.Price; vbs=o.Volume;}}
	else if (o.Type==OrderTypes.SellStop){ss++; if(pss==0||o.Price<pss){pss=o.Price; vss=o.Volume;}}
	else if (o.Type==OrderTypes.BuyLimit){bl++; if(pbl==0||o.Price<pbl){pbl=o.Price; vbl=o.Volume;}}
	else if (o.Type==OrderTypes.SellLimit){sl++; if(o.Price>psl){psl=o.Price; vsl=o.Volume;}}
}
	var step=Security.PriceStep??1m;
	if(bs<OrdersBuyStop) BuyStop(bs==0?Lot:vbs+LotPlus, bs==0?t.Price+FirstLevel*step:pbs+StepBuyStop*step);
	if(ss<OrdersSellStop) SellStop(ss==0?Lot:vss+LotPlus, ss==0?t.Price-FirstLevel*step:pss-StepSellStop*step);
	if(bl<OrdersBuyLimit) BuyLimit(bl==0?Lot:vbl+LotPlus, bl==0?t.Price-FirstLevel*step:pbl-StepBuyLimit*step);
	if(sl<OrdersSellLimit) SellLimit(sl==0?Lot:vsl+LotPlus, sl==0?t.Price+FirstLevel*step:psl+StepSellLimit*step);

	if(Position>0 && PnL>=CloseProfitB) SellMarket(Position);
	else if(Position<0 && PnL>=CloseProfitS) BuyMarket(-Position);

	if(PnL>=ProfitClose){ CloseAll(); _trailHigh=0m; return; }

	if(PnL>=TralStart && _trailHigh==0m) _trailHigh=PnL;
	if(PnL>_trailHigh) _trailHigh=PnL;
	if(_trailHigh!=0m && PnL<=_trailHigh-TralClose){ CloseAll(); _trailHigh=0m; }
}

	private void CloseAll()
{
	CancelActiveOrders();
	if(Position>0) SellMarket(Position);
	else if(Position<0) BuyMarket(-Position);
}
}
