using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI-based trading strategy.
/// Buys when RSI crosses above BuyPoint, sells when RSI crosses below SellPoint.
/// </summary>
public class RsiTraderV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _buyPoint;
	private readonly StrategyParam<decimal> _sellPoint;

	private decimal _prevRsi;
	private decimal _prevPrevRsi;
	private bool _hasPrev;
	private bool _hasPrevPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal BuyPoint { get => _buyPoint.Value; set => _buyPoint.Value = value; }
	public decimal SellPoint { get => _sellPoint.Value; set => _sellPoint.Value = value; }

	public RsiTraderV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Calculation period", "RSI");

		_buyPoint = Param(nameof(BuyPoint), 30m)
			.SetDisplay("Buy Threshold", "RSI level for long entry", "RSI");

		_sellPoint = Param(nameof(SellPoint), 70m)
			.SetDisplay("Sell Threshold", "RSI level for short entry", "RSI");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_prevPrevRsi = 0;
		_hasPrev = false;
		_hasPrevPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		SubscribeCandles(CandleType)
			.Bind(rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevRsi = rsiValue;
			_hasPrev = true;
			return;
		}

		if (!_hasPrevPrev)
		{
			_prevPrevRsi = _prevRsi;
			_prevRsi = rsiValue;
			_hasPrevPrev = true;
			return;
		}

		var longSignal = rsiValue > BuyPoint && _prevRsi < BuyPoint && _prevPrevRsi < BuyPoint;
		var shortSignal = rsiValue < SellPoint && _prevRsi > SellPoint && _prevPrevRsi > SellPoint;

		if (longSignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (shortSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevPrevRsi = _prevRsi;
		_prevRsi = rsiValue;
	}
}
