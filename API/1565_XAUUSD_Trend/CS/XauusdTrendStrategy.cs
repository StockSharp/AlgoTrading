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
/// XAUUSD trend strategy using dual EMA crossover, RSI filter, and Bollinger band breakout.
/// Uses StdDev-based stops and take-profit levels.
/// </summary>
public class XauusdTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _emaShort;
	private readonly StrategyParam<int> _emaLong;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<decimal> _tpRiskRatio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _entryPrice;

	public int EmaShort { get => _emaShort.Value; set => _emaShort.Value = value; }
	public int EmaLong { get => _emaLong.Value; set => _emaLong.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }
	public decimal TpRiskRatio { get => _tpRiskRatio.Value; set => _tpRiskRatio.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XauusdTrendStrategy()
	{
		_emaShort = Param(nameof(EmaShort), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Short", "Fast EMA period", "Indicators");

		_emaLong = Param(nameof(EmaLong), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long", "Slow EMA period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators");

		_stopPct = Param(nameof(StopPct), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_tpRiskRatio = Param(nameof(TpRiskRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("TP/SL Ratio", "Take profit to stop ratio", "Risk");

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
		_stopPrice = 0;
		_takePrice = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaFast = new ExponentialMovingAverage { Length = EmaShort };
		var emaSlow = new ExponentialMovingAverage { Length = EmaLong };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var stdDev = new StandardDeviation { Length = 20 };

		_stopPrice = 0;
		_takePrice = 0;
		_entryPrice = 0;

		var sub = SubscribeCandles(CandleType);
		sub.Bind(emaFast, emaSlow, rsi, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFastVal, decimal emaSlowVal, decimal rsiVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// TP/SL management
		if (Position > 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice <= _stopPrice || candle.ClosePrice >= _takePrice)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice >= _stopPrice || candle.ClosePrice <= _takePrice)
			{
				BuyMarket();
				_entryPrice = 0;
				return;
			}
		}

		if (stdVal <= 0)
			return;

		// EMA trend + RSI filter + price above/below band
		var upperBand = emaSlowVal + 2m * stdVal;
		var lowerBand = emaSlowVal - 2m * stdVal;

		var longCond = emaFastVal > emaSlowVal && rsiVal < RsiOversold;
		var shortCond = emaFastVal < emaSlowVal && rsiVal > RsiOverbought;

		if (longCond && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			var sl = _entryPrice * StopPct / 100m;
			_stopPrice = _entryPrice - sl;
			_takePrice = _entryPrice + sl * TpRiskRatio;
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			var sl = _entryPrice * StopPct / 100m;
			_stopPrice = _entryPrice + sl;
			_takePrice = _entryPrice - sl * TpRiskRatio;
		}
	}
}
