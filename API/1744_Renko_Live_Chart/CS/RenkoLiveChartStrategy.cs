using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Renko live chart emulation strategy.
/// </summary>
public class RenkoLiveChartStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _brickSize;

	private decimal _renkoPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal BrickSize { get => _brickSize.Value; set => _brickSize.Value = value; }

	public RenkoLiveChartStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");

		_brickSize = Param(nameof(BrickSize), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Brick Size", "Renko brick size", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var size = BrickSize;

		if (_renkoPrice == 0m)
		{
			_renkoPrice = close;
			return;
		}

		var diff = close - _renkoPrice;
		if (Math.Abs(diff) < size)
			return;

		var direction = Math.Sign(diff);
		_renkoPrice += direction * size;

		if (direction > 0 && Position <= 0)
			BuyMarket();
		else if (direction < 0 && Position >= 0)
			SellMarket();
	}
}
