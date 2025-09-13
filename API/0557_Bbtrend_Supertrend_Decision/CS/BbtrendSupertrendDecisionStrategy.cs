
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BBTrend with SuperTrend decision strategy.
/// Computes BBTrend from two Bollinger Bands and applies a SuperTrend
/// calculation to generate long and short entries.
/// </summary>
public class BbtrendSupertrendDecisionStrategy : Strategy
{
	private readonly StrategyParam<int> _shortBbLength;
	private readonly StrategyParam<int> _longBbLength;
	private readonly StrategyParam<decimal> _stdDev;
	private readonly StrategyParam<int> _supertrendLength;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<Sides?> _tradeDirection;
	private readonly StrategyParam<TpSlMode> _tpSlCondition;
	private readonly StrategyParam<decimal> _takeProfitPerc;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _previousBbTrend;
	private decimal? _prevUp;
	private decimal? _prevDn;
	private decimal? _prevAtr;
	private decimal? _prevSt;
	
	/// <summary>
	/// Short Bollinger Bands length.
	/// </summary>
	public int ShortBbLength
	{
		get => _shortBbLength.Value;
		set => _shortBbLength.Value = value;
	}
	
	/// <summary>
	/// Long Bollinger Bands length.
	/// </summary>
	public int LongBbLength
	{
		get => _longBbLength.Value;
		set => _longBbLength.Value = value;
	}
	
	/// <summary>
	/// Bollinger Bands standard deviation.
	/// </summary>
	public decimal StdDev
	{
		get => _stdDev.Value;
		set => _stdDev.Value = value;
	}
	
	/// <summary>
	/// SuperTrend ATR period.
	/// </summary>
	public int SupertrendLength
	{
		get => _supertrendLength.Value;
		set => _supertrendLength.Value = value;
	}
	
	/// <summary>
	/// SuperTrend multiplier.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}
	
	/// <summary>
	/// Trading direction.
	/// </summary>
	public Sides? TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}
	
	/// <summary>
	/// Take profit / stop loss mode.
	/// </summary>
	public TpSlMode TpSlCondition
	{
		get => _tpSlCondition.Value;
		set => _tpSlCondition.Value = value;
	}
	
	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPerc
	{
		get => _takeProfitPerc.Value;
		set => _takeProfitPerc.Value = value;
	}
	
	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPerc
	{
		get => _stopLossPerc.Value;
		set => _stopLossPerc.Value = value;
	}
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public BbtrendSupertrendDecisionStrategy()
	{
		_shortBbLength = Param(nameof(ShortBbLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Short BB Length", "Short Bollinger Bands length", "BBTrend")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);
		
		_longBbLength = Param(nameof(LongBbLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Long BB Length", "Long Bollinger Bands length", "BBTrend")
		.SetCanOptimize(true)
		.SetOptimize(30, 100, 5);
		
		_stdDev = Param(nameof(StdDev), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Std Dev", "Standard deviation", "BBTrend");
		
		_supertrendLength = Param(nameof(SupertrendLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("ST Length", "SuperTrend ATR period", "SuperTrend")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 7m)
		.SetGreaterThanZero()
		.SetDisplay("ST Factor", "SuperTrend multiplier", "SuperTrend")
		.SetCanOptimize(true)
		.SetOptimize(1m, 10m, 1m);
		
		_tradeDirection = Param(nameof(TradeDirection), (Sides?)null)
		.SetDisplay("Direction", "Allowed trading direction", "Trading");
		
		_tpSlCondition = Param(nameof(TpSlCondition), Strategies.TpSlMode.None)
		.SetDisplay("TP/SL Mode", "Protection mode", "Risk");
		
		_takeProfitPerc = Param(nameof(TakeProfitPerc), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (%)", "Take profit percentage", "Risk");
		
		_stopLossPerc = Param(nameof(StopLossPerc), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (%)", "Stop loss percentage", "Risk");
		
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
		
		var shortBb = new BollingerBands { Length = ShortBbLength, Width = StdDev };
		var longBb = new BollingerBands { Length = LongBbLength, Width = StdDev };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(shortBb, longBb, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortBb);
			DrawIndicator(area, longBb);
			DrawOwnTrades(area);
		}
		
		Unit tp = default;
		Unit sl = default;
		if (TpSlCondition == TpSlMode.TP || TpSlCondition == TpSlMode.Both)
		tp = TakeProfitPerc.Percents();
		if (TpSlCondition == TpSlMode.SL || TpSlCondition == TpSlMode.Both)
		sl = StopLossPerc.Percents();
		if (tp != default || sl != default)
		StartProtection(tp, sl);
	}
	
	private void ProcessCandle(ICandleMessage candle,
	decimal shortMiddle, decimal shortUpper, decimal shortLower,
	decimal longMiddle, decimal longUpper, decimal longLower)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var bbTrend = (Math.Abs(shortLower - longLower) - Math.Abs(shortUpper - longUpper)) / shortMiddle * 100m;
		
		if (_previousBbTrend is null)
		{
			_previousBbTrend = bbTrend;
			return;
		}
		
		var open = _previousBbTrend.Value;
		var close = bbTrend;
		var high = Math.Max(open, close);
		var low = Math.Min(open, close);
		
		var tr = Math.Max(Math.Max(high - low, Math.Abs(high - open)), Math.Abs(low - open));
		var atr = _prevAtr is null ? tr : _prevAtr.Value + (tr - _prevAtr.Value) / SupertrendLength;
		
		var hl2 = (high + low) / 2m;
		var up = hl2 + SupertrendMultiplier * atr;
		if (_prevUp is not null && !((up < _prevUp.Value) || (open > _prevUp.Value)))
		up = _prevUp.Value;
		var dn = hl2 - SupertrendMultiplier * atr;
		if (_prevDn is not null && !((dn > _prevDn.Value) || (open < _prevDn.Value)))
		dn = _prevDn.Value;
		
		int dir;
		if (_prevAtr is null)
		dir = 1;
		else if (_prevSt.HasValue && _prevUp.HasValue && _prevSt.Value == _prevUp.Value)
		dir = close > up ? -1 : 1;
		else
		dir = close < dn ? 1 : -1;
		
		var st = dir == -1 ? dn : up;
		
		var allowLong = TradeDirection == null || TradeDirection == Sides.Buy;
		var allowShort = TradeDirection == null || TradeDirection == Sides.Sell;
		
		if (dir > 0 && Position > 0)
		SellMarket();
		if (dir < 0 && Position < 0)
		BuyMarket();
		
		if (allowLong && dir < 0 && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
		BuyMarket();
		if (allowShort && dir > 0 && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
		SellMarket();
		
		_previousBbTrend = close;
		_prevAtr = atr;
		_prevUp = up;
		_prevDn = dn;
		_prevSt = st;
	}
}

/// <summary>
/// Protection mode for take profit and stop loss.
/// </summary>
public enum TpSlMode
{
	None,
	TP,
	SL,
	Both
}
