using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Above/Below MA Rejoin strategy.
/// Buys when price is below a rising EMA (pullback in uptrend).
/// Sells when price is above a falling EMA (pullback in downtrend).
/// </summary>
public class AboveBelowMaRejoinStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevClose;
	private bool _hasPrev;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AboveBelowMaRejoinStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Period", "EMA lookback period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_prevEma = 0m;
		_prevClose = 0m;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevEma = emaValue;
			_prevClose = close;
			_hasPrev = true;
			return;
		}

		var emaRising = emaValue > _prevEma;
		var emaFalling = emaValue < _prevEma;

		// Price rejoins from below in uptrend - buy
		if (emaRising && _prevClose < _prevEma && close >= emaValue && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Price rejoins from above in downtrend - sell
		else if (emaFalling && _prevClose > _prevEma && close <= emaValue && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevEma = emaValue;
		_prevClose = close;
	}
}
