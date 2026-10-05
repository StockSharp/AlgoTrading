using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull Moving Average slope breakout.
/// Enters in the direction of the slope when it exceeds its average by a standard deviation multiplier
/// and exits when the slope returns to its average.
/// </summary>
public class HullMaSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _hullLength;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _slopeAverage;
	private StandardDeviation _slopeStdDev;
	private decimal? _prevHull;

	/// <summary>
	/// Hull Moving Average length.
	/// </summary>
	public int HullLength
	{
		get => _hullLength.Value;
		set => _hullLength.Value = value;
	}

	/// <summary>
	/// Lookback period for slope statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for breakout detection.
	/// </summary>
	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss value.
	/// </summary>
	public Unit StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
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
	/// Initialize <see cref="HullMaSlopeBreakoutStrategy"/>.
	/// </summary>
	public HullMaSlopeBreakoutStrategy()
	{
		_hullLength = Param(nameof(HullLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Hull MA Length", "Period for Hull Moving Average", "Indicator Parameters")
			.SetOptimize(5, 20, 1);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for slope statistics calculation", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
			.SetDisplay("Stop Loss", "Protective stop-loss", "Risk Management");

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
		_slopeAverage = null;
		_slopeStdDev = null;
		_prevHull = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var hull = new HullMovingAverage { Length = HullLength };
		_slopeAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_slopeStdDev = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hull, ProcessCandle)
			.Start();

		StartProtection(takeProfit: null, stopLoss: StopLoss);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hull);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hullValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevHull is not decimal prev)
		{
			_prevHull = hullValue;
			return;
		}

		_prevHull = hullValue;

		var slope = hullValue - prev;
		var avgSlope = _slopeAverage.Process(slope, candle.ServerTime, true).ToDecimal();
		var stdSlope = _slopeStdDev.Process(slope, candle.ServerTime, true).ToDecimal();

		if (!_slopeAverage.IsFormed || !_slopeStdDev.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (slope > avgSlope + DeviationMultiplier * stdSlope && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (slope < avgSlope - DeviationMultiplier * stdSlope && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && slope < avgSlope)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && slope > avgSlope)
		{
			BuyMarket(-Position);
		}
	}
}
