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

	private readonly List<decimal> _volumes = new();
	private decimal _entryPrice;
	private decimal _stopDist;

	public int VolAvgLength { get => _volAvgLength.Value; set => _volAvgLength.Value = value; }
	public decimal VolMult { get => _volMult.Value; set => _volMult.Value = value; }
	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }
	public decimal TpPct { get => _tpPct.Value; set => _tpPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolumeBySessionStrategy()
	{
		_volAvgLength = Param(nameof(VolAvgLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Vol Avg Length", "Volume average period", "Parameters");

		_volMult = Param(nameof(VolMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Vol Multiplier", "Volume spike multiplier", "Parameters");

		_stopPct = Param(nameof(StopPct), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_tpPct = Param(nameof(TpPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_entryPrice = 0;
		_stopDist = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 2 };

		_volumes.Clear();
		_entryPrice = 0;
		_stopDist = 0;

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

		var vol = candle.TotalVolume;
		_volumes.Add(vol);

		while (_volumes.Count > VolAvgLength + 1)
			_volumes.RemoveAt(0);

		// TP/SL
		if (Position > 0 && _entryPrice > 0 && _stopDist > 0)
		{
			if (candle.ClosePrice <= _entryPrice - _stopDist || candle.ClosePrice >= _entryPrice + _stopDist * (TpPct / StopPct))
			{
				SellMarket();
				_entryPrice = 0;
				_stopDist = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && _stopDist > 0)
		{
			if (candle.ClosePrice >= _entryPrice + _stopDist || candle.ClosePrice <= _entryPrice - _stopDist * (TpPct / StopPct))
			{
				BuyMarket();
				_entryPrice = 0;
				_stopDist = 0;
			}
		}

		if (_volumes.Count < VolAvgLength)
			return;

		var avgVol = _volumes.Take(VolAvgLength).Sum() / VolAvgLength;
		var bullish = candle.ClosePrice > candle.OpenPrice;
		var bearish = candle.ClosePrice < candle.OpenPrice;
		var highVol = vol > avgVol * VolMult;

		if (highVol && bullish && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopDist = candle.ClosePrice * StopPct / 100m;
		}
		else if (highVol && bearish && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopDist = candle.ClosePrice * StopPct / 100m;
		}
	}
}
