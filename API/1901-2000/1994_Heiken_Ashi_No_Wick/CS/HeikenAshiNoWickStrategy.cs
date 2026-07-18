using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading Heiken Ashi candle color changes.
/// Buys when HA turns bullish, sells when HA turns bearish.
/// </summary>
public class HeikenAshiNoWickStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private bool _prevIsBull;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public HeikenAshiNoWickStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

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

		if (_hasPrev)
		{
			// Buy on bearish -> bullish transition
			if (isBull && !_prevIsBull && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Sell on bullish -> bearish transition
			else if (!isBull && _prevIsBull && Position >= 0)
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
