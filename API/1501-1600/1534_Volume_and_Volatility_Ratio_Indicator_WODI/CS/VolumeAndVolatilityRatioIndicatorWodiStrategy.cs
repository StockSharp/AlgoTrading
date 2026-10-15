using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume and volatility ratio indicator strategy (WODI).
/// Detects increased volume and volatility to enter reversal trades.
/// Uses volume MA and volatility index with short/long MA crossover.
/// </summary>
public class VolumeAndVolatilityRatioIndicatorWodiStrategy : Strategy
{
	private readonly StrategyParam<int> _volLength;
	private readonly StrategyParam<int> _indexLength;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<decimal> _tpPct;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = new();
	private readonly List<decimal> _volIndices = new();
	private decimal _entryPrice;
	private decimal _stopDist;
	private ICandleMessage _prevCandle;
	private ICandleMessage _prevPrevCandle;
	private int _cooldownRemaining;

	public int VolLength { get => _volLength.Value; set => _volLength.Value = value; }
	public int IndexLength { get => _indexLength.Value; set => _indexLength.Value = value; }
	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }
	public decimal TpPct { get => _tpPct.Value; set => _tpPct.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolumeAndVolatilityRatioIndicatorWodiStrategy()
	{
		_volLength = Param(nameof(VolLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Volume average period", "Parameters");

		_indexLength = Param(nameof(IndexLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Index Length", "Volatility index average period", "Parameters");

		_stopPct = Param(nameof(StopPct), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_tpPct = Param(nameof(TpPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 24)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_volumes.Clear();
		_volIndices.Clear();
		_entryPrice = 0;
		_stopDist = 0;
		_prevCandle = null;
		_prevPrevCandle = null;
		_cooldownRemaining = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 2 };

		_volumes.Clear();
		_volIndices.Clear();
		_entryPrice = 0;
		_stopDist = 0;
		_prevCandle = null;
		_prevPrevCandle = null;
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _dummy)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var vol = candle.TotalVolume;
		var volatility = candle.ClosePrice > 0 ? (candle.HighPrice - candle.LowPrice) / candle.ClosePrice * 100m : 0;
		var volIndex = vol * volatility;

		_volumes.Add(vol);
		_volIndices.Add(volIndex);

		while (_volumes.Count > VolLength + 1)
			_volumes.RemoveAt(0);
		while (_volIndices.Count > IndexLength + 1)
			_volIndices.RemoveAt(0);

		// TP/SL management
		if (Position > 0 && _entryPrice > 0 && _stopDist > 0)
		{
			if (candle.ClosePrice <= _entryPrice - _stopDist || candle.ClosePrice >= _entryPrice + _stopDist * (TpPct / StopPct))
			{
				SellMarket();
				_entryPrice = 0;
				_stopDist = 0;
				_cooldownRemaining = SignalCooldownBars;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && _stopDist > 0)
		{
			if (candle.ClosePrice >= _entryPrice + _stopDist || candle.ClosePrice <= _entryPrice - _stopDist * (TpPct / StopPct))
			{
				BuyMarket();
				_entryPrice = 0;
				_stopDist = 0;
				_cooldownRemaining = SignalCooldownBars;
			}
		}

		if (_volumes.Count < VolLength || _volIndices.Count < IndexLength || _prevCandle == null || _prevPrevCandle == null)
		{
			_prevPrevCandle = _prevCandle;
			_prevCandle = candle;
			return;
		}

		// Calculate averages
		var volAvg = _volumes.Take(VolLength).Sum() / VolLength;
		var indexAvg = _volIndices.Take(IndexLength).Sum() / IndexLength;

		// Entry conditions
		var highVol = vol > volAvg;
		var highVolIndex = volIndex > indexAvg * 2.5m;

		var isLongPattern = highVol && highVolIndex
			&& _prevCandle.ClosePrice < _prevPrevCandle.ClosePrice
			&& candle.ClosePrice > _prevCandle.ClosePrice;

		var isShortPattern = highVol && highVolIndex
			&& _prevCandle.ClosePrice > _prevPrevCandle.ClosePrice
			&& candle.ClosePrice < _prevCandle.ClosePrice;

		if (_cooldownRemaining == 0 && isLongPattern && Position == 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopDist = candle.ClosePrice * StopPct / 100m;
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && isShortPattern && Position == 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopDist = candle.ClosePrice * StopPct / 100m;
			_cooldownRemaining = SignalCooldownBars;
		}

		_prevPrevCandle = _prevCandle;
		_prevCandle = candle;
	}
}
