using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy based on Average Directional Index crossover logic.
/// </summary>
public class AdxSimpleStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousAdx;

	/// <summary>
	/// Gets or sets the ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AdxSimpleStrategy"/>.
	/// </summary>
	public AdxSimpleStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Average Directional Index lookback length.", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for signal calculations.", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Reset cached ADX values when restarting the strategy.
		_previousAdx = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare the ADX indicator that supplies DI+ and DI- series.
		var adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		// Subscribe to candles and bind the indicator in high-level mode.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		// Optionally visualize candles, indicator, and trades on a chart.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		// Only react to finished candles to avoid premature decisions.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the indicator produced a finalized value with full lookback.
		if (!adxValue.IsFinal)
			return;

		var typedAdx = (AverageDirectionalIndexValue)adxValue;

		// Extract the ADX main line.
		if (typedAdx.MovingAverage is not decimal currentAdx)
			return;

		// Extract positive and negative directional indicators.
		var dx = typedAdx.Dx;
		if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
			return;

		if (_previousAdx is not decimal previousAdx)
		{
			// Cache the first valid ADX value for future slope comparisons.
			_previousAdx = currentAdx;
			return;
		}

		var isAdxRising = currentAdx > previousAdx;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousAdx = currentAdx;
			return;
		}

		if (Position > 0)
		{
			// Close long positions when DI- overtakes DI+.
			if (minusDi > plusDi)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			// Close short positions when DI+ overtakes DI-.
			if (plusDi > minusDi)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
		else if (isAdxRising)
		{
			// Enter long when DI+ leads during strengthening trend.
			if (plusDi > minusDi)
			{
				BuyMarket(Volume);
			}
			// Enter short when DI- leads during strengthening trend.
			else if (minusDi > plusDi)
			{
				SellMarket(Volume);
			}
		}

		// Update cached ADX value for the next candle.
		_previousAdx = currentAdx;
	}
}
