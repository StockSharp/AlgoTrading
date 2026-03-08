using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on consecutive same-direction candles with EMA filter.
/// </summary>
public class SheKanskigorStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _prevPrevOpen;
	private decimal _prevPrevClose;
	private int _candleCount;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SheKanskigorStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen = 0;
		_prevClose = 0;
		_prevPrevOpen = 0;
		_prevPrevClose = 0;
		_candleCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		SubscribeCandles(CandleType).Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		_candleCount++;

		if (_candleCount < 3)
		{
			_prevPrevOpen = _prevOpen;
			_prevPrevClose = _prevClose;
			_prevOpen = candle.OpenPrice;
			_prevClose = candle.ClosePrice;
			return;
		}

		// Two consecutive bearish candles -> buy reversal (with EMA confirmation)
		var twoBearish = _prevPrevOpen > _prevPrevClose && _prevOpen > _prevClose;
		// Two consecutive bullish candles -> sell reversal
		var twoBullish = _prevPrevOpen < _prevPrevClose && _prevOpen < _prevClose;

		if (twoBearish && candle.ClosePrice > emaValue && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (twoBullish && candle.ClosePrice < emaValue && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevPrevOpen = _prevOpen;
		_prevPrevClose = _prevClose;
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}
}
