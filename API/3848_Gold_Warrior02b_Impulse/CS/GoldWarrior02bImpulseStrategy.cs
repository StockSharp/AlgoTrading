using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold Warrior Impulse strategy - CCI crossover with EMA trend filter.
/// Buys when CCI crosses above zero while price is above EMA.
/// Sells when CCI crosses below zero while price is below EMA.
/// </summary>
public class GoldWarrior02bImpulseStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCci;
	private bool _hasPrev;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GoldWarrior02bImpulseStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "CCI lookback", "Indicators");

		_emaPeriod = Param(nameof(EmaPeriod), 21)
			.SetDisplay("EMA Period", "EMA trend filter", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevCci = cci;
			_hasPrev = true;
			return;
		}

		// CCI crosses above zero + price above EMA = buy
		if (_prevCci <= 0 && cci > 0 && close > ema && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// CCI crosses below zero + price below EMA = sell
		else if (_prevCci >= 0 && cci < 0 && close < ema && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevCci = cci;
	}
}
