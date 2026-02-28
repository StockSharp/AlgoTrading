using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exp Fibo ZZ: Fibonacci-style breakout using high/low channel with ATR stops.
/// </summary>
public class ExpFiboZzStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _emaLength;

	private decimal _entryPrice;
	private decimal _prevHigh;
	private decimal _prevLow;
	private readonly decimal[] _highs = new decimal[15];
	private readonly decimal[] _lows = new decimal[15];
	private int _barCount;

	public ExpFiboZzStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_channelLength = Param(nameof(ChannelLength), 15)
			.SetDisplay("Channel", "N-bar channel lookback.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "Trend filter.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_barCount = 0;
		_prevHigh = 0;
		_prevLow = 0;

		var atr = new AverageTrueRange { Length = AtrLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrVal, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var len = Math.Min(ChannelLength, _highs.Length);
		var idx = _barCount % len;
		_highs[idx] = candle.HighPrice;
		_lows[idx] = candle.LowPrice;
		_barCount++;

		if (_barCount < len || atrVal <= 0)
			return;

		var high = decimal.MinValue;
		var low = decimal.MaxValue;
		for (var i = 0; i < len; i++)
		{
			if (_highs[i] > high) high = _highs[i];
			if (_lows[i] < low) low = _lows[i];
		}

		var close = candle.ClosePrice;

		if (_prevHigh == 0 || _prevLow == 0)
		{
			_prevHigh = high;
			_prevLow = low;
			return;
		}

		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 3m || close <= _entryPrice - atrVal * 1.5m)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 3m || close >= _entryPrice + atrVal * 1.5m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (Position == 0)
		{
			if (close > _prevHigh && close > emaVal)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (close < _prevLow && close < emaVal)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevHigh = high;
		_prevLow = low;
	}
}
