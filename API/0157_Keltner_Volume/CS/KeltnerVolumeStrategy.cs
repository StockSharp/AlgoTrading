using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining manual Keltner Channels with volume confirmation.
/// Buys on upper band breakout with high volume, sells on lower band breakout.
/// </summary>
public class KeltnerVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _closes = new();
	private readonly List<decimal> _volumes = new();
	private decimal _emaValue;
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
		_highs.Clear();
		_lows.Clear();
		_closes.Clear();
		_volumes.Clear();
		_emaValue = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Use RSI as binding indicator (simple, reliable)
		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
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
		_closes.Add(close);
		_volumes.Add(vol);

		var emaPrd = EmaPeriod;
		var atrPrd = AtrPeriod;
		var volPrd = VolumeAvgPeriod;
		var minBars = Math.Max(Math.Max(emaPrd, atrPrd + 1), volPrd);

		if (_closes.Count < minBars)
		{
			if (_cooldown > 0) _cooldown--;
			return;
		}

		// Manual EMA
		if (_emaValue == 0)
		{
			// Initialize with SMA
			decimal sum = 0;
			for (int i = _closes.Count - emaPrd; i < _closes.Count; i++)
				sum += _closes[i];
			_emaValue = sum / emaPrd;
		}
		else
		{
			var k = 2m / (emaPrd + 1);
			_emaValue = close * k + _emaValue * (1m - k);
		}

		// Manual ATR
		decimal sumTr = 0;
		var count = _highs.Count;
		for (int i = count - atrPrd; i < count; i++)
		{
			var h = _highs[i];
			var l = _lows[i];
			var prevC = _closes[i - 1];
			var tr = Math.Max(h - l, Math.Max(Math.Abs(h - prevC), Math.Abs(l - prevC)));
			sumTr += tr;
		}
		var atr = sumTr / atrPrd;

		// Keltner bands
		var upperBand = _emaValue + Multiplier * atr;
		var lowerBand = _emaValue - Multiplier * atr;

		// Volume average
		decimal sumVol = 0;
		for (int i = count - volPrd; i < count; i++)
			sumVol += _volumes[i];
		var avgVol = sumVol / volPrd;
		var highVolume = vol > avgVol;

		// Trim lists
		var maxKeep = minBars * 3;
		if (_highs.Count > maxKeep)
		{
			var trim = _highs.Count - minBars * 2;
			_highs.RemoveRange(0, trim);
			_lows.RemoveRange(0, trim);
			_closes.RemoveRange(0, trim);
			_volumes.RemoveRange(0, trim);
		}

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
		if (Position > 0 && close < _emaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price above EMA
		else if (Position < 0 && close > _emaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
