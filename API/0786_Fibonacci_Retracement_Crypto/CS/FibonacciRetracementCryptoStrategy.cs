using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci retracement strategy for crypto.
/// Buys when price crosses above the 61.8% level.
/// Sells when price crosses below the 38.2% level.
/// Exits long at the 23.6% level and exits short at the 78.6% level.
/// </summary>
public class FibonacciRetracementCryptoStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private decimal _prevClose;

	/// <summary>
	/// Lookback period for Fibonacci levels.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FibonacciRetracementCryptoStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Lookback period for Fibonacci calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for processing", "Parameters");
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
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = LookbackPeriod };
		_lowest = new Lowest { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var diff = highestValue - lowestValue;
		var level23_6 = highestValue - diff * 0.236m;
		var level38_2 = highestValue - diff * 0.382m;
		var level61_8 = highestValue - diff * 0.618m;
		var level78_6 = highestValue - diff * 0.786m;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (_prevClose <= level61_8 && candle.ClosePrice > level61_8 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_prevClose >= level38_2 && candle.ClosePrice < level38_2 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (Position > 0 && candle.ClosePrice >= level23_6)
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && candle.ClosePrice <= level78_6)
			BuyMarket(Math.Abs(Position));

		_prevClose = candle.ClosePrice;
	}
}
