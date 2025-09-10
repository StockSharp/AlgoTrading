using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands with Fibonacci filter strategy.
/// Long when price crosses above upper band and low > Fibonacci low.
/// Short when price crosses below lower band and high < Fibonacci high.
/// </summary>
public class BollingerBandsFibonacciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _fibonacciLength;
	private readonly StrategyParam<decimal> _fibonacciLevel0;
	private readonly StrategyParam<decimal> _fibonacciLevel100;

	private BollingerBands _bollinger = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevMiddle;
	private bool _isInitialized;

	/// <summary>
	/// Type of candles for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }

	/// <summary>
	/// Lookback for Fibonacci levels.
	/// </summary>
	public int FibonacciLength { get => _fibonacciLength.Value; set => _fibonacciLength.Value = value; }

	/// <summary>
	/// Top retracement level.
	/// </summary>
	public decimal FibonacciLevel0 { get => _fibonacciLevel0.Value; set => _fibonacciLevel0.Value = value; }

	/// <summary>
	/// Bottom retracement level.
	/// </summary>
	public decimal FibonacciLevel100 { get => _fibonacciLevel100.Value; set => _fibonacciLevel100.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BollingerBandsFibonacciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy calculation", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Period for Bollinger Bands", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiplier", "Standard deviation multiplier", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_fibonacciLength = Param(nameof(FibonacciLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fibonacci Length", "Lookback for Fibonacci levels", "Fibonacci")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_fibonacciLevel0 = Param(nameof(FibonacciLevel0), 0m)
			.SetDisplay("Fibonacci Level 0", "Top retracement level", "Fibonacci")
			.SetCanOptimize(true)
			.SetOptimize(-0.5m, 0.5m, 0.1m);

		_fibonacciLevel100 = Param(nameof(FibonacciLevel100), 1m)
			.SetDisplay("Fibonacci Level 100", "Bottom retracement level", "Fibonacci")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 1.5m, 0.1m);
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

		_prevClose = default;
		_prevUpper = default;
		_prevLower = default;
		_prevMiddle = default;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };
		_highest = new Highest { Length = FibonacciLength };
		_lowest = new Lowest { Length = FibonacciLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highestVal = _highest.Process(candle.HighPrice).ToNullableDecimal();
		var lowestVal = _lowest.Process(candle.LowPrice).ToNullableDecimal();

		if (!_bollinger.IsFormed || !_highest.IsFormed || !_lowest.IsFormed || highestVal is not decimal high || lowestVal is not decimal low)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			_prevMiddle = middleBand;
			return;
		}

		if (!_isInitialized)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			_prevMiddle = middleBand;
			_isInitialized = true;
			return;
		}

		var fibRange = high - low;
		var fibHigh = high - fibRange * FibonacciLevel0;
		var fibLow = low + fibRange * FibonacciLevel100;

		var crossOver = _prevClose <= _prevUpper && candle.ClosePrice > upperBand;
		var crossUnder = _prevClose >= _prevLower && candle.ClosePrice < lowerBand;

		if (crossOver && candle.LowPrice > fibLow && Position <= 0)
			BuyMarket();

		if (crossUnder && candle.HighPrice < fibHigh && Position >= 0)
			SellMarket();

		if (Position > 0 && _prevClose >= _prevMiddle && candle.ClosePrice < middleBand)
			SellMarket();

		if (Position < 0 && _prevClose <= _prevMiddle && candle.ClosePrice > middleBand)
			BuyMarket();

		_prevClose = candle.ClosePrice;
		_prevUpper = upperBand;
		_prevLower = lowerBand;
		_prevMiddle = middleBand;
	}
}
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _fibonacciLength;
	private readonly StrategyParam<decimal> _fibonacciLevel0;
	private readonly StrategyParam<decimal> _fibonacciLevel100;

	private BollingerBands _bollinger = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevMiddle;
	private bool _isInitialized;

	/// <summary>
	/// Type of candles for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }

	/// <summary>
	/// Lookback for Fibonacci levels.
	/// </summary>
	public int FibonacciLength { get => _fibonacciLength.Value; set => _fibonacciLength.Value = value; }

	/// <summary>
	/// Top retracement level.
	/// </summary>
	public decimal FibonacciLevel0 { get => _fibonacciLevel0.Value; set => _fibonacciLevel0.Value = value; }

	/// <summary>
	/// Bottom retracement level.
	/// </summary>
	public decimal FibonacciLevel100 { get => _fibonacciLevel100.Value; set => _fibonacciLevel100.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BollingerBandsFibonacciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy calculation", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Period for Bollinger Bands", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiplier", "Standard deviation multiplier", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_fibonacciLength = Param(nameof(FibonacciLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fibonacci Length", "Lookback for Fibonacci levels", "Fibonacci")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_fibonacciLevel0 = Param(nameof(FibonacciLevel0), 0m)
			.SetDisplay("Fibonacci Level 0", "Top retracement level", "Fibonacci")
			.SetCanOptimize(true)
			.SetOptimize(-0.5m, 0.5m, 0.1m);

		_fibonacciLevel100 = Param(nameof(FibonacciLevel100), 1m)
			.SetDisplay("Fibonacci Level 100", "Bottom retracement level", "Fibonacci")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 1.5m, 0.1m);
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

		_prevClose = default;
		_prevUpper = default;
		_prevLower = default;
		_prevMiddle = default;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };
		_highest = new Highest { Length = FibonacciLength };
		_lowest = new Lowest { Length = FibonacciLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highestVal = _highest.Process(candle.HighPrice).ToNullableDecimal();
		var lowestVal = _lowest.Process(candle.LowPrice).ToNullableDecimal();

		if (!_bollinger.IsFormed || !_highest.IsFormed || !_lowest.IsFormed || highestVal is not decimal high || lowestVal is not decimal low)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			_prevMiddle = middleBand;
			return;
		}

		if (!_isInitialized)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			_prevMiddle = middleBand;
			_isInitialized = true;
			return;
		}

		var fibRange = high - low;
		var fibHigh = high - fibRange * FibonacciLevel0;
		var fibLow = low + fibRange * FibonacciLevel100;

		var crossOver = _prevClose <= _prevUpper && candle.ClosePrice > upperBand;
		var crossUnder = _prevClose >= _prevLower && candle.ClosePrice < lowerBand;

		if (crossOver && candle.LowPrice > fibLow && Position <= 0)
			BuyMarket();

		if (crossUnder && candle.HighPrice < fibHigh && Position >= 0)
			SellMarket();

		if (Position > 0 && _prevClose >= _prevMiddle && candle.ClosePrice < middleBand)
			SellMarket();

		if (Position < 0 && _prevClose <= _prevMiddle && candle.ClosePrice > middleBand)
			BuyMarket();

		_prevClose = candle.ClosePrice;
		_prevUpper = upperBand;
		_prevLower = lowerBand;
		_prevMiddle = middleBand;
	}
}
