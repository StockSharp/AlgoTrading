using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy using ADX and Parabolic SAR.
/// Opens long positions when ADX confirms uptrend above SAR and shorts on opposite conditions.
/// </summary>
public class TrendFollowingAdxParabolicSarStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Parabolic SAR step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR max step.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private AverageDirectionalIndex _adx = null!;
	private ParabolicSar _sar = null!;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrendFollowingAdxParabolicSarStrategy"/>.
	/// </summary>
	public TrendFollowingAdxParabolicSarStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "Minimum ADX level", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration factor", "Parameters");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_sar = new ParabolicSar { AccelerationStep = SarStep, AccelerationMax = SarMax };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_adx, _sar, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawIndicator(area, _sar);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue sarValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx ||
		adxTyped.Dx.Plus is not decimal diPlus ||
		adxTyped.Dx.Minus is not decimal diMinus)
		return;

		var sar = sarValue.ToDecimal();

		var longCondition = adx > AdxThreshold && diPlus > diMinus && candle.ClosePrice > sar;
		var shortCondition = adx > AdxThreshold && diMinus > diPlus && candle.ClosePrice < sar;

		if (longCondition && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCondition && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		}
	}
}
