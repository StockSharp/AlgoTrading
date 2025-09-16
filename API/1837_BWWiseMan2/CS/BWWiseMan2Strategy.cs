using System;
using System.Collections.Generic;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		StartProtection();
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

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var buySignal = _ao4 > 0m && _ao3 > 0m && _ao4 < _ao3 && _ao3 > _ao2 && _ao2 > _ao1 && _ao1 > _ao0;
		var sellSignal = _ao4 < 0m && _ao3 < 0m && _ao4 > _ao3 && _ao3 < _ao2 && _ao2 < _ao1 && _ao1 < _ao0;

		if (buySignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
