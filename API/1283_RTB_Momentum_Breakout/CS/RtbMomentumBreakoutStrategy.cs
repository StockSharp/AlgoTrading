using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RTB momentum breakout strategy.
/// </summary>
public class RtbMomentumBreakoutStrategy : Strategy
{
	private const int MaxLookback = 100;

	private readonly StrategyParam<int> _emaFastLen;
	private readonly StrategyParam<int> _emaSlowLen;
	private readonly StrategyParam<int> _rsiLen;
	private readonly StrategyParam<int> _rsiOb;
	private readonly StrategyParam<int> _rsiOs;
	private readonly StrategyParam<int> _breakoutPeriod;
	private readonly StrategyParam<int> _atrLen;
	private readonly StrategyParam<decimal> _atrMultSl;
	private readonly StrategyParam<decimal> _atrMultTrail;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _breakoutHigh;
	private Lowest _breakoutLow;

	private decimal? _prevResistance;
	private decimal? _prevSupport;

public int EmaFastLength { get => _emaFastLen.Value; set => _emaFastLen.Value = value; }
public int EmaSlowLength { get => _emaSlowLen.Value; set => _emaSlowLen.Value = value; }
public int RsiLength { get => _rsiLen.Value; set => _rsiLen.Value = value; }
public int RsiOverbought { get => _rsiOb.Value; set => _rsiOb.Value = value; }
public int RsiOversold { get => _rsiOs.Value; set => _rsiOs.Value = value; }
public int BreakoutPeriod { get => _breakoutPeriod.Value; set => _breakoutPeriod.Value = value; }
public int AtrLength { get => _atrLen.Value; set => _atrLen.Value = value; }
public decimal AtrStopMultiplier { get => _atrMultSl.Value; set => _atrMultSl.Value = value; }
public decimal AtrTrailMultiplier { get => _atrMultTrail.Value; set => _atrMultTrail.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public RtbMomentumBreakoutStrategy()
{
	_emaFastLen = Param(nameof(EmaFastLength), 20);
	_emaSlowLen = Param(nameof(EmaSlowLength), 50);
	_rsiLen = Param(nameof(RsiLength), 14);
	_rsiOb = Param(nameof(RsiOverbought), 70);
	_rsiOs = Param(nameof(RsiOversold), 30);
	_breakoutPeriod = Param(nameof(BreakoutPeriod), 5);
	_atrLen = Param(nameof(AtrLength), 14);
	_atrMultSl = Param(nameof(AtrStopMultiplier), 1.5m);
	_atrMultTrail = Param(nameof(AtrTrailMultiplier), 1.5m);
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	return [(Security, CandleType)];
}

protected override void OnReseted()
{
	base.OnReseted();
	_prevResistance = null;
	_prevSupport = null;
}

protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	var emaFast = new EMA { Length = EmaFastLength };
	var emaSlow = new EMA { Length = EmaSlowLength };
	var rsi = new RSI { Length = RsiLength };
	var atr = new ATR { Length = AtrLength };
	_breakoutHigh = new Highest { Length = BreakoutPeriod };
	_breakoutLow = new Lowest { Length = BreakoutPeriod };
	var longHigh = new Highest { Length = MaxLookback };
	var shortLow = new Lowest { Length = MaxLookback };

	var subscription = SubscribeCandles(CandleType);
	subscription
		.Bind(emaFast, emaSlow, rsi, atr, _breakoutHigh, _breakoutLow, longHigh, shortLow, ProcessCandle)
		.Start();
}
private void ProcessCandle(ICandleMessage candle, decimal emaFastVal, decimal emaSlowVal, decimal rsiVal, decimal atrVal, decimal breakoutHighVal, decimal breakoutLowVal, decimal longHighVal, decimal shortLowVal)
{
	if (candle.State != CandleStates.Finished)
		return;

	if (!_breakoutHigh.IsFormed || !_breakoutLow.IsFormed)
	{
		_prevResistance = breakoutHighVal;
		_prevSupport = breakoutLowVal;
		return;
	}

	var resistance = _prevResistance;
	var support = _prevSupport;
	_prevResistance = breakoutHighVal;
	_prevSupport = breakoutLowVal;

	if (resistance is null || support is null)
		return;

	var longCond = candle.ClosePrice > resistance && emaFastVal > emaSlowVal && rsiVal < RsiOverbought;
	var shortCond = candle.ClosePrice < support && emaFastVal < emaSlowVal && rsiVal > RsiOversold;

	if (longCond)
		BuyMarket();
	else if (shortCond)
		SellMarket();

	if (Position > 0)
	{
		var stopLoss = PositionAvgPrice - atrVal * AtrStopMultiplier;
		var trail = longHighVal - atrVal * AtrTrailMultiplier;
		var exitPrice = Math.Max(stopLoss, trail);

		if (candle.LowPrice <= exitPrice)
			SellMarket(Math.Abs(Position));
	}
	else if (Position < 0)
	{
		var stopLoss = PositionAvgPrice + atrVal * AtrStopMultiplier;
		var trail = shortLowVal + atrVal * AtrTrailMultiplier;
		var exitPrice = Math.Min(stopLoss, trail);

		if (candle.HighPrice >= exitPrice)
			BuyMarket(Math.Abs(Position));
	}
}
}
