using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Spread By strategy.
/// Trades based on distance from a moving average using standard deviation bands.
/// Buys when price falls below the lower band and sells when price rises above the upper band.
/// </summary>
public class SpreadByStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Calculation period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SpreadByStrategy()
	{
		_length = Param(nameof(Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Calculation period", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		var sma = new SimpleMovingAverage { Length = Length };
		var std = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, std, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upper1 = smaValue + stdValue;
		var lower1 = smaValue - stdValue;

		if (candle.ClosePrice > upper1 && Position <= 0)
		{
			SellMarket();
		}
		else if (candle.ClosePrice < lower1 && Position >= 0)
		{
			BuyMarket();
		}
		else if ((Position > 0 && candle.ClosePrice >= smaValue) ||
			(Position < 0 && candle.ClosePrice <= smaValue))
		{
			ClosePosition();
		}
	}
}
