using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AutoFib breakout strategy for uptrend assets.
/// Calculates Fibonacci extension from recent swing high and low
/// and enters long when price breaks above the 1.618 level with
/// ATR based risk management.
/// </summary>
public class AutoFibBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _fibLevel;
	private readonly StrategyParam<int> _pivotPeriod;
	
	private decimal _fibBase;
	private decimal _fibTop;
	private decimal _entryPrice;
	private decimal _atrOnEntry;
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	
	/// <summary>
	/// Fibonacci extension level.
	/// </summary>
	public decimal FibLevel
	{
		get => _fibLevel.Value;
		set => _fibLevel.Value = value;
	}
	
	/// <summary>
	/// Period for swing high/low detection.
	/// </summary>
	public int PivotPeriod
	{
		get => _pivotPeriod.Value;
		set => _pivotPeriod.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="AutoFibBreakoutStrategy"/>.
	/// </summary>
	public AutoFibBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_emaLength = Param(nameof(EmaLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "Period for EMA trend filter", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(100, 300, 50);
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "Period for ATR", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 7);
		
		_fibLevel = Param(nameof(FibLevel), 1.618m)
		.SetGreaterThanZero()
		.SetDisplay("Fib Level", "Fibonacci extension level", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1.2m, 2.0m, 0.1m);
		
		_pivotPeriod = Param(nameof(PivotPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Pivot Period", "Period for swing high/low", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_fibBase = 0;
		_fibTop = 0;
		_entryPrice = 0;
		_atrOnEntry = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var highest = new Highest { Length = PivotPeriod };
		var lowest = new Lowest { Length = PivotPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(ema, atr, highest, lowest, ProcessCandle)
		.Start();
		
		StartProtection(new Unit(0), new Unit(0), useMarketOrders: true);
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		_fibTop = highest;
		_fibBase = lowest;
		var fibDiff = _fibTop - _fibBase;
		var fibTarget = _fibTop + fibDiff * (FibLevel - 1);
		
		if (Position <= 0 && candle.ClosePrice > fibTarget && candle.ClosePrice > emaValue)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_atrOnEntry = atrValue;
			LogInfo($"Long entry at {candle.ClosePrice}, fib target {fibTarget}");
			return;
		}
		
		if (Position > 0)
		{
			var stop = _entryPrice - _atrOnEntry;
			var target = _entryPrice + _atrOnEntry * 3m;
			
			if (candle.ClosePrice <= stop || candle.ClosePrice >= target)
			{
				SellMarket(Math.Abs(Position));
				var reason = candle.ClosePrice <= stop ? "stop" : "target";
				LogInfo($"Exit {reason} at {candle.ClosePrice}");
			}
		}
	}
}
