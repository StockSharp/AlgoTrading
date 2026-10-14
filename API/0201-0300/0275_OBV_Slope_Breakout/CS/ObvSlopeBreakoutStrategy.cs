using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// On-Balance Volume slope breakout.
/// The slope is the OBV change over <see cref="SlopeLength"/> bars. Enters in the direction of the slope
/// when it exceeds its average by a standard deviation multiplier and exits when it returns to the average.
/// </summary>
public class ObvSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _slopeLength;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _obvHistory = new();
	private SimpleMovingAverage _slopeAverage;
	private StandardDeviation _slopeStdDev;

	/// <summary>
	/// Lookback period for slope statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars used to measure the OBV slope.
	/// </summary>
	public int SlopeLength
	{
		get => _slopeLength.Value;
		set => _slopeLength.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for breakout detection.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLoss
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
	/// Initialize <see cref="ObvSlopeBreakoutStrategy"/>.
	/// </summary>
	public ObvSlopeBreakoutStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for slope statistics", "Strategy");

		_slopeLength = Param(nameof(SlopeLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slope Length", "Bars used to measure the OBV slope", "Indicators");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Standard deviation multiplier for breakout", "Strategy");

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management");

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
		_obvHistory.Clear();
		_slopeAverage = null;
		_slopeStdDev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var obv = new OnBalanceVolume();
		_slopeAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_slopeStdDev = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(obv, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: null,
			stopLoss: StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Percent) : null
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var obvArea = CreateChartArea();
			if (obvArea != null)
				DrawIndicator(obvArea, obv);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_obvHistory.Enqueue(obvValue);

		if (_obvHistory.Count <= SlopeLength)
			return;

		var slope = obvValue - _obvHistory.Dequeue();
		var avgSlope = _slopeAverage.Process(slope, candle.ServerTime, true).ToDecimal();
		var stdSlope = _slopeStdDev.Process(slope, candle.ServerTime, true).ToDecimal();

		if (!_slopeAverage.IsFormed || !_slopeStdDev.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (slope > avgSlope + Multiplier * stdSlope && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (slope < avgSlope - Multiplier * stdSlope && Position >= 0)
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
