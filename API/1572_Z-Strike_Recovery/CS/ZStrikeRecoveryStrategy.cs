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
/// Long when price change Z-Score exceeds threshold and exit after fixed periods.
/// Computes Z-score of bar-to-bar price changes.
/// </summary>
public class ZStrikeRecoveryStrategy : Strategy
{
	private readonly StrategyParam<int> _zLength;
	private readonly StrategyParam<decimal> _zThreshold;
	private readonly StrategyParam<int> _exitPeriods;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _changes = new();
	private decimal _prevClose;
	private int _barsInPosition;

	public int ZLength { get => _zLength.Value; set => _zLength.Value = value; }
	public decimal ZThreshold { get => _zThreshold.Value; set => _zThreshold.Value = value; }
	public int ExitPeriods { get => _exitPeriods.Value; set => _exitPeriods.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZStrikeRecoveryStrategy()
	{
		_zLength = Param(nameof(ZLength), 16)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Length", "Lookback length for z-score", "Indicators");

		_zThreshold = Param(nameof(ZThreshold), 1.3m)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Threshold", "Entry threshold", "Trading");

		_exitPeriods = Param(nameof(ExitPeriods), 10)
			.SetGreaterThanZero()
			.SetDisplay("Exit Periods", "Bars to hold position", "Trading");

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
		_changes.Clear();
		_prevClose = 0;
		_barsInPosition = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 2 };

		_changes.Clear();
		_prevClose = 0;
		_barsInPosition = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _dummy)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var change = candle.ClosePrice - _prevClose;
		_prevClose = candle.ClosePrice;

		_changes.Add(change);
		if (_changes.Count > ZLength * 2)
			_changes.RemoveAt(0);

		// Position management: exit after N bars
		if (Position != 0)
		{
			_barsInPosition++;

			if (_barsInPosition >= ExitPeriods)
			{
				if (Position > 0)
					SellMarket();
				else
					BuyMarket();
				_barsInPosition = 0;
			}
		}

		if (_changes.Count < ZLength)
			return;

		// Compute Z-score
		var recent = _changes.Skip(_changes.Count - ZLength).ToList();
		var mean = recent.Average();
		var sumSq = recent.Sum(v => (v - mean) * (v - mean));
		var std = (decimal)Math.Sqrt((double)(sumSq / ZLength));

		if (std == 0)
			return;

		var z = (change - mean) / std;

		// Entry: Z-score spike above threshold (strong upward move)
		if (z > ZThreshold && Position == 0)
		{
			BuyMarket();
			_barsInPosition = 0;
		}
		// Also allow short on extreme negative Z-score
		else if (z < -ZThreshold && Position == 0)
		{
			SellMarket();
			_barsInPosition = 0;
		}
	}
}
