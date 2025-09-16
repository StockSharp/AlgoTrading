using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class PriceRollbackStrategy : Strategy
{
	// Parameters and state
	readonly StrategyParam<decimal> _corridor,_stopLoss,_takeProfit,_trailingStop,_trailingStep;
	readonly StrategyParam<int> _day;
	readonly StrategyParam<DataType> _type;
	readonly Queue<decimal> _opens = new();
	decimal _prevClose,_entry,_stop;

	public decimal Corridor{get=>_corridor.Value;set=>_corridor.Value=value;}
	public decimal StopLoss{get=>_stopLoss.Value;set=>_stopLoss.Value=value;}
	public decimal TakeProfit{get=>_takeProfit.Value;set=>_takeProfit.Value=value;}
	public decimal TrailingStop{get=>_trailingStop.Value;set=>_trailingStop.Value=value;}
	public decimal TrailingStep{get=>_trailingStep.Value;set=>_trailingStep.Value=value;}
	public int TradingDay{get=>_day.Value;set=>_day.Value=value;}
	public DataType CandleType{get=>_type.Value;set=>_type.Value=value;}

	public PriceRollbackStrategy()
	{
		_corridor=Param(nameof(Corridor),1m).SetDisplay("Corridor","Gap","General");
		_stopLoss=Param(nameof(StopLoss),50m).SetDisplay("Stop","Stop","Risk");
		_takeProfit=Param(nameof(TakeProfit),50m).SetDisplay("Take","Target","Risk");
		_trailingStop=Param(nameof(TrailingStop),5m).SetDisplay("Trail","Trail","Risk");
		_trailingStep=Param(nameof(TrailingStep),5m).SetDisplay("Step","Step","Risk");
		_day=Param(nameof(TradingDay),5).SetDisplay("Day","0=Sun","Time");
		_type=Param(nameof(CandleType),TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Type","Frame","General");
	}

	public override IEnumerable<(Security,DataType)> GetWorkingSecurities() => [(Security,CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_opens.Clear(); _prevClose=_entry=_stop=0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();
		SubscribeCandles(CandleType).Bind(Process).Start();
	}

	void Process(ICandleMessage c)
	{
		var t=c.CloseTime;
		if(Position!=0 && t.Hour==22 && t.Minute>=45) Close();

		// Open positions at day start if gap exceeds corridor
		if(c.State==CandleStates.Active)
		{
			if(!IsFormedAndOnlineAndAllowTrading()) return;
			var o=c.OpenTime;
			if(_opens.Count==24 && Position==0 && o.Hour==0 && o.Minute<=3 && o.DayOfWeek==(DayOfWeek)TradingDay)
			{
				var o24=_opens.Peek();
				if(o24-_prevClose>Corridor){_entry=c.OpenPrice;_stop=_entry-StopLoss;BuyMarket();}
				else if(_prevClose-o24>Corridor){_entry=c.OpenPrice;_stop=_entry+StopLoss;SellMarket();}
			}
			return;
		}

		if(c.State!=CandleStates.Finished) return;
		if(_opens.Count==24) _opens.Dequeue();
		_opens.Enqueue(c.OpenPrice);
		_prevClose=c.ClosePrice;
		if(!IsFormedAndOnlineAndAllowTrading()) return;

		if(Position>0)
		{
			var p=c.ClosePrice-_entry;
			if(p>TrailingStop+TrailingStep && _stop<c.ClosePrice-(TrailingStop+TrailingStep)) _stop=c.ClosePrice-TrailingStop;
			if(c.ClosePrice<=_stop || c.ClosePrice>=_entry+TakeProfit) Close();
		}
		else if(Position<0)
		{
			var p=_entry-c.ClosePrice;
			if(p>TrailingStop+TrailingStep && (_stop==0 || _stop>c.ClosePrice+(TrailingStop+TrailingStep))) _stop=c.ClosePrice+TrailingStop;
			if(c.ClosePrice>=_stop || c.ClosePrice<=_entry-TakeProfit) Close();
		}
	}

	void Close()
	{
		if(Position>0) SellMarket(Position);
		else if(Position<0) BuyMarket(-Position);
		_entry=_stop=0;
	}
}
