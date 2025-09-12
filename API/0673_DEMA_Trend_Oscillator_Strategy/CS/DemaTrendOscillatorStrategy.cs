using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Double Exponential Moving Average (DEMA) trend oscillator with normalization.
/// </summary>
public class DemaTrendOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _demaPeriod;
	private readonly StrategyParam<int> _baseLength;
	private readonly StrategyParam<decimal> _longThreshold;
	private readonly StrategyParam<decimal> _shortThreshold;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	
	private ExponentialMovingAverage _ema1;
	private ExponentialMovingAverage _ema2;
	private SimpleMovingAverage _baseSma;
	private StandardDeviation _sd;
	private AverageTrueRange _atr;
	
	private bool _prevLongCond;
	private bool _prevShortCond;
	private string _lastDirection = "none";
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _trailingStop;
	
	/// <summary>
	/// DEMA period.
	/// </summary>
	public int DemaPeriod
	{
		get => _demaPeriod.Value;
		set => _demaPeriod.Value = value;
	}
	
	/// <summary>
	/// Base length for SMA and standard deviation.
	/// </summary>
	public int BaseLength
	{
		get => _baseLength.Value;
		set => _baseLength.Value = value;
	}
	
	/// <summary>
	/// Normalized value threshold for long entries.
	/// </summary>
	public decimal LongThreshold
	{
		get => _longThreshold.Value;
		set => _longThreshold.Value = value;
	}
	
	/// <summary>
	/// Normalized value threshold for short entries.
	/// </summary>
	public decimal ShortThreshold
	{
		get => _shortThreshold.Value;
		set => _shortThreshold.Value = value;
	}
	
	/// <summary>
	/// Risk-reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for trailing stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Initializes a new instance of <see cref="DemaTrendOscillatorStrategy"/>.
	/// </summary>
	public DemaTrendOscillatorStrategy()
	{
		_demaPeriod = Param(nameof(DemaPeriod), 40)
		.SetDisplay("DEMA Period", "Period for double EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 80, 5);
		
		_baseLength = Param(nameof(BaseLength), 20)
		.SetDisplay("Base Length", "Length for base SMA and deviation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);
		
		_longThreshold = Param(nameof(LongThreshold), 55m)
		.SetDisplay("Long Threshold", "Normalized value for long entries", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(50m, 60m, 5m);
		
		_shortThreshold = Param(nameof(ShortThreshold), 45m)
		.SetDisplay("Short Threshold", "Normalized value for short entries", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(40m, 50m, 5m);
		
		_riskReward = Param(nameof(RiskReward), 1.5m)
		.SetDisplay("Risk Reward", "Risk-reward ratio", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
		.SetDisplay("ATR Multiplier", "ATR multiplier for trailing stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		
		_ema1 = null;
		_ema2 = null;
		_baseSma = null;
		_sd = null;
		_atr = null;
		
		_prevLongCond = false;
		_prevShortCond = false;
		_lastDirection = "none";
		_stopPrice = null;
		_takePrice = null;
		_trailingStop = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_ema1 = new ExponentialMovingAverage { Length = DemaPeriod };
		_ema2 = new ExponentialMovingAverage { Length = DemaPeriod };
		_baseSma = new SimpleMovingAverage { Length = BaseLength };
		_sd = new StandardDeviation { Length = BaseLength };
		_atr = new AverageTrueRange { Length = 14 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var ema1Value = _ema1!.Process(candle).ToDecimal();
		var ema2Value = _ema2!.Process(ema1Value, candle.ServerTime, true).ToDecimal();
		var dema = 2m * ema1Value - ema2Value;
		
		var baseValue = _baseSma!.Process(dema, candle.ServerTime, true).ToDecimal();
		var sdValue = _sd!.Process(dema, candle.ServerTime, true).ToDecimal() * 2m;
		var upperSd = baseValue + sdValue;
		var lowerSd = baseValue - sdValue;
		var normBase = upperSd != lowerSd ? 100m * (dema - lowerSd) / (upperSd - lowerSd) : 0m;
		
		var atrValue = _atr!.Process(candle).ToDecimal();
		var trailOffset = atrValue * AtrMultiplier;
		
		if (Position > 0)
		{
			_trailingStop = _trailingStop is decimal prev ? Math.Max(prev, candle.ClosePrice - trailOffset) : candle.ClosePrice - trailOffset;
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
			}
			else if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetPositionState();
			}
			else if (_trailingStop is decimal trail && candle.LowPrice <= trail)
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			_trailingStop = _trailingStop is decimal prev ? Math.Min(prev, candle.ClosePrice + trailOffset) : candle.ClosePrice + trailOffset;
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetPositionState();
			}
			else if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ResetPositionState();
			}
			else if (_trailingStop is decimal trail && candle.HighPrice >= trail)
			{
				BuyMarket(-Position);
				ResetPositionState();
			}
		}
		
		var longCond = normBase > LongThreshold && candle.LowPrice > upperSd;
		var shortCond = normBase < ShortThreshold && candle.HighPrice < lowerSd;
		
		var longTrigger = _prevLongCond;
		var shortTrigger = _prevShortCond;
		_prevLongCond = longCond;
		_prevShortCond = shortCond;
		
		if (longTrigger && _lastDirection != "long" && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_stopPrice = upperSd;
			var risk = candle.ClosePrice - upperSd;
			_takePrice = candle.ClosePrice + risk * RiskReward;
			_trailingStop = candle.ClosePrice - trailOffset;
			_lastDirection = "long";
		}
		else if (shortTrigger && _lastDirection != "short" && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_stopPrice = lowerSd;
			var risk = lowerSd - candle.ClosePrice;
			_takePrice = candle.ClosePrice - risk * RiskReward;
			_trailingStop = candle.ClosePrice + trailOffset;
			_lastDirection = "short";
		}
	}
	
	private void ResetPositionState()
	{
		_stopPrice = null;
		_takePrice = null;
		_trailingStop = null;
	}
}
