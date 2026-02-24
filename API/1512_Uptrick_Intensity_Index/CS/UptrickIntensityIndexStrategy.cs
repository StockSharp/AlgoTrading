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
/// Uptrick Intensity Index strategy.
/// Uses trend intensity index calculated from three moving averages.
/// Buys when TII crosses above its average, sells when it crosses below.
/// </summary>
public class UptrickIntensityIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<int> _ma3Length;
	private readonly StrategyParam<int> _tiiSmooth;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _tiiValues = new();
	private decimal _prevDiff;
	private bool _initialized;

	public int Ma1Length { get => _ma1Length.Value; set => _ma1Length.Value = value; }
	public int Ma2Length { get => _ma2Length.Value; set => _ma2Length.Value = value; }
	public int Ma3Length { get => _ma3Length.Value; set => _ma3Length.Value = value; }
	public int TiiSmooth { get => _tiiSmooth.Value; set => _tiiSmooth.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UptrickIntensityIndexStrategy()
	{
		_ma1Length = Param(nameof(Ma1Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Length", "Length of first SMA", "General");

		_ma2Length = Param(nameof(Ma2Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Length", "Length of second SMA", "General");

		_ma3Length = Param(nameof(Ma3Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA3 Length", "Length of third SMA", "General");

		_tiiSmooth = Param(nameof(TiiSmooth), 20)
			.SetGreaterThanZero()
			.SetDisplay("TII Smooth", "TII smoothing length", "General");

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
		_tiiValues.Clear();
		_prevDiff = 0;
		_initialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma1 = new SimpleMovingAverage { Length = Ma1Length };
		var ma2 = new SimpleMovingAverage { Length = Ma2Length };
		var ma3 = new SimpleMovingAverage { Length = Ma3Length };

		_tiiValues.Clear();
		_prevDiff = 0;
		_initialized = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ma1, ma2, ma3, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma1Val, decimal ma2Val, decimal ma3Val)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// TII: average relative distance from 3 MAs
		var rel1 = ma1Val > 0 ? (close - ma1Val) / ma1Val : 0;
		var rel2 = ma2Val > 0 ? (close - ma2Val) / ma2Val : 0;
		var rel3 = ma3Val > 0 ? (close - ma3Val) / ma3Val : 0;

		var tii = (rel1 + rel2 + rel3) / 3m * 100m;
		_tiiValues.Add(tii);

		while (_tiiValues.Count > TiiSmooth + 2)
			_tiiValues.RemoveAt(0);

		if (_tiiValues.Count < TiiSmooth)
			return;

		// SMA of TII
		decimal sum = 0;
		for (int i = _tiiValues.Count - TiiSmooth; i < _tiiValues.Count; i++)
			sum += _tiiValues[i];
		var tiiMa = sum / TiiSmooth;

		var diff = tii - tiiMa;

		if (!_initialized)
		{
			_prevDiff = diff;
			_initialized = true;
			return;
		}

		if (_prevDiff <= 0m && diff > 0m && Position <= 0)
			BuyMarket();
		else if (_prevDiff >= 0m && diff < 0m && Position >= 0)
			SellMarket();

		_prevDiff = diff;
	}
}
