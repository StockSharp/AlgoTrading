using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy using EMA crossover with ADX and RSI filters.
/// </summary>
public class TigerEmaAdxRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _takePrice;
	private decimal _stopPrice;

	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiUpper { get => _rsiUpper.Value; set => _rsiUpper.Value = value; }
	public decimal RsiLower { get => _rsiLower.Value; set => _rsiLower.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TigerEmaAdxRsiStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 21)
			.SetDisplay("Fast EMA", "Fast EMA period", "Parameters")
			.SetOptimize(5, 50, 5);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 89)
			.SetDisplay("Slow EMA", "Slow EMA period", "Parameters")
			.SetOptimize(50, 200, 10);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "ADX calculation period", "Parameters")
			.SetOptimize(7, 28, 7);

		_adxThreshold = Param(nameof(AdxThreshold), 27m)
			.SetDisplay("ADX Threshold", "Minimum ADX value", "Parameters")
			.SetOptimize(20m, 40m, 5m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Parameters")
			.SetOptimize(7, 28, 7);

		_rsiUpper = Param(nameof(RsiUpper), 65m)
			.SetDisplay("RSI Upper", "Upper RSI bound", "Parameters")
			.SetOptimize(60m, 80m, 5m);

		_rsiLower = Param(nameof(RsiLower), 35m)
			.SetDisplay("RSI Lower", "Lower RSI bound", "Parameters")
			.SetOptimize(20m, 40m, 5m);

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk")
			.SetOptimize(100m, 1000m, 100m);

		_stopLoss = Param(nameof(StopLoss), 200m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk")
			.SetOptimize(50m, 500m, 50m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);

		var fastEma = new ExponentialMovingAverage { Length = FastMaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowMaPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, adx, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue adxValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFinal || !slowValue.IsFinal || !adxValue.IsFinal || !rsiValue.IsFinal)
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adxVal)
			return;

		var trendUp = fast > slow;
		var trendDown = fast < slow;
		var canTrade = adxVal > AdxThreshold && rsi > RsiLower && rsi < RsiUpper;

		if (Position == 0)
		{
			if (canTrade)
			{
				if (trendUp)
				{
					BuyMarket();
					_takePrice = candle.ClosePrice + TakeProfit;
					_stopPrice = candle.ClosePrice - StopLoss;
				}
				else if (trendDown)
				{
					SellMarket();
					_takePrice = candle.ClosePrice - TakeProfit;
					_stopPrice = candle.ClosePrice + StopLoss;
				}
			}
		}
		else if (Position > 0)
		{
			if (candle.ClosePrice >= _takePrice || candle.ClosePrice <= _stopPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= _takePrice || candle.ClosePrice >= _stopPrice)
				BuyMarket();
		}
	}
}
