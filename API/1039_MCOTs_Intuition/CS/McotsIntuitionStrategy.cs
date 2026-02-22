using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class McotsIntuitionStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal _prevRsi;
	private decimal _prevMomentum;
	private decimal _takeProfitPrice;
	private decimal _stopLossPrice;
	private bool _hasPrev;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public McotsIntuitionStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_prevRsi = 0;
		_prevMomentum = 0;
		_hasPrev = false;
		_takeProfitPrice = 0;
		_stopLossPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
		{
			_prevRsi = rsiValue;
			return;
		}

		var momentum = rsiValue - _prevRsi;

		if (!_hasPrev)
		{
			_prevRsi = rsiValue;
			_prevMomentum = momentum;
			_hasPrev = true;
			return;
		}

		if (Position == 0)
		{
			// Positive momentum increasing = buy signal
			if (momentum > 1m && momentum > _prevMomentum)
			{
				BuyMarket();
				_takeProfitPrice = candle.ClosePrice * 1.01m;
				_stopLossPrice = candle.ClosePrice * 0.98m;
			}
			// Negative momentum decreasing = sell signal
			else if (momentum < -1m && momentum < _prevMomentum)
			{
				SellMarket();
				_takeProfitPrice = candle.ClosePrice * 0.99m;
				_stopLossPrice = candle.ClosePrice * 1.02m;
			}
		}
		else if (Position > 0)
		{
			if (candle.HighPrice >= _takeProfitPrice || candle.LowPrice <= _stopLossPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _takeProfitPrice || candle.HighPrice >= _stopLossPrice)
				BuyMarket();
		}

		_prevRsi = rsiValue;
		_prevMomentum = momentum;
	}
}
