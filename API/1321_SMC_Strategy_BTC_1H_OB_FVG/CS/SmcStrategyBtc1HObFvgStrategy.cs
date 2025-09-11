using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smart Money Concepts strategy using order blocks and fair value gaps on BTC 1H.
/// Enters long when price returns to a recent order block or FVG after a bullish break of structure.
/// Stop loss uses ATR multiplier and take profit is based on risk/reward ratio.
/// </summary>
public class SmcStrategyBtc1HObFvgStrategy : Strategy
{
	private readonly StrategyParam<bool> _useOrderBlock;
	private readonly StrategyParam<bool> _useFvg;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _zoneTimeout;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _lastSwingHigh;
	private decimal _prevHigh1;
	private decimal _prevHigh2;
	private decimal _prevHigh3;
	private decimal _prevHigh4;
	
	private decimal? _obLow;
	private decimal? _obHigh;
	private int _obTimer;
	
	private decimal? _fvgLow;
	private decimal? _fvgHigh;
	private int _fvgTimer;
	
	private ICandleMessage _prevCandle1;
	private ICandleMessage _prevCandle2;
	
	/// <summary>
	/// Use order block entries.
	/// </summary>
	public bool UseOrderBlock { get => _useOrderBlock.Value; set => _useOrderBlock.Value = value; }
	
	/// <summary>
	/// Use fair value gap entries.
	/// </summary>
	public bool UseFvg { get => _useFvg.Value; set => _useFvg.Value = value; }
	
	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrFactor { get => _atrFactor.Value; set => _atrFactor.Value = value; }
	
	/// <summary>
	/// Risk/reward ratio for take profit.
	/// </summary>
	public decimal RiskRewardRatio { get => _riskReward.Value; set => _riskReward.Value = value; }
	
	/// <summary>
	/// Maximum bars for zone validity.
	/// </summary>
	public int ZoneTimeout { get => _zoneTimeout.Value; set => _zoneTimeout.Value = value; }
	
	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public SmcStrategyBtc1HObFvgStrategy()
	{
		_useOrderBlock = Param(nameof(UseOrderBlock), true)
		.SetDisplay("Use Order Block", "Enable order block entry", "General");
		
		_useFvg = Param(nameof(UseFvg), true)
		.SetDisplay("Use FVG", "Enable fair value gap entry", "General");
		
		_atrFactor = Param(nameof(AtrFactor), 6m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Factor", "ATR multiplier for stop loss", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 10m, 1m);
		
		_riskReward = Param(nameof(RiskRewardRatio), 2.5m)
		.SetGreaterThanZero()
		.SetDisplay("Risk/Reward", "Take profit multiplier", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);
		
		_zoneTimeout = Param(nameof(ZoneTimeout), 10)
		.SetGreaterThanZero()
		.SetDisplay("Zone Timeout", "Bars until zone expires", "General")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle time frame", "General");
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
		
		_lastSwingHigh = null;
		_prevHigh1 = _prevHigh2 = _prevHigh3 = _prevHigh4 = 0m;
		_obLow = _obHigh = null;
		_obTimer = 0;
		_fvgLow = _fvgHigh = null;
		_fvgTimer = 0;
		_prevCandle1 = null;
		_prevCandle2 = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();
		
		var atr = new ATR { Length = 14 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var lastSwingPrev = _lastSwingHigh;
		
		var swingHigh = candle.HighPrice > _prevHigh1 && candle.HighPrice > _prevHigh2 &&
		candle.HighPrice > _prevHigh3 && candle.HighPrice > _prevHigh4;
		
		if (swingHigh)
		_lastSwingHigh = candle.HighPrice;
		
		var bullishBos = lastSwingPrev.HasValue && candle.ClosePrice > lastSwingPrev.Value;
		
		var isDownCandle = _prevCandle1 != null && _prevCandle1.ClosePrice < _prevCandle1.OpenPrice;
		var newOb = bullishBos && isDownCandle;
		
		if (newOb && _prevCandle1 != null)
		{
			_obLow = _prevCandle1.LowPrice;
			_obHigh = _prevCandle1.HighPrice;
			_obTimer = ZoneTimeout;
		}
		
		if (_obTimer > 0)
		_obTimer--;
		
		var obActive = _obTimer > 0;
		
		var fvgExists = _prevCandle2 != null && _prevCandle2.HighPrice < candle.LowPrice;
		
		if (fvgExists)
		{
			_fvgHigh = _prevCandle2.HighPrice;
			_fvgLow = candle.LowPrice;
			_fvgTimer = ZoneTimeout;
		}
		
		if (_fvgTimer > 0)
		_fvgTimer--;
		
		var fvgActive = _fvgTimer > 0;
		
		var inOb = obActive && _obLow.HasValue && _obHigh.HasValue &&
		candle.ClosePrice <= _obHigh.Value && candle.ClosePrice >= _obLow.Value;
		
		var inFvg = fvgActive && _fvgLow.HasValue && _fvgHigh.HasValue &&
		candle.ClosePrice <= _fvgLow.Value && candle.ClosePrice >= _fvgHigh.Value;
		
		if (Position == 0 && ((_useOrderBlock.Value && inOb) || (_useFvg.Value && inFvg)) && _obLow.HasValue)
		{
			var entryPrice = _obLow.Value;
			var stopLoss = entryPrice - atrValue * _atrFactor.Value;
			var takeProfit = entryPrice + (entryPrice - stopLoss) * _riskReward.Value;
			
			BuyMarket();
			SellStop(stopLoss);
			SellLimit(takeProfit);
			
			_obTimer = 0;
			_fvgTimer = 0;
		}
		
		_prevHigh4 = _prevHigh3;
		_prevHigh3 = _prevHigh2;
		_prevHigh2 = _prevHigh1;
		_prevHigh1 = candle.HighPrice;
		
		_prevCandle2 = _prevCandle1;
		_prevCandle1 = candle;
	}
}
