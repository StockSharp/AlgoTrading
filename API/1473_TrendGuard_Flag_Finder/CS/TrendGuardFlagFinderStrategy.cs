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
/// TrendGuard Flag Finder strategy.
/// Uses EMA trend direction and detects consolidation (flag) patterns
/// after a strong move (pole), then enters on breakout from consolidation.
/// </summary>
public class TrendGuardFlagFinderStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _poleMinPct;
	private readonly StrategyParam<decimal> _squeezePct;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _candles = new();

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public decimal PoleMinPct { get => _poleMinPct.Value; set => _poleMinPct.Value = value; }
	public decimal SqueezePct { get => _squeezePct.Value; set => _squeezePct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendGuardFlagFinderStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "EMA for trend direction", "Trend");

		_lookback = Param(nameof(Lookback), 10)
			.SetDisplay("Lookback", "Bars to look back for pole and flag", "Flag");

		_poleMinPct = Param(nameof(PoleMinPct), 0.3m)
			.SetDisplay("Pole Min %", "Min percent move for pole detection", "Flag");

		_squeezePct = Param(nameof(SqueezePct), 0.5m)
			.SetDisplay("Squeeze %", "Max range percent for flag consolidation", "Flag");

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
		_candles.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Add(candle);

		// Keep enough candles for analysis
		var needed = Lookback * 2 + 2;
		if (_candles.Count > needed)
			_candles.RemoveAt(0);

		if (_candles.Count < Lookback + 2)
			return;

		var count = _candles.Count;
		var isUptrend = candle.ClosePrice > emaVal;
		var isDowntrend = candle.ClosePrice < emaVal;

		// Split into pole period (first half) and flag period (second half)
		var halfLen = Lookback / 2;
		if (halfLen < 2) halfLen = 2;

		// Pole: bars from [count - Lookback] to [count - halfLen - 1]
		// Flag: bars from [count - halfLen] to [count - 2] (excluding current)
		var poleStart = count - Lookback;
		var poleEnd = count - halfLen - 1;
		var flagStart = count - halfLen;
		var flagEnd = count - 2;

		if (poleStart < 0 || poleEnd <= poleStart || flagEnd < flagStart)
			return;

		// Calculate pole move
		var poleOpenPrice = _candles[poleStart].OpenPrice;
		var poleClosePrice = _candles[poleEnd].ClosePrice;

		var poleHighest = 0m;
		var poleLowest = decimal.MaxValue;
		for (var i = poleStart; i <= poleEnd; i++)
		{
			if (_candles[i].HighPrice > poleHighest) poleHighest = _candles[i].HighPrice;
			if (_candles[i].LowPrice < poleLowest) poleLowest = _candles[i].LowPrice;
		}

		var poleRange = poleLowest > 0 ? ((poleHighest - poleLowest) / poleLowest) * 100m : 0m;
		var poleBullish = poleClosePrice > poleOpenPrice;
		var poleBearish = poleClosePrice < poleOpenPrice;

		// Calculate flag consolidation range
		var flagHigh = 0m;
		var flagLow = decimal.MaxValue;
		for (var i = flagStart; i <= flagEnd; i++)
		{
			if (_candles[i].HighPrice > flagHigh) flagHigh = _candles[i].HighPrice;
			if (_candles[i].LowPrice < flagLow) flagLow = _candles[i].LowPrice;
		}

		var flagRange = flagLow > 0 ? ((flagHigh - flagLow) / flagLow) * 100m : 0m;

		// Flag must be a tighter range than the pole (consolidation)
		var hasStrongPole = poleRange >= PoleMinPct;
		var hasTightFlag = flagRange <= SqueezePct && flagRange < poleRange;

		if (!hasStrongPole || !hasTightFlag)
			return;

		// Bull flag: pole was up, flag consolidates, breakout above flag high
		if (poleBullish && isUptrend && candle.ClosePrice > flagHigh && Position <= 0)
		{
			BuyMarket();
		}
		// Bear flag: pole was down, flag consolidates, breakout below flag low
		else if (poleBearish && isDowntrend && candle.ClosePrice < flagLow && Position >= 0)
		{
			SellMarket();
		}
	}
}
