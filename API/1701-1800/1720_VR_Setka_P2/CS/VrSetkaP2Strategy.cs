using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid based strategy using candle direction and EMA trend.
/// </summary>
public class VrSetkaP2Strategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VrSetkaP2Strategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen = 0;
		_prevClose = 0;
		_hasPrev = false;
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

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevOpen = candle.OpenPrice;
			_prevClose = close;
			_hasPrev = true;
			return;
		}

		// Previous candle bullish + close above EMA => buy
		if (_prevClose > _prevOpen && close > emaValue && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Previous candle bearish + close below EMA => sell
		else if (_prevClose < _prevOpen && close < emaValue && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = close;
	}
}
