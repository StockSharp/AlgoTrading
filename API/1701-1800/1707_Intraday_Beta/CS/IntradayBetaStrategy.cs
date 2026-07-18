using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Intraday strategy based on SMA slope changes and RSI with ATR trailing stop.
/// </summary>
public class IntradayBetaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMa10;
	private decimal _prevSlope;
	private decimal _prevCandleDiff;
	private decimal _longStop;
	private decimal _shortStop;
	private decimal _entryPrice;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public IntradayBetaStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for trailing", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMa10 = 0; _prevSlope = 0; _prevCandleDiff = 0;
		_longStop = 0; _shortStop = 0; _entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma10 = new SimpleMovingAverage { Length = 10 };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new StandardDeviation { Length = AtrPeriod };

		SubscribeCandles(CandleType).Bind(ma10, rsi, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma10Value, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevMa10 == 0) { _prevMa10 = ma10Value; return; }

		var ma10Slope = ma10Value - _prevMa10;
		var candleDiff = candle.ClosePrice - candle.OpenPrice;
		var trailDist = atrValue > 0 ? atrValue * 2 : 100;

		var sellSignal = ma10Slope < 0 && _prevSlope > 0 && rsiValue >= 30 && _prevCandleDiff < 0;
		var buySignal = ma10Slope > 0 && _prevSlope < 0 && rsiValue <= 70 && _prevCandleDiff > 0;

		if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_shortStop = _entryPrice + trailDist;
		}
		else if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_longStop = _entryPrice - trailDist;
		}

		if (Position > 0)
		{
			var newStop = candle.ClosePrice - trailDist;
			if (newStop > _longStop && candle.ClosePrice > _entryPrice)
				_longStop = newStop;
			if (candle.LowPrice <= _longStop) SellMarket();
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + trailDist;
			if (newStop < _shortStop && candle.ClosePrice < _entryPrice)
				_shortStop = newStop;
			if (candle.HighPrice >= _shortStop) BuyMarket();
		}

		_prevMa10 = ma10Value;
		_prevSlope = ma10Slope;
		_prevCandleDiff = candleDiff;
	}
}
