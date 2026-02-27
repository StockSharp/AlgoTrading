using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a digital FIR filter crossing a trigger line.
/// Buys when the digital filter crosses below the trigger (reversal expected up).
/// Sells when the filter crosses above the trigger (reversal expected down).
/// </summary>
public class DigitalFilterT01Strategy : Strategy
{
	private static readonly decimal[] _coeffs =
	{
		0.24470985659780m, 0.23139774006970m, 0.20613796947320m, 0.17166230340640m,
		0.13146907903600m, 0.08950387549560m, 0.04960091651250m, 0.01502270569607m,
		-0.01188033734430m, -0.02989873856137m, -0.03898967104900m, -0.04014113626390m,
		-0.03511968085800m, -0.02611613850342m, -0.01539056955666m, -0.00495353651394m,
		0.00368588764825m, 0.00963614049782m, 0.01265138888314m, 0.01307496106868m,
		0.01169702291063m, 0.00974841844086m, 0.00898900012545m, -0.00649745721156m
	};

	private readonly StrategyParam<decimal> _halfChannel;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _prices = new();
	private decimal _prevDigital;
	private decimal _prevTrigger;
	private bool _hasPrev;

	public decimal HalfChannel
	{
		get => _halfChannel.Value;
		set => _halfChannel.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DigitalFilterT01Strategy()
	{
		_halfChannel = Param(nameof(HalfChannel), 50m)
			.SetDisplay("Half Channel", "Half channel distance for trigger", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for the strategy", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prices.Clear();
		_prevDigital = 0;
		_prevTrigger = 0;
		_hasPrev = false;

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

		_prices.Enqueue(candle.ClosePrice);
		if (_prices.Count > _coeffs.Length)
			_prices.Dequeue();

		if (_prices.Count < _coeffs.Length)
			return;

		var arr = new decimal[_coeffs.Length];
		_prices.CopyTo(arr, 0);

		decimal digital = 0;
		for (var i = 0; i < _coeffs.Length; i++)
			digital += _coeffs[i] * arr[_coeffs.Length - 1 - i];

		var prevClose = arr[^2];
		var trigger = digital >= prevClose ? prevClose + HalfChannel : prevClose - HalfChannel;

		if (!_hasPrev)
		{
			_prevDigital = digital;
			_prevTrigger = trigger;
			_hasPrev = true;
			return;
		}

		// Cross detection
		if (_prevDigital > _prevTrigger && digital < trigger)
		{
			// Filter crossed below trigger - buy signal
			if (Position <= 0)
				BuyMarket();
		}
		else if (_prevDigital < _prevTrigger && digital > trigger)
		{
			// Filter crossed above trigger - sell signal
			if (Position >= 0)
				SellMarket();
		}

		_prevDigital = digital;
		_prevTrigger = trigger;
	}
}
