using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on moving average and standard deviation bands.
/// The idea is to open positions when price breaks outside a wide band
/// and close them when price returns inside a narrower band.
/// </summary>
public class CaiStandardDeviationStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<decimal> _openMultiplier;
	private readonly StrategyParam<decimal> _closeMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Moving average length (default: 12).
	/// </summary>
	public int MaLength
	
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Standard deviation period (default: 9).
	/// </summary>
	public int StdDevPeriod
	
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for entry band (default: 2.5).
	/// </summary>
	public decimal OpenMultiplier
	
	{
		get => _openMultiplier.Value;
		set => _openMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier for exit band (default: 1.5).
	/// </summary>
	public decimal CloseMultiplier
	
	{
		get => _closeMultiplier.Value;
		set => _closeMultiplier.Value = value;
	}

	/// <summary>
	/// Type of candles used for analysis.
	/// </summary>
	public DataType CandleType
	
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public CaiStandardDeviationStrategy()
	
	{
		_maLength = Param(nameof(MaLength), 12)
		.SetDisplay("MA Length", "Moving average length", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);

		_stdDevPeriod = Param(nameof(StdDevPeriod), 9)
		.SetDisplay("StdDev Period", "Standard deviation period", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);

		_openMultiplier = Param(nameof(OpenMultiplier), 2.5m)
		.SetDisplay("Open Multiplier", "StdDev multiplier for entries", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);

		_closeMultiplier = Param(nameof(CloseMultiplier), 1.5m)
		.SetDisplay("Close Multiplier", "StdDev multiplier for exits", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.5m);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
		.SetDisplay("Candle Type", "Type of candles used", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage { Length = MaLength };
		var stdDev = new StandardDeviation { Length = StdDevPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(sma, stdDev, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, stdDev);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdDevValue)
	
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var upperOpen = smaValue + OpenMultiplier * stdDevValue;
		var lowerOpen = smaValue - OpenMultiplier * stdDevValue;
		var upperClose = smaValue + CloseMultiplier * stdDevValue;
		var lowerClose = smaValue - CloseMultiplier * stdDevValue;

		if (Position <= 0 && candle.ClosePrice > upperOpen)
		BuyMarket();

		if (Position >= 0 && candle.ClosePrice < lowerOpen)
		SellMarket();

		if (Position > 0 && candle.ClosePrice < upperClose)
		SellMarket();

		if (Position < 0 && candle.ClosePrice > lowerClose)
		BuyMarket();
	}
}
