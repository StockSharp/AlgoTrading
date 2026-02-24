using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Resonance Hunter" MetaTrader expert.
/// Uses multiple Stochastic oscillators on different periods to find
/// resonance (all pointing same direction) for entry signals.
/// </summary>
public class ResonanceHunterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastKPeriod;
	private readonly StrategyParam<int> _slowKPeriod;
	private readonly StrategyParam<int> _dPeriod;

	// Manual stochastic: track highest high and lowest low over K periods
	private readonly decimal[] _highs1 = new decimal[100];
	private readonly decimal[] _lows1 = new decimal[100];
	private readonly decimal[] _highs2 = new decimal[100];
	private readonly decimal[] _lows2 = new decimal[100];
	private int _barCount;

	private decimal? _prevFastK;
	private decimal? _prevSlowK;
	private decimal? _prevFastD;
	private decimal? _prevSlowD;

	// Simple smoothing queues for %D
	private readonly decimal[] _fastKHistory = new decimal[3];
	private readonly decimal[] _slowKHistory = new decimal[3];
	private int _fastKCount;
	private int _slowKCount;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastKPeriod
	{
		get => _fastKPeriod.Value;
		set => _fastKPeriod.Value = value;
	}

	public int SlowKPeriod
	{
		get => _slowKPeriod.Value;
		set => _slowKPeriod.Value = value;
	}

	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	public ResonanceHunterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastKPeriod = Param(nameof(FastKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast K Period", "Fast stochastic K period", "Indicators");

		_slowKPeriod = Param(nameof(SlowKPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow K Period", "Slow stochastic K period", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Smoothing period for %D line", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barCount = 0;
		_prevFastK = null;
		_prevSlowK = null;
		_prevFastD = null;
		_prevSlowD = null;
		_fastKCount = 0;
		_slowKCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var idx = _barCount % 100;
		_highs1[idx] = candle.HighPrice;
		_lows1[idx] = candle.LowPrice;
		_highs2[idx] = candle.HighPrice;
		_lows2[idx] = candle.LowPrice;
		_barCount++;

		if (_barCount < SlowKPeriod)
			return;

		// Calculate fast stochastic %K
		var fastK = CalculateStochasticK(_highs1, _lows1, candle.ClosePrice, FastKPeriod);
		// Calculate slow stochastic %K
		var slowK = CalculateStochasticK(_highs2, _lows2, candle.ClosePrice, SlowKPeriod);

		if (fastK == null || slowK == null)
			return;

		// Calculate %D as SMA of %K
		var fastD = AddToSmoothing(_fastKHistory, ref _fastKCount, fastK.Value, DPeriod);
		var slowD = AddToSmoothing(_slowKHistory, ref _slowKCount, slowK.Value, DPeriod);

		if (fastD == null || slowD == null || _prevFastK == null || _prevSlowK == null || _prevFastD == null || _prevSlowD == null)
		{
			_prevFastK = fastK;
			_prevSlowK = slowK;
			_prevFastD = fastD;
			_prevSlowD = slowD;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Resonance buy: both stochastics cross above their D lines
		var fastBullCross = _prevFastK.Value < _prevFastD.Value && fastK.Value > fastD.Value;
		var slowBullCross = _prevSlowK.Value < _prevSlowD.Value && slowK.Value > slowD.Value;
		var bothOversold = fastK.Value < 50 && slowK.Value < 50;

		// Resonance sell: both stochastics cross below their D lines
		var fastBearCross = _prevFastK.Value > _prevFastD.Value && fastK.Value < fastD.Value;
		var slowBearCross = _prevSlowK.Value > _prevSlowD.Value && slowK.Value < slowD.Value;
		var bothOverbought = fastK.Value > 50 && slowK.Value > 50;

		// Buy when both signals confirm or fast crosses with slow already bullish
		var buySignal = (fastBullCross && (slowBullCross || slowK.Value > slowD.Value)) && bothOversold;
		// Sell when both signals confirm or fast crosses with slow already bearish
		var sellSignal = (fastBearCross && (slowBearCross || slowK.Value < slowD.Value)) && bothOverbought;

		if (buySignal)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (sellSignal)
		{
			if (Position > 0)
				SellMarket(Position);
			if (Position >= 0)
				SellMarket(volume);
		}

		_prevFastK = fastK;
		_prevSlowK = slowK;
		_prevFastD = fastD;
		_prevSlowD = slowD;
	}

	private decimal? CalculateStochasticK(decimal[] highs, decimal[] lows, decimal close, int period)
	{
		if (_barCount < period)
			return null;

		var hh = decimal.MinValue;
		var ll = decimal.MaxValue;

		for (var i = 0; i < period; i++)
		{
			var idx = (_barCount - 1 - i) % 100;
			if (idx < 0) idx += 100;
			if (highs[idx] > hh) hh = highs[idx];
			if (lows[idx] < ll) ll = lows[idx];
		}

		var range = hh - ll;
		if (range <= 0)
			return 50m;

		return (close - ll) / range * 100m;
	}

	private static decimal? AddToSmoothing(decimal[] history, ref int count, decimal value, int period)
	{
		var idx = count % history.Length;
		history[idx] = value;
		count++;

		if (count < period)
			return null;

		var sum = 0m;
		var n = Math.Min(period, history.Length);
		for (var i = 0; i < n; i++)
		{
			var j = (count - 1 - i) % history.Length;
			if (j < 0) j += history.Length;
			sum += history[j];
		}

		return sum / n;
	}
}
