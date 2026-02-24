using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot breakout strategy with ATR and ADX filters.
/// Enters when price breaks previous support or resistance levels.
/// </summary>
public class EmVolStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;
	private decimal _prevAtr;
	private decimal _entryPrice;
	private decimal _stopPrice;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EmVolStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 1000m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk");

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR period", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "ADX period", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 50m)
			.SetDisplay("ADX Threshold", "Trades allowed when ADX below", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var adx = new ADX { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { atr, adx }, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (values[0].IsEmpty || values[1].IsEmpty)
			return;

		var atr = values[0].GetValue<decimal>();
		var adxTyped = (AverageDirectionalIndexValue)values[1];
		if (adxTyped.MovingAverage is not decimal adxVal)
			return;

		if (_prevCandle == null)
		{
			_prevCandle = candle;
			_prevAtr = atr;
			return;
		}

		var res1 = _prevCandle.HighPrice + _prevAtr;
		var sup1 = _prevCandle.LowPrice - _prevAtr;
		var price = candle.ClosePrice;

		if (Position == 0)
		{
			if (adxVal < AdxThreshold)
			{
				if (price > res1)
				{
					BuyMarket();
					_entryPrice = price;
					_stopPrice = price - StopLoss;
				}
				else if (price < sup1)
				{
					SellMarket();
					_entryPrice = price;
					_stopPrice = price + StopLoss;
				}
			}
		}
		else if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit || price <= _stopPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit || price >= _stopPrice)
				BuyMarket();
		}

		_prevCandle = candle;
		_prevAtr = atr;
	}
}
