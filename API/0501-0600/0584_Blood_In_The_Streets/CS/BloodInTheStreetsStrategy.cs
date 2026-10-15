using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Blood In The Streets strategy.
/// The drawdown is the percent distance of the close below the highest high of the last LookbackPeriod candles. When it falls to
/// its StdDevLength mean plus StdDevThreshold standard deviations or lower, the strategy buys, and it closes the long after ExitBars candles.
/// </summary>
public class BloodInTheStreetsStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _stdDevLength;
	private readonly StrategyParam<decimal> _stdDevThreshold;
	private readonly StrategyParam<int> _exitBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private SimpleMovingAverage _drawdownMean;
	private StandardDeviation _drawdownDeviation;
	private int _barsInPosition;

	/// <summary>
	/// Candles the highest high spans.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Candles the drawdown mean and standard deviation span.
	/// </summary>
	public int StdDevLength
	{
		get => _stdDevLength.Value;
		set => _stdDevLength.Value = value;
	}

	/// <summary>
	/// Standard deviations from the mean the drawdown has to reach.
	/// </summary>
	public decimal StdDevThreshold
	{
		get => _stdDevThreshold.Value;
		set => _stdDevThreshold.Value = value;
	}

	/// <summary>
	/// Candles a position is held.
	/// </summary>
	public int ExitBars
	{
		get => _exitBars.Value;
		set => _exitBars.Value = value;
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
	public BloodInTheStreetsStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Candles the highest high spans", "Indicators");

		_stdDevLength = Param(nameof(StdDevLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Length", "Candles the drawdown mean and standard deviation span", "Indicators");

		_stdDevThreshold = Param(nameof(StdDevThreshold), -1m)
			.SetDisplay("StdDev Threshold", "Standard deviations from the mean the drawdown has to reach", "Indicators");

		_exitBars = Param(nameof(ExitBars), 35)
			.SetGreaterThanZero()
			.SetDisplay("Exit Bars", "Candles a position is held", "Exit");

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
		_highest = null;
		_drawdownMean = null;
		_drawdownDeviation = null;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = LookbackPeriod };
		_drawdownMean = new SimpleMovingAverage { Length = StdDevLength };
		_drawdownDeviation = new StandardDeviation { Length = StdDevLength };
		_barsInPosition = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highestValue = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, candle.OpenTime) { IsFinal = true });

		if (!highestValue.IsFormed)
			return;

		var highest = highestValue.GetValue<decimal>();

		if (highest <= 0)
			return;

		var drawdown = (candle.ClosePrice - highest) / highest * 100m;
		var meanValue = _drawdownMean.Process(new DecimalIndicatorValue(_drawdownMean, drawdown, candle.OpenTime) { IsFinal = true });
		var deviationValue = _drawdownDeviation.Process(new DecimalIndicatorValue(_drawdownDeviation, drawdown, candle.OpenTime) { IsFinal = true });

		if (Position > 0)
			_barsInPosition++;

		if (!meanValue.IsFormed || !deviationValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (_barsInPosition >= ExitBars)
				SellMarket(Position);

			return;
		}

		var threshold = meanValue.GetValue<decimal>() + StdDevThreshold * deviationValue.GetValue<decimal>();

		if (Position == 0 && drawdown <= threshold)
		{
			BuyMarket(Volume);
			_barsInPosition = 0;
		}
	}
}
