using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fourier Smoothed Volume Zone Oscillator strategy.
/// The Volume Zone Oscillator is 100 times the EMA of volume signed by the close-to-close direction divided by the EMA of volume,
/// both over VzoLength candles. It is smoothed by rebuilding its latest value from the mean and the fundamental harmonic of a
/// discrete Fourier transform over the last SmoothLength values. Above Threshold the strategy is long, below -Threshold short,
/// reversing an opposite position; between them the position is closed when CloseAllPositions is set.
/// </summary>
public class FourierSmoothedVzoStrategy : Strategy
{
	private readonly StrategyParam<int> _vzoLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _closeAllPositions;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _signedVolumeEma;
	private ExponentialMovingAverage _volumeEma;
	private readonly List<decimal> _vzoValues = [];
	private decimal? _prevClose;

	/// <summary>
	/// EMA length of the oscillator.
	/// </summary>
	public int VzoLength
	{
		get => _vzoLength.Value;
		set => _vzoLength.Value = value;
	}

	/// <summary>
	/// Window of the Fourier smoothing.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Oscillator level for signals; its negative is the short level.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Close the position when there is no signal.
	/// </summary>
	public bool CloseAllPositions
	{
		get => _closeAllPositions.Value;
		set => _closeAllPositions.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FourierSmoothedVzoStrategy()
	{
		_vzoLength = Param(nameof(VzoLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("VZO Length", "EMA length of the oscillator", "Indicators");

		_smoothLength = Param(nameof(SmoothLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "Window of the Fourier smoothing", "Indicators");

		_threshold = Param(nameof(Threshold), 0m)
			.SetNotNegative()
			.SetDisplay("Threshold", "Oscillator level for signals", "Signals");

		_closeAllPositions = Param(nameof(CloseAllPositions), true)
			.SetDisplay("Close All Positions", "Close the position when there is no signal", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_signedVolumeEma = null;
		_volumeEma = null;
		_vzoValues.Clear();
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_signedVolumeEma = new ExponentialMovingAverage { Length = VzoLength };
		_volumeEma = new ExponentialMovingAverage { Length = VzoLength };
		_vzoValues.Clear();
		_prevClose = null;

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

		var prevClose = _prevClose;
		_prevClose = candle.ClosePrice;

		if (prevClose is not decimal lastClose)
			return;

		var volume = candle.TotalVolume;
		var direction = Math.Sign(candle.ClosePrice - lastClose);

		var signedEma = _signedVolumeEma.Process(direction * volume, candle.OpenTime, true);
		var volumeEma = _volumeEma.Process(volume, candle.OpenTime, true);

		if (!_signedVolumeEma.IsFormed || !_volumeEma.IsFormed)
			return;

		var total = volumeEma.ToDecimal();
		if (total == 0)
			return;

		_vzoValues.Add(100m * signedEma.ToDecimal() / total);
		if (_vzoValues.Count > SmoothLength)
			_vzoValues.RemoveAt(0);

		if (_vzoValues.Count < SmoothLength)
			return;

		var oscillator = FourierSmooth(_vzoValues);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (oscillator > Threshold)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (oscillator < -Threshold)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else if (CloseAllPositions)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);
		}
	}

	private static decimal FourierSmooth(List<decimal> values)
	{
		var n = values.Count;
		var mean = 0.0;
		var cosSum = 0.0;
		var sinSum = 0.0;

		for (var i = 0; i < n; i++)
		{
			var x = (double)values[i];
			var angle = 2 * Math.PI * i / n;
			mean += x;
			cosSum += x * Math.Cos(angle);
			sinSum += x * Math.Sin(angle);
		}

		mean /= n;

		if (n < 2)
			return (decimal)mean;

		// The Nyquist harmonic of an even window has no sine part and counts once, not twice.
		var scale = n == 2 ? 1.0 / n : 2.0 / n;
		var last = 2 * Math.PI * (n - 1) / n;

		return (decimal)(mean + scale * (cosSum * Math.Cos(last) + sinSum * Math.Sin(last)));
	}
}
