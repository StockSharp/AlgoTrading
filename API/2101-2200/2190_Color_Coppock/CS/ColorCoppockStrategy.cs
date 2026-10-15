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
/// Strategy based on the Coppock Curve oscillator.
/// Uses ROC sum smoothed by SMA.
/// </summary>
public class ColorCoppockStrategy : Strategy
{
	private readonly StrategyParam<int> _roc1Period;
	private readonly StrategyParam<int> _roc2Period;
	private readonly StrategyParam<int> _smoothingPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();
	private readonly List<decimal> _coppockValues = new();
	private decimal? _prevCoppock;
	private decimal? _prevPrevCoppock;

	public int Roc1Period { get => _roc1Period.Value; set => _roc1Period.Value = value; }
	public int Roc2Period { get => _roc2Period.Value; set => _roc2Period.Value = value; }
	public int SmoothingPeriod { get => _smoothingPeriod.Value; set => _smoothingPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorCoppockStrategy()
	{
		_roc1Period = Param(nameof(Roc1Period), 14)
			.SetDisplay("ROC1 Period", "First ROC calculation period", "Parameters");
		_roc2Period = Param(nameof(Roc2Period), 10)
			.SetDisplay("ROC2 Period", "Second ROC calculation period", "Parameters");
		_smoothingPeriod = Param(nameof(SmoothingPeriod), 10)
			.SetDisplay("Smoothing Period", "SMA period for ROC sum", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for processing", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_coppockValues.Clear();
		_prevCoppock = _prevPrevCoppock = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new ExponentialMovingAverage { Length = Roc1Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);
		var maxPeriod = Math.Max(Roc1Period, Roc2Period);
		if (_closes.Count > maxPeriod + SmoothingPeriod + 5)
			_closes.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_closes.Count <= maxPeriod)
			return;

		// Calculate ROC values
		var idx = _closes.Count - 1;
		decimal roc1 = 0, roc2 = 0;

		if (idx >= Roc1Period && _closes[idx - Roc1Period] != 0)
			roc1 = (_closes[idx] - _closes[idx - Roc1Period]) / _closes[idx - Roc1Period] * 100m;
		if (idx >= Roc2Period && _closes[idx - Roc2Period] != 0)
			roc2 = (_closes[idx] - _closes[idx - Roc2Period]) / _closes[idx - Roc2Period] * 100m;

		_coppockValues.Add(roc1 + roc2);
		if (_coppockValues.Count > SmoothingPeriod + 5)
			_coppockValues.RemoveAt(0);

		if (_coppockValues.Count < SmoothingPeriod)
			return;

		// SMA of ROC sum
		var coppock = _coppockValues.Skip(_coppockValues.Count - SmoothingPeriod).Average();

		if (_prevCoppock is decimal prev && _prevPrevCoppock is decimal prevPrev)
		{
			// Buy when Coppock turns up from a bottom
			if (prev < prevPrev && coppock > prev && Position <= 0)
				BuyMarket();
			// Sell when Coppock turns down from a top
			else if (prev > prevPrev && coppock < prev && Position >= 0)
				SellMarket();
		}

		_prevPrevCoppock = _prevCoppock;
		_prevCoppock = coppock;
	}
}
