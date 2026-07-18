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
/// Zone trading strategy based on Bill Williams' Awesome and Accelerator Oscillators.
/// Buys when both oscillators turn green and sells when both turn red.
/// </summary>
public class ZonalTradingOscillatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _medians = new();
	private readonly List<decimal> _aoValues = new();
	private decimal? _prevAo;
	private decimal? _prevAc;
	private int _aoTrend;
	private int _acTrend;
	private int _lastSignal;

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZonalTradingOscillatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for oscillators", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_medians.Clear();
		_aoValues.Clear();
		_prevAo = null;
		_prevAc = null;
		_aoTrend = 0;
		_acTrend = 0;
		_lastSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_medians.Add((candle.HighPrice + candle.LowPrice) / 2m);
		if (_medians.Count > 34)
			_medians.RemoveAt(0);

		if (_medians.Count < 34)
			return;

		var ao = GetAverage(_medians, 5) - GetAverage(_medians, 34);
		_aoValues.Add(ao);
		if (_aoValues.Count > 5)
			_aoValues.RemoveAt(0);

		if (_aoValues.Count < 5)
		{
			_prevAo = ao;
			return;
		}

		var ac = ao - GetAverage(_aoValues, 5);
		if (_prevAo is not null && _prevAc is not null)
		{
			_aoTrend = ao > _prevAo ? 1 : ao < _prevAo ? -1 : _aoTrend;
			_acTrend = ac > _prevAc ? 1 : ac < _prevAc ? -1 : _acTrend;

			if (_aoTrend > 0 && _acTrend > 0 && _lastSignal != 1 && Position <= 0)
			{
				BuyMarket();
				_lastSignal = 1;
			}
			else if (_aoTrend < 0 && _acTrend < 0 && _lastSignal != -1 && Position >= 0)
			{
				SellMarket();
				_lastSignal = -1;
			}
		}

		_prevAo = ao;
		_prevAc = ac;
	}

	private static decimal GetAverage(List<decimal> values, int length)
	{
		var start = values.Count - length;
		var sum = 0m;

		for (var i = start; i < values.Count; i++)
			sum += values[i];

		return sum / length;
	}
}
