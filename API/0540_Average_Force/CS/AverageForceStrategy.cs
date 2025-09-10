using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Average Force strategy.
/// Calculates the position of the close within the recent high-low range
/// and smooths it with a moving average.
/// Buys when the smoothed value is above zero and sells when it is below zero.
/// </summary>
public class AverageForceStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _smooth;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private SimpleMovingAverage _sma;

	/// <summary>
	/// Lookback period for highest high and lowest low.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Smoothing period for the oscillator.
	/// </summary>
	public int Smooth
	{
		get => _smooth.Value;
		set => _smooth.Value = value;
	}

	/// <summary>
	/// The type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AverageForceStrategy"/>.
	/// </summary>
	public AverageForceStrategy()
	{
		_period = Param(nameof(Period), 18)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Lookback for highest and lowest", "Average Force")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_smooth = Param(nameof(Smooth), 6)
			.SetGreaterThanZero()
			.SetDisplay("Smooth", "Smoothing period", "Average Force")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_highest = null;
		_lowest = null;
		_sma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Period };
		_lowest = new Lowest { Length = Period };
		_sma = new SimpleMovingAverage { Length = Smooth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			var indArea = CreateChartArea("AverageForce");
			DrawIndicator(indArea, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var highestValue = _highest.Process(candle).ToDecimal();
		var lowestValue = _lowest.Process(candle).ToDecimal();

		var range = highestValue - lowestValue;
		var af = range == 0m ? 0m : (candle.ClosePrice - lowestValue) / range - 0.5m;
		var smoothed = _sma.Process(af).ToDecimal();

		if (!_sma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
		return;

		if (smoothed > 0m && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (smoothed < 0m && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		}
	}
}
