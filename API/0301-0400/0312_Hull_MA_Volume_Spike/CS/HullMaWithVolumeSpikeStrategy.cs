using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy that requires a Hull moving average slope change to be confirmed by a volume spike.
/// </summary>
public class HullMaWithVolumeSpikeStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<decimal> _volumeThresholdFactor;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private HullMovingAverage _hma;
	private SimpleMovingAverage _volumeSma;
	private StandardDeviation _volumeStdDev;
	private decimal _prevHmaValue;
	private bool _isInitialized;
	private int _cooldown;

	/// <summary>
	/// Hull moving average period.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for volume statistics.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to volume standard deviation.
	/// </summary>
	public decimal VolumeThresholdFactor
	{
		get => _volumeThresholdFactor.Value;
		set => _volumeThresholdFactor.Value = value;
	}

	/// <summary>
	/// Bars to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public HullMaWithVolumeSpikeStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 9)
			.SetRange(2, 100)
			.SetDisplay("HMA Period", "Period for the Hull moving average", "Indicators");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetRange(2, 100)
			.SetDisplay("Volume Avg Period", "Period for volume statistics", "Indicators");

		_volumeThresholdFactor = Param(nameof(VolumeThresholdFactor), 1.8m)
			.SetRange(0.1m, 10m)
			.SetDisplay("Volume Threshold Factor", "Multiplier for volume spike detection", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 72)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_hma = null;
		_volumeSma = null;
		_volumeStdDev = null;
		_prevHmaValue = 0m;
		_isInitialized = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_hma = new HullMovingAverage { Length = HmaPeriod };
		_volumeSma = new SimpleMovingAverage { Length = VolumeAvgPeriod };
		_volumeStdDev = new StandardDeviation { Length = VolumeAvgPeriod };
		_isInitialized = false;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_hma, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _hma);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeAvgValue = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		var volumeStdDevValue = _volumeStdDev.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();

		if (!_hma.IsFormed || !_volumeSma.IsFormed || !_volumeStdDev.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (!_isInitialized)
		{
			_prevHmaValue = hmaValue;
			_isInitialized = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevHmaValue = hmaValue;
			return;
		}

		var isHmaRising = hmaValue > _prevHmaValue;
		var isHmaFalling = hmaValue < _prevHmaValue;
		var volumeThreshold = volumeAvgValue + VolumeThresholdFactor * volumeStdDevValue;
		var isVolumeSpiking = candle.TotalVolume >= volumeThreshold;

		if (Position == 0)
		{
			if (isHmaRising && isVolumeSpiking)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (isHmaFalling && isVolumeSpiking)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (isHmaFalling)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (isHmaRising)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}

		_prevHmaValue = hmaValue;
	}
}
