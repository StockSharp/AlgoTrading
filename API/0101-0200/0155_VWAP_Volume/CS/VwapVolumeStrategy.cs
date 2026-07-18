using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining VWAP with volume confirmation.
/// Buys on VWAP breakout with above-average volume, sells on breakdown.
/// </summary>
public class VwapVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _volumes = new();
	private readonly List<decimal> _typicalPriceVol = new();
	private decimal _cumVol;
	private decimal _cumTpv;
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
	/// Period for volume moving average.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// Volume threshold multiplier.
	/// </summary>
	public decimal VolumeThreshold
	{
		get => _volumeThreshold.Value;
		set => _volumeThreshold.Value = value;
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
	public VwapVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Volume MA Period", "Period for volume moving average", "Indicators");

		_volumeThreshold = Param(nameof(VolumeThreshold), 1.5m)
			.SetDisplay("Volume Threshold", "Multiplier for average volume", "Trading Levels");

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
		_volumes.Clear();
		_typicalPriceVol.Clear();
		_cumVol = 0;
		_cumTpv = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = VolumePeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var vol = candle.TotalVolume;
		var typicalPrice = (high + low + close) / 3m;

		_volumes.Add(vol);
		_cumVol += vol;
		_cumTpv += typicalPrice * vol;

		var volPrd = VolumePeriod;

		if (_volumes.Count < volPrd)
		{
			if (_cooldown > 0) _cooldown--;
			return;
		}

		// Manual VWAP (cumulative)
		var vwapValue = _cumVol > 0 ? _cumTpv / _cumVol : close;

		// Manual volume average
		decimal sumVol = 0;
		var count = _volumes.Count;
		for (int i = count - volPrd; i < count; i++)
			sumVol += _volumes[i];
		var avgVol = sumVol / volPrd;

		var highVolume = vol > avgVol * VolumeThreshold;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Buy: price above VWAP + high volume
		if (close > vwapValue && highVolume && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: price below VWAP + high volume
		else if (close < vwapValue && highVolume && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price below VWAP
		if (Position > 0 && close < vwapValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price above VWAP
		else if (Position < 0 && close > vwapValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
