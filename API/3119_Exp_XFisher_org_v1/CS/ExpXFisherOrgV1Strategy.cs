using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 5 expert Exp_XFisher_org_v1.
/// Computes the Fisher transform inline and trades on turning points.
/// </summary>
public class ExpXFisherOrgV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _smoothingLength;

	private Highest _highest;
	private Lowest _lowest;
	private SimpleMovingAverage _smoother;
	private decimal _valuePrev;
	private decimal _fishPrev;
	private decimal? _prevSmoothed;
	private decimal? _prevPrevSmoothed;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }

	public ExpXFisherOrgV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_length = Param(nameof(Length), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fisher Length", "High/Low lookback", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing", "Smoothing MA length", "Indicator");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_valuePrev = 0;
		_fishPrev = 0;
		_prevSmoothed = null;
		_prevPrevSmoothed = null;

		_highest = new Highest { Length = Length };
		_lowest = new Lowest { Length = Length };
		_smoother = new SimpleMovingAverage { Length = SmoothingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highVal, decimal lowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var max = highVal;
		var min = lowVal;
		var range = max - min;
		if (range == 0) range = 0.0000001m;

		var price = candle.ClosePrice;
		var wpr = (price - min) / range;

		var value = (wpr - 0.5m) + 0.67m * _valuePrev;
		value = Math.Min(Math.Max(value, -0.999999m), 0.999999m);

		var denom = 1m - value;
		if (denom == 0) denom = 1m;

		var ratio = (1m + value) / denom;
		if (ratio < 0.0000001m) ratio = 1m;

		var fish = 0.5m * (decimal)Math.Log((double)ratio) + 0.5m * _fishPrev;

		_valuePrev = value;
		_fishPrev = fish;

		// smooth the fisher
		var smoothed = _smoother.Process(new DecimalIndicatorValue(_smoother, fish, candle.OpenTime) { IsFinal = true });
		if (!smoothed.IsFinal || !smoothed.IsFormed)
			return;

		var fisher = smoothed.ToDecimal();

		if (_prevSmoothed == null)
		{
			_prevSmoothed = fisher;
			return;
		}

		if (_prevPrevSmoothed == null)
		{
			_prevPrevSmoothed = _prevSmoothed;
			_prevSmoothed = fisher;
			return;
		}

		var curr = fisher;
		var prev = _prevSmoothed.Value;
		var prior = _prevPrevSmoothed.Value;

		_prevPrevSmoothed = _prevSmoothed;
		_prevSmoothed = fisher;

		// turning points: Fisher was going up and now turns down, or vice versa
		var buySignal = prev < prior && curr > prev; // turning up
		var sellSignal = prev > prior && curr < prev; // turning down

		if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
