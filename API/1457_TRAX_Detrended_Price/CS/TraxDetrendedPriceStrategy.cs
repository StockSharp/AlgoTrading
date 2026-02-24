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
/// TRAX Detrended Price strategy.
/// Uses triple-smoothed moving average rate of change (TRAX) and Detrended Price Oscillator (DPO).
/// </summary>
public class TraxDetrendedPriceStrategy : Strategy
{
	private readonly StrategyParam<int> _traxLength;
	private readonly StrategyParam<int> _dpoLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();
	private decimal _prevTrax;
	private decimal _prevDpo;
	private bool _isInitialized;

	public int TraxLength { get => _traxLength.Value; set => _traxLength.Value = value; }
	public int DpoLength { get => _dpoLength.Value; set => _dpoLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TraxDetrendedPriceStrategy()
	{
		_traxLength = Param(nameof(TraxLength), 12)
			.SetDisplay("TRAX Length", "Length for TRAX calculation", "Indicators");

		_dpoLength = Param(nameof(DpoLength), 19)
			.SetDisplay("DPO Length", "Length for DPO calculation", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_prevTrax = 0;
		_prevDpo = 0;
		_isInitialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = DpoLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);

		// Need enough bars for DPO lookback
		var barsBack = DpoLength / 2 + 1;
		var minBars = Math.Max(TraxLength * 3, barsBack + 1);

		if (_closes.Count < minBars)
			return;

		// Calculate TRAX: triple smoothed MA rate of change
		var trax = CalculateTrax();

		// Calculate DPO: price - SMA shifted back
		var lagIdx = _closes.Count - 1 - barsBack;
		var dpo = lagIdx >= 0 ? _closes[lagIdx] - smaVal : 0m;

		if (!_isInitialized)
		{
			_prevTrax = trax;
			_prevDpo = dpo;
			_isInitialized = true;
			return;
		}

		// Crossover signals
		var crossOver = _prevDpo <= _prevTrax && dpo > trax;
		var crossUnder = _prevDpo >= _prevTrax && dpo < trax;

		var confirmUp = candle.ClosePrice > smaVal;
		var confirmDown = candle.ClosePrice < smaVal;

		if (crossOver && confirmUp && Position <= 0)
			BuyMarket();
		else if (crossUnder && confirmDown && Position >= 0)
			SellMarket();

		_prevTrax = trax;
		_prevDpo = dpo;

		// Keep buffer manageable
		if (_closes.Count > 500)
			_closes.RemoveRange(0, 200);
	}

	private decimal CalculateTrax()
	{
		var n = _closes.Count;
		var len = TraxLength;

		// Simple triple smoothing of log prices
		decimal sum1 = 0;
		var start1 = Math.Max(0, n - len);
		for (var i = start1; i < n; i++)
			sum1 += (decimal)Math.Log((double)_closes[i]);
		var avg1 = sum1 / Math.Min(len, n);

		// Rate of change of smoothed value
		if (n > len + 1)
		{
			decimal prevSum = 0;
			var pstart = Math.Max(0, n - 1 - len);
			for (var i = pstart; i < n - 1; i++)
				prevSum += (decimal)Math.Log((double)_closes[i]);
			var prevAvg = prevSum / Math.Min(len, n - 1);
			return 10000m * (avg1 - prevAvg);
		}

		return 0m;
	}
}
