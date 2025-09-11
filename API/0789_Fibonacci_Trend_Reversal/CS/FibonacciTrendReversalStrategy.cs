namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Fibonacci trend reversal strategy with ATR based risk management.
/// </summary>
public class FibonacciTrendReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _sensitivity;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<bool> _usePartialTp;
	private readonly StrategyParam<TradeDirection> _direction;
	
	private Highest _highest;
	private Lowest _lowest;
	private ATR _atr;
	
	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _tp1;
	private decimal _tp2;
	private bool _firstTargetHit;
	
	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public TradeDirection Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Sensitivity.
	/// </summary>
	public int Sensitivity
	{
		get => _sensitivity.Value;
		set => _sensitivity.Value = value;
	}
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	
	/// <summary>
	/// Risk reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}
	
	/// <summary>
	/// Use partial take profit.
	/// </summary>
	public bool UsePartialTp
	{
		get => _usePartialTp.Value;
		set => _usePartialTp.Value = value;
	}
	
	/// <summary>
	/// Direction options.
	/// </summary>
	public enum TradeDirection
	{
		LongOnly,
		ShortOnly,
		Both
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="FibonacciTrendReversalStrategy"/>.
	/// </summary>
	public FibonacciTrendReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_sensitivity = Param(nameof(Sensitivity), 18)
		.SetDisplay("Sensitivity", "Base sensitivity", "Fibonacci");
		
		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR length", "Risk");
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 3.5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "ATR multiplier for stop loss", "Risk");
		
		_riskReward = Param(nameof(RiskReward), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Reward", "Risk reward ratio", "Risk");
		
		_usePartialTp = Param(nameof(UsePartialTp), true)
		.SetDisplay("Use Partial TP", "Close half position at first target", "Risk");
		
		_direction = Param(nameof(Direction), TradeDirection.Both)
		.SetDisplay("Trade Direction", "Allowed trade direction", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_entryPrice = _stopLoss = _tp1 = _tp2 = default;
		_firstTargetHit = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_highest = new Highest { Length = Sensitivity * 10 };
		_lowest = new Lowest { Length = Sensitivity * 10 };
		_atr = new ATR { Length = AtrPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var highLine = _highest.Process(candle.HighPrice).ToNullableDecimal();
		var lowLine = _lowest.Process(candle.LowPrice).ToNullableDecimal();
		
		if (highLine is null || lowLine is null)
		return;
		
		var range = highLine.Value - lowLine.Value;
		var midLine = highLine.Value - range * 0.5m;
		
		var canLong = candle.ClosePrice >= midLine && candle.OpenPrice < midLine;
		var canShort = candle.ClosePrice <= midLine && candle.OpenPrice > midLine;
		
		var allowLong = Direction != TradeDirection.ShortOnly;
		var allowShort = Direction != TradeDirection.LongOnly;
		
		if (Position == 0)
		{
			_firstTargetHit = false;
			
			if (canLong && allowLong)
			{
				_entryPrice = candle.ClosePrice;
				_stopLoss = _entryPrice - atr * AtrMultiplier;
				var risk = _entryPrice - _stopLoss;
				_tp1 = _entryPrice + risk * RiskReward / 2m;
				_tp2 = _entryPrice + risk * RiskReward;
				BuyMarket(Volume);
			}
			else if (canShort && allowShort)
			{
				_entryPrice = candle.ClosePrice;
				_stopLoss = _entryPrice + atr * AtrMultiplier;
				var risk = _stopLoss - _entryPrice;
				_tp1 = _entryPrice - risk * RiskReward / 2m;
				_tp2 = _entryPrice - risk * RiskReward;
				SellMarket(Volume);
			}
			
			return;
		}
		
		if (Position > 0)
		{
			if (UsePartialTp && !_firstTargetHit && candle.HighPrice >= _tp1)
			{
				SellMarket(Position / 2m);
				_firstTargetHit = true;
				_stopLoss = _entryPrice;
			}
			
			if (candle.LowPrice <= _stopLoss)
			{
				SellMarket(Position);
			}
			else if ((!UsePartialTp || _firstTargetHit) && candle.HighPrice >= _tp2)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (UsePartialTp && !_firstTargetHit && candle.LowPrice <= _tp1)
			{
				BuyMarket(-Position / 2m);
				_firstTargetHit = true;
				_stopLoss = _entryPrice;
			}
			
			if (candle.HighPrice >= _stopLoss)
			{
				BuyMarket(-Position);
			}
			else if ((!UsePartialTp || _firstTargetHit) && candle.LowPrice <= _tp2)
			{
				BuyMarket(-Position);
			}
		}
	}
}
