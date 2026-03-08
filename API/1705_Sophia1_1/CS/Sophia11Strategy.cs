using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid mean-reversion strategy. Enters on 3-bar momentum, exits at SMA or ATR stop.
/// </summary>
public class Sophia11Strategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prev1, _prev2, _prev3;

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Sophia11Strategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA for exit target", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR for stops", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prev1 = _prev2 = _prev3 = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var atr = new StandardDeviation { Length = AtrPeriod };

		SubscribeCandles(CandleType).Bind(sma, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;

		if (_prev3 > 0)
		{
			// 3-bar declining => counter-trend buy
			if (_prev1 < _prev2 && _prev2 < _prev3 && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// 3-bar rising => counter-trend sell
			else if (_prev1 > _prev2 && _prev2 > _prev3 && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
			// Exit long at SMA or ATR stop
			else if (Position > 0 && (close >= sma || (atr > 0 && close < sma - atr * 3)))
			{
				SellMarket();
			}
			// Exit short at SMA or ATR stop
			else if (Position < 0 && (close <= sma || (atr > 0 && close > sma + atr * 3)))
			{
				BuyMarket();
			}
		}

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = close;
	}
}
