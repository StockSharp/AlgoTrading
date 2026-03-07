using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume by session strategy.
/// Tracks average volume and trades on volume deviations with price confirmation.
/// Buys when volume spikes above average and price is rising, sells when opposite.
/// </summary>
public class VolumeBySessionStrategy : Strategy
{
	private readonly StrategyParam<int> _volAvgLength;
	private readonly StrategyParam<decimal> _volMult;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<decimal> _tpPct;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalCooldownBars;

	private readonly List<decimal> _volumes = new();
	private decimal _volumeSum;
	private decimal _entryPrice;
	private decimal _stopDist;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private int _cooldownRemaining;

	public int VolAvgLength { get => _volAvgLength.Value; set => _volAvgLength.Value = value; }
	public decimal VolMult { get => _volMult.Value; set => _volMult.Value = value; }
	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }
	public decimal TpPct { get => _tpPct.Value; set => _tpPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public VolumeBySessionStrategy()
	{
		_volAvgLength = Param(nameof(VolAvgLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Vol Avg Length", "Volume average period", "Parameters");

		_volMult = Param(nameof(VolMult), 2.25m)
			.SetGreaterThanZero()
			.SetDisplay("Vol Multiplier", "Volume spike multiplier", "Parameters");

		_stopPct = Param(nameof(StopPct), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_tpPct = Param(nameof(TpPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between new entries", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_volumes.Clear();
		_volumeSum = 0m;
		_entryPrice = 0;
		_stopDist = 0;
		_prevHigh = null;
		_prevLow = null;
		_cooldownRemaining = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumes.Clear();
		_volumeSum = 0m;
		_entryPrice = 0;
		_stopDist = 0;
		_prevHigh = null;
		_prevLow = null;
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var vol = candle.TotalVolume;
		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		// TP/SL
		if (Position > 0 && _entryPrice > 0 && _stopDist > 0)
		{
			if (candle.ClosePrice <= _entryPrice - _stopDist || candle.ClosePrice >= _entryPrice + _stopDist * (TpPct / StopPct))
			{
				SellMarket(Position);
				_entryPrice = 0;
				_stopDist = 0;
				_cooldownRemaining = SignalCooldownBars;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && _stopDist > 0)
		{
			if (candle.ClosePrice >= _entryPrice + _stopDist || candle.ClosePrice <= _entryPrice - _stopDist * (TpPct / StopPct))
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0;
				_stopDist = 0;
				_cooldownRemaining = SignalCooldownBars;
			}
		}

		if (_volumes.Count >= VolAvgLength && _prevHigh.HasValue && _prevLow.HasValue && _cooldownRemaining == 0 && Position == 0)
		{
			var avgVol = _volumeSum / _volumes.Count;
			var highVol = vol >= avgVol * VolMult;
			var bullishBreakout = candle.ClosePrice > _prevHigh.Value && candle.ClosePrice > candle.OpenPrice;
			var bearishBreakout = candle.ClosePrice < _prevLow.Value && candle.ClosePrice < candle.OpenPrice;
			var minBody = Math.Max(candle.ClosePrice * 0.001m, Security?.PriceStep ?? 0m);
			var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);

			if (highVol && bullishBreakout && body >= minBody)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopDist = candle.ClosePrice * StopPct / 100m;
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (highVol && bearishBreakout && body >= minBody)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopDist = candle.ClosePrice * StopPct / 100m;
				_cooldownRemaining = SignalCooldownBars;
			}
		}

		_volumes.Add(vol);
		_volumeSum += vol;

		if (_volumes.Count > VolAvgLength)
		{
			_volumeSum -= _volumes[0];
			_volumes.RemoveAt(0);
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
