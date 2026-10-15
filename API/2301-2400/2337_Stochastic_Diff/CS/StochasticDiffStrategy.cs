using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading strategy based on the smoothed difference between Stochastic %K and %D.
/// Opens long when the diff turns upward and short when it turns downward.
/// </summary>
public class StochasticDiffStrategy : Strategy
{
	private const int BufferSize = 64;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _cooldownCandles;

	private readonly decimal[] _highs = new decimal[BufferSize];
	private readonly decimal[] _lows = new decimal[BufferSize];
	private readonly decimal[] _rawK = new decimal[BufferSize];

	private int _priceIndex;
	private int _priceCount;
	private int _kIndex;
	private int _kCount;
	private int _barsSinceSignal;
	private decimal? _smoothedDiff;
	private decimal? _prevDiff;
	private decimal? _prevPrevDiff;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public int CooldownCandles { get => _cooldownCandles.Value; set => _cooldownCandles.Value = value; }

	public StochasticDiffStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for analysis", "General");

		_kPeriod = Param(nameof(KPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Stochastic %K period", "Stochastic");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Stochastic %D period", "Stochastic");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Length for diff smoothing", "Stochastic");

		_cooldownCandles = Param(nameof(CooldownCandles), 2)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Candles", "Minimum candles between entries", "Trading");
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

		Array.Clear(_highs);
		Array.Clear(_lows);
		Array.Clear(_rawK);
		_priceIndex = 0;
		_priceCount = 0;
		_kIndex = 0;
		_kCount = 0;
		_barsSinceSignal = CooldownCandles;
		_smoothedDiff = null;
		_prevDiff = null;
		_prevPrevDiff = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		Array.Clear(_highs);
		Array.Clear(_lows);
		Array.Clear(_rawK);
		_priceIndex = 0;
		_priceCount = 0;
		_kIndex = 0;
		_kCount = 0;
		_barsSinceSignal = CooldownCandles;
		_smoothedDiff = null;
		_prevDiff = null;
		_prevPrevDiff = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		PushPrice(candle.HighPrice, candle.LowPrice);
		_barsSinceSignal++;

		if (_priceCount < KPeriod)
			return;

		var highest = GetHighest(KPeriod);
		var lowest = GetLowest(KPeriod);
		var range = highest - lowest;
		var k = range > 0 ? (candle.ClosePrice - lowest) / range * 100m : 50m;

		PushRawK(k);

		if (_kCount < DPeriod)
			return;

		var d = GetRawKAverage(DPeriod);
		var diff = k - d;
		var current = UpdateSmoothedDiff(diff);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPrevDiff = _prevDiff;
			_prevDiff = current;
			return;
		}

		if (_prevPrevDiff.HasValue && _prevDiff.HasValue)
		{
			var turningUp = _prevDiff < _prevPrevDiff && current >= _prevDiff;
			var turningDown = _prevDiff > _prevPrevDiff && current <= _prevDiff;

			if (_barsSinceSignal >= CooldownCandles && k <= 25m && turningUp && Position <= 0)
			{
				BuyMarket();
				_barsSinceSignal = 0;
			}
			else if (_barsSinceSignal >= CooldownCandles && k >= 75m && turningDown && Position >= 0)
			{
				SellMarket();
				_barsSinceSignal = 0;
			}
		}

		_prevPrevDiff = _prevDiff;
		_prevDiff = current;
	}

	private void PushPrice(decimal high, decimal low)
	{
		_highs[_priceIndex] = high;
		_lows[_priceIndex] = low;
		_priceIndex = (_priceIndex + 1) % BufferSize;

		if (_priceCount < BufferSize)
			_priceCount++;
	}

	private void PushRawK(decimal value)
	{
		_rawK[_kIndex] = value;
		_kIndex = (_kIndex + 1) % BufferSize;

		if (_kCount < BufferSize)
			_kCount++;
	}

	private decimal GetHighest(int period)
	{
		var highest = decimal.MinValue;
		var count = Math.Min(period, _priceCount);

		for (var i = 0; i < count; i++)
		{
			var idx = (_priceIndex - 1 - i + BufferSize) % BufferSize;
			if (_highs[idx] > highest)
				highest = _highs[idx];
		}

		return highest;
	}

	private decimal GetLowest(int period)
	{
		var lowest = decimal.MaxValue;
		var count = Math.Min(period, _priceCount);

		for (var i = 0; i < count; i++)
		{
			var idx = (_priceIndex - 1 - i + BufferSize) % BufferSize;
			if (_lows[idx] < lowest)
				lowest = _lows[idx];
		}

		return lowest;
	}

	private decimal GetRawKAverage(int period)
	{
		var count = Math.Min(period, _kCount);
		var sum = 0m;

		for (var i = 0; i < count; i++)
		{
			var idx = (_kIndex - 1 - i + BufferSize) % BufferSize;
			sum += _rawK[idx];
		}

		return sum / count;
	}

	private decimal UpdateSmoothedDiff(decimal value)
	{
		if (_smoothedDiff is null)
		{
			_smoothedDiff = value;
			return value;
		}

		var multiplier = 2m / (SmoothingLength + 1);
		_smoothedDiff = _smoothedDiff.Value + ((value - _smoothedDiff.Value) * multiplier);
		return _smoothedDiff.Value;
	}
}
