using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Energy Advanced Policy strategy. Simplified version.
/// </summary>
public class EnergyAdvancedPolicyStrategy : Strategy
{
private readonly StrategyParam<string> _newsSentiment;
private readonly StrategyParam<bool> _enableNewsFilter;
private readonly StrategyParam<bool> _enablePolicyDetection;
private readonly StrategyParam<decimal> _policyVolumeThreshold;
private readonly StrategyParam<decimal> _policyPriceThreshold;
private readonly StrategyParam<int> _rsiLength;
private readonly StrategyParam<int> _rsiOverbought;
private readonly StrategyParam<int> _fastLength;
private readonly StrategyParam<int> _slowLength;
private readonly StrategyParam<int> _bbLength;
private readonly StrategyParam<decimal> _bbMult;
private readonly StrategyParam<DataType> _candleType;

private RelativeStrengthIndex _rsi;
private ExponentialMovingAverage _fastMa;
private ExponentialMovingAverage _slowMa;
private BollingerBands _bb;

public EnergyAdvancedPolicyStrategy()
{
_newsSentiment = Param(nameof(NewsSentiment), "positive");
_enableNewsFilter = Param(nameof(EnableNewsFilter), true);
_enablePolicyDetection = Param(nameof(EnablePolicyDetection), true);
_policyVolumeThreshold = Param(nameof(PolicyVolumeThreshold), 2m);
_policyPriceThreshold = Param(nameof(PolicyPriceThreshold), 3m);
_rsiLength = Param(nameof(RsiLength), 14);
_rsiOverbought = Param(nameof(RsiOverbought), 75);
_fastLength = Param(nameof(FastLength), 21);
_slowLength = Param(nameof(SlowLength), 55);
_bbLength = Param(nameof(BbLength), 20);
_bbMult = Param(nameof(BbMult), 2m);
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
}

public string NewsSentiment { get => _newsSentiment.Value; set => _newsSentiment.Value = value; }
public bool EnableNewsFilter { get => _enableNewsFilter.Value; set => _enableNewsFilter.Value = value; }
public bool EnablePolicyDetection { get => _enablePolicyDetection.Value; set => _enablePolicyDetection.Value = value; }
public decimal PolicyVolumeThreshold { get => _policyVolumeThreshold.Value; set => _policyVolumeThreshold.Value = value; }
public decimal PolicyPriceThreshold { get => _policyPriceThreshold.Value; set => _policyPriceThreshold.Value = value; }
public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
public decimal BbMult { get => _bbMult.Value; set => _bbMult.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_rsi = new RelativeStrengthIndex { Length = RsiLength };
_fastMa = new ExponentialMovingAverage { Length = FastLength };
_slowMa = new ExponentialMovingAverage { Length = SlowLength };
_bb = new BollingerBands { Length = BbLength, Width = BbMult };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(_rsi, _fastMa, _slowMa, _bb, Process).Start();
}

private void Process(ICandleMessage candle, decimal rsi, decimal fast, decimal slow, decimal middle, decimal upper, decimal lower)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var maBullish = fast > slow;
var bbSqueeze = (upper - lower) / middle < 0.1m;
var longCond = maBullish && rsi < RsiOverbought && !bbSqueeze;

if (longCond && Position == 0)
BuyMarket(Volume);
else if (Position > 0 && (rsi > RsiOverbought || !maBullish))
SellMarket(Position);
}
}
