using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aeron JJN breakout strategy.
/// Buys when candle reverses from bearish to bullish with body confirmation.
/// Sells when candle reverses from bullish to bearish.
/// </summary>
public class AeronJjnStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AeronJjnStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen = 0;
		_prevClose = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_hasPrev)
		{
			var prevBull = _prevClose > _prevOpen;
			var prevBear = _prevClose < _prevOpen;
			var currBull = candle.ClosePrice > candle.OpenPrice;
			var currBear = candle.ClosePrice < candle.OpenPrice;

			// Buy: bearish to bullish reversal with price above EMA
			if (prevBear && currBull && candle.ClosePrice > emaValue && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Sell: bullish to bearish reversal with price below EMA
			else if (prevBull && currBear && candle.ClosePrice < emaValue && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}
