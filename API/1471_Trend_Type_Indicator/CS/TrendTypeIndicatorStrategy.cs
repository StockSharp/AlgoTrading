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
/// Trend Type Indicator strategy.
/// Detects uptrend, downtrend or sideways market using EMA slope and volatility.
/// Goes long on uptrend, short on downtrend, exits on sideways.
/// </summary>
public class TrendTypeIndicatorStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _stdLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevPrevEma;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int StdLength { get => _stdLength.Value; set => _stdLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendTypeIndicatorStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "EMA period for trend", "General");

		_stdLength = Param(nameof(StdLength), 14)
			.SetDisplay("StdDev Length", "StdDev period for volatility", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = 0;
		_prevPrevEma = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var stdDev = new StandardDeviation { Length = StdLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevEma == 0 || _prevPrevEma == 0 || stdVal <= 0)
		{
			_prevPrevEma = _prevEma;
			_prevEma = emaVal;
			return;
		}

		// Trend detection: EMA slope relative to volatility
		var slope = emaVal - _prevEma;
		var prevSlope = _prevEma - _prevPrevEma;
		var slopeRatio = slope / stdVal;

		// Uptrend: positive slope, downtrend: negative slope, sideways: near zero
		var isUptrend = slopeRatio > 0.1m && slope > 0 && prevSlope > 0;
		var isDowntrend = slopeRatio < -0.1m && slope < 0 && prevSlope < 0;
		var isSideways = !isUptrend && !isDowntrend;

		if (isUptrend && Position <= 0)
			BuyMarket();
		else if (isDowntrend && Position >= 0)
			SellMarket();
		else if (isSideways && Position > 0)
			SellMarket();
		else if (isSideways && Position < 0)
			BuyMarket();

		_prevPrevEma = _prevEma;
		_prevEma = emaVal;
	}
}
