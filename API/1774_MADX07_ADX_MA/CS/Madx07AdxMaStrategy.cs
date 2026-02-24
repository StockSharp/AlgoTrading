using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combines moving averages and ADX for trend following entries.
/// Buys when fast MA > slow MA with rising ADX, sells on opposite.
/// </summary>
public class Madx07AdxMaStrategy : Strategy
{
	private readonly StrategyParam<int> _bigMaPeriod;
	private readonly StrategyParam<int> _smallMaPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxLevel;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAdx;
	private decimal _entryPrice;

	public int BigMaPeriod { get => _bigMaPeriod.Value; set => _bigMaPeriod.Value = value; }
	public int SmallMaPeriod { get => _smallMaPeriod.Value; set => _smallMaPeriod.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxLevel { get => _adxLevel.Value; set => _adxLevel.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Madx07AdxMaStrategy()
	{
		_bigMaPeriod = Param(nameof(BigMaPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Big MA Period", "Period of the slower MA", "MA");

		_smallMaPeriod = Param(nameof(SmallMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Small MA Period", "Period of the faster MA", "MA");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX indicator", "ADX");

		_adxLevel = Param(nameof(AdxLevel), 15m)
			.SetDisplay("ADX Level", "Minimal ADX value for entry", "ADX");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAdx = 0;
		_entryPrice = 0;

		var bigMa = new SimpleMovingAverage { Length = BigMaPeriod };
		var smallMa = new ExponentialMovingAverage { Length = SmallMaPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(new IIndicator[] { bigMa, smallMa, adx }, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!values[0].IsFinal || !values[1].IsFinal || !values[2].IsFinal)
			return;

		var bigMaVal = values[0].GetValue<decimal>();
		var smallMaVal = values[1].GetValue<decimal>();

		var adxTyped = (AverageDirectionalIndexValue)values[2];
		if (adxTyped.MovingAverage is not decimal adxVal)
			return;

		// Get +DI and -DI from the Dx sub-value
		var dxVal = adxTyped.Dx;
		if (dxVal.Plus is not decimal plusDi || dxVal.Minus is not decimal minusDi)
			return;

		var price = candle.ClosePrice;

		// Manage exits
		if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit || _entryPrice - price >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				_prevAdx = adxVal;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit || price - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				_prevAdx = adxVal;
				return;
			}
		}

		// Entry logic
		if (Position == 0 && _prevAdx > 0)
		{
			// Buy: fast MA above slow MA, ADX rising and above level, +DI > -DI
			if (smallMaVal > bigMaVal && adxVal > AdxLevel && adxVal > _prevAdx && plusDi > minusDi)
			{
				BuyMarket();
				_entryPrice = price;
			}
			// Sell: fast MA below slow MA, ADX rising and above level, -DI > +DI
			else if (smallMaVal < bigMaVal && adxVal > AdxLevel && adxVal > _prevAdx && minusDi > plusDi)
			{
				SellMarket();
				_entryPrice = price;
			}
		}

		_prevAdx = adxVal;
	}
}
