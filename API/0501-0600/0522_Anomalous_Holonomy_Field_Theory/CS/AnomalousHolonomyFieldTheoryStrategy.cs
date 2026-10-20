namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>EMA, RSI, MACD, volatility-normalized ROC and bar VWAP composite with a frozen ATR stop.</summary>
public class AnomalousHolonomyFieldTheoryStrategy : Strategy
{
	private readonly StrategyParam<decimal> _signalThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<int> _rocPeriod;
	private readonly StrategyParam<int> _vwapPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<decimal> _closes = new();
	private readonly Queue<(decimal price, decimal volume)> _vwapBars = new();
	private Order _pending;
	private int _bar;
	private int? _lastEntry;

	/// <summary>Initializes the composite and risk parameters.</summary>
	public AnomalousHolonomyFieldTheoryStrategy()
	{
		_signalThreshold = Param(nameof(SignalThreshold), 2m).SetGreaterThanZero()
			.SetDisplay("SignalThreshold", "Absolute composite threshold", "Parameters");
		_cooldownBars = Param(nameof(CooldownBars), 50).SetNotNegative()
			.SetDisplay("CooldownBars", "Minimum finished bars between entries", "Parameters");
		_atrLength = Param(nameof(AtrLength), 14).SetGreaterThanZero()
			.SetDisplay("AtrLength", "Wilder ATR window", "Parameters");
		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 2m).SetGreaterThanZero()
			.SetDisplay("AtrStopMultiplier", "Entry-frozen ATR stop multiple", "Parameters");
		_rocPeriod = Param(nameof(RocPeriod), 10).SetGreaterThanZero()
			.SetDisplay("RocPeriod", "Rate-of-change interval", "Parameters");
		_vwapPeriod = Param(nameof(VwapPeriod), 20).SetGreaterThanZero()
			.SetDisplay("VwapPeriod", "Volume-weighted typical-price window", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle interval", "Parameters");
	}

	/// <summary>Absolute composite threshold.</summary>
	public decimal SignalThreshold
	{
		get => _signalThreshold.Value;
		set => _signalThreshold.Value = value;
	}

	/// <summary>Minimum finished bars between entries.</summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>Wilder ATR window.</summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>Entry-frozen ATR stop multiple.</summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	/// <summary>Rate-of-change interval.</summary>
	public int RocPeriod
	{
		get => _rocPeriod.Value;
		set => _rocPeriod.Value = value;
	}

	/// <summary>Volume-weighted typical-price window.</summary>
	public int VwapPeriod
	{
		get => _vwapPeriod.Value;
		set => _vwapPeriod.Value = value;
	}

	/// <summary>Finished signal candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_vwapBars.Clear();
		_pending = null;
		_bar = 0;
		_lastEntry = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var ema20 = new ExponentialMovingAverage { Length = 20 };
		var ema50 = new ExponentialMovingAverage { Length = 50 };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var macd = new MovingAverageConvergenceDivergenceSignal();
		macd.Macd.ShortMa.Length = 12;
		macd.Macd.LongMa.Length = 26;
		macd.SignalMa.Length = 9;
		var atr = new AverageTrueRange { Length = AtrLength };
		var candles = SubscribeCandles(CandleType);
		candles.BindEx([ema20, ema50, rsi, macd, atr], ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, ema20);
			DrawIndicator(area, ema50);
			DrawOwnTrades(area);
		}
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Start();
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;
		_bar++;
		_closes.Enqueue(candle.ClosePrice);
		if (_closes.Count > RocPeriod + 1)
			_closes.Dequeue();
		_vwapBars.Enqueue(((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m, candle.TotalVolume));
		if (_vwapBars.Count > VwapPeriod)
			_vwapBars.Dequeue();
		if (values.Any(v => !v.IsFormed) || _closes.Count <= RocPeriod || _vwapBars.Count < VwapPeriod
			|| values[3] is not MovingAverageConvergenceDivergenceSignalValue macd
			|| macd.Macd is not decimal line || macd.Signal is not decimal signal)
			return;
		var atr = values[4].GetValue<decimal>();
		var volume = _vwapBars.Sum(b => b.volume);
		if (atr <= 0m || volume <= 0m || _closes.Peek() <= 0m)
			return;
		var close = candle.ClosePrice;
		var fast = values[0].GetValue<decimal>();
		var slow = values[1].GetValue<decimal>();
		var rsi = values[2].GetValue<decimal>();
		var composite = close > fast ? close > slow ? 1.5m : 1m : close < fast ? close < slow ? -1.5m : -1m : 0m;
		var vwap = _vwapBars.Sum(b => b.price * b.volume) / volume;
		composite += Math.Clamp((close - vwap) / atr, -2m, 2m);
		if (rsi < 30m && composite > 0m)
			composite += 1.5m;
		else if (rsi < 40m && composite > 0m)
			composite += 0.5m;
		else if (rsi > 70m && composite < 0m)
			composite -= 1.5m;
		else if (rsi > 60m && composite < 0m)
			composite -= 0.5m;
		composite += Math.Clamp((line - signal) / atr, -1m, 1m);
		// ROC / ATR% uses the same lagged price denominator, so it simplifies to a price change / ATR.
		composite += Math.Clamp((close - _closes.Peek()) / atr, -1m, 1m);
		if (Position != 0m || _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed)
			|| _lastEntry is int lastEntry && _bar - lastEntry <= CooldownBars || !IsFormedAndOnlineAndAllowTrading()
			|| Math.Abs(composite) < SignalThreshold)
			return;
		StartProtection(new Unit(), new Unit(atr * AtrStopMultiplier, UnitTypes.Absolute), useMarketOrders: true, isLocalStop: true);
		_lastEntry = _bar;
		_pending = CreateOrder(composite > 0m ? Sides.Buy : Sides.Sell, 0m, Volume);
		_pending.Type = OrderTypes.Market;
		_pending.Comment = "0522Entry";
		RegisterOrder(_pending);
	}
}
