using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class CustomBuyBidStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	
	private SuperTrend _supertrend;
	private bool _isAbove;
	
	public int SupertrendPeriod { get => _supertrendPeriod.Value; set => _supertrendPeriod.Value = value; }
	public decimal SupertrendMultiplier { get => _supertrendMultiplier.Value; set => _supertrendMultiplier.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }
	
	public CustomBuyBidStrategy()
	{
	_supertrendPeriod = Param(nameof(SupertrendPeriod),10).SetGreaterThanZero().SetDisplay("Supertrend Period","Period for ATR calculation in Supertrend","Indicator").SetCanOptimize(true).SetOptimize(5,20,5);
	_supertrendMultiplier = Param(nameof(SupertrendMultiplier),3m).SetGreaterThanZero().SetDisplay("Supertrend Multiplier","Multiplier for ATR in Supertrend","Indicator").SetCanOptimize(true).SetOptimize(1m,5m,0.5m);
	_takeProfitPercent = Param(nameof(TakeProfitPercent),5m).SetGreaterThanZero().SetDisplay("Take Profit (%)","Take profit percentage","Risk");
	_stopLossPercent = Param(nameof(StopLossPercent),2m).SetGreaterThanZero().SetDisplay("Stop Loss (%)","Stop loss percentage","Risk");
	_candleType = Param(nameof(CandleType),TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type","Type of candles to use","General");
	_startDate = Param(nameof(StartDate),new DateTimeOffset(new DateTime(2018,9,1))).SetDisplay("Start Date","Start date for trading","General");
	_endDate = Param(nameof(EndDate),new DateTimeOffset(new DateTime(9999,1,1))).SetDisplay("End Date","End date for trading","General");
	}
	
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security,CandleType)];
		
		protected override void OnReseted()
		{
		base.OnReseted();
		_isAbove = false;
		}
		
		protected override void OnStarted(DateTimeOffset time)
		{
		_supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };
		var sub = SubscribeCandles(CandleType);
		sub.Bind(_supertrend, Process).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, sub);
		DrawIndicator(area, _supertrend);
		DrawOwnTrades(area);
		}
		
		StartProtection(takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent), stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));
		base.OnStarted(time);
		}
		
		private void Process(ICandleMessage candle, decimal st)
		{
		if (candle.State != CandleStates.Finished)
		return;
		if (candle.OpenTime < StartDate || candle.OpenTime > EndDate)
		return;
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var isAbove = candle.ClosePrice > st;
		if (isAbove && !_isAbove && Position <= 0)
		BuyMarket();
		_isAbove = isAbove;
		}
		}
