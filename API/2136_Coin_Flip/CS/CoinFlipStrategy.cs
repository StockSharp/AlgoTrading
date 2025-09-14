using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that randomly opens long or short positions.
/// Uses a martingale multiplier after losses and manages
/// take profit, stop loss and optional trailing stop in price steps.
/// </summary>
public class CoinFlipStrategy : Strategy
{
	private readonly StrategyParam<decimal> _martingale;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _trailingStart;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	
	private readonly Random _random = new();
	
	private decimal _entryPrice;
	private decimal _currentVolume;
	private decimal _trailingLevel;
	private bool _isLong;
	private bool _lastTradeLoss;
	
	/// <summary>
	/// Base order size used for the first trade.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}
	
	/// <summary>
	/// Multiplier applied to volume after a losing trade.
	/// </summary>
	public decimal Martingale
	{
		get => _martingale.Value;
		set => _martingale.Value = value;
	}
	
	/// <summary>
	/// Maximum allowed volume after applying martingale.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}
	
	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Distance in price steps to activate trailing stop.
	/// </summary>
	public int TrailingStart
	{
		get => _trailingStart.Value;
		set => _trailingStart.Value = value;
	}
	
	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}
	
	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public CoinFlipStrategy()
	{
		_volume = Param(nameof(Volume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Base order size", "General");
		
		_martingale = Param(nameof(Martingale), 1.8m)
		.SetGreaterThanZero()
		.SetDisplay("Martingale", "Volume multiplier after loss", "General");
		
		_maxVolume = Param(nameof(MaxVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Max Volume", "Upper limit for volume", "General");
		
		_takeProfit = Param(nameof(TakeProfit), 50)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Profit target in steps", "Risk");
		
		_stopLoss = Param(nameof(StopLoss), 25)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Loss limit in steps", "Risk");
		
		_trailingStart = Param(nameof(TrailingStart), 14)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Trailing Start", "Steps to activate trailing", "Risk");
		
		_trailingStop = Param(nameof(TrailingStop), 3)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Trailing Stop", "Trailing distance in steps", "Risk");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for analysis", "General");
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
		_entryPrice = 0m;
		_currentVolume = Volume;
		_trailingLevel = 0m;
		_isLong = false;
		_lastTradeLoss = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_currentVolume = Volume;
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
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
		
		var step = Security.MinPriceStep ?? 1m;
		
		if (Position != 0)
		{
			// Manage open position
			var priceDiff = _isLong ? candle.ClosePrice - _entryPrice : _entryPrice - candle.ClosePrice;
			
			// Update trailing stop if in profit
			if (TrailingStart > 0 && priceDiff >= TrailingStart * step)
			{
				var newLevel = _isLong
				? candle.ClosePrice - TrailingStop * step
				: candle.ClosePrice + TrailingStop * step;
				
				if (_trailingLevel == 0m)
				_trailingLevel = newLevel;
				else if (_isLong && newLevel > _trailingLevel)
				_trailingLevel = newLevel;
				else if (!_isLong && newLevel < _trailingLevel)
				_trailingLevel = newLevel;
			}
			
			// Check exits
			if (priceDiff >= TakeProfit * step)
			{
				ExitPosition(candle.ClosePrice, false);
				return;
			}
			
			if (priceDiff <= -StopLoss * step)
			{
				ExitPosition(candle.ClosePrice, true);
				return;
			}
			
			if (_trailingLevel != 0m)
			{
				if ((_isLong && candle.ClosePrice <= _trailingLevel) ||
				(!_isLong && candle.ClosePrice >= _trailingLevel))
				{
					ExitPosition(candle.ClosePrice, candle.ClosePrice < _entryPrice);
				}
			}
			
			return;
		}
		
		// No open position -> decide direction
		var flip = _random.Next(0, 2);
		_isLong = flip == 0;
		
		_currentVolume = _lastTradeLoss
		? Math.Min(_currentVolume * Martingale, MaxVolume)
		: Volume;
		
		_entryPrice = candle.ClosePrice;
		_trailingLevel = 0m;
		
		if (_isLong)
		BuyMarket(_currentVolume);
		else
		SellMarket(_currentVolume);
	}
	
	private void ExitPosition(decimal closePrice, bool isLoss)
	{
		if (_isLong)
		{
			SellMarket(Math.Abs(Position));
		}
		else
		{
			BuyMarket(Math.Abs(Position));
		}
		
		_lastTradeLoss = isLoss;
		_entryPrice = 0m;
		_trailingLevel = 0m;
	}
}
