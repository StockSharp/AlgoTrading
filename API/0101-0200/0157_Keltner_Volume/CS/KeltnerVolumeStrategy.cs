using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Keltner Channels with volume confirmation.
/// Buys on upper channel breakout with above-average volume,
/// sells on lower channel breakdown with above-average volume.
/// </summary>
public class KeltnerVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _averageVolume;
	private int _volumeCounter;
	private int _cooldown;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EMA period for center line.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for channel width.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for channel width.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Volume average period.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public KeltnerVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetRange(10, 40)
			.SetDisplay("EMA Period", "EMA period for center line", "Keltner");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("ATR Period", "ATR period for channel width", "Keltner");

		_multiplier = Param(nameof(Multiplier), 2.0m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR", "Keltner");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Volume Avg Period", "Period for volume average", "Volume");

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
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
		_averageVolume = 0;
		_volumeCounter = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var volume = candle.TotalVolume;

		if (_volumeCounter < VolumeAvgPeriod)
		{
			_volumeCounter++;
			_averageVolume = ((_averageVolume * (_volumeCounter - 1)) + volume) / _volumeCounter;
		}
		else
		{
			_averageVolume = (_averageVolume * (VolumeAvgPeriod - 1) + volume) / VolumeAvgPeriod;
		}

		if (_volumeCounter < VolumeAvgPeriod)
		{
			if (_cooldown > 0)
				_cooldown--;
			return;
		}

		var upperBand = emaValue + Multiplier * atrValue;
		var lowerBand = emaValue - Multiplier * atrValue;
		var highVolume = volume > _averageVolume;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Buy: price above upper band + high volume
		if (close > upperBand && highVolume && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: price below lower band + high volume
		else if (close < lowerBand && highVolume && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price below EMA
		if (Position > 0 && close < emaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price above EMA
		else if (Position < 0 && close > emaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
