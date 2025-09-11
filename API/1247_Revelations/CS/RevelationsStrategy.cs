using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility spike based strategy inspired by REVELATIONS (VoVix - PoC).
/// Enters on strong ATR spikes confirmed by local extrema and regime index.
/// </summary>
public class RevelationsStrategy : Strategy
{
	private readonly StrategyParam<int> _atrFast;
	private readonly StrategyParam<int> _atrSlow;
	private readonly StrategyParam<int> _atrStd;
	private readonly StrategyParam<decimal> _spikeThreshold;
	private readonly StrategyParam<decimal> _superSpikeMultiplier;
	private readonly StrategyParam<int> _regimeWindow;
	private readonly StrategyParam<int> _regimeEvents;
	private readonly StrategyParam<int> _localWindow;
	private readonly StrategyParam<decimal> _maxQty;
	private readonly StrategyParam<decimal> _minQty;
	private readonly StrategyParam<decimal> _stopPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private SMA _vovixAvg;
	private Highest _spikeMax;
	private Lowest _priceLow;
	private Highest _priceHigh;
	private Sum _regimeSum;
	private decimal _prevVoSpike;
	
	/// <summary>
	/// Fast ATR period.
	/// </summary>
	public int AtrFast
	{
		get => _atrFast.Value;
		set => _atrFast.Value = value;
	}
	
	/// <summary>
	/// Slow ATR period.
	/// </summary>
	public int AtrSlow
	{
		get => _atrSlow.Value;
		set => _atrSlow.Value = value;
	}
	
	/// <summary>
	/// ATR standard deviation window.
	/// </summary>
	public int AtrStd
	{
		get => _atrStd.Value;
		set => _atrStd.Value = value;
	}
	
	/// <summary>
	/// Base spike threshold.
	/// </summary>
	public decimal SpikeThreshold
	{
		get => _spikeThreshold.Value;
		set => _spikeThreshold.Value = value;
	}
	
	/// <summary>
	/// Super spike multiplier.
	/// </summary>
	public decimal SuperSpikeMultiplier
	{
		get => _superSpikeMultiplier.Value;
		set => _superSpikeMultiplier.Value = value;
	}
	
	/// <summary>
	/// Regime window length.
	/// </summary>
	public int RegimeWindow
	{
		get => _regimeWindow.Value;
		set => _regimeWindow.Value = value;
	}
	
	/// <summary>
	/// Minimum spike events to allow trading.
	/// </summary>
	public int RegimeEvents
	{
		get => _regimeEvents.Value;
		set => _regimeEvents.Value = value;
	}
	
	/// <summary>
	/// Window for local extremes.
	/// </summary>
	public int LocalWindow
	{
		get => _localWindow.Value;
		set => _localWindow.Value = value;
	}
	
	/// <summary>
	/// Maximum position size.
	/// </summary>
	public decimal MaxQty
	{
		get => _maxQty.Value;
		set => _maxQty.Value = value;
	}
	
	/// <summary>
	/// Minimum position size.
	/// </summary>
	public decimal MinQty
	{
		get => _minQty.Value;
		set => _minQty.Value = value;
	}
	
	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopPercent
	{
		get => _stopPercent.Value;
		set => _stopPercent.Value = value;
	}
	
	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}
	
	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="RevelationsStrategy"/>.
	/// </summary>
	public RevelationsStrategy()
	{
		_atrFast = Param(nameof(AtrFast), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Fast", "Fast ATR length", "Parameters");
		
		_atrSlow = Param(nameof(AtrSlow), 21)
		.SetGreaterThanZero()
		.SetDisplay("ATR Slow", "Slow ATR length", "Parameters");
		
		_atrStd = Param(nameof(AtrStd), 12)
		.SetGreaterThanZero()
		.SetDisplay("ATR StdDev", "ATR standard deviation window", "Parameters");
		
		_spikeThreshold = Param(nameof(SpikeThreshold), 0.5m)
		.SetDisplay("Spike Threshold", "Base spike threshold", "Parameters");
		
		_superSpikeMultiplier = Param(nameof(SuperSpikeMultiplier), 1.5m)
		.SetDisplay("Super Spike Mult", "Super spike multiplier", "Parameters");
		
		_regimeWindow = Param(nameof(RegimeWindow), 8)
		.SetGreaterThanZero()
		.SetDisplay("Regime Window", "Regime window length", "Parameters");
		
		_regimeEvents = Param(nameof(RegimeEvents), 3)
		.SetGreaterThanZero()
		.SetDisplay("Regime Events", "Required spikes in window", "Parameters");
		
		_localWindow = Param(nameof(LocalWindow), 3)
		.SetGreaterThanZero()
		.SetDisplay("Local Window", "Window for local extremes", "Parameters");
		
		_maxQty = Param(nameof(MaxQty), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Max Quantity", "Maximum contracts", "Size");
		
		_minQty = Param(nameof(MinQty), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Min Quantity", "Minimum contracts", "Size");
		
		_stopPercent = Param(nameof(StopPercent), 0.9m)
		.SetGreaterThanZero()
		.SetDisplay("Stop %", "Stop percent", "Trade Management");
		
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1.8m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit percent", "Trade Management");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_vovixAvg = new SMA { Length = LocalWindow * 4 };
		_spikeMax = new Highest { Length = LocalWindow };
		_priceLow = new Lowest { Length = LocalWindow };
		_priceHigh = new Highest { Length = LocalWindow };
		_regimeSum = new Sum { Length = RegimeWindow };
		
		var atrFast = new ATR { Length = AtrFast };
		var atrSlow = new ATR { Length = AtrSlow };
		var atrStd = new StandardDeviation { Length = AtrStd };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atrFast, atrSlow, atrStd, ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atrFast, decimal atrSlow, decimal atrStd)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var voSpike = (atrFast - atrSlow) / (atrStd + 1m);
		var absSpike = Math.Abs(voSpike);
		
		var vovixAvgVal = _vovixAvg.Process(absSpike, candle.ServerTime, true);
		var spikeMaxVal = _spikeMax.Process(absSpike, candle.ServerTime, true);
		var lowVal = _priceLow.Process(candle.LowPrice);
		var highVal = _priceHigh.Process(candle.HighPrice);
		var regimeVal = _regimeSum.Process(Math.Abs(_prevVoSpike) > SpikeThreshold ? 1m : 0m, candle.ServerTime, true);
		
		if (!vovixAvgVal.IsFinal || !spikeMaxVal.IsFinal || !lowVal.IsFinal || !highVal.IsFinal || !regimeVal.IsFinal)
		{
			_prevVoSpike = voSpike;
			return;
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevVoSpike = voSpike;
			return;
		}
		
		var vovixAvg = vovixAvgVal.ToDecimal();
		var spikeMax = spikeMaxVal.ToDecimal();
		var localLow = lowVal.ToDecimal();
		var localHigh = highVal.ToDecimal();
		var regimeIndex = (int)regimeVal.ToDecimal();
		
		var superSpike = absSpike > vovixAvg * SuperSpikeMultiplier;
		var baseSpike = Math.Abs(_prevVoSpike) > SpikeThreshold;
		var vovixLocalMax = absSpike == spikeMax;
		
		var deltaSpike = Math.Abs(voSpike - vovixAvg);
		var sizingFactor = deltaSpike / (SpikeThreshold * SuperSpikeMultiplier + 1e-8m);
		var tradeQty = MinQty + (MaxQty - MinQty) * Tanh(sizingFactor);
		tradeQty = Math.Clamp(tradeQty, MinQty, MaxQty);
		
		var canLong = baseSpike && vovixLocalMax && superSpike && regimeIndex >= RegimeEvents && voSpike > 0 && candle.LowPrice == localLow;
		var canShort = baseSpike && vovixLocalMax && superSpike && regimeIndex >= RegimeEvents && voSpike < 0 && candle.HighPrice == localHigh;
		
		if (Position == 0)
		{
			if (canLong)
			BuyMarket(tradeQty);
		else if (canShort)
		SellMarket(tradeQty);
	}
else if (Position > 0)
{
	var tp = PositionAvgPrice * (1 + TakeProfitPercent / 100m);
	var sl = PositionAvgPrice * (1 - StopPercent / 100m);
	
	if (candle.HighPrice >= tp || candle.LowPrice <= sl)
	SellMarket(Position);
}
else if (Position < 0)
{
	var tp = PositionAvgPrice * (1 - TakeProfitPercent / 100m);
	var sl = PositionAvgPrice * (1 + StopPercent / 100m);
	
	if (candle.LowPrice <= tp || candle.HighPrice >= sl)
	BuyMarket(-Position);
}

_prevVoSpike = voSpike;
}

private static decimal Tanh(decimal x)
=> (decimal)Math.Tanh((double)x);
}
