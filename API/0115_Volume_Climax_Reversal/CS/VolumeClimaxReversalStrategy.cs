using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Climax Reversal strategy.
/// Enters counter-trend when volume spikes above average with MA confirmation.
/// Uses cooldown and MA cross for exits.
/// </summary>
public class VolumeClimaxReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;

	private decimal _prevMa;
	private decimal _prevClose;
	private readonly List<decimal> _volumes = new();
	private int _cooldown;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// MA period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Volume multiplier for climax detection.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
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
	public VolumeClimaxReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "SMA period", "Indicators")
			.SetRange(10, 50);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetDisplay("Volume Multiplier", "Volume spike threshold", "Volume")
			.SetRange(1.5m, 5m);

		_cooldownBars = Param(nameof(CooldownBars), 400)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(10, 2000);
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
		_ma = default;
		_prevMa = 0;
		_prevClose = 0;
		_volumes.Clear();
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma = new SimpleMovingAverage { Length = MaPeriod };
		_volumes.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var volume = candle.TotalVolume;

		// Track volumes for average calculation
		_volumes.Add(volume);
		if (_volumes.Count > MaPeriod)
			_volumes.RemoveAt(0);

		if (_volumes.Count < MaPeriod || _prevMa == 0)
		{
			_prevMa = ma;
			_prevClose = close;
			return;
		}

		// Calculate average volume
		decimal avgVolume = 0;
		for (int i = 0; i < _volumes.Count; i++)
			avgVolume += _volumes[i];
		avgVolume /= _volumes.Count;

		var isVolumeClimax = avgVolume > 0 && volume > avgVolume * VolumeMultiplier;
		var isBullish = close > candle.OpenPrice;
		var isBearish = close < candle.OpenPrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMa = ma;
			_prevClose = close;
			return;
		}

		// Exit logic: MA cross
		if (Position > 0 && close < ma && _prevClose >= _prevMa)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && close > ma && _prevClose <= _prevMa)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		// Entry logic: volume climax reversal
		if (Position == 0 && isVolumeClimax)
		{
			// Bullish reversal: high volume bearish candle below MA (selling climax)
			if (isBearish && close < ma)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			// Bearish reversal: high volume bullish candle above MA (buying climax)
			else if (isBullish && close > ma)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}

		_prevMa = ma;
		_prevClose = close;
	}
}
