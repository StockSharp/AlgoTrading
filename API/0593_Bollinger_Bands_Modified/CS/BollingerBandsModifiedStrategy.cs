namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Bollinger Bands Modified strategy.
/// Enters on price crossing Bollinger Bands with optional EMA trend filter.
/// </summary>
public class BollingerBandsModifiedStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _highestLength;
	private readonly StrategyParam<int> _lowestLength;
	private readonly StrategyParam<decimal> _targetFactor;
	private readonly StrategyParam<bool> _emaTrend;
	private readonly StrategyParam<bool> _crossoverCheck;
	private readonly StrategyParam<bool> _crossunderCheck;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _hasPrev;
	private bool _isLongEntry;
	private bool _isShortEntry;
	
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int HighestLength { get => _highestLength.Value; set => _highestLength.Value = value; }
	public int LowestLength { get => _lowestLength.Value; set => _lowestLength.Value = value; }
	public decimal TargetFactor { get => _targetFactor.Value; set => _targetFactor.Value = value; }
	public bool EmaTrend { get => _emaTrend.Value; set => _emaTrend.Value = value; }
	public bool CrossoverCheck { get => _crossoverCheck.Value; set => _crossoverCheck.Value = value; }
	public bool CrossunderCheck { get => _crossunderCheck.Value; set => _crossunderCheck.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public BollingerBandsModifiedStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
		.SetDisplay("Bollinger Length", "Bollinger Bands length", "Indicators");
		
		_bollingerDeviation = Param(nameof(BollingerDeviation), 0.38m)
		.SetDisplay("Bollinger Deviation", "Bollinger Bands standard deviation", "Indicators");
		
		_emaLength = Param(nameof(EmaLength), 80)
		.SetDisplay("EMA Length", "EMA calculation length", "Indicators");
		
		_highestLength = Param(nameof(HighestLength), 7)
		.SetDisplay("Highest High Length", "Period for highest high", "Risk");
		
		_lowestLength = Param(nameof(LowestLength), 7)
		.SetDisplay("Lowest Low Length", "Period for lowest low", "Risk");
		
		_targetFactor = Param(nameof(TargetFactor), 1.6m)
		.SetDisplay("Target Factor", "Profit target multiplier", "Risk");
		
		_emaTrend = Param(nameof(EmaTrend), true)
		.SetDisplay("Use EMA Trend", "Require price relative to EMA", "Filters");
		
		_crossoverCheck = Param(nameof(CrossoverCheck), false)
		.SetDisplay("Extra Crossover Check", "Require candle to cross above upper band", "Filters");
		
		_crossunderCheck = Param(nameof(CrossunderCheck), false)
		.SetDisplay("Extra Crossunder Check", "Require candle to cross below lower band", "Filters");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for strategy", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_entryPrice = default;
		_stopPrice = default;
		_targetPrice = default;
		_prevClose = default;
		_prevUpper = default;
		_prevLower = default;
		_hasPrev = default;
		_isLongEntry = default;
		_isShortEntry = default;
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var bb = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerDeviation
		};
		
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var highest = new Highest { Length = HighestLength };
		var lowest = new Lowest { Length = LowestLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(bb, ema, highest, lowest, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal ema, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!_hasPrev)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upper;
			_prevLower = lower;
			_hasPrev = true;
			return;
		}
		
		var isCrossover = _prevClose <= _prevUpper && candle.ClosePrice > upper;
		var isCrossunder = _prevClose >= _prevLower && candle.ClosePrice < lower;
		
		var isBarLong = candle.ClosePrice > candle.OpenPrice;
		var isBarShort = candle.ClosePrice < candle.OpenPrice;
		
		var isLongCross = CrossoverCheck
		? (isBarLong && candle.OpenPrice < upper && candle.ClosePrice > upper)
		: isCrossover;
		
		var isShortCross = CrossunderCheck
		? (isBarShort && candle.ClosePrice < lower && candle.OpenPrice > lower)
		: isCrossunder;
		
		var isCandleAboveEma = candle.ClosePrice > ema;
		var isCandleBelowEma = candle.ClosePrice < ema;
		
		var isLongCondition = EmaTrend ? isLongCross && isCandleAboveEma : isLongCross;
		var isShortCondition = EmaTrend ? isShortCross && isCandleBelowEma : isShortCross;
		
		if (isLongCondition && Position <= 0 && !_isLongEntry)
		{
			_isLongEntry = true;
			_isShortEntry = false;
			_entryPrice = candle.ClosePrice;
			_stopPrice = lowest;
			_targetPrice = _entryPrice + Math.Abs(_entryPrice - _stopPrice) * TargetFactor;
			RegisterBuy();
		}
		else if (isShortCondition && Position >= 0 && !_isShortEntry)
		{
			_isShortEntry = true;
			_isLongEntry = false;
			_entryPrice = candle.ClosePrice;
			_stopPrice = highest;
			_targetPrice = _entryPrice - Math.Abs(_entryPrice - _stopPrice) * TargetFactor;
			RegisterSell();
		}
		
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
			{
				ClosePosition();
				_isLongEntry = false;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
			{
				ClosePosition();
				_isShortEntry = false;
			}
		}
		
		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
