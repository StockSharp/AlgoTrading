using System;
using System.Collections.Generic;


using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reversal & Breakout strategy with ORB.
/// </summary>
public class ReversalBreakoutOrbStrategy : Strategy
{
	private readonly StrategyParam<int> _ema9;
	private readonly StrategyParam<int> _ema20;
	private readonly StrategyParam<int> _sma50;
	private readonly StrategyParam<int> _sma200;
	private readonly StrategyParam<int> _rsiLen;
	private readonly StrategyParam<int> _rsiOb;
	private readonly StrategyParam<int> _rsiOs;
	private readonly StrategyParam<int> _atrLen;
	private readonly StrategyParam<decimal> _stopMult;
	private readonly StrategyParam<int> _stopLookback;
	private readonly StrategyParam<decimal> _rr1;
	private readonly StrategyParam<decimal> _rr2;
	private readonly StrategyParam<decimal> _tp1Pct;
	private readonly StrategyParam<int> _orbBars;
	private readonly StrategyParam<decimal> _volThr;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _stopHigh;
	private Lowest _stopLow;
	private Highest _orbHigh;
	private Lowest _orbLow;
	private SMA _orbVol;

	private decimal? _orbHighVal;
	private decimal? _orbLowVal;
	private decimal? _orbVolAvg;

	private decimal _prevClose;
	private decimal _prevSma50;
	private decimal _prevEma9;
	private decimal _prevEma20;
	private decimal _lastLow;
	private decimal _lastHigh;

	private decimal? _stop;
	private decimal? _tp1;
	private decimal? _tp2;
	private decimal? _qty;
	private bool _tp1Done;
	private bool _beSet;

public int Ema9Length { get => _ema9.Value; set => _ema9.Value = value; }
public int Ema20Length { get => _ema20.Value; set => _ema20.Value = value; }
public int Sma50Length { get => _sma50.Value; set => _sma50.Value = value; }
public int Sma200Length { get => _sma200.Value; set => _sma200.Value = value; }
public int RsiLength { get => _rsiLen.Value; set => _rsiLen.Value = value; }
public int RsiOverbought { get => _rsiOb.Value; set => _rsiOb.Value = value; }
public int RsiOversold { get => _rsiOs.Value; set => _rsiOs.Value = value; }
public int AtrLength { get => _atrLen.Value; set => _atrLen.Value = value; }
public decimal StopAtrMultiplier { get => _stopMult.Value; set => _stopMult.Value = value; }
public int StopLookback { get => _stopLookback.Value; set => _stopLookback.Value = value; }
public decimal Rr1 { get => _rr1.Value; set => _rr1.Value = value; }
public decimal Rr2 { get => _rr2.Value; set => _rr2.Value = value; }
public decimal Target1Percent { get => _tp1Pct.Value; set => _tp1Pct.Value = value; }
public int OrbBars { get => _orbBars.Value; set => _orbBars.Value = value; }
public decimal VolThreshold { get => _volThr.Value; set => _volThr.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public ReversalBreakoutOrbStrategy()
{
	_ema9 = Param(nameof(Ema9Length), 9);
	_ema20 = Param(nameof(Ema20Length), 20);
	_sma50 = Param(nameof(Sma50Length), 50);
	_sma200 = Param(nameof(Sma200Length), 200);
	_rsiLen = Param(nameof(RsiLength), 14);
	_rsiOb = Param(nameof(RsiOverbought), 70);
	_rsiOs = Param(nameof(RsiOversold), 30);
	_atrLen = Param(nameof(AtrLength), 14);
	_stopMult = Param(nameof(StopAtrMultiplier), 1.5m);
	_stopLookback = Param(nameof(StopLookback), 7);
	_rr1 = Param(nameof(Rr1), 1m);
	_rr2 = Param(nameof(Rr2), 2m);
	_tp1Pct = Param(nameof(Target1Percent), 50m);
	_orbBars = Param(nameof(OrbBars), 15);
	_volThr = Param(nameof(VolThreshold), 1.5m);
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(2).TimeFrame());
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	return [(Security, CandleType)];
}

protected override void OnReseted()
{
	base.OnReseted();
	_orbHighVal = null;
	_orbLowVal = null;
	_orbVolAvg = null;
	_prevClose = 0m;
	_prevSma50 = 0m;
	_prevEma9 = 0m;
	_prevEma20 = 0m;
	_stop = null;
	_tp1 = null;
	_tp2 = null;
	_qty = null;
	_tp1Done = false;
	_beSet = false;
}

protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	var ema9 = new EMA { Length = Ema9Length };
	var ema20 = new EMA { Length = Ema20Length };
	var sma50 = new SMA { Length = Sma50Length };
	var sma200 = new SMA { Length = Sma200Length };
	var rsi = new RSI { Length = RsiLength };
	var atr = new ATR { Length = AtrLength };
	var vwap = new VolumeWeightedMovingAverage();

	_stopHigh = new Highest { Length = StopLookback };
	_stopLow = new Lowest { Length = StopLookback };
	_orbHigh = new Highest { Length = OrbBars };
	_orbLow = new Lowest { Length = OrbBars };
	_orbVol = new SMA { Length = OrbBars };

	var sub = SubscribeCandles(CandleType);
	sub.Bind(ema9, ema20, sma50, sma200, rsi, atr, vwap, Process).Start();

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, sub);
		DrawIndicator(area, ema9);
		DrawIndicator(area, ema20);
		DrawIndicator(area, sma50);
		DrawIndicator(area, sma200);
		DrawIndicator(area, vwap);
		DrawOwnTrades(area);
	}
}

private void Process(ICandleMessage candle, decimal ema9Val, decimal ema20Val, decimal sma50Val, decimal sma200Val, decimal rsiVal, decimal atrVal, decimal vwapVal)
{
	if (candle.State != CandleStates.Finished)
	return;

	var lowVal = _stopLow.Process(candle.LowPrice);
	var highVal = _stopHigh.Process(candle.HighPrice);
	var orbHigh = _orbHigh.Process(candle.HighPrice);
	var orbLow = _orbLow.Process(candle.LowPrice);
	var volVal = _orbVol.Process(candle.TotalVolume);

	if (_orbHighVal == null && _orbHigh.IsFormed)
	{
		_orbHighVal = orbHigh.ToDecimal();
		_orbLowVal = orbLow.ToDecimal();
		_orbVolAvg = volVal.ToDecimal();
	}

	var orbLong = _orbHighVal.HasValue && _orbVolAvg.HasValue && _prevClose <= _orbHighVal && candle.ClosePrice > _orbHighVal && candle.TotalVolume > _orbVolAvg * VolThreshold;
	var orbShort = _orbLowVal.HasValue && _orbVolAvg.HasValue && _prevClose >= _orbLowVal && candle.ClosePrice < _orbLowVal && candle.TotalVolume > _orbVolAvg * VolThreshold;

	var crossSma50Up = _prevClose <= _prevSma50 && candle.ClosePrice > sma50Val;
	var crossSma50Down = _prevClose >= _prevSma50 && candle.ClosePrice < sma50Val;
	var crossEmaUp = _prevEma9 <= _prevEma20 && ema9Val > ema20Val;
	var crossEmaDown = _prevEma9 >= _prevEma20 && ema9Val < ema20Val;

	var trendUp = candle.ClosePrice > sma200Val;
	var trendDown = candle.ClosePrice < sma200Val;

	var longCond = (crossSma50Up && rsiVal < RsiOversold && candle.ClosePrice < vwapVal && trendUp)
	|| (crossEmaUp && candle.ClosePrice > vwapVal && trendUp)
	|| orbLong;
	var shortCond = (crossSma50Down && rsiVal > RsiOverbought && candle.ClosePrice > vwapVal && trendDown)
	|| (crossEmaDown && candle.ClosePrice < vwapVal && trendDown)
	|| orbShort;

	if (lowVal.IsFinal)
	_lastLow = lowVal.ToDecimal();
	if (highVal.IsFinal)
	_lastHigh = highVal.ToDecimal();

	var longStop = _lastLow - atrVal * StopAtrMultiplier;
	var shortStop = _lastHigh + atrVal * StopAtrMultiplier;

	if (longCond && Position == 0)
	{
		var vol = Volume;
		BuyMarket(vol);
		_qty = vol;
		_stop = longStop;
		var dist = candle.ClosePrice - longStop;
		_tp1 = candle.ClosePrice + dist * Rr1;
		_tp2 = candle.ClosePrice + dist * Rr2;
		_tp1Done = false;
		_beSet = false;
	}
	else if (shortCond && Position == 0)
	{
		var vol = Volume;
		SellMarket(vol);
		_qty = vol;
		_stop = shortStop;
		var dist = shortStop - candle.ClosePrice;
		_tp1 = candle.ClosePrice - dist * Rr1;
		_tp2 = candle.ClosePrice - dist * Rr2;
		_tp1Done = false;
		_beSet = false;
	}

	if (Position > 0)
	{
		if (candle.LowPrice <= _stop)
		{
			SellMarket(Math.Abs(Position));
			ResetTrade();
		}
		else
		{
			if (!_tp1Done && _tp1.HasValue && candle.HighPrice >= _tp1)
			{
				var q = _qty.Value * (Target1Percent / 100m);
				SellMarket(q);
				_tp1Done = true;
			}
			if (_tp1Done && !_beSet)
			{
				_stop = PositionAvgPrice;
				_beSet = true;
			}
			if (_tp2.HasValue && candle.HighPrice >= _tp2)
			{
				SellMarket(Math.Abs(Position));
				ResetTrade();
			}
		}
	}
	else if (Position < 0)
	{
		if (candle.HighPrice >= _stop)
		{
			BuyMarket(Math.Abs(Position));
			ResetTrade();
		}
		else
		{
			if (!_tp1Done && _tp1.HasValue && candle.LowPrice <= _tp1)
			{
				var q = _qty.Value * (Target1Percent / 100m);
				BuyMarket(q);
				_tp1Done = true;
			}
			if (_tp1Done && !_beSet)
			{
				_stop = PositionAvgPrice;
				_beSet = true;
			}
			if (_tp2.HasValue && candle.LowPrice <= _tp2)
			{
				BuyMarket(Math.Abs(Position));
				ResetTrade();
			}
		}
	}

	_prevClose = candle.ClosePrice;
	_prevSma50 = sma50Val;
	_prevEma9 = ema9Val;
	_prevEma20 = ema20Val;
}

private void ResetTrade()
{
	_stop = null;
	_tp1 = null;
	_tp2 = null;
	_qty = null;
	_tp1Done = false;
	_beSet = false;
}
}
