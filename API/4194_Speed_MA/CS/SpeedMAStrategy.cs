using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Speed MA: EMA slope breakout with ATR stops.
/// </summary>
public class SpeedMAStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevEma;
	private decimal _prevPrevEma;
	private decimal _entryPrice;

	public SpeedMAStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");
		_emaLength = Param(nameof(EmaLength), 13)
			.SetDisplay("EMA Length", "Moving average period.", "Indicators");
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevEma = 0; _prevPrevEma = 0; _entryPrice = 0;
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, atr, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, ema); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevEma == 0 || atrVal <= 0) { _prevPrevEma = _prevEma; _prevEma = emaVal; return; }
		if (_prevPrevEma == 0) { _prevPrevEma = _prevEma; _prevEma = emaVal; return; }
		var close = candle.ClosePrice;
		var slope = emaVal - _prevEma;
		var prevSlope = _prevEma - _prevPrevEma;

		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 2.5m || close <= _entryPrice - atrVal * 1.5m || slope < 0) { SellMarket(); _entryPrice = 0; }
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 2.5m || close >= _entryPrice + atrVal * 1.5m || slope > 0) { BuyMarket(); _entryPrice = 0; }
		}

		if (Position == 0)
		{
			if (slope > 0 && prevSlope <= 0) { _entryPrice = close; BuyMarket(); }
			else if (slope < 0 && prevSlope >= 0) { _entryPrice = close; SellMarket(); }
		}
		_prevPrevEma = _prevEma; _prevEma = emaVal;
	}
}
