using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bar Counter Trend Reversal strategy.
/// Detects consecutive rises or falls with optional volume and channel confirmation.
/// </summary>
public class BarCounterTrendReversalStrategy : Strategy
{
private readonly StrategyParam<int> _noOfRises;
private readonly StrategyParam<int> _noOfFalls;
private readonly StrategyParam<bool> _volumeConfirm;
private readonly StrategyParam<bool> _channelConfirm;
private readonly StrategyParam<ChannelType> _channelType;
private readonly StrategyParam<int> _channelLength;
private readonly StrategyParam<int> _channelMultiplier;
private readonly StrategyParam<DataType> _candleType;

private IIndicator _channelIndicator = null!;
private int _riseCount;
private int _fallCount;
private int _volRiseCount;
private decimal _prevClose;
private decimal _prevVolume;
private bool _riseTriangleReady;
private bool _fallTriangleReady;
private bool _riseTrianglePlotted;
private bool _fallTrianglePlotted;

/// <summary>
/// Channel indicator type.
/// </summary>
public enum ChannelType
{
/// <summary> Keltner Channel. </summary>
Kc,
/// <summary> Bollinger Bands. </summary>
Bb
}

/// <summary>Number of rising closes to trigger short setup.</summary>
public int NoOfRises { get => _noOfRises.Value; set => _noOfRises.Value = value; }
/// <summary>Number of falling closes to trigger long setup.</summary>
public int NoOfFalls { get => _noOfFalls.Value; set => _noOfFalls.Value = value; }
/// <summary>Require volume rising confirmation.</summary>
public bool VolumeConfirm { get => _volumeConfirm.Value; set => _volumeConfirm.Value = value; }
/// <summary>Require channel breakout confirmation.</summary>
public bool ChannelConfirm { get => _channelConfirm.Value; set => _channelConfirm.Value = value; }
/// <summary>Selected channel type.</summary>
public ChannelType Channel { get => _channelType.Value; set => _channelType.Value = value; }
/// <summary>Channel indicator length.</summary>
public int ChannelLength { get => _channelLength.Value; set => _channelLength.Value = value; }
/// <summary>Channel multiplier.</summary>
public int ChannelMultiplier { get => _channelMultiplier.Value; set => _channelMultiplier.Value = value; }
/// <summary>Candle type.</summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Initialize <see cref="BarCounterTrendReversalStrategy"/>.
/// </summary>
public BarCounterTrendReversalStrategy()
{
_noOfRises = Param(nameof(NoOfRises), 3)
.SetDisplay("No. of Rises", "Consecutive rising bars", "Parameters")
.SetGreaterThanZero();
_noOfFalls = Param(nameof(NoOfFalls), 3)
.SetDisplay("No. of Falls", "Consecutive falling bars", "Parameters")
.SetGreaterThanZero();
_volumeConfirm = Param(nameof(VolumeConfirm), false)
.SetDisplay("Volume Confirm", "Require volume rising", "Parameters");
_channelConfirm = Param(nameof(ChannelConfirm), true)
.SetDisplay("Channel Confirm", "Use channel breakout", "Parameters");
_channelType = Param(nameof(Channel), ChannelType.Kc)
.SetDisplay("Channel Type", "Channel indicator type", "Parameters");
_channelLength = Param(nameof(ChannelLength), 20)
.SetDisplay("Channel Length", "Length for channel indicator", "Parameters")
.SetGreaterThanZero();
_channelMultiplier = Param(nameof(ChannelMultiplier), 2)
.SetDisplay("Channel Multiplier", "Multiplier for channel width", "Parameters")
.SetGreaterThanZero();
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "Parameters");
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

_riseCount = _fallCount = _volRiseCount = 0;
_prevClose = _prevVolume = 0m;
_riseTriangleReady = _fallTriangleReady = false;
_riseTrianglePlotted = _fallTrianglePlotted = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_channelIndicator = Channel == ChannelType.Kc
? new KeltnerChannels { Length = ChannelLength, Multiplier = ChannelMultiplier }
: new BollingerBands { Length = ChannelLength, Width = ChannelMultiplier };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_channelIndicator, ProcessCandle)
.Start();

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue channelValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

decimal upper;
decimal lower;

if (channelValue is KeltnerChannelsValue kc)
{
upper = kc.Upper;
lower = kc.Lower;
}
else if (channelValue is BollingerBandsValue bb)
{
if (bb.UpBand is not decimal up || bb.LowBand is not decimal low)
return;
upper = up;
lower = low;
}
else
{
return;
}

var prevClose = _prevClose;
var prevVolume = _prevVolume;

if (prevClose != 0m)
{
if (candle.ClosePrice > prevClose)
{
_riseCount++;
_fallCount = 0;
}
else if (candle.ClosePrice < prevClose)
{
_fallCount++;
_riseCount = 0;
}
else
{
_riseCount = _fallCount = 0;
}
}

if (prevVolume != 0m)
{
if (candle.TotalVolume > prevVolume)
_volRiseCount++;
else if (candle.TotalVolume < prevVolume)
_volRiseCount = 0;
}

_prevClose = candle.ClosePrice;
_prevVolume = candle.TotalVolume;

_riseTriangleReady = ChannelConfirm && VolumeConfirm
? _fallCount >= NoOfFalls && _volRiseCount >= NoOfFalls && candle.HighPrice > upper
: ChannelConfirm
? _fallCount >= NoOfFalls && candle.LowPrice < lower
: VolumeConfirm
? _fallCount >= NoOfFalls && _volRiseCount >= NoOfFalls
: _fallCount >= NoOfFalls;

_fallTriangleReady = ChannelConfirm && VolumeConfirm
? _riseCount >= NoOfRises && _volRiseCount >= NoOfRises && candle.LowPrice < lower
: ChannelConfirm
? _riseCount >= NoOfRises && candle.HighPrice > upper
: VolumeConfirm
? _riseCount >= NoOfRises && _volRiseCount >= NoOfRises
: _riseCount >= NoOfRises;

if (candle.ClosePrice > prevClose)
_riseTrianglePlotted = false;
if (candle.ClosePrice < prevClose)
_fallTrianglePlotted = false;

if (_riseTriangleReady && !_riseTrianglePlotted && Position <= 0)
{
BuyMarket();
_riseTrianglePlotted = true;
_riseTriangleReady = false;
}
else if (_fallTriangleReady && !_fallTrianglePlotted && Position >= 0)
{
SellMarket();
_fallTrianglePlotted = true;
_fallTriangleReady = false;
}
}
}
