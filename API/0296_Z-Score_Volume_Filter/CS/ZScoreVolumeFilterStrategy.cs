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
/// Mean-reversion strategy that combines price Z-score extremes with above-average volume.
/// </summary>
public class ZScoreVolumeFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _zScoreThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<decimal> _volumeFactor;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _priceSma;
	private StandardDeviation _priceStdDev;
	private SimpleMovingAverage _volumeSma;
	private int _cooldown;

	/// <summary>
	/// Lookback period for price and volume statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Absolute Z-score required for entry.
	/// </summary>
	public decimal ZScoreThreshold
	{
		get => _zScoreThreshold.Value;
		set => _zScoreThreshold.Value = value;
	}

	/// <summary>
	/// Absolute Z-score required for exit after mean reversion.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Minimum multiple of average volume required for entry.
	/// </summary>
	public decimal VolumeFactor
	{
		get => _volumeFactor.Value;
		set => _volumeFactor.Value = value;
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
	/// Number of finished candles to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle series used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ZScoreVolumeFilterStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetRange(10, 100)
			.SetDisplay("Lookback Period", "Lookback period for price and volume statistics", "Parameters");

		_zScoreThreshold = Param(nameof(ZScoreThreshold), 2.0m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Entry Z-Score", "Absolute Z-score required for entry", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.3m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Z-Score", "Absolute Z-score required for exit", "Signals");

		_volumeFactor = Param(nameof(VolumeFactor), 1.2m)
			.SetRange(0.1m, 3m)
			.SetDisplay("Volume Factor", "Minimum multiple of average volume required for entry", "Signals");

		_stopLossPercent = Param(nameof(StopLossPercent), 3m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetRange(1, 200)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for signals", "General");
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

		_priceSma = null;
		_priceStdDev = null;
		_volumeSma = null;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_priceSma = new SimpleMovingAverage { Length = LookbackPeriod };
		_priceStdDev = new StandardDeviation { Length = LookbackPeriod };
		_volumeSma = new SimpleMovingAverage { Length = LookbackPeriod };
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_priceSma, _priceStdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _priceSma);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(StopLossPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent),
			false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdDevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volInput = new DecimalIndicatorValue(_volumeSma, candle.TotalVolume, candle.OpenTime) { IsFinal = true };
		var volumeAverage = _volumeSma.Process(volInput).ToDecimal();

		if (!_priceSma.IsFormed || !_priceStdDev.IsFormed || !_volumeSma.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (stdDevValue <= 0)
			return;

		var zScore = (candle.ClosePrice - smaValue) / stdDevValue;
		var isHighVolume = candle.TotalVolume >= volumeAverage * VolumeFactor;

		if (Position == 0)
		{
			if (!isHighVolume)
				return;

			if (zScore <= -ZScoreThreshold)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (zScore >= ZScoreThreshold)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}

			return;
		}

		if (Position > 0 && zScore >= -ExitThreshold)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && zScore <= ExitThreshold)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
