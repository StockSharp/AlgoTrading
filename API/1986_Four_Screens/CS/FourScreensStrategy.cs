using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Heikin-Ashi color with EMA filter.
/// Buys when HA turns bullish and price above EMA.
/// Sells when HA turns bearish and price below EMA.
/// </summary>
public class FourScreensStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private bool _prevIsBull;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FourScreensStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicator");

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
		_prevHaOpen = 0;
		_prevHaClose = 0;
		_prevIsBull = false;
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

		// Calculate Heikin-Ashi
		decimal haOpen;
		decimal haClose;

		if (_prevHaOpen == 0 && _prevHaClose == 0)
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		}
		else
		{
			haOpen = (_prevHaOpen + _prevHaClose) / 2m;
			haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		}

		var isBull = haClose > haOpen;
		var close = candle.ClosePrice;

		if (_hasPrev)
		{
			// Buy: HA turns bullish + price above EMA
			if (isBull && !_prevIsBull && close > emaValue && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Sell: HA turns bearish + price below EMA
			else if (!isBull && _prevIsBull && close < emaValue && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevIsBull = isBull;
		_hasPrev = true;
	}
}
