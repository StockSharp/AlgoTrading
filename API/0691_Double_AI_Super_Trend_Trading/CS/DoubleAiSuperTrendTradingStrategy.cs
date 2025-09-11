using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double AI Super Trend Trading Strategy - combines two SuperTrend indicators with WMA filters.
/// </summary>
public class DoubleAiSuperTrendTradingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod1;
	private readonly StrategyParam<decimal> _atrFactor1;
	private readonly StrategyParam<int> _atrPeriod2;
	private readonly StrategyParam<decimal> _atrFactor2;
	private readonly StrategyParam<int> _priceWmaLength1;
	private readonly StrategyParam<int> _superWmaLength1;
	private readonly StrategyParam<int> _priceWmaLength2;
	private readonly StrategyParam<int> _superWmaLength2;
	private readonly StrategyParam<string> _tradeDirection;
	
	private SuperTrend _superTrend1;
	private SuperTrend _superTrend2;
	private AverageTrueRange _atr1;
	private AverageTrueRange _atr2;
	private WeightedMovingAverage _priceWma1;
	private WeightedMovingAverage _superWma1;
	private WeightedMovingAverage _priceWma2;
	private WeightedMovingAverage _superWma2;
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// ATR period for first SuperTrend.
	/// </summary>
	public int AtrPeriod1
	{
		get => _atrPeriod1.Value;
		set => _atrPeriod1.Value = value;
	}
	
	/// <summary>
	/// ATR factor for first SuperTrend.
	/// </summary>
	public decimal AtrFactor1
	{
		get => _atrFactor1.Value;
		set => _atrFactor1.Value = value;
	}
	
	/// <summary>
	/// ATR period for second SuperTrend.
	/// </summary>
	public int AtrPeriod2
	{
		get => _atrPeriod2.Value;
		set => _atrPeriod2.Value = value;
	}
	
	/// <summary>
	/// ATR factor for second SuperTrend.
	/// </summary>
	public decimal AtrFactor2
	{
		get => _atrFactor2.Value;
		set => _atrFactor2.Value = value;
	}
	
	/// <summary>
	/// Price WMA length for first SuperTrend.
	/// </summary>
	public int PriceWmaLength1
	{
		get => _priceWmaLength1.Value;
		set => _priceWmaLength1.Value = value;
	}
	
	/// <summary>
	/// SuperTrend WMA length for first SuperTrend.
	/// </summary>
	public int SuperWmaLength1
	{
		get => _superWmaLength1.Value;
		set => _superWmaLength1.Value = value;
	}
	
	/// <summary>
	/// Price WMA length for second SuperTrend.
	/// </summary>
	public int PriceWmaLength2
	{
		get => _priceWmaLength2.Value;
		set => _priceWmaLength2.Value = value;
	}
	
	/// <summary>
	/// SuperTrend WMA length for second SuperTrend.
	/// </summary>
	public int SuperWmaLength2
	{
		get => _superWmaLength2.Value;
		set => _superWmaLength2.Value = value;
	}
	
	/// <summary>
	/// Trading direction (Long/Short/Both).
	/// </summary>
	public string TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public DoubleAiSuperTrendTradingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_atrPeriod1 = Param(nameof(AtrPeriod1), 10)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period 1", "ATR period for first SuperTrend", "SuperTrend1");
		
		_atrFactor1 = Param(nameof(AtrFactor1), 4m)
		.SetRange(0.5m, 10m)
		.SetDisplay("ATR Factor 1", "ATR factor for first SuperTrend", "SuperTrend1");
		
		_atrPeriod2 = Param(nameof(AtrPeriod2), 5)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period 2", "ATR period for second SuperTrend", "SuperTrend2");
		
		_atrFactor2 = Param(nameof(AtrFactor2), 3m)
		.SetRange(0.5m, 10m)
		.SetDisplay("ATR Factor 2", "ATR factor for second SuperTrend", "SuperTrend2");
		
		_priceWmaLength1 = Param(nameof(PriceWmaLength1), 20)
		.SetGreaterThanZero()
		.SetDisplay("Price WMA Length 1", "Price WMA length for first SuperTrend", "AI");
		
		_superWmaLength1 = Param(nameof(SuperWmaLength1), 80)
		.SetGreaterThanZero()
		.SetDisplay("SuperTrend WMA Length 1", "SuperTrend WMA length for first SuperTrend", "AI");
		
		_priceWmaLength2 = Param(nameof(PriceWmaLength2), 40)
		.SetGreaterThanZero()
		.SetDisplay("Price WMA Length 2", "Price WMA length for second SuperTrend", "AI");
		
		_superWmaLength2 = Param(nameof(SuperWmaLength2), 80)
		.SetGreaterThanZero()
		.SetDisplay("SuperTrend WMA Length 2", "SuperTrend WMA length for second SuperTrend", "AI");
		
		_tradeDirection = Param(nameof(TradeDirection), "Both")
		.SetDisplay("Direction", "Trading direction (Long/Short/Both)", "Trading");
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_superTrend1 = new() { Length = AtrPeriod1, Multiplier = AtrFactor1 };
		_superTrend2 = new() { Length = AtrPeriod2, Multiplier = AtrFactor2 };
		_atr1 = new() { Length = AtrPeriod1 };
		_atr2 = new() { Length = AtrPeriod2 };
		_priceWma1 = new() { Length = PriceWmaLength1 };
		_superWma1 = new() { Length = SuperWmaLength1 };
		_priceWma2 = new() { Length = PriceWmaLength2 };
		_superWma2 = new() { Length = SuperWmaLength2 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_superTrend1, _atr1, _superTrend2, _atr2, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _superTrend1);
			DrawIndicator(area, _superTrend2);
			DrawIndicator(area, _priceWma1);
			DrawIndicator(area, _priceWma2);
			DrawIndicator(area, _superWma1);
			DrawIndicator(area, _superWma2);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal st1Value, decimal atr1Value, decimal st2Value, decimal atr2Value)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var priceWma1 = _priceWma1.Process(candle.ClosePrice, candle.ServerTime, true).ToDecimal();
		var superWma1 = _superWma1.Process(st1Value, candle.ServerTime, true).ToDecimal();
		var priceWma2 = _priceWma2.Process(candle.ClosePrice, candle.ServerTime, true).ToDecimal();
		var superWma2 = _superWma2.Process(st2Value, candle.ServerTime, true).ToDecimal();
		
		var isBull1 = priceWma1 > superWma1;
		var isBull2 = priceWma2 > superWma2;
		
		var dir1 = candle.ClosePrice > st1Value ? -1 : 1;
		var dir2 = candle.ClosePrice > st2Value ? -1 : 1;
		
		var longCondition = dir1 == -1 && isBull1 && dir2 == -1 && isBull2;
		var shortCondition = dir1 == 1 && !isBull1 && dir2 == 1 && !isBull2;
		
		var longExit = !(dir1 == -1 && isBull1 && dir2 == -1 && isBull2);
		var shortExit = !(dir1 == 1 && !isBull1 && dir2 == 1 && !isBull2);
		
		var longStop = st1Value - atr1Value * AtrFactor1;
		var shortStop = st1Value + atr1Value * AtrFactor1;
		
		if ((TradeDirection == "Long" || TradeDirection == "Both") && longCondition && Position <= 0)
		BuyMarket();
		
		if ((TradeDirection == "Short" || TradeDirection == "Both") && shortCondition && Position >= 0)
		SellMarket();
		
		if (Position > 0 && (longExit || candle.LowPrice <= longStop))
		SellMarket();
		
		if (Position < 0 && (shortExit || candle.HighPrice >= shortStop))
		BuyMarket();
	}
}
