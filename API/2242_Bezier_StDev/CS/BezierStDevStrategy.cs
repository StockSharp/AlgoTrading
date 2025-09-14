using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on standard deviation turning points.
/// Opens long at local minima and short at local maxima of the indicator.
/// </summary>
public class BezierStDevStrategy : Strategy
{
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevValue1;
	private decimal _prevValue2;

	/// <summary>
	/// Standard deviation calculation period.
	/// </summary>
	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public BezierStDevStrategy()
	{
		_stdDevPeriod = Param(nameof(StdDevPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Period", "Period for standard deviation calculation", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used", "General");
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

		_prevValue1 = 0m;
		_prevValue2 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stdDev = new StandardDeviation { Length = StdDevPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stdDev);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdDevValue)
	{
		// We only work with finished candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is ready for trading.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Check for local minima and maxima at the previous value.
		if (_prevValue2 != 0m)
		{
			var isLocalMin = _prevValue1 < _prevValue2 && _prevValue1 < stdDevValue;
			var isLocalMax = _prevValue1 > _prevValue2 && _prevValue1 > stdDevValue;

			if (isLocalMin)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (isLocalMax)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
		}

		// Shift stored values for next calculation.
		_prevValue2 = _prevValue1;
		_prevValue1 = stdDevValue;
	}
}
