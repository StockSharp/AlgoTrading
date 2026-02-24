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
/// Tomas Ratio strategy: momentum scoring with gain/loss ratio.
/// Accumulates signal points based on momentum vs mean, enters when threshold reached.
/// </summary>
public class TomasRatioWithMultiTimeFrameAnalysisStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _entryThreshold;

	private decimal _prevHlc3;
	private decimal _prevSignal;
	private decimal _signalPoints;
	private int _gainsCount;
	private int _lossesCount;
	private decimal _gainsSum;
	private decimal _lossesSum;
	private int _barCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public decimal EntryThreshold { get => _entryThreshold.Value; set => _entryThreshold.Value = value; }

	public TomasRatioWithMultiTimeFrameAnalysisStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle type", "General");

		_lookback = Param(nameof(Lookback), 50)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Lookback for ratio calculation", "Parameters");

		_entryThreshold = Param(nameof(EntryThreshold), 5m)
			.SetDisplay("Entry Threshold", "Signal points threshold for entry", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHlc3 = 0;
		_prevSignal = 0;
		_signalPoints = 0;
		_gainsCount = 0;
		_lossesCount = 0;
		_gainsSum = 0;
		_lossesSum = 0;
		_barCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = 50 };

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

		var hlc3 = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		if (_prevHlc3 == 0)
		{
			_prevHlc3 = hlc3;
			return;
		}

		_barCount++;
		var change = hlc3 - _prevHlc3;

		if (change > 0)
		{
			_gainsCount++;
			_gainsSum += change;
		}
		else if (change < 0)
		{
			_lossesCount++;
			_lossesSum += Math.Abs(change);
		}

		if (_barCount < Lookback)
		{
			_prevHlc3 = hlc3;
			return;
		}

		// Tomas ratio: weighted gain/loss ratio
		var avgGain = _gainsCount > 0 ? _gainsSum / _gainsCount : 0m;
		var avgLoss = _lossesCount > 0 ? _lossesSum / _lossesCount : 1m;
		var ratio = avgLoss != 0 ? avgGain / avgLoss : 1m;
		var signal = (ratio - 1m) * 100m; // positive = bullish momentum

		// Accumulate points
		if (signal > _prevSignal)
			_signalPoints += signal * 0.1m;
		else
			_signalPoints -= Math.Abs(signal) * 0.1m;

		// Clamp
		_signalPoints = Math.Max(-EntryThreshold * 2, Math.Min(EntryThreshold * 2, _signalPoints));

		// Trading logic
		if (_signalPoints >= EntryThreshold && candle.ClosePrice > emaVal && Position <= 0)
		{
			BuyMarket();
			_signalPoints = 0;
		}
		else if (_signalPoints <= -EntryThreshold && candle.ClosePrice < emaVal && Position >= 0)
		{
			SellMarket();
			_signalPoints = 0;
		}
		else if (Position > 0 && signal < _prevSignal && _signalPoints < 0)
		{
			SellMarket();
		}
		else if (Position < 0 && signal > _prevSignal && _signalPoints > 0)
		{
			BuyMarket();
		}

		_prevSignal = signal;
		_prevHlc3 = hlc3;
	}
}
