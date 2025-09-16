using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractals minimum distance breakout strategy converted from MetaTrader.
/// </summary>
public class FractalsMinimumDistanceStrategy : Strategy
{
	private readonly StrategyParam<int> _distancePips;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpperFractal;
	private decimal? _prevLowerFractal;
	private decimal[] _highs = Array.Empty<decimal>();
	private decimal[] _lows = Array.Empty<decimal>();
	private int _bufferCount;
	private int _windowSize;
	private int _signalOffset;
	private decimal _pipSize;

	public int DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public FractalsMinimumDistanceStrategy()
	{
		_distancePips = Param(nameof(DistancePips), 15)
			.SetDisplay("Distance (pips)", "Minimum allowed gap between the last two fractals", "Risk")
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 3)
			.SetDisplay("Signal bar offset", "How many closed bars ago the fractal must appear", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary candle series used for signals", "Data")
			.SetCanOptimize(false);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevUpperFractal = null;
		_prevLowerFractal = null;
		_highs = Array.Empty<decimal>();
		_lows = Array.Empty<decimal>();
		_bufferCount = 0;
		_windowSize = 0;
		_signalOffset = 0;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeBuffers();
		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void InitializeBuffers()
	{
		_signalOffset = Math.Max(2, SignalBar);
		_windowSize = Math.Max(_signalOffset + 3, 5);
		_highs = new decimal[_windowSize];
		_lows = new decimal[_windowSize];
		_bufferCount = 0;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _windowSize == 0)
			return;

		// Shift the rolling buffers to keep the configured number of historical bars.
		for (var i = 0; i < _windowSize - 1; i++)
		{
			_highs[i] = _highs[i + 1];
			_lows[i] = _lows[i + 1];
		}

		// Append the latest candle extremes.
		_highs[_windowSize - 1] = candle.HighPrice;
		_lows[_windowSize - 1] = candle.LowPrice;

		if (_bufferCount < _windowSize)
			_bufferCount++;

		if (_bufferCount < _windowSize)
			return;

		var centerIndex = _windowSize - 1 - _signalOffset;
		if (centerIndex < 2 || centerIndex > _windowSize - 3)
			return;

		var high = _highs[centerIndex];
		var low = _lows[centerIndex];

		var isUpperFractal =
			high > _highs[centerIndex - 1] &&
			high > _highs[centerIndex - 2] &&
			high > _highs[centerIndex + 1] &&
			high > _highs[centerIndex + 2];

		var isLowerFractal =
			low < _lows[centerIndex - 1] &&
			low < _lows[centerIndex - 2] &&
			low < _lows[centerIndex + 1] &&
			low < _lows[centerIndex + 2];

		var distanceThreshold = DistancePips * _pipSize;

		if (isUpperFractal)
		{
			_prevUpperFractal = high;

			// Close existing long exposure before reversing.
			if (Position > 0)
				SellMarket(Position);

			// Enter a short position if the fractals are far enough apart.
			if (ShouldOpenTrade(distanceThreshold))
				SellMarket(Volume);
		}

		if (isLowerFractal)
		{
			_prevLowerFractal = low;

			// Close existing short exposure before reversing.
			if (Position < 0)
				BuyMarket(-Position);

			// Enter a long position if the fractals are far enough apart.
			if (ShouldOpenTrade(distanceThreshold))
				BuyMarket(Volume);
		}
	}

	private bool ShouldOpenTrade(decimal distanceThreshold)
	{
		if (Volume <= 0 || !IsFormedAndOnlineAndAllowTrading())
			return false;

		if (_prevUpperFractal is not decimal upper || _prevLowerFractal is not decimal lower)
			return false;

		var threshold = Math.Abs(distanceThreshold);
		return Math.Abs(upper - lower) >= threshold;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep;

		if (priceStep is not decimal step || step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);

		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}
