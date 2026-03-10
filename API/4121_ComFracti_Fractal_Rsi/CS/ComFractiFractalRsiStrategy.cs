using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ComFracti Fractal RSI: Fractal breakout with RSI filter and ATR stops.
/// </summary>
public class ComFractiFractalRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _entryPrice;
	private decimal _prevHigh5;
	private decimal _prevLow5;
	private decimal _high1, _high2, _high3, _high4, _high5;
	private decimal _low1, _low2, _low3, _low4, _low5;
	private int _barCount;

	public ComFractiFractalRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
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
		_barCount = 0;
		_prevHigh5 = 0;
		_prevLow5 = 0;
		_high1 = _high2 = _high3 = _high4 = _high5 = 0;
		_low1 = _low2 = _low3 = _low4 = _low5 = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_barCount = 0;
		_prevHigh5 = 0;
		_prevLow5 = 0;
		_high1 = _high2 = _high3 = _high4 = _high5 = 0;
		_low1 = _low2 = _low3 = _low4 = _low5 = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Shift fractal window
		_high5 = _high4; _high4 = _high3; _high3 = _high2; _high2 = _high1;
		_high1 = candle.HighPrice;
		_low5 = _low4; _low4 = _low3; _low3 = _low2; _low2 = _low1;
		_low1 = candle.LowPrice;
		_barCount++;

		if (_barCount < 5 || atrVal <= 0)
			return;

		var close = candle.ClosePrice;

		// Detect fractal high (center bar _high3 is highest)
		var fractalUp = _high3 > _high1 && _high3 > _high2 && _high3 > _high4 && _high3 > _high5;
		// Detect fractal low (center bar _low3 is lowest)
		var fractalDown = _low3 < _low1 && _low3 < _low2 && _low3 < _low4 && _low3 < _low5;

		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 3m || close <= _entryPrice - atrVal * 2m || fractalDown)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 3m || close >= _entryPrice + atrVal * 2m || fractalUp)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (Position == 0)
		{
			if (fractalDown && rsiVal < 45)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (fractalUp && rsiVal > 55)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevHigh5 = _high5;
		_prevLow5 = _low5;
	}
}
