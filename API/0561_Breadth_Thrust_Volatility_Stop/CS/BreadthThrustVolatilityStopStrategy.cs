using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breadth Thrust strategy with volatility-based stop-loss.
/// Buys when the smoothed breadth ratio crosses above a threshold.
/// Uses ATR for trailing stop and exits after a fixed holding period.
/// </summary>
public class BreadthThrustVolatilityStopStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _thresholdLow;
	private readonly StrategyParam<bool> _useVolume;
	private readonly StrategyParam<int> _holdPeriods;
	private readonly StrategyParam<decimal> _volatilityMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _advancingStocks;
	private readonly StrategyParam<Security> _decliningStocks;
	private readonly StrategyParam<Security> _advancingVolume;
	private readonly StrategyParam<Security> _decliningVolume;
	
	private decimal _advancingStocksValue;
	private decimal _decliningStocksValue;
	private decimal _advancingVolumeValue;
	private decimal _decliningVolumeValue;
	
	private decimal _smoothedCombined;
	private decimal _prevSmoothedCombined;
	private decimal _stopLossLevel;
	private int _barsHeld;
	private DateTime _lastBreadthDate = DateTime.MinValue;
	
	private SMA _sma;
	private ATR _atr;
	
	/// <summary>
	/// Smoothing length for the breadth ratio.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }
	
	/// <summary>
	/// Low threshold for breadth crossover.
	/// </summary>
	public decimal ThresholdLow { get => _thresholdLow.Value; set => _thresholdLow.Value = value; }
	
	/// <summary>
	/// Include volume ratio into calculations.
	/// </summary>
	public bool UseVolume { get => _useVolume.Value; set => _useVolume.Value = value; }
	
	/// <summary>
	/// Number of candles to hold the position.
	/// </summary>
	public int HoldPeriods { get => _holdPeriods.Value; set => _holdPeriods.Value = value; }
	
	/// <summary>
	/// Multiplier for ATR-based stop-loss.
	/// </summary>
	public decimal VolatilityMultiplier { get => _volatilityMultiplier.Value; set => _volatilityMultiplier.Value = value; }
	
	/// <summary>
	/// Candle type for all securities.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Security providing advancing stocks count.
	/// </summary>
	public Security AdvancingStocks { get => _advancingStocks.Value; set => _advancingStocks.Value = value; }
	
	/// <summary>
	/// Security providing declining stocks count.
	/// </summary>
	public Security DecliningStocks { get => _decliningStocks.Value; set => _decliningStocks.Value = value; }
	
	/// <summary>
	/// Security providing advancing volume.
	/// </summary>
	public Security AdvancingVolume { get => _advancingVolume.Value; set => _advancingVolume.Value = value; }
	
	/// <summary>
	/// Security providing declining volume.
	/// </summary>
	public Security DecliningVolume { get => _decliningVolume.Value; set => _decliningVolume.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of <see cref="BreadthThrustVolatilityStopStrategy"/>.
	/// </summary>
	public BreadthThrustVolatilityStopStrategy()
	{
		_length = Param(nameof(Length), 10)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "SMA period for breadth", "General");
		
		_thresholdLow = Param(nameof(ThresholdLow), 0.4m)
		.SetDisplay("Low Threshold", "Breadth crossover level", "General");
		
		_useVolume = Param(nameof(UseVolume), true)
		.SetDisplay("Use Volume", "Include volume ratio", "General");
		
		_holdPeriods = Param(nameof(HoldPeriods), 9)
		.SetGreaterThanZero()
		.SetDisplay("Hold Periods", "Bars to hold position", "Trading");
		
		_volatilityMultiplier = Param(nameof(VolatilityMultiplier), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Volatility Multiplier", "ATR multiplier for stop", "Risk");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for analysis", "General");
		
		_advancingStocks = Param<Security>(nameof(AdvancingStocks), null)
		.SetDisplay("Advancing Stocks", "Security with advancing issues", "Breadth");
		
		_decliningStocks = Param<Security>(nameof(DecliningStocks), null)
		.SetDisplay("Declining Stocks", "Security with declining issues", "Breadth");
		
		_advancingVolume = Param<Security>(nameof(AdvancingVolume), null)
		.SetDisplay("Advancing Volume", "Security with advancing volume", "Breadth");
		
		_decliningVolume = Param<Security>(nameof(DecliningVolume), null)
		.SetDisplay("Declining Volume", "Security with declining volume", "Breadth");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null || AdvancingStocks == null || DecliningStocks == null || AdvancingVolume == null || DecliningVolume == null)
		throw new InvalidOperationException("Set all securities.");
		
		return new[]
		{
			(Security, CandleType),
			(AdvancingStocks, CandleType),
			(DecliningStocks, CandleType),
			(AdvancingVolume, CandleType),
			(DecliningVolume, CandleType),
		};
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_advancingStocksValue = 0m;
		_decliningStocksValue = 0m;
		_advancingVolumeValue = 0m;
		_decliningVolumeValue = 0m;
		_smoothedCombined = 0m;
		_prevSmoothedCombined = 0m;
		_stopLossLevel = 0m;
		_barsHeld = 0;
		_lastBreadthDate = DateTime.MinValue;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_sma = new SMA { Length = Length };
		_atr = new ATR { Length = 14 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, ProcessMain)
		.Start();
		
		SubscribeCandles(CandleType, true, AdvancingStocks)
		.Bind(c => ProcessBreadth(c, BreadthType.AdvancingStocks))
		.Start();
		
		SubscribeCandles(CandleType, true, DecliningStocks)
		.Bind(c => ProcessBreadth(c, BreadthType.DecliningStocks))
		.Start();
		
		SubscribeCandles(CandleType, true, AdvancingVolume)
		.Bind(c => ProcessBreadth(c, BreadthType.AdvancingVolume))
		.Start();
		
		SubscribeCandles(CandleType, true, DecliningVolume)
		.Bind(c => ProcessBreadth(c, BreadthType.DecliningVolume))
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}
	
	private enum BreadthType
	{
		AdvancingStocks,
		DecliningStocks,
		AdvancingVolume,
		DecliningVolume
	}
	
	private void ProcessBreadth(ICandleMessage candle, BreadthType type)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		switch (type)
		{
		case BreadthType.AdvancingStocks:
		_advancingStocksValue = candle.ClosePrice;
		break;
	case BreadthType.DecliningStocks:
	_decliningStocksValue = candle.ClosePrice;
	break;
case BreadthType.AdvancingVolume:
_advancingVolumeValue = candle.ClosePrice;
break;
case BreadthType.DecliningVolume:
_decliningVolumeValue = candle.ClosePrice;
break;
}

UpdateBreadth(candle.OpenTime);
}

private void UpdateBreadth(DateTimeOffset time)
{
	var day = time.Date;
	if (day == _lastBreadthDate)
	return;
	
	if (_advancingStocksValue <= 0m || _decliningStocksValue <= 0m)
	return;
	
	var totalStocks = _advancingStocksValue + _decliningStocksValue;
	var breadthRatio = _advancingStocksValue / totalStocks;
	
	var combined = breadthRatio;
	
	if (UseVolume && _advancingVolumeValue > 0m && _decliningVolumeValue > 0m)
	{
		var totalVolume = _advancingVolumeValue + _decliningVolumeValue;
		var volumeRatio = _advancingVolumeValue / totalVolume;
		combined = (breadthRatio + volumeRatio) / 2m;
	}
	
	_prevSmoothedCombined = _smoothedCombined;
	_smoothedCombined = _sma.Process(combined, time, true).ToDecimal();
	_lastBreadthDate = day;
}

private void ProcessMain(ICandleMessage candle, decimal atr)
{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (!_sma.IsFormed)
	return;
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	if (Position <= 0)
	{
		if (_prevSmoothedCombined < ThresholdLow && _smoothedCombined >= ThresholdLow)
		{
			_stopLossLevel = candle.ClosePrice - VolatilityMultiplier * atr;
			BuyMarket();
			_barsHeld = 0;
		}
	}
	else
	{
		var newStop = candle.ClosePrice - VolatilityMultiplier * atr;
		if (newStop > _stopLossLevel)
		_stopLossLevel = newStop;
		
		if (candle.LowPrice <= _stopLossLevel)
		{
			SellMarket();
			_stopLossLevel = 0m;
			_barsHeld = 0;
		}
		else
		{
			_barsHeld++;
			if (_barsHeld >= HoldPeriods)
			{
				SellMarket();
				_stopLossLevel = 0m;
				_barsHeld = 0;
			}
		}
	}
}
}
