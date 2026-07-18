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
/// Simplified Waindrops strategy comparing VWAP of two period halves.
/// </summary>
public class WaindropsMakit0Strategy : Strategy
{
	private readonly StrategyParam<int> _periodMinutes;
	private readonly StrategyParam<DataType> _candleType;

	private VolumeWeightedMovingAverage _leftVwap;
	private VolumeWeightedMovingAverage _rightVwap;
	private int _counter;
	private decimal _leftValue;
	private decimal _rightValue;

	public int PeriodMinutes { get => _periodMinutes.Value; set => _periodMinutes.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public WaindropsMakit0Strategy()
	{
		_periodMinutes = Param(nameof(PeriodMinutes), 120)
			.SetDisplay("Period", "Full period in candles", "General")

			.SetOptimize(30, 120, 30);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_leftVwap = null;
		_rightVwap = null;
		_counter = 0;
		_leftValue = 0m;
		_rightValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_leftVwap = new VolumeWeightedMovingAverage();
		_rightVwap = new VolumeWeightedMovingAverage();

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

		var half = PeriodMinutes / 2;
		if (_counter < half)
		{
			var res = _leftVwap.Process(candle);
			if (res.IsEmpty)
				return;
			_leftValue = res.ToDecimal();
		}
		else
		{
			var res = _rightVwap.Process(candle);
			if (res.IsEmpty)
				return;
			_rightValue = res.ToDecimal();
		}

		_counter++;

		if (_counter == half)
		{
			_rightVwap.Reset();
		}
		else if (_counter >= PeriodMinutes)
		{
			_counter = 0;
			_leftVwap.Reset();
			_rightVwap.Reset();

			if (_rightValue > _leftValue && Position <= 0)
				BuyMarket();
			else if (_rightValue < _leftValue && Position >= 0)
				SellMarket();
		}
	}
}
