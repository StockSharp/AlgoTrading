namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy that combines moving average and volume indicators to identify
/// potential trend breakouts with volume confirmation.
/// </summary>
public class MaVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _volumeThreshold;

	private decimal _prevClose;
	private decimal _prevSma;
	private bool _hasPrev;
	private decimal _volumeSum;
	private int _volumeCount;
	private readonly Queue<decimal> _volumeQueue = new();

	/// <summary>
	/// Data type for candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for moving average calculation.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Period for volume moving average calculation.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// Volume threshold multiplier for volume confirmation.
	/// </summary>
	public decimal VolumeThreshold
	{
		get => _volumeThreshold.Value;
		set => _volumeThreshold.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MaVolumeStrategy"/>.
	/// </summary>
	public MaVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "Period for moving average calculation", "MA Settings");

		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetDisplay("Volume MA Period", "Period for volume moving average calculation", "Volume Settings");

		_volumeThreshold = Param(nameof(VolumeThreshold), 1.2m)
			.SetDisplay("Volume Threshold", "Volume threshold multiplier for volume confirmation", "Volume Settings");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = 0;
		_prevSma = 0;
		_hasPrev = false;
		_volumeSum = 0;
		_volumeCount = 0;
		_volumeQueue.Clear();

		var priceSma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(priceSma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, priceSma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manual volume average calculation
		var vol = candle.TotalVolume;
		_volumeQueue.Enqueue(vol);
		_volumeSum += vol;
		_volumeCount++;

		if (_volumeCount > VolumePeriod)
		{
			_volumeSum -= _volumeQueue.Dequeue();
			_volumeCount = VolumePeriod;
		}

		var avgVolume = _volumeCount > 0 ? _volumeSum / _volumeCount : 0m;
		var volumeConfirmation = _volumeCount >= VolumePeriod && vol > avgVolume * VolumeThreshold;

		if (_hasPrev)
		{
			if (volumeConfirmation)
			{
				// Price crosses above MA - buy signal
				if (_prevClose <= _prevSma && candle.ClosePrice > smaValue && Position <= 0)
				{
					BuyMarket();
				}
				// Price crosses below MA - sell signal
				else if (_prevClose >= _prevSma && candle.ClosePrice < smaValue && Position >= 0)
				{
					SellMarket();
				}
			}
		}

		_prevClose = candle.ClosePrice;
		_prevSma = smaValue;
		_hasPrev = true;
	}
}
