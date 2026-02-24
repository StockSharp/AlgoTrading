using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fracture breakout strategy using fractals and smoothed MAs.
/// Enters on fractal breakouts when ADX is below threshold, or on MA alignment when trending.
/// </summary>
public class FractureStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxLine;
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _ma3Period;
	private readonly StrategyParam<decimal> _minProfit;

	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	private decimal _up, _down;
	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxLine { get => _adxLine.Value; set => _adxLine.Value = value; }
	public int Ma1Period { get => _ma1Period.Value; set => _ma1Period.Value = value; }
	public int Ma2Period { get => _ma2Period.Value; set => _ma2Period.Value = value; }
	public int Ma3Period { get => _ma3Period.Value; set => _ma3Period.Value = value; }
	public decimal MinProfit { get => _minProfit.Value; set => _minProfit.Value = value; }

	public FractureStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle", "Candle type", "General");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR", "ATR period", "Params");
		_adxPeriod = Param(nameof(AdxPeriod), 22)
			.SetGreaterThanZero()
			.SetDisplay("ADX", "ADX period", "Params");
		_adxLine = Param(nameof(AdxLine), 40m)
			.SetDisplay("ADX Line", "ADX threshold", "Params");
		_ma1Period = Param(nameof(Ma1Period), 5)
			.SetGreaterThanZero()
			.SetDisplay("MA1", "First SMMA", "MA");
		_ma2Period = Param(nameof(Ma2Period), 9)
			.SetGreaterThanZero()
			.SetDisplay("MA2", "Second SMMA", "MA");
		_ma3Period = Param(nameof(Ma3Period), 22)
			.SetGreaterThanZero()
			.SetDisplay("MA3", "Third SMMA", "MA");
		_minProfit = Param(nameof(MinProfit), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Min Profit", "ATR profit target multiplier", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_h1 = _h2 = _h3 = _h4 = _h5 = 0;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0;
		_up = _down = 0;
		_entryPrice = 0;

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var ma1 = new SmoothedMovingAverage { Length = Ma1Period };
		var ma2 = new SmoothedMovingAverage { Length = Ma2Period };
		var ma3 = new SmoothedMovingAverage { Length = Ma3Period };

		var sub = SubscribeCandles(CandleType);
		sub.BindEx(new IIndicator[] { atr, adx, ma1, ma2, ma3 }, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!values[0].IsFinal || !values[1].IsFinal || !values[2].IsFinal)
			return;

		var atrVal = values[0].GetValue<decimal>();

		var adxTyped = (AverageDirectionalIndexValue)values[1];
		if (adxTyped.MovingAverage is not decimal adxVal)
			return;

		var ma1Val = values[2].GetValue<decimal>();
		var ma2Val = values[3].GetValue<decimal>();
		var ma3Val = values[4].GetValue<decimal>();

		// Update fractal window
		_h1 = _h2; _h2 = _h3; _h3 = _h4; _h4 = _h5; _h5 = candle.HighPrice;
		_l1 = _l2; _l2 = _l3; _l3 = _l4; _l4 = _l5; _l5 = candle.LowPrice;

		if (_h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
			_up = _h3;
		if (_l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
			_down = _l3;

		var close = candle.ClosePrice;

		// Exit on ATR-based profit target
		if (Position > 0 && atrVal > 0 && close - _entryPrice >= atrVal * MinProfit)
		{
			SellMarket();
			_entryPrice = 0;
			return;
		}
		else if (Position < 0 && atrVal > 0 && _entryPrice - close >= atrVal * MinProfit)
		{
			BuyMarket();
			_entryPrice = 0;
			return;
		}

		if (Position != 0)
			return;

		// Entry conditions
		if (adxVal < AdxLine)
		{
			// Range mode: fractal breakout
			if (_up != 0 && close >= _up && close >= ma1Val)
			{
				BuyMarket();
				_entryPrice = close;
			}
			else if (_down != 0 && close <= _down && close <= ma1Val)
			{
				SellMarket();
				_entryPrice = close;
			}
		}
		else
		{
			// Trend mode: MA alignment
			if (ma1Val >= ma2Val && ma2Val >= ma3Val && close > ma1Val)
			{
				BuyMarket();
				_entryPrice = close;
			}
			else if (ma1Val <= ma2Val && ma2Val <= ma3Val && close < ma1Val)
			{
				SellMarket();
				_entryPrice = close;
			}
		}
	}
}
