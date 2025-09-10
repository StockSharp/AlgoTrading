using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bias Ratio Strategy - trades when price deviates from moving averages by a threshold.
/// </summary>
public class BiasRatioStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _biasThreshold;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Price deviation ratio from moving averages.
	/// </summary>
	public decimal BiasThreshold
	{
		get => _biasThreshold.Value;
		set => _biasThreshold.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BiasRatioStrategy"/> class.
	/// </summary>
	public BiasRatioStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(3).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_biasThreshold = Param(nameof(BiasThreshold), 0.025m)
			.SetDisplay("Bias Threshold", "Price deviation ratio from MA", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.005m);
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

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var longCondition = emaValue != 0 && close / emaValue >= 1 + BiasThreshold;
		var shortCondition = smaValue != 0 && close / smaValue <= 1 - BiasThreshold;

		if (longCondition && Position <= 0)
		{
			BuyMarket();
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket();
		}
	}
}
