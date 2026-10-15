using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Average Force strategy.
/// The raw force is the position of the close inside the highest high and lowest low of the last Period candles, centred on zero
/// ((close - lowest) / (highest - lowest) - 0.5), and the Average Force is its SMA over Smooth candles. A positive Average
/// Force holds a long and a negative one holds a short, so the position reverses whenever it crosses zero.
/// </summary>
public class AverageForceStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _smooth;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _forceAverage;

	/// <summary>
	/// Lookback of the highest high and lowest low.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// SMA period of the force.
	/// </summary>
	public int Smooth
	{
		get => _smooth.Value;
		set => _smooth.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AverageForceStrategy()
	{
		_period = Param(nameof(Period), 18)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Lookback of the highest high and lowest low", "Indicator");

		_smooth = Param(nameof(Smooth), 6)
			.SetGreaterThanZero()
			.SetDisplay("Smooth", "SMA period of the force", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_forceAverage = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };
		_forceAverage = new SimpleMovingAverage { Length = Smooth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = highest - lowest;
		var force = range > 0m ? (candle.ClosePrice - lowest) / range - 0.5m : 0m;
		var averageForce = _forceAverage.Process(force, candle.ServerTime, true).ToDecimal();

		if (!_forceAverage.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (averageForce > 0m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (averageForce < 0m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
