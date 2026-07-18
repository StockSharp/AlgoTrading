using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SendCloseOrder: Fractal high/low breakout with EMA filter and ATR stops.
/// </summary>
public class SendCloseOrderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _entryPrice;
	private decimal _fractalHigh;
	private decimal _fractalLow;
	private decimal _prev2High;
	private decimal _prev1High;
	private decimal _prev2Low;
	private decimal _prev1Low;
	private int _barCount;

	public SendCloseOrderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "Trend filter.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
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

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0;
		_fractalHigh = 0;
		_fractalLow = 0;
		_prev2High = 0;
		_prev1High = 0;
		_prev2Low = 0;
		_prev1Low = 0;
		_barCount = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_fractalHigh = 0;
		_fractalLow = 0;
		_prev2High = 0;
		_prev1High = 0;
		_prev2Low = 0;
		_prev1Low = 0;
		_barCount = 0;

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

		_barCount++;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		// Detect fractal high: prev1High > prev2High and prev1High > current high
		if (_barCount > 3 && _prev1High > _prev2High && _prev1High > high)
			_fractalHigh = _prev1High;

		// Detect fractal low: prev1Low < prev2Low and prev1Low < current low
		if (_barCount > 3 && _prev1Low < _prev2Low && _prev1Low < low)
			_fractalLow = _prev1Low;

		_prev2High = _prev1High;
		_prev1High = high;
		_prev2Low = _prev1Low;
		_prev1Low = low;

		if (_fractalHigh == 0 || _fractalLow == 0 || atrVal <= 0)
			return;

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
			if (close > _fractalHigh && close > emaVal)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (close < _fractalLow && close < emaVal)
			{
				_entryPrice = close;
				SellMarket();
			}
		}
	}
}
