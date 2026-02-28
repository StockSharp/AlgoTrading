using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// For Max V2: N-bar engulfing breakout with EMA filter and ATR stops.
/// </summary>
public class ForMaxV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _lookback;

	private decimal _entryPrice;
	private decimal _prevHigh;
	private decimal _prevLow;
	private readonly decimal[] _highs = new decimal[10];
	private readonly decimal[] _lows = new decimal[10];
	private int _barCount;

	public ForMaxV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_emaLength = Param(nameof(EmaLength), 30)
			.SetDisplay("EMA Length", "Trend filter.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");

		_lookback = Param(nameof(Lookback), 10)
			.SetDisplay("Lookback", "N-bar channel lookback.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_barCount = 0;
		_prevHigh = 0;
		_prevLow = 0;

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var len = Math.Min(Lookback, _highs.Length);
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
