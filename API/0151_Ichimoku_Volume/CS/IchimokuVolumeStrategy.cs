using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Ichimoku (manual Tenkan/Kijun) with volume filter.
/// Buys when price above Kumo, Tenkan above Kijun, volume above average.
/// Sells when price below Kumo, Tenkan below Kijun, volume above average.
/// </summary>
public class IchimokuVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _vols = new();
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
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
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
	public IchimokuVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetRange(5, 20)
			.SetDisplay("Tenkan Period", "Tenkan-sen period (fast)", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetRange(15, 40)
			.SetDisplay("Kijun Period", "Kijun-sen period (slow)", "Ichimoku");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Volume Average Period", "Period for volume moving average", "Volume");

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
		_highs.Clear();
		_lows.Clear();
		_vols.Clear();
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = KijunPeriod };

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

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;
		var vol = candle.TotalVolume;

		_highs.Add(high);
		_lows.Add(low);
		_vols.Add(vol);

		var tenkanPrd = TenkanPeriod;
		var kijunPrd = KijunPeriod;
		var volPrd = VolumeAvgPeriod;
		var minBars = Math.Max(kijunPrd, volPrd);

		if (_highs.Count < minBars)
		{
			if (_cooldown > 0) _cooldown--;
			return;
		}

		// Manual Tenkan-sen: (highest high + lowest low) / 2 over tenkan period
		var count = _highs.Count;
		decimal tenkanHH = decimal.MinValue, tenkanLL = decimal.MaxValue;
		for (int i = count - tenkanPrd; i < count; i++)
		{
			if (_highs[i] > tenkanHH) tenkanHH = _highs[i];
			if (_lows[i] < tenkanLL) tenkanLL = _lows[i];
		}
		var tenkan = (tenkanHH + tenkanLL) / 2m;

		// Manual Kijun-sen
		decimal kijunHH = decimal.MinValue, kijunLL = decimal.MaxValue;
		for (int i = count - kijunPrd; i < count; i++)
		{
			if (_highs[i] > kijunHH) kijunHH = _highs[i];
			if (_lows[i] < kijunLL) kijunLL = _lows[i];
		}
		var kijun = (kijunHH + kijunLL) / 2m;

		// Senkou Span A = (Tenkan + Kijun) / 2
		var senkouA = (tenkan + kijun) / 2m;

		// Senkou Span B = (highest high + lowest low) / 2 over 2*kijun period (use kijun period for simplicity)
		var senkouB = kijun; // simplified: use Kijun as proxy for Senkou B

		var upperKumo = Math.Max(senkouA, senkouB);
		var lowerKumo = Math.Min(senkouA, senkouB);

		// Volume average
		decimal sumVol = 0;
		for (int i = count - volPrd; i < count; i++)
			sumVol += _vols[i];
		var avgVol = sumVol / volPrd;
		var highVolume = vol > avgVol;

		// Trim lists
		var maxKeep = minBars * 3;
		if (_highs.Count > maxKeep)
		{
			var trim = _highs.Count - minBars * 2;
			_highs.RemoveRange(0, trim);
			_lows.RemoveRange(0, trim);
			_vols.RemoveRange(0, trim);
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Buy: price above cloud + Tenkan above Kijun + high volume
		if (close > upperKumo && tenkan > kijun && highVolume && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: price below cloud + Tenkan below Kijun + high volume
		else if (close < lowerKumo && tenkan < kijun && highVolume && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price drops below kijun
		if (Position > 0 && close < kijun)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price rises above kijun
		else if (Position < 0 && close > kijun)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
