using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy looking for bullish/bearish reversal candlestick patterns with MA filter.
/// </summary>
public class BullishReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;

	private decimal _prevOpen1, _prevClose1, _prevLow1;
	private decimal _prevOpen2, _prevClose2, _prevLow2;
	private decimal _prevOpen3, _prevClose3, _prevLow3;
	private int _candleCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	public BullishReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetDisplay("MA Period", "EMA length", "Parameters")
			.SetGreaterThanZero();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen1 = 0; _prevClose1 = 0; _prevLow1 = 0;
		_prevOpen2 = 0; _prevClose2 = 0; _prevLow2 = 0;
		_prevOpen3 = 0; _prevClose3 = 0; _prevLow3 = 0;
		_candleCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = MaPeriod };

		SubscribeCandles(CandleType).Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished) return;

		_candleCount++;

		if (_candleCount < 4)
		{
			ShiftCandles(candle);
			return;
		}

		// Bullish patterns using stored values
		var threeWhiteSoldiers = _prevOpen3 < _prevClose3 && _prevOpen2 < _prevClose2 && _prevOpen1 < _prevClose1 &&
			_prevClose3 < _prevClose2 && _prevClose2 < _prevClose1;

		var threeInsideUp = _prevOpen3 > _prevClose3 &&
			Math.Abs(_prevClose2 - _prevOpen2) <= 0.6m * Math.Abs(_prevOpen3 - _prevClose3) &&
			_prevClose2 > _prevOpen2 && _prevClose1 > _prevOpen1 && _prevClose1 > _prevOpen3;

		// Bearish patterns
		var threeBlackCrows = _prevOpen3 > _prevClose3 && _prevOpen2 > _prevClose2 && _prevOpen1 > _prevClose1 &&
			_prevClose3 > _prevClose2 && _prevClose2 > _prevClose1;

		var threeInsideDown = _prevOpen3 < _prevClose3 &&
			Math.Abs(_prevClose2 - _prevOpen2) <= 0.6m * Math.Abs(_prevOpen3 - _prevClose3) &&
			_prevClose2 < _prevOpen2 && _prevClose1 < _prevOpen1 && _prevClose1 < _prevOpen3;

		var bullSignal = threeWhiteSoldiers || threeInsideUp;
		var bearSignal = threeBlackCrows || threeInsideDown;

		if (bullSignal && candle.ClosePrice > ma && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (bearSignal && candle.ClosePrice < ma && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		ShiftCandles(candle);
	}

	private void ShiftCandles(ICandleMessage candle)
	{
		_prevOpen3 = _prevOpen2; _prevClose3 = _prevClose2; _prevLow3 = _prevLow2;
		_prevOpen2 = _prevOpen1; _prevClose2 = _prevClose1; _prevLow2 = _prevLow1;
		_prevOpen1 = candle.OpenPrice; _prevClose1 = candle.ClosePrice; _prevLow1 = candle.LowPrice;
	}
}
