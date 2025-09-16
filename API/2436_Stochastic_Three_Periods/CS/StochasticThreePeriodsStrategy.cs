using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
namespace StockSharp.Samples.Strategies;
/// <summary>
/// Stochastic alignment across three timeframes.
/// </summary>
public class StochasticThreePeriodsStrategy:Strategy
{
	private readonly StrategyParam<int> _k1,_k2p,_k3p,_kExit,_shift,_tp,_sl;
	private readonly StrategyParam<DataType> _t1,_t2,_t3;
	private readonly Queue<decimal> _kQ=new(),_dQ=new();
	private decimal _k2,_d2,_k3,_d3,_prevExitK,_prevExitD,_prevClose;
	private int _lastOrder; private bool _init;
	public int KPeriod1{get=>_k1.Value;set=>_k1.Value=value;}
	public int KPeriod2{get=>_k2p.Value;set=>_k2p.Value=value;}
	public int KPeriod3{get=>_k3p.Value;set=>_k3p.Value=value;}
	public int KExitPeriod{get=>_kExit.Value;set=>_kExit.Value=value;}
	public int ShiftEntrance{get=>_shift.Value;set=>_shift.Value=value;}
	public int TakeProfitPoints{get=>_tp.Value;set=>_tp.Value=value;}
	public int StopLossPoints{get=>_sl.Value;set=>_sl.Value=value;}
	public DataType CandleType1{get=>_t1.Value;set=>_t1.Value=value;}
	public DataType CandleType2{get=>_t2.Value;set=>_t2.Value=value;}
	public DataType CandleType3{get=>_t3.Value;set=>_t3.Value=value;}
	public StochasticThreePeriodsStrategy(){
		_k1=Param(nameof(KPeriod1),5).SetGreaterThanZero().SetDisplay("K1","Fast %K","General");
		_k2p=Param(nameof(KPeriod2),5).SetGreaterThanZero().SetDisplay("K2","Mid %K","General");
		_k3p=Param(nameof(KPeriod3),5).SetGreaterThanZero().SetDisplay("K3","Slow %K","General");
		_kExit=Param(nameof(KExitPeriod),5).SetGreaterThanZero().SetDisplay("K Exit","Exit %K","General");
		_shift=Param(nameof(ShiftEntrance),3).SetGreaterThanZero().SetDisplay("Shift","Bars back","Signals");
		_tp=Param(nameof(TakeProfitPoints),30).SetGreaterThanZero().SetDisplay("TP","Points","Risk");
		_sl=Param(nameof(StopLossPoints),10).SetGreaterThanZero().SetDisplay("SL","Points","Risk");
		_t1=Param(nameof(CandleType1),TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("TF1","Base","TF");
		_t2=Param(nameof(CandleType2),TimeSpan.FromMinutes(15).TimeFrame()).SetDisplay("TF2","Mid","TF");
		_t3=Param(nameof(CandleType3),TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("TF3","Slow","TF");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType1), (Security, CandleType2), (Security, CandleType3)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var st1=new StochasticOscillator{Length=KPeriod1};
		var st2=new StochasticOscillator{Length=KPeriod2};
		var st3=new StochasticOscillator{Length=KPeriod3};
		var stExit=new StochasticOscillator{Length=KExitPeriod};
		SubscribeCandles(CandleType2).BindEx(st2,ProcessTf2).Start();
		SubscribeCandles(CandleType3).BindEx(st3,ProcessTf3).Start();
		var sub1=SubscribeCandles(CandleType1);
		sub1.BindEx(st1,stExit,ProcessTf1).Start();
		var area=CreateChartArea();
		if(area!=null){DrawCandles(area,sub1);DrawIndicator(area,st1);DrawIndicator(area,st2);DrawIndicator(area,st3);DrawOwnTrades(area);}
		var step=Security.PriceStep??1m;
		StartProtection(takeProfit:new Unit(TakeProfitPoints*step,UnitTypes.Point),stopLoss:new Unit(StopLossPoints*step,UnitTypes.Point));
	}

	private void ProcessTf2(ICandleMessage c,IIndicatorValue v)
	{
		if(c.State!=CandleStates.Finished)return;
		var val=(StochasticOscillatorValue)v;
		if(val.K is not decimal k||val.D is not decimal d)return;
		_k2=k;_d2=d;
	}
	private void ProcessTf3(ICandleMessage c,IIndicatorValue v)
	{
		if(c.State!=CandleStates.Finished)return;
		var val=(StochasticOscillatorValue)v;
		if(val.K is not decimal k||val.D is not decimal d)return;
		_k3=k;_d3=d;
	}

	private void ProcessTf1(ICandleMessage c,IIndicatorValue sv,IIndicatorValue ev)
	{
		if(c.State!=CandleStates.Finished)return;
		if(!IsFormedAndOnlineAndAllowTrading())return;
		var st=(StochasticOscillatorValue)sv;
		var ex=(StochasticOscillatorValue)ev;
		if(st.K is not decimal k1||st.D is not decimal d1)return;
		if(ex.K is not decimal exK||ex.D is not decimal exD)return;
		var cap=ShiftEntrance+1;
		_kQ.Enqueue(k1);_dQ.Enqueue(d1);
		if(_kQ.Count>cap)_kQ.Dequeue();
		if(_dQ.Count>cap)_dQ.Dequeue();
		var dir=0;
		if(_prevExitK>_prevExitD)dir=1;else if(_prevExitK<_prevExitD)dir=2;
		if(Position>0&&dir==2)SellMarket(Position);
		else if(Position<0&&dir==1)BuyMarket(-Position);
		if(_init&&_kQ.Count==cap&&Position==0){var kShift=_kQ.Peek();var dShift=_dQ.Peek();var buy=k1>d1&&kShift<dShift&&_k2>_d2&&_k3>_d3&&c.ClosePrice>_prevClose;var sell=k1<d1&&kShift>dShift&&_k2<_d2&&_k3<_d3&&c.ClosePrice<_prevClose;if(buy&&_lastOrder!=1){BuyMarket(Volume);_lastOrder=1;}else if(sell&&_lastOrder!=2){SellMarket(Volume);_lastOrder=2;}}
		_prevExitK=exK;_prevExitD=exD;_prevClose=c.ClosePrice;_init=true;
	}
}
