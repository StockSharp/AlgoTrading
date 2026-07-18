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
/// YinYang RSI Volume Trend strategy.
/// Defines dynamic buy/sell zones using SMA, StdDev, and RSI.
/// Enters long when price crosses above lower zone, short when below upper zone.
/// </summary>
public class YinYangRsiVolumeTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevZoneHigh;
	private decimal _prevZoneLow;
	private decimal _prevZoneBasis;
	private bool _initialized;

	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public decimal StopLossMultiplier { get => _stopLossMultiplier.Value; set => _stopLossMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public YinYangRsiVolumeTrendStrategy()
	{
		_trendLength = Param(nameof(TrendLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Lookback length", "General");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("SL Mult %", "Stop distance percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevZoneHigh = 0;
		_prevZoneLow = 0;
		_prevZoneBasis = 0;
		_initialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = TrendLength };
		var stdDev = new StandardDeviation { Length = TrendLength };
		var rsi = new RelativeStrengthIndex { Length = 14 };

		_prevClose = 0;
		_prevZoneHigh = 0;
		_prevZoneLow = 0;
		_prevZoneBasis = 0;
		_initialized = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, stdDev, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal stdVal, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdVal <= 0)
			return;

		// Dynamic zones based on SMA +/- RSI-weighted StdDev
		var rsiWeight = rsiVal / 100m; // 0..1
		var zoneWidth = stdVal * (0.5m + rsiWeight);
		var zoneBasis = smaVal;
		var zoneHigh = zoneBasis + zoneWidth;
		var zoneLow = zoneBasis - zoneWidth;
		var stopHigh = zoneHigh * (1 + StopLossMultiplier / 100m);
		var stopLow = zoneLow * (1 - StopLossMultiplier / 100m);

		var close = candle.ClosePrice;

		if (!_initialized)
		{
			_prevClose = close;
			_prevZoneHigh = zoneHigh;
			_prevZoneLow = zoneLow;
			_prevZoneBasis = zoneBasis;
			_initialized = true;
			return;
		}

		// Cross detections
		var longStart = _prevClose <= _prevZoneLow && close > zoneLow;
		var longEnd = _prevClose <= _prevZoneHigh && close > zoneHigh;
		var longStopLoss = _prevClose >= _prevZoneLow && close < stopLow;

		var shortStart = _prevClose >= _prevZoneHigh && close < zoneHigh;
		var shortEnd = _prevClose >= _prevZoneLow && close < zoneLow;
		var shortStopLoss = _prevClose <= _prevZoneHigh && close > stopHigh;

		// Long entry: price crosses up from below lower zone
		if (longStart && Position <= 0)
		{
			BuyMarket();
		}
		else if (Position > 0 && (longEnd || longStopLoss))
		{
			SellMarket();
		}

		// Short entry: price crosses down from above upper zone
		if (shortStart && Position >= 0)
		{
			SellMarket();
		}
		else if (Position < 0 && (shortEnd || shortStopLoss))
		{
			BuyMarket();
		}

		_prevClose = close;
		_prevZoneHigh = zoneHigh;
		_prevZoneLow = zoneLow;
		_prevZoneBasis = zoneBasis;
	}
}
