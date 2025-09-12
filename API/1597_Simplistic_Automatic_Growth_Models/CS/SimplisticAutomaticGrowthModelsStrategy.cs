using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplistic Automatic Growth Models Strategy - trades when price crosses averaged growth bands.
/// </summary>
public class SimplisticAutomaticGrowthModelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;

	private Highest _highest;
	private Lowest _lowest;

	private decimal _cumHigh;
	private decimal _cumLow;
	private int _count;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback length for highest and lowest calculations.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SimplisticAutomaticGrowthModelsStrategy()
	{
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Type of candles for processing", "General");

		_length = Param(nameof(Length), 10)
			.SetDisplay("Length", "Lookback length for bands", "Indicators")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Length };
		_lowest = new Lowest { Length = Length };

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hi = _highest.Process(candle.HighPrice).ToDecimal();
		var lo = _lowest.Process(candle.LowPrice).ToDecimal();

		_cumHigh += hi;
		_cumLow += lo;
		_count++;

		var chi = _cumHigh / _count;
		var clo = _cumLow / _count;

		if (candle.ClosePrice > chi && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < clo && Position >= 0)
			SellMarket();
	}
}

