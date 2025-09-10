using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AI Supertrend x Pivot Percentile Strategy - combines two Supertrend indicators
/// with ADX and Williams %R filters.
/// </summary>
public class AiSupertrendPivotPercentileStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<decimal> _slPercent;

	private SuperTrend _supertrend1;
	private SuperTrend _supertrend2;
	private AverageDirectionalIndex _adx;
	private WilliamsR _williamsR;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Length for first Supertrend.
	/// </summary>
	public int Length1 { get => _length1.Value; set => _length1.Value = value; }

	/// <summary>
	/// Factor for first Supertrend.
	/// </summary>
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }

	/// <summary>
	/// Length for second Supertrend.
	/// </summary>
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }

	/// <summary>
	/// Factor for second Supertrend.
	/// </summary>
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }

	/// <summary>
	/// Minimum ADX value to allow trading.
	/// </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }

	/// <summary>
	/// Length for Williams %R calculation.
	/// </summary>
	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TpPercent { get => _tpPercent.Value; set => _tpPercent.Value = value; }

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal SlPercent { get => _slPercent.Value; set => _slPercent.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public AiSupertrendPivotPercentileStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_length1 = Param(nameof(Length1), 10)
			.SetGreaterThanZero()
			.SetDisplay("ST1 Length", "First Supertrend ATR length", "Supertrend")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_factor1 = Param(nameof(Factor1), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ST1 Factor", "First Supertrend multiplier", "Supertrend")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_length2 = Param(nameof(Length2), 20)
			.SetGreaterThanZero()
			.SetDisplay("ST2 Length", "Second Supertrend ATR length", "Supertrend")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 10);

		_factor2 = Param(nameof(Factor2), 4m)
			.SetGreaterThanZero()
			.SetDisplay("ST2 Factor", "Second Supertrend multiplier", "Supertrend")
			.SetCanOptimize(true)
			.SetOptimize(2m, 6m, 1m);

		_adxLength = Param(nameof(AdxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX calculation period", "Trend Filter")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Minimum ADX for trading", "Trend Filter")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_pivotLength = Param(nameof(PivotLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Length for Williams %R", "Pivot Percentile")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_tpPercent = Param(nameof(TpPercent), 2m)
			.SetDisplay("TP Percent", "Take profit in percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_slPercent = Param(nameof(SlPercent), 1m)
			.SetDisplay("SL Percent", "Stop loss in percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_supertrend1 = new SuperTrend { Length = Length1, Multiplier = Factor1 };
	_supertrend2 = new SuperTrend { Length = Length2, Multiplier = Factor2 };
	_adx = new AverageDirectionalIndex { Length = AdxLength };
	_williamsR = new WilliamsR { Length = PivotLength };

	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(_supertrend1, _supertrend2, _adx, _williamsR, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _supertrend1);
	DrawIndicator(area, _supertrend2);
	DrawIndicator(area, _adx);
	DrawIndicator(area, _williamsR);
	DrawOwnTrades(area);
	}

	StartProtection(
	new Unit(TpPercent / 100m, UnitTypes.Percent),
	new Unit(SlPercent / 100m, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle,
	IIndicatorValue st1Value,
	IIndicatorValue st2Value,
	IIndicatorValue adxValue,
	IIndicatorValue wprValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var st1 = (SuperTrendIndicatorValue)st1Value;
	var st2 = (SuperTrendIndicatorValue)st2Value;
	var adx = (AverageDirectionalIndexValue)adxValue;
	var wpr = wprValue.ToDecimal();

	var isBull = candle.ClosePrice > st1.Value && candle.ClosePrice > st2.Value;
	var isBear = candle.ClosePrice < st1.Value && candle.ClosePrice < st2.Value;
	var strongTrend = adx.MovingAverage > AdxThreshold;
	var pivotBull = wpr > -50m;
	var pivotBear = wpr < -50m;

	if (Position == 0)
	{
	if (isBull && strongTrend && pivotBull)
	BuyMarket(Volume);
	else if (isBear && strongTrend && pivotBear)
	SellMarket(Volume);
	}
	else if (Position > 0)
	{
	if (!isBull || !pivotBull)
	SellMarket(Volume + Position);
	}
	else if (Position < 0)
	{
	if (!isBear || !pivotBear)
	BuyMarket(Volume + Math.Abs(Position));
	}
	}
}