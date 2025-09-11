using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci trend strategy.
/// </summary>
public class FibonacciTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _fibLevel1;
	private readonly StrategyParam<decimal> _fibLevel2;
	private readonly StrategyParam<decimal> _fibLevel3;
	private readonly StrategyParam<decimal> _fibLevel4;
	private readonly StrategyParam<decimal> _fibLevel5;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _swingHigh;
	private decimal _swingLow;
	private decimal _fib1;
	private decimal _fib2;
	private decimal _fib3;
	private decimal _fib4;
	private decimal _fib5;

	private ICandleMessage _prev1Candle;
	private ICandleMessage _prev2Candle;

	/// <summary>
	/// Lookback period.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// First Fibonacci level.
	/// </summary>
	public decimal FibLevel1 { get => _fibLevel1.Value; set => _fibLevel1.Value = value; }

	/// <summary>
	/// Second Fibonacci level.
	/// </summary>
	public decimal FibLevel2 { get => _fibLevel2.Value; set => _fibLevel2.Value = value; }

	/// <summary>
	/// Third Fibonacci level.
	/// </summary>
	public decimal FibLevel3 { get => _fibLevel3.Value; set => _fibLevel3.Value = value; }

	/// <summary>
	/// Fourth Fibonacci level.
	/// </summary>
	public decimal FibLevel4 { get => _fibLevel4.Value; set => _fibLevel4.Value = value; }

	/// <summary>
	/// Fifth Fibonacci level.
	/// </summary>
	public decimal FibLevel5 { get => _fibLevel5.Value; set => _fibLevel5.Value = value; }

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public FibonacciTrendStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Bars used for swing detection", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_fibLevel1 = Param(nameof(FibLevel1), 0.236m).SetDisplay("Fib Level 1", "First Fibonacci ratio", "Fibonacci");
		_fibLevel2 = Param(nameof(FibLevel2), 0.382m).SetDisplay("Fib Level 2", "Second Fibonacci ratio", "Fibonacci");
		_fibLevel3 = Param(nameof(FibLevel3), 0.5m).SetDisplay("Fib Level 3", "Third Fibonacci ratio", "Fibonacci");
		_fibLevel4 = Param(nameof(FibLevel4), 0.618m).SetDisplay("Fib Level 4", "Fourth Fibonacci ratio", "Fibonacci");
		_fibLevel5 = Param(nameof(FibLevel5), 0.786m).SetDisplay("Fib Level 5", "Fifth Fibonacci ratio", "Fibonacci");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_swingHigh = 0m;
		_swingLow = 0m;
		_fib1 = 0m;
		_fib2 = 0m;
		_fib3 = 0m;
		_fib4 = 0m;
		_fib5 = 0m;
		_prev1Candle = null;
		_prev2Candle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prev1Candle != null && _prev2Candle != null)
		{
			if (_prev1Candle.HighPrice > _prev2Candle.HighPrice && _prev1Candle.HighPrice > candle.HighPrice)
			{
				_swingHigh = _prev1Candle.HighPrice;
			}

			if (_prev1Candle.LowPrice < _prev2Candle.LowPrice && _prev1Candle.LowPrice < candle.LowPrice)
			{
				_swingLow = _prev1Candle.LowPrice;
			}
		}

		if (_swingHigh != 0m && _swingLow != 0m)
		{
			var priceDiff = _swingHigh - _swingLow;
			_fib1 = _swingLow + priceDiff * FibLevel1;
			_fib2 = _swingLow + priceDiff * FibLevel2;
			_fib3 = _swingLow + priceDiff * FibLevel3;
			_fib4 = _swingLow + priceDiff * FibLevel4;
			_fib5 = _swingLow + priceDiff * FibLevel5;
		}

		var longCondition = candle.ClosePrice > _fib3;
		var shortCondition = candle.ClosePrice < _fib3;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prev2Candle = _prev1Candle;
		_prev1Candle = candle;
	}
}

