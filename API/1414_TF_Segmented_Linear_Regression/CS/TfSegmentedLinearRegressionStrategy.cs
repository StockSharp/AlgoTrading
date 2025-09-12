using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Segmented linear regression strategy using RMSE channel.
/// Buys when price crosses above the upper channel and sells when it crosses below the lower channel.
/// </summary>
public class TfSegmentedLinearRegressionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _segment;
	private readonly StrategyParam<decimal> _multiplier;

	private LinearRegression _linearReg;
	private StandardDeviation _stdDev;
	private DateTimeOffset _segmentStart;
	private int _count;
	private decimal _prevClose;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Time segment length.
	/// </summary>
	public TimeSpan Segment { get => _segment.Value; set => _segment.Value = value; }

	/// <summary>
	/// Channel width multiplier.
	/// </summary>
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TfSegmentedLinearRegressionStrategy"/> class.
	/// </summary>
	public TfSegmentedLinearRegressionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_segment = Param(nameof(Segment), TimeSpan.FromDays(1))
			.SetDisplay("Segment", "Time segment length", "Parameters");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Channel width multiplier", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);
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
		_segmentStart = default;
		_count = 0;
		_prevClose = 0m;
		_linearReg?.Reset();
		_stdDev?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_linearReg = new LinearRegression();
		_stdDev = new StandardDeviation();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_linearReg, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _linearReg);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue regValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var segTicks = Segment.Ticks;
		var currentSeg = new DateTimeOffset((candle.OpenTime.UtcTicks / segTicks) * segTicks, TimeSpan.Zero);
		if (_segmentStart != currentSeg)
		{
			_segmentStart = currentSeg;
			_linearReg.Reset();
			_stdDev.Reset();
			_count = 0;
			_prevClose = 0m;
		}

		_count++;
		_linearReg.Length = _count;
		_stdDev.Length = _count;

		var regTyped = (LinearRegressionValue)regValue;
		if (regTyped.LinearReg is not decimal regression)
			return;

		var stdValue = _stdDev.Process(candle.ClosePrice - regression, candle.ServerTime, true).ToNullableDecimal();
		if (stdValue is not decimal stdDev || !_linearReg.IsFormed || !_stdDev.IsFormed)
			return;

		var upper = regression + stdDev * Multiplier;
		var lower = regression - stdDev * Multiplier;

		if (Position <= 0 && _prevClose < lower && candle.ClosePrice > lower)
			BuyMarket(Volume + Math.Abs(Position));
		else if (Position >= 0 && _prevClose > upper && candle.ClosePrice < upper)
			SellMarket(Volume + Math.Abs(Position));

		_prevClose = candle.ClosePrice;
	}
}
