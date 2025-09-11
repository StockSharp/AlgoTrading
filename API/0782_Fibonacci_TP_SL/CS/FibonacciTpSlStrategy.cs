using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci strategy with ATR stop loss and percentage take profit.
/// </summary>
public class FibonacciTpSlStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _minBarsBetweenTrades;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _maxWeeklyReturn;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private AverageTrueRange _atr;

	private int _barIndex;
	private int _lastTradeBar;
	private int _currentWeek;
	private decimal _weekStartEquity;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Minimum bars between trades.
	/// </summary>
	public int MinBarsBetweenTrades
	{
		get => _minBarsBetweenTrades.Value;
		set => _minBarsBetweenTrades.Value = value;
	}

	/// <summary>
	/// Lookback period for Fibonacci levels.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop-loss.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum weekly return before trading stops.
	/// </summary>
	public decimal MaxWeeklyReturn
	{
		get => _maxWeeklyReturn.Value;
		set => _maxWeeklyReturn.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FibonacciTpSlStrategy"/>.
	/// </summary>
	public FibonacciTpSlStrategy()
	{
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 10m, 1m);

		_minBarsBetweenTrades = Param(nameof(MinBarsBetweenTrades), 10)
		.SetGreaterThanZero()
		.SetDisplay("Min Bars Between Trades", "Minimum bars between trades", "General")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);

		_lookback = Param(nameof(Lookback), 100)
		.SetGreaterThanZero()
		.SetDisplay("Lookback", "Lookback for Fibonacci levels", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(50, 150, 25);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR calculation period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 7);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);

		_maxWeeklyReturn = Param(nameof(MaxWeeklyReturn), 0.15m)
		.SetGreaterThanZero()
		.SetDisplay("Max Weekly Return", "Maximum weekly return before stopping trading", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 0.3m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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

		_highest = null;
		_lowest = null;
		_atr = null;
		_barIndex = 0;
		_lastTradeBar = -MinBarsBetweenTrades;
		_currentWeek = -1;
		_weekStartEquity = 0m;
		_stopLoss = 0m;
		_takeProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, _atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var calendar = CultureInfo.InvariantCulture.Calendar;
		var week = calendar.GetWeekOfYear(candle.ServerTime.LocalDateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

		if (week != _currentWeek)
		{
			_currentWeek = week;
			_weekStartEquity = Portfolio?.CurrentValue ?? 0m;
		}

		var equity = Portfolio?.CurrentValue ?? 0m;
		var weekReturn = _weekStartEquity != 0m ? (equity - _weekStartEquity) / _weekStartEquity : 0m;
		var canTradeWeek = weekReturn <= MaxWeeklyReturn;
		var canTradeBar = _barIndex - _lastTradeBar >= MinBarsBetweenTrades;

		var range = highestValue - lowestValue;
		var fib382 = highestValue - range * 0.382m;
		var fib786 = highestValue - range * 0.786m;
		var fib236 = highestValue - range * 0.236m;
		var fib618 = highestValue - range * 0.618m;

		var price = candle.ClosePrice;

		var buySignal = price <= fib382 && price >= fib786 && canTradeBar;
		var sellSignal = price <= fib236 && price >= fib618 && canTradeBar;

		if (buySignal && equity > 0m && canTradeWeek && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopLoss = price - atrValue * AtrMultiplier;
			_takeProfit = price * (1 + TakeProfitPercent / 100m);
			_lastTradeBar = _barIndex;
		}
		else if (sellSignal && equity > 0m && canTradeWeek && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopLoss = price + atrValue * AtrMultiplier;
			_takeProfit = price * (1 - TakeProfitPercent / 100m);
			_lastTradeBar = _barIndex;
		}

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

		_barIndex++;
	}
}
