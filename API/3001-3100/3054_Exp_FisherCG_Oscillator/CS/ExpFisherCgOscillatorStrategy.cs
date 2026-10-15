namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo.Candles;

/// <summary>
/// Fisher Center of Gravity oscillator crossover strategy.
/// </summary>
public class ExpFisherCgOscillatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly List<decimal> _medianPrices = new();
	private readonly List<decimal> _cgValues = new();
	private readonly decimal[] _valueBuffer = new decimal[4];
	private int _valueCount;
	private decimal? _previousFisher;

	private readonly List<(decimal Main, decimal Trigger)> _oscillatorHistory = new();
	private decimal? _entryPrice;
	private int _length = 10;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpFisherCgOscillatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_medianPrices.Clear();
		_cgValues.Clear();
		Array.Clear(_valueBuffer);
		_valueCount = 0;
		_previousFisher = null;
		_oscillatorHistory.Clear();
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		OnReseted();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate Fisher CG oscillator inline
		var price = (candle.HighPrice + candle.LowPrice) / 2m;
		_medianPrices.Add(price);
		while (_medianPrices.Count > _length)
			_medianPrices.RemoveAt(0);

		if (_medianPrices.Count < _length)
			return;

		decimal num = 0m;
		decimal denom = 0m;
		var weight = 1;

		for (var index = _medianPrices.Count - 1; index >= 0; index--)
		{
			var median = _medianPrices[index];
			num += weight * median;
			denom += median;
			weight++;
		}

		decimal cg;
		if (denom != 0m)
			cg = -num / denom + (_length + 1m) / 2m;
		else
			cg = 0m;

		_cgValues.Add(cg);
		while (_cgValues.Count > _length)
			_cgValues.RemoveAt(0);

		var high = cg;
		var low = cg;
		for (var i = 0; i < _cgValues.Count; i++)
		{
			var v = _cgValues[i];
			if (v > high) high = v;
			if (v < low) low = v;
		}

		decimal normalized;
		if (high != low)
			normalized = (cg - low) / (high - low);
		else
			normalized = 0m;

		var limit = Math.Min(_valueCount, 3);
		for (var shift = limit; shift > 0; shift--)
			_valueBuffer[shift] = _valueBuffer[shift - 1];

		_valueBuffer[0] = normalized;
		if (_valueCount < 4)
			_valueCount++;

		if (_valueCount < 4)
			return;

		var value2 = (4m * _valueBuffer[0] + 3m * _valueBuffer[1] + 2m * _valueBuffer[2] + _valueBuffer[3]) / 10m;
		var x = 1.98m * (value2 - 0.5m);
		if (x > 0.999m)
			x = 0.999m;
		else if (x < -0.999m)
			x = -0.999m;

		var numerator = 1m + x;
		var denominator = 1m - x;
		if (denominator == 0m)
			denominator = 0.0000001m;

		var ratio = numerator / denominator;
		if (ratio <= 0m)
			ratio = 0.0000001m;

		var fisher = 0.5m * (decimal)Math.Log((double)ratio);
		var trigger = _previousFisher ?? fisher;
		_previousFisher = fisher;

		// Store history
		_oscillatorHistory.Add((fisher, trigger));
		while (_oscillatorHistory.Count > 10)
			_oscillatorHistory.RemoveAt(0);

		if (_oscillatorHistory.Count < 3)
			return;

		// Handle risk management
		HandleRiskManagement(candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var current = _oscillatorHistory[^1];
		var previous = _oscillatorHistory[^2];

		var previousAbove = previous.Main > previous.Trigger;
		var previousBelow = previous.Main < previous.Trigger;

		var buyOpen = previousAbove && current.Main <= current.Trigger;
		var sellOpen = previousBelow && current.Main >= current.Trigger;

		var buyClose = previousBelow;
		var sellClose = previousAbove;

		if (sellClose && Position < 0)
		{
			BuyMarket();
			_entryPrice = null;
		}

		if (buyClose && Position > 0)
		{
			SellMarket();
			_entryPrice = null;
		}

		if (buyOpen && Position <= 0)
		{
			if (Position < 0)
			{
				BuyMarket();
				_entryPrice = null;
				return;
			}

			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (sellOpen && Position >= 0)
		{
			if (Position > 0)
			{
				SellMarket();
				_entryPrice = null;
				return;
			}

			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
	}

	private void HandleRiskManagement(decimal closePrice)
	{
		if (_entryPrice is null || Position == 0)
			return;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m) step = 1m;

		var stopDistance = 1000 * step;
		var takeDistance = 2000 * step;

		if (Position > 0)
		{
			if (closePrice <= _entryPrice.Value - stopDistance)
			{
				SellMarket();
				_entryPrice = null;
				return;
			}
			if (closePrice >= _entryPrice.Value + takeDistance)
			{
				SellMarket();
				_entryPrice = null;
			}
		}
		else if (Position < 0)
		{
			if (closePrice >= _entryPrice.Value + stopDistance)
			{
				BuyMarket();
				_entryPrice = null;
				return;
			}
			if (closePrice <= _entryPrice.Value - takeDistance)
			{
				BuyMarket();
				_entryPrice = null;
			}
		}
	}
}
