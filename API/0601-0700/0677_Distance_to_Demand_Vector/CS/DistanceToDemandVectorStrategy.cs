using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Distance to demand vector strategy.
/// The long vector is the lowest low and the short vector the highest high of the last Length candles.
/// When the distance from the close to the long vector rises above the distance to the short vector the strategy goes long,
/// and when it falls below it goes short, reversing an opposite position.
/// </summary>
public class DistanceToDemandVectorStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevDiff;

	/// <summary>
	/// Candles that define the demand vectors.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
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
	public DistanceToDemandVectorStrategy()
	{
		_length = Param(nameof(Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Candles that define the demand vectors", "Indicators");

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
		_prevDiff = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevDiff = null;

		var highest = new Highest { Length = Length };
		var lowest = new Lowest { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue highestValue, IIndicatorValue lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!highestValue.IsFormed || !lowestValue.IsFormed)
			return;

		var close = candle.ClosePrice;
		var distanceToLong = close - lowestValue.GetValue<decimal>();
		var distanceToShort = highestValue.GetValue<decimal>() - close;
		var diff = distanceToLong - distanceToShort;

		var prev = _prevDiff;
		_prevDiff = diff;

		if (prev is not decimal prevDiff)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (prevDiff <= 0m && diff > 0m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (prevDiff >= 0m && diff < 0m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
