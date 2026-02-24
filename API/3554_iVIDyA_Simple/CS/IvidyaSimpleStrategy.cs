using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "iVIDyA Simple" MetaTrader expert.
/// Computes a Variable Index Dynamic Average using CMO and EMA smoothing.
/// Trades when price crosses above/below the VIDYA line.
/// </summary>
public class IvidyaSimpleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cmoPeriod;
	private readonly StrategyParam<int> _emaPeriod;

	private ChandeMomentumOscillator _cmo;
	private readonly List<decimal> _vidyaHistory = new();

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CmoPeriod
	{
		get => _cmoPeriod.Value;
		set => _cmoPeriod.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public IvidyaSimpleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");

		_cmoPeriod = Param(nameof(CmoPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("CMO Period", "Chande Momentum Oscillator length", "Indicator");

		_emaPeriod = Param(nameof(EmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Base EMA length used by VIDYA", "Indicator");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cmo = new ChandeMomentumOscillator { Length = CmoPeriod };
		_vidyaHistory.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cmo, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cmoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_cmo.IsFormed)
			return;

		var close = candle.ClosePrice;

		// VIDYA = alpha * |CMO/100| * price + (1 - alpha * |CMO/100|) * prevVidya
		var alpha = 2m / (EmaPeriod + 1m);
		var momentumFactor = Math.Abs(cmoValue) / 100m;
		var sf = alpha * momentumFactor;

		var prevVidya = _vidyaHistory.Count == 0 ? close : _vidyaHistory[^1];
		var currentVidya = sf * close + (1m - sf) * prevVidya;

		_vidyaHistory.Add(currentVidya);
		if (_vidyaHistory.Count > 5)
			_vidyaHistory.RemoveAt(0);

		if (_vidyaHistory.Count < 2)
			return;

		var vidya = currentVidya;
		var open = candle.OpenPrice;

		// Price crosses above VIDYA -> buy
		var crossUp = open < vidya && close > vidya;
		// Price crosses below VIDYA -> sell
		var crossDown = open > vidya && close < vidya;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		if (crossUp)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossDown)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}
	}
}
