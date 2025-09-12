namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Moving average crossover strategy with optional trend, RSI filters and ATR-based exit.
/// </summary>
public class OptimizedAutoDetectStrategy : Strategy
{
	#region Parameters
	
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLongThreshold;
	private readonly StrategyParam<decimal> _rsiShortThreshold;
	private readonly StrategyParam<bool> _useAtrStop;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;
	
	/// <summary>
	/// Use 200 SMA trend filter.
	/// </summary>
	public bool UseTrendFilter { get => _useTrendFilter.Value; set => _useTrendFilter.Value = value; }
	
	/// <summary>
	/// Use RSI filter.
	/// </summary>
	public bool UseRsiFilter { get => _useRsiFilter.Value; set => _useRsiFilter.Value = value; }
	
	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	
	/// <summary>
	/// RSI long threshold.
	/// </summary>
	public decimal RsiLongThreshold { get => _rsiLongThreshold.Value; set => _rsiLongThreshold.Value = value; }
	
	/// <summary>
	/// RSI short threshold.
	/// </summary>
	public decimal RsiShortThreshold { get => _rsiShortThreshold.Value; set => _rsiShortThreshold.Value = value; }
	
	/// <summary>
	/// Use ATR based stop.
	/// </summary>
	public bool UseAtrStop { get => _useAtrStop.Value; set => _useAtrStop.Value = value; }
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	
	/// <summary>
	/// ATR multiplier for stop distance.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	
	/// <summary>
	/// Risk reward ratio.
	/// </summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	
	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	#endregion
	
	private decimal _prevShortSma;
	private decimal _prevLongSma;
	private bool _hasPrev;
	
	private int _shortMaPeriod;
	private int _longMaPeriod;
	private decimal _customAtrMult;
	private decimal _customRr;
	
	/// <summary>
	/// Initializes a new instance of <see cref="OptimizedAutoDetectStrategy"/>.
	/// </summary>
	public OptimizedAutoDetectStrategy()
	{
		_useTrendFilter = Param(nameof(UseTrendFilter), true)
		.SetDisplay("Use Trend Filter", "Enable 200 SMA trend filter", "General");
		_useRsiFilter = Param(nameof(UseRsiFilter), false)
		.SetDisplay("Use RSI Filter", "Enable RSI filter", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "Period for RSI", "Indicators")
		.SetGreaterThanZero();
		_rsiLongThreshold = Param(nameof(RsiLongThreshold), 50m)
		.SetDisplay("RSI Long Threshold", "Minimum RSI for long", "Indicators")
		.SetRange(1m, 99m);
		_rsiShortThreshold = Param(nameof(RsiShortThreshold), 50m)
		.SetDisplay("RSI Short Threshold", "Maximum RSI for short", "Indicators")
		.SetRange(1m, 99m);
		_useAtrStop = Param(nameof(UseAtrStop), true)
		.SetDisplay("Use ATR Stop", "Use ATR for stop distance", "Risk");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetDisplay("ATR Period", "ATR calculation period", "Indicators")
		.SetGreaterThanZero();
		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
		.SetDisplay("ATR Multiplier", "ATR multiplier for stop", "Risk")
		.SetGreaterThanZero();
		_riskReward = Param(nameof(RiskReward), 3m)
		.SetDisplay("Risk Reward Ratio", "Take profit multiplier", "Risk")
		.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		
		_prevShortSma = 0m;
		_prevLongSma = 0m;
		_hasPrev = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		_shortMaPeriod = 9;
		_longMaPeriod = 21;
		_customAtrMult = AtrMultiplier;
		_customRr = RiskReward;
		
		var symbol = Security?.Id?.ToString();
		switch (symbol)
		{
			case "OANDA:EURUSD":
			_shortMaPeriod = 10;
			_longMaPeriod = 30;
			_customAtrMult = 1m;
			_customRr = 3m;
			break;
			case "OANDA:GBPUSD":
			_shortMaPeriod = 12;
			_longMaPeriod = 35;
			_customAtrMult = 1.2m;
			_customRr = 2.5m;
			break;
			case "OANDA:USDJPY":
			_shortMaPeriod = 9;
			_longMaPeriod = 21;
			_customAtrMult = 1m;
			_customRr = 3m;
			break;
			case "OANDA:AUDUSD":
			_shortMaPeriod = 10;
			_longMaPeriod = 34;
			_customAtrMult = 1.2m;
			_customRr = 2.5m;
			break;
			case "OANDA:USDCAD":
			_shortMaPeriod = 8;
			_longMaPeriod = 21;
			_customAtrMult = 1m;
			_customRr = 3m;
			break;
			case "OANDA:XAUUSD":
			_shortMaPeriod = 20;
			_longMaPeriod = 50;
			_customAtrMult = 2m;
			_customRr = 2m;
			break;
			case "OANDA:NZDUSD":
			_shortMaPeriod = 10;
			_longMaPeriod = 30;
			_customAtrMult = 1.2m;
			_customRr = 2.5m;
			break;
		}
		
		var shortSma = new SimpleMovingAverage { Length = _shortMaPeriod };
		var longSma = new SimpleMovingAverage { Length = _longMaPeriod };
		var trendSma = new SimpleMovingAverage { Length = 200 };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(shortSma, longSma, trendSma, rsi, atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortSma);
			DrawIndicator(area, longSma);
			DrawIndicator(area, trendSma);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal shortSma, decimal longSma, decimal trendSma, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!_hasPrev)
		{
			_prevShortSma = shortSma;
			_prevLongSma = longSma;
			_hasPrev = true;
			return;
		}
		
		var maLong = _prevShortSma <= _prevLongSma && shortSma > longSma;
		var maShort = _prevShortSma >= _prevLongSma && shortSma < longSma;
		
		if (UseTrendFilter)
		{
			var bullishTrend = candle.ClosePrice > trendSma;
			var bearishTrend = candle.ClosePrice < trendSma;
			maLong &= bullishTrend;
			maShort &= bearishTrend;
		}
		
		if (UseRsiFilter)
		{
			var rsiLongAllowed = rsi > RsiLongThreshold;
			var rsiShortAllowed = rsi < RsiShortThreshold;
			maLong &= rsiLongAllowed;
			maShort &= rsiShortAllowed;
		}
		
		if (maLong && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (maShort && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		
		if (Position > 0)
		{
			var stopDist = UseAtrStop ? _customAtrMult * atr : PositionPrice * 0.01m;
			var stopPrice = PositionPrice - stopDist;
			var tpPrice = PositionPrice + stopDist * _customRr;
			
			if (candle.LowPrice <= stopPrice)
			SellMarket(Position);
			else if (candle.HighPrice >= tpPrice)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			var stopDist = UseAtrStop ? _customAtrMult * atr : PositionPrice * 0.01m;
			var stopPrice = PositionPrice + stopDist;
			var tpPrice = PositionPrice - stopDist * _customRr;
			
			if (candle.HighPrice >= stopPrice)
			BuyMarket(Math.Abs(Position));
			else if (candle.LowPrice <= tpPrice)
			BuyMarket(Math.Abs(Position));
		}
		
		_prevShortSma = shortSma;
		_prevLongSma = longSma;
	}
}
