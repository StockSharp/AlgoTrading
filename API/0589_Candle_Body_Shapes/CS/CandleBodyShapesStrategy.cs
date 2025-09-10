using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading strong bullish or bearish candles based on their body position.
/// </summary>
public class CandleBodyShapesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _bodyThreshold;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Percentage of candle range used to qualify a big body.
	/// </summary>
	public decimal BodyThreshold
	{
		get => _bodyThreshold.Value;
		set => _bodyThreshold.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CandleBodyShapesStrategy()
	{
		_bodyThreshold = Param(nameof(BodyThreshold), 0.2m)
			.SetGreaterThanZero()
			.SetLessOrEqual(0.5m)
			.SetDisplay("Body Threshold", "Fraction of range to detect large bodies", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0)
			return;

		var openPos = (candle.OpenPrice - candle.LowPrice) / range;
		var closePos = (candle.ClosePrice - candle.LowPrice) / range;

		if (openPos < BodyThreshold && closePos > 1 - BodyThreshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (openPos > 1 - BodyThreshold && closePos < BodyThreshold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
