using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple Fibonacci retracement strategy.
/// Calculates retracement levels from recent high/low and trades on price crosses.
/// </summary>
public class SimpleFibonacciRetracementStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<FibDirection> _fibDirection;
	private readonly StrategyParam<decimal> _fibLevel236;
	private readonly StrategyParam<decimal> _fibLevel382;
	private readonly StrategyParam<decimal> _fibLevel50;
	private readonly StrategyParam<decimal> _fibLevel618;
	private readonly StrategyParam<FibLevel> _buyEntryLevel;
	private readonly StrategyParam<FibLevel> _sellEntryLevel;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<DataType> _candleType;
	
	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal? _prevClose;
	private decimal _stopLoss;
	private decimal _takeProfit;
	
	/// <summary>
	/// Lookback period for finding highest and lowest prices.
	/// </summary>
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	
	/// <summary>
	/// Direction for Fibonacci calculation.
	/// </summary>
	public FibDirection Direction { get => _fibDirection.Value; set => _fibDirection.Value = value; }
	
	/// <summary>
	/// 23.6% Fibonacci level.
	/// </summary>
	public decimal FibLevel236 { get => _fibLevel236.Value; set => _fibLevel236.Value = value; }
	
	/// <summary>
	/// 38.2% Fibonacci level.
	/// </summary>
	public decimal FibLevel382 { get => _fibLevel382.Value; set => _fibLevel382.Value = value; }
	
	/// <summary>
	/// 50% Fibonacci level.
	/// </summary>
	public decimal FibLevel50 { get => _fibLevel50.Value; set => _fibLevel50.Value = value; }
	
	/// <summary>
	/// 61.8% Fibonacci level.
	/// </summary>
	public decimal FibLevel618 { get => _fibLevel618.Value; set => _fibLevel618.Value = value; }
	
	/// <summary>
	/// Entry level for long positions.
	/// </summary>
	public FibLevel BuyEntryLevel { get => _buyEntryLevel.Value; set => _buyEntryLevel.Value = value; }
	
	/// <summary>
	/// Entry level for short positions.
	/// </summary>
	public FibLevel SellEntryLevel { get => _sellEntryLevel.Value; set => _sellEntryLevel.Value = value; }
	
	/// <summary>
	/// Take profit in pips.
	/// </summary>
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	
	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SimpleFibonacciRetracementStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("Lookback Period", "Period for highest/lowest", "General")
		.SetCanOptimize(true);
		
		_fibDirection = Param(nameof(Direction), FibDirection.TopToBottom)
		.SetDisplay("Fibonacci Direction", "Direction of calculation", "General")
		.SetCanOptimize(true);
		
		_fibLevel236 = Param(nameof(FibLevel236), 0.236m)
		.SetDisplay("Fib 23.6%", "Fibonacci 23.6% level", "Fibonacci")
		.SetCanOptimize(true);
		
		_fibLevel382 = Param(nameof(FibLevel382), 0.382m)
		.SetDisplay("Fib 38.2%", "Fibonacci 38.2% level", "Fibonacci")
		.SetCanOptimize(true);
		
		_fibLevel50 = Param(nameof(FibLevel50), 0.5m)
		.SetDisplay("Fib 50%", "Fibonacci 50% level", "Fibonacci")
		.SetCanOptimize(true);
		
		_fibLevel618 = Param(nameof(FibLevel618), 0.618m)
		.SetDisplay("Fib 61.8%", "Fibonacci 61.8% level", "Fibonacci")
		.SetCanOptimize(true);
		
		_buyEntryLevel = Param(nameof(BuyEntryLevel), FibLevel.Fib618)
		.SetDisplay("Buy Entry Level", "Fibonacci level for longs", "Entries")
		.SetCanOptimize(true);
		
		_sellEntryLevel = Param(nameof(SellEntryLevel), FibLevel.Fib382)
		.SetDisplay("Sell Entry Level", "Fibonacci level for shorts", "Entries")
		.SetCanOptimize(true);
		
		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Take profit in pips", "Risk")
		.SetCanOptimize(true);
		
		_stopLossPips = Param(nameof(StopLossPips), 20)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Stop loss in pips", "Risk")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevClose = null;
		_stopLoss = 0m;
		_takeProfit = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_highest = new Highest { Length = LookbackPeriod };
		_lowest = new Lowest { Length = LookbackPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (!_highest.IsFormed || !_lowest.IsFormed)
		return;
		
		var high = highest;
		var low = lowest;
		
		decimal fib0;
		decimal fib100;
		decimal fib236;
		decimal fib382;
		decimal fib50;
		decimal fib618;
		
		if (Direction == FibDirection.TopToBottom)
		{
			fib0 = high;
			fib100 = low;
			var range = high - low;
			fib236 = high - range * FibLevel236;
			fib382 = high - range * FibLevel382;
			fib50 = high - range * FibLevel50;
			fib618 = high - range * FibLevel618;
		}
		else
		{
			fib0 = low;
			fib100 = high;
			var range = high - low;
			fib236 = low + range * FibLevel236;
			fib382 = low + range * FibLevel382;
			fib50 = low + range * FibLevel50;
			fib618 = low + range * FibLevel618;
		}
		
		var buyLevel = GetLevel(BuyEntryLevel, fib236, fib382, fib50, fib618);
		var sellLevel = GetLevel(SellEntryLevel, fib236, fib382, fib50, fib618);
		
		var longSignal = false;
		var shortSignal = false;
		
		if (_prevClose is decimal prev)
		{
			longSignal = prev <= buyLevel && candle.ClosePrice > buyLevel;
			shortSignal = prev >= sellLevel && candle.ClosePrice < sellLevel;
		}
		
		_prevClose = candle.ClosePrice;
		
		var pipValue = Security.Step * 10m;
		var tpOffset = TakeProfitPips * pipValue;
		var slOffset = StopLossPips * pipValue;
		
		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopLoss = candle.ClosePrice - slOffset;
			_takeProfit = candle.ClosePrice + tpOffset;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopLoss = candle.ClosePrice + slOffset;
			_takeProfit = candle.ClosePrice - tpOffset;
		}
		
		var price = candle.ClosePrice;
		
		if (Position > 0)
		{
			if (price <= _stopLoss || price >= _takeProfit)
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (price >= _stopLoss || price <= _takeProfit)
			BuyMarket(Math.Abs(Position));
		}
	}
	
	private static decimal GetLevel(FibLevel level, decimal fib236, decimal fib382, decimal fib50, decimal fib618)
	{
		return level switch
		{
			FibLevel.Fib236 => fib236,
			FibLevel.Fib382 => fib382,
			FibLevel.Fib50 => fib50,
			_ => fib618,
		};
	}
	
	public enum FibDirection
	{
		TopToBottom,
		BottomToTop
	}
	
	public enum FibLevel
	{
		Fib236,
		Fib382,
		Fib50,
		Fib618
	}
}
