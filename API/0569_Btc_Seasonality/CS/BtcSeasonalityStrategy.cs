using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class BtcSeasonalityStrategy:Strategy
{
	private readonly StrategyParam<DayOfWeek> _entryDay,_exitDay;
	private readonly StrategyParam<int> _entryHour,_exitHour;
	private readonly StrategyParam<bool> _isLong;
	private readonly StrategyParam<DataType> _candleType;

	public DayOfWeek EntryDay{get=>_entryDay.Value;set=>_entryDay.Value=value;}
	public DayOfWeek ExitDay{get=>_exitDay.Value;set=>_exitDay.Value=value;}
	public int EntryHour{get=>_entryHour.Value;set=>_entryHour.Value=value;}
	public int ExitHour{get=>_exitHour.Value;set=>_exitHour.Value=value;}
	public bool IsLong{get=>_isLong.Value;set=>_isLong.Value=value;}
	public DataType CandleType{get=>_candleType.Value;set=>_candleType.Value=value;}

	public BtcSeasonalityStrategy()
	{
		_entryDay=Param(nameof(EntryDay),DayOfWeek.Saturday).SetDisplay("Entry Day","Day to enter trade (EST)","General");
		_exitDay=Param(nameof(ExitDay),DayOfWeek.Monday).SetDisplay("Exit Day","Day to exit trade (EST)","General");
		_entryHour=Param(nameof(EntryHour),10).SetRange(0,23).SetDisplay("Entry Hour","Entry hour in EST","General");
		_exitHour=Param(nameof(ExitHour),10).SetRange(0,23).SetDisplay("Exit Hour","Exit hour in EST","General");
		_isLong=Param(nameof(IsLong),true).SetDisplay("Long Trade","Open long if true, else short","General");
		_candleType=Param(nameof(CandleType),TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type","Type of candles","General");
	}

	public override IEnumerable<(Security,DataType)> GetWorkingSecurities(){return[(Security,CandleType)];}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var sub=SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if(candle.State!=CandleStates.Finished)return;
		var est=candle.OpenTime-TimeSpan.FromHours(5);
		var d=est.DayOfWeek;var h=est.Hour;
		if(d==EntryDay&&h==EntryHour)
		{
			if(IsLong)
			{
				if(Position<=0)RegisterBuy();
			}
			else
			{
				if(Position>=0)RegisterSell();
			}
		}
		if(d==ExitDay&&h==ExitHour)
		{
			if(Position>0)RegisterSell();
			else if(Position<0)RegisterBuy();
		}
	}
}
