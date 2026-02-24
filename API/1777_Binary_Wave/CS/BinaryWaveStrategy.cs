using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Binary Wave strategy.
/// Combines multiple indicators into a binary wave signal and trades on zero crossings.
/// </summary>
public class BinaryWaveStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private decimal _prevWave;
	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	public BinaryWaveStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "EMA period", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX period", "Indicators");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevWave = 0;
		_entryPrice = 0;

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(new IIndicator[] { ema, cci, rsi, adx }, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!values[0].IsFinal || !values[1].IsFinal || !values[2].IsFinal || !values[3].IsFinal)
			return;

		var close = candle.ClosePrice;
		var emaVal = values[0].GetValue<decimal>();
		var cciVal = values[1].GetValue<decimal>();
		var rsiVal = values[2].GetValue<decimal>();

		var adxTyped = (AverageDirectionalIndexValue)values[3];
		if (adxTyped.MovingAverage is not decimal adxVal)
			return;
		var dxVal = adxTyped.Dx;
		if (dxVal.Plus is not decimal plusDi || dxVal.Minus is not decimal minusDi)
			return;

		// Compute binary wave: sum of binary signals from each indicator
		var wave = 0m;

		// MA: price above/below EMA
		wave += close > emaVal ? 1m : close < emaVal ? -1m : 0m;

		// CCI: above/below zero
		wave += cciVal > 0 ? 1m : cciVal < 0 ? -1m : 0m;

		// RSI: above/below 50
		wave += rsiVal > 50m ? 1m : rsiVal < 50m ? -1m : 0m;

		// ADX directional: +DI vs -DI
		wave += plusDi > minusDi ? 1m : plusDi < minusDi ? -1m : 0m;

		// Manage exits
		if (Position > 0)
		{
			if (close - _entryPrice >= TakeProfit || _entryPrice - close >= StopLoss || (_prevWave > 0 && wave <= 0))
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - close >= TakeProfit || close - _entryPrice >= StopLoss || (_prevWave < 0 && wave >= 0))
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry on wave zero crossing
		if (Position == 0)
		{
			if (_prevWave <= 0 && wave > 0)
			{
				BuyMarket();
				_entryPrice = close;
			}
			else if (_prevWave >= 0 && wave < 0)
			{
				SellMarket();
				_entryPrice = close;
			}
		}

		_prevWave = wave;
	}
}
