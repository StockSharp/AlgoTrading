using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smart money strategy using HTF zones, cumulative delta and wick pullback.
/// </summary>
public class YuriGarciaSmartMoneyStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _zoneLookback;
	private readonly StrategyParam<decimal> _zoneBuffer;
private readonly StrategyParam<Sides?> _tradeDirection;
	private readonly StrategyParam<DataType> _candleType;

	private ATR _atr = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _entryPrice;
	private decimal _atrValue;
	private decimal _cumDelta;
	private decimal _prevCumDelta;
	private bool _prevBull;
	private bool _prevBear;

	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public int ZoneLookback { get => _zoneLookback.Value; set => _zoneLookback.Value = value; }
	public decimal ZoneBuffer { get => _zoneBuffer.Value; set => _zoneBuffer.Value = value; }
	public Sides? TradeDirection { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public YuriGarciaSmartMoneyStrategy()
	{
	    _atrLength = Param(nameof(AtrLength), 14)
	        .SetGreaterThanZero()
	        .SetDisplay("ATR Length", "Period of ATR", "General");

	    _atrMultiplier = Param(nameof(AtrMultiplier), 2m)
	        .SetGreaterThanZero()
	        .SetDisplay("ATR Mult", "Stop multiplier", "General");

	    _riskReward = Param(nameof(RiskReward), 2m)
	        .SetGreaterThanZero()
	        .SetDisplay("RRR", "Risk reward ratio", "General");

	    _zoneLookback = Param(nameof(ZoneLookback), 20)
	        .SetGreaterThanZero()
	        .SetDisplay("Zone Lookback", "Lookback for high/low zone", "General");

	    _zoneBuffer = Param(nameof(ZoneBuffer), 0.002m)
	        .SetGreaterThanZero()
	        .SetDisplay("Zone Buffer", "Buffer percent", "General");

		_tradeDirection = Param(nameof(TradeDirection), (Sides?)null)
			.SetDisplay("Trade Direction", "Both / Buy Only / Sell Only", "General");

	    _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
	        .SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	    return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    _atr = new ATR { Length = AtrLength };
	    _highest = new Highest { Length = ZoneLookback };
	    _lowest = new Lowest { Length = ZoneLookback };

	    var subscription = SubscribeCandles(CandleType);
	    subscription.Bind(ProcessCandle).Start();
	}

	private bool CanBuy => TradeDirection != Sides.Sell;
	private bool CanSell => TradeDirection != Sides.Buy;

	private void ProcessCandle(ICandleMessage candle)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    _atrValue = _atr.Process(candle).ToDecimal();
	    var highZone = _highest.Process(candle).ToDecimal();
	    var lowZone = _lowest.Process(candle).ToDecimal();

	    var top = highZone * (1 + ZoneBuffer);
	    var bottom = lowZone * (1 - ZoneBuffer);
	    var inZone = candle.ClosePrice <= top && candle.ClosePrice >= bottom;

	    var upVol = candle.ClosePrice > candle.OpenPrice ? candle.TotalVolume : 0m;
	    var downVol = candle.ClosePrice < candle.OpenPrice ? candle.TotalVolume : 0m;
	    _cumDelta += upVol - downVol;
	    var deltaConfLong = _cumDelta > _prevCumDelta;
	    var deltaConfShort = _cumDelta < _prevCumDelta;
	    _prevCumDelta = _cumDelta;

	    var isBull = candle.ClosePrice > candle.OpenPrice;
	    var isBear = candle.ClosePrice < candle.OpenPrice;
	    var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
	    var pullLong = isBull && _prevBear && candle.LowPrice <= candle.OpenPrice - body / 2m;
	    var pullShort = isBear && _prevBull && candle.HighPrice >= candle.ClosePrice + body / 2m;
	    _prevBull = isBull;
	    _prevBear = isBear;

	    var condLong = inZone && pullLong && deltaConfLong;
	    var condShort = inZone && pullShort && deltaConfShort;

	    if (condLong && CanBuy && Position <= 0)
	    {
	        BuyMarket();
	        _entryPrice = candle.ClosePrice;
	    }
	    else if (condShort && CanSell && Position >= 0)
	    {
	        SellMarket();
	        _entryPrice = candle.ClosePrice;
	    }

	    if (Position > 0)
	    {
	        var stop = _entryPrice - _atrValue * AtrMultiplier;
	        var target = _entryPrice + _atrValue * AtrMultiplier * RiskReward;
	        if (candle.ClosePrice <= stop || candle.ClosePrice >= target)
	            SellMarket();
	    }
	    else if (Position < 0)
	    {
	        var stop = _entryPrice + _atrValue * AtrMultiplier;
	        var target = _entryPrice - _atrValue * AtrMultiplier * RiskReward;
	        if (candle.ClosePrice >= stop || candle.ClosePrice <= target)
	            BuyMarket();
	    }
	}
}
