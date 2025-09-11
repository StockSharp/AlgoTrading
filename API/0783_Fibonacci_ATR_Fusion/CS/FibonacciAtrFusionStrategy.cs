namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public enum TradeDirectionOption
{
	Both,
	Long,
	Short,
}

/// <summary>
/// Integrates weighted buying pressure ratios and ATR thresholds.
/// </summary>
public class FibonacciAtrFusionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _longEntryThreshold;
	private readonly StrategyParam<decimal> _shortEntryThreshold;
	private readonly StrategyParam<decimal> _longExitThreshold;
	private readonly StrategyParam<decimal> _shortExitThreshold;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _tp1Atr;
	private readonly StrategyParam<decimal> _tp2Atr;
	private readonly StrategyParam<decimal> _tp3Atr;
	private readonly StrategyParam<decimal> _tp1Percent;
	private readonly StrategyParam<decimal> _tp2Percent;
	private readonly StrategyParam<decimal> _tp3Percent;
	private readonly StrategyParam<TradeDirectionOption> _tradeDirection;
	private readonly StrategyParam<DataType> _candleType;
	
	private SimpleMovingAverage _bp8;
	private SimpleMovingAverage _bp13;
	private SimpleMovingAverage _bp21;
	private SimpleMovingAverage _bp34;
	private SimpleMovingAverage _bp55;
	private AverageTrueRange _atr8;
	private AverageTrueRange _atr13;
	private AverageTrueRange _atr21;
	private AverageTrueRange _atr34;
	private AverageTrueRange _atr55;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _weightedSma;
	private decimal? _prevClose;
	private decimal _prevWeighted;
	
	public decimal LongEntryThreshold
	{
		get => _longEntryThreshold.Value;
		set => _longEntryThreshold.Value = value;
	}
	
	public decimal ShortEntryThreshold
	{
		get => _shortEntryThreshold.Value;
		set => _shortEntryThreshold.Value = value;
	}
	
	public decimal LongExitThreshold
	{
		get => _longExitThreshold.Value;
		set => _longExitThreshold.Value = value;
	}
	
	public decimal ShortExitThreshold
	{
		get => _shortExitThreshold.Value;
		set => _shortExitThreshold.Value = value;
	}
	
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}
	
	public decimal Tp1Atr
	{
		get => _tp1Atr.Value;
		set => _tp1Atr.Value = value;
	}
	
	public decimal Tp2Atr
	{
		get => _tp2Atr.Value;
		set => _tp2Atr.Value = value;
	}
	
	public decimal Tp3Atr
	{
		get => _tp3Atr.Value;
		set => _tp3Atr.Value = value;
	}
	
	public decimal Tp1Percent
	{
		get => _tp1Percent.Value;
		set => _tp1Percent.Value = value;
	}
	
	public decimal Tp2Percent
	{
		get => _tp2Percent.Value;
		set => _tp2Percent.Value = value;
	}
	
	public decimal Tp3Percent
	{
		get => _tp3Percent.Value;
		set => _tp3Percent.Value = value;
	}
	
	public TradeDirectionOption TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public FibonacciAtrFusionStrategy()
	{
		_longEntryThreshold = Param(nameof(LongEntryThreshold), 58m)
		.SetDisplay("Long Entry Threshold", "Threshold to enter long", "General");
		
		_shortEntryThreshold = Param(nameof(ShortEntryThreshold), 42m)
		.SetDisplay("Short Entry Threshold", "Threshold to enter short", "General");
		
		_longExitThreshold = Param(nameof(LongExitThreshold), 42m)
		.SetDisplay("Long Exit Threshold", "Threshold to exit long", "General");
		
		_shortExitThreshold = Param(nameof(ShortExitThreshold), 58m)
		.SetDisplay("Short Exit Threshold", "Threshold to exit short", "General");
		
		_useTakeProfit = Param(nameof(UseTakeProfit), false)
		.SetDisplay("Enable Take Profit", "Use ATR based take profit", "Risk");
		
		_tp1Atr = Param(nameof(Tp1Atr), 3m)
		.SetDisplay("TP1 ATR Mult", "ATR multiplier for TP1", "Risk");
		_tp2Atr = Param(nameof(Tp2Atr), 8m)
		.SetDisplay("TP2 ATR Mult", "ATR multiplier for TP2", "Risk");
		_tp3Atr = Param(nameof(Tp3Atr), 14m)
		.SetDisplay("TP3 ATR Mult", "ATR multiplier for TP3", "Risk");
		
		_tp1Percent = Param(nameof(Tp1Percent), 12m)
		.SetDisplay("TP1 Percent", "Percent to close at TP1", "Risk");
		_tp2Percent = Param(nameof(Tp2Percent), 12m)
		.SetDisplay("TP2 Percent", "Percent to close at TP2", "Risk");
		_tp3Percent = Param(nameof(Tp3Percent), 12m)
		.SetDisplay("TP3 Percent", "Percent to close at TP3", "Risk");
		
		_tradeDirection = Param(nameof(TradeDirection), TradeDirectionOption.Both)
		.SetDisplay("Trade Direction", "Allowed trade direction", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevClose = null;
		_prevWeighted = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_bp8 = new SimpleMovingAverage { Length = 8 };
		_bp13 = new SimpleMovingAverage { Length = 13 };
		_bp21 = new SimpleMovingAverage { Length = 21 };
		_bp34 = new SimpleMovingAverage { Length = 34 };
		_bp55 = new SimpleMovingAverage { Length = 55 };
		
		_atr8 = new AverageTrueRange { Length = 8 };
		_atr13 = new AverageTrueRange { Length = 13 };
		_atr21 = new AverageTrueRange { Length = 21 };
		_atr34 = new AverageTrueRange { Length = 34 };
		_atr55 = new AverageTrueRange { Length = 55 };
		_atr = new AverageTrueRange { Length = 14 };
		_weightedSma = new SimpleMovingAverage { Length = 3 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var prevClose = _prevClose ?? candle.ClosePrice;
		var trueLow = Math.Min(candle.LowPrice, prevClose);
		var trueHigh = Math.Max(candle.HighPrice, prevClose);
		var bp = candle.ClosePrice - trueLow;
		
		var atr8 = _atr8.Process(candle).ToDecimal();
		var atr13 = _atr13.Process(candle).ToDecimal();
		var atr21 = _atr21.Process(candle).ToDecimal();
		var atr34 = _atr34.Process(candle).ToDecimal();
		var atr55 = _atr55.Process(candle).ToDecimal();
		var atrValue = _atr.Process(candle).ToDecimal();
		
		var bp8 = _bp8.Process(bp, candle.ServerTime, true).ToDecimal();
		var bp13 = _bp13.Process(bp, candle.ServerTime, true).ToDecimal();
		var bp21 = _bp21.Process(bp, candle.ServerTime, true).ToDecimal();
		var bp34 = _bp34.Process(bp, candle.ServerTime, true).ToDecimal();
		var bp55 = _bp55.Process(bp, candle.ServerTime, true).ToDecimal();
		
		if (!_bp55.IsFormed || !_atr55.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevWeighted = 0m;
			return;
		}
		
		var ratio8 = atr8 == 0m ? 0m : 100m * bp8 / atr8;
		var ratio13 = atr13 == 0m ? 0m : 100m * bp13 / atr13;
		var ratio21 = atr21 == 0m ? 0m : 100m * bp21 / atr21;
		var ratio34 = atr34 == 0m ? 0m : 100m * bp34 / atr34;
		var ratio55 = atr55 == 0m ? 0m : 100m * bp55 / atr55;
		
		var weighted = (5m * ratio8 + 4m * ratio13 + 3m * ratio21 + 2m * ratio34 + ratio55) / 15m;
		var weightedSma = _weightedSma.Process(weighted, candle.ServerTime, true).ToDecimal();
		
		if (!_weightedSma.IsFormed)
		{
			_prevWeighted = weightedSma;
			_prevClose = candle.ClosePrice;
			return;
		}
		
		var longEntry = _prevWeighted <= LongEntryThreshold && weightedSma > LongEntryThreshold;
		var shortEntry = _prevWeighted >= ShortEntryThreshold && weightedSma < ShortEntryThreshold;
		var longExit = _prevWeighted >= LongExitThreshold && weightedSma < LongExitThreshold;
		var shortExit = _prevWeighted <= ShortExitThreshold && weightedSma > ShortExitThreshold;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevWeighted = weightedSma;
			_prevClose = candle.ClosePrice;
			return;
		}
		
		if (longExit && Position > 0)
		{
			CancelActiveOrders();
			ClosePosition();
		}
	else if (shortExit && Position < 0)
	{
		CancelActiveOrders();
		ClosePosition();
	}
	
	if (longEntry && Position <= 0 && (TradeDirection == TradeDirectionOption.Long || TradeDirection == TradeDirectionOption.Both))
	{
		CancelActiveOrders();
		BuyMarket();
		
		if (UseTakeProfit)
		PlaceTakeProfits(candle.ClosePrice, atrValue, true);
	}
else if (shortEntry && Position >= 0 && (TradeDirection == TradeDirectionOption.Short || TradeDirection == TradeDirectionOption.Both))
{
	CancelActiveOrders();
	SellMarket();
	
	if (UseTakeProfit)
	PlaceTakeProfits(candle.ClosePrice, atrValue, false);
}

_prevWeighted = weightedSma;
_prevClose = candle.ClosePrice;
}

private void PlaceTakeProfits(decimal entryPrice, decimal atr, bool isLong)
{
	var volume = Volume;
	
	if (isLong)
	{
		if (Tp1Percent > 0m)
		SellLimit(entryPrice + Tp1Atr * atr, volume * Tp1Percent / 100m);
		if (Tp2Percent > 0m)
		SellLimit(entryPrice + Tp2Atr * atr, volume * Tp2Percent / 100m);
		if (Tp3Percent > 0m)
		SellLimit(entryPrice + Tp3Atr * atr, volume * Tp3Percent / 100m);
	}
else
{
	if (Tp1Percent > 0m)
	BuyLimit(entryPrice - Tp1Atr * atr, volume * Tp1Percent / 100m);
	if (Tp2Percent > 0m)
	BuyLimit(entryPrice - Tp2Atr * atr, volume * Tp2Percent / 100m);
	if (Tp3Percent > 0m)
	BuyLimit(entryPrice - Tp3Atr * atr, volume * Tp3Percent / 100m);
}
}
}
