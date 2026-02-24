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
/// Bill Williams Wise Man 2 strategy.
/// Generates signals based on Awesome Oscillator pattern.
/// </summary>
public class BWWiseMan2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _ao0, _ao1, _ao2, _ao3, _ao4;
	private int _aoCount;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BWWiseMan2Strategy"/>.
	/// </summary>
	public BWWiseMan2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ao = new AwesomeOscillator();
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ao, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ao);
			DrawOwnTrades(area);
		}

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Shift AO values to keep last five readings
		_ao4 = _ao3;
		_ao3 = _ao2;
		_ao2 = _ao1;
		_ao1 = _ao0;
		_ao0 = aoValue;

		if (_aoCount < 5)
		{
			_aoCount++;
			return; // Not enough data yet
		}

		// Buy: AO crosses from negative to positive area, or 3 consecutive rising bars
		var buySignal = (_ao2 < 0 && _ao1 < 0 && _ao0 > 0) || (_ao2 < _ao1 && _ao1 < _ao0 && _ao0 > 0);
		// Sell: AO crosses from positive to negative area, or 3 consecutive falling bars
		var sellSignal = (_ao2 > 0 && _ao1 > 0 && _ao0 < 0) || (_ao2 > _ao1 && _ao1 > _ao0 && _ao0 < 0);

		if (buySignal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
	}
}
