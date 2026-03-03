using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dark Pool Prints strategy.
/// Detects unusually high volume candles and trades in the direction of the candle
/// when confirmed by SMA trend direction.
/// Uses cooldown to control trade frequency.
/// </summary>
public class DarkPoolPrintsStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _volumeLookback;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _volumeHistory = new();
	private int _cooldown;

	/// <summary>
	/// MA period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Volume lookback.
	/// </summary>
	public int VolumeLookback
	{
		get => _volumeLookback.Value;
		set => _volumeLookback.Value = value;
	}

	/// <summary>
	/// Volume multiplier threshold.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
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
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DarkPoolPrintsStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetRange(5, 50)
			.SetDisplay("MA Period", "Period for trend SMA", "Indicators");

		_volumeLookback = Param(nameof(VolumeLookback), 20)
			.SetRange(5, 50)
			.SetDisplay("Volume Lookback", "Bars for volume average", "Volume");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
			.SetRange(1.2m, 5m)
			.SetDisplay("Volume Multiplier", "Threshold multiplier for high volume", "Volume");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_volumeHistory.Clear();
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumeHistory.Clear();
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Track volume history
		_volumeHistory.Add(candle.TotalVolume);
		if (_volumeHistory.Count > VolumeLookback)
			_volumeHistory.RemoveAt(0);

		if (_volumeHistory.Count < VolumeLookback)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Calculate average volume
		decimal avgVolume = 0;
		for (int i = 0; i < _volumeHistory.Count - 1; i++)
			avgVolume += _volumeHistory[i];
		avgVolume /= (_volumeHistory.Count - 1);

		var isHighVolume = candle.TotalVolume > avgVolume * VolumeMultiplier;
		if (!isHighVolume)
			return;

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;
		var isAboveSma = candle.ClosePrice > smaValue;
		var isBelowSma = candle.ClosePrice < smaValue;

		if (Position == 0 && isBullish && isAboveSma)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && isBearish && isBelowSma)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && isBelowSma)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && isAboveSma)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
