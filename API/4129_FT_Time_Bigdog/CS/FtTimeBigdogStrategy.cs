using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FT Time Bigdog: Range breakout using N-bar high/low channel with ATR stops.
/// </summary>
public class FtTimeBigdogStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _entryPrice;
	private decimal _highest;
	private decimal _lowest;
	private int _barCount;
	private readonly decimal[] _highs = new decimal[20];
	private readonly decimal[] _lows = new decimal[20];

	public FtTimeBigdogStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_channelLength = Param(nameof(ChannelLength), 20)
			.SetDisplay("Channel Length", "Lookback for high/low channel.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
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

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_barCount = 0;
		_highest = 0;
		_lowest = 0;
		Array.Clear(_highs, 0, _highs.Length);
		Array.Clear(_lows, 0, _lows.Length);

		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrVal)
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

		var prevHigh = _highest;
		var prevLow = _lowest;
		_highest = high;
		_lowest = low;

		if (prevHigh == 0 || prevLow == 0)
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 3m || close <= _entryPrice - atrVal * 2m)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 3m || close >= _entryPrice + atrVal * 2m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (Position == 0)
		{
			if (close > prevHigh)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (close < prevLow)
			{
				_entryPrice = close;
				SellMarket();
			}
		}
	}
}
