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
/// Stochastic-like oscillator strategy using RSI as proxy with K/D crossover logic.
/// Supports long and short trades with percent-based TP/SL.
/// </summary>
public class UltimateStochasticsStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _rsiValues = new();
	private decimal _prevK;
	private decimal _prevD;
	private decimal _entryPrice;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int SmoothLength { get => _smoothLength.Value; set => _smoothLength.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UltimateStochasticsStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_smoothLength = Param(nameof(SmoothLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "Smoothing for D line", "Indicators");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "Overbought level", "Levels");

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "Oversold level", "Levels");

		_takeProfitPct = Param(nameof(TakeProfitPct), 2m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");

		_stopLossPct = Param(nameof(StopLossPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");

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
		_rsiValues.Clear();
		_prevK = 0;
		_prevD = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		_rsiValues.Clear();
		_prevK = 0;
		_prevD = 0;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// RSI as K, smoothed RSI as D
		_rsiValues.Add(rsiValue);
		while (_rsiValues.Count > SmoothLength + 2)
			_rsiValues.RemoveAt(0);

		if (_rsiValues.Count < SmoothLength)
			return;

		var k = rsiValue;
		decimal dSum = 0;
		for (int i = _rsiValues.Count - SmoothLength; i < _rsiValues.Count; i++)
			dSum += _rsiValues[i];
		var d = dSum / SmoothLength;

		if (_prevK == 0)
		{
			_prevK = k;
			_prevD = d;
			return;
		}

		// Check TP/SL
		if (Position > 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice >= _entryPrice * (1m + TakeProfitPct / 100m) ||
				candle.ClosePrice <= _entryPrice * (1m - StopLossPct / 100m))
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice <= _entryPrice * (1m - TakeProfitPct / 100m) ||
				candle.ClosePrice >= _entryPrice * (1m + StopLossPct / 100m))
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry signals: K crosses D in oversold/overbought zone
		var longSignal = _prevK <= _prevD && k > d && k < Oversold;
		var shortSignal = _prevK >= _prevD && k < d && k > Overbought;

		if (longSignal && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}

		_prevK = k;
		_prevD = d;
	}
}
