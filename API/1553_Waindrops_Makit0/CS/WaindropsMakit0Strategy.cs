using System;
using System.Collections.Generic;

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
		_periodMinutes = Param(nameof(PeriodMinutes), 60)
			.SetDisplay("Period", "Full period in minutes", "General")
			.SetCanOptimize(true)
			.SetOptimize(30, 120, 30);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
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
			_leftValue = _leftVwap.Process(candle).ToDecimal();
		}
		else
		{
			_rightValue = _rightVwap.Process(candle).ToDecimal();
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

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (_rightValue > _leftValue && Position <= 0)
				BuyMarket();
			else if (_rightValue < _leftValue && Position >= 0)
				SellMarket();
		}
	}
}
