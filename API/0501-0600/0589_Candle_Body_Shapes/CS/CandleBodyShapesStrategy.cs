using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle Body Shapes strategy.
/// "Near" means within BodyThreshold of the candle range. A candle that opens near its low and closes near its high goes long,
/// one that opens near its high and closes near its low goes short; the opposite signal reverses the position.
/// </summary>
public class CandleBodyShapesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _bodyThreshold;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fraction of the candle range that counts as near its high or low.
	/// </summary>
	public decimal BodyThreshold
	{
		get => _bodyThreshold.Value;
		set => _bodyThreshold.Value = value;
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
	public CandleBodyShapesStrategy()
	{
		_bodyThreshold = Param(nameof(BodyThreshold), 0.2m)
			.SetRange(0m, 1m)
			.SetDisplay("Body Threshold", "Fraction of the candle range that counts as near its high or low", "Pattern");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		var near = BodyThreshold * range;
		var openNearLow = candle.OpenPrice - candle.LowPrice <= near;
		var openNearHigh = candle.HighPrice - candle.OpenPrice <= near;
		var closeNearLow = candle.ClosePrice - candle.LowPrice <= near;
		var closeNearHigh = candle.HighPrice - candle.ClosePrice <= near;

		if (openNearLow && closeNearHigh && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (openNearHigh && closeNearLow && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
