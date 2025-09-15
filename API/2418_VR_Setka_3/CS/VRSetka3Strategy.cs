namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Grid strategy inspired by VR SETKA 3.
/// </summary>
public class VRSetka3Strategy : Strategy
{
	private readonly StrategyParam<decimal> _startOffset;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _gridDistance;
	private readonly StrategyParam<decimal> _stepDistance;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	
	private Order _buyOrder;
	private Order _sellOrder;
	private decimal _buyAvgPrice;
	private decimal _buyVolume;
	private decimal _sellAvgPrice;
	private decimal _sellVolume;
	private int _buyCount;
	private int _sellCount;
	
	/// <summary>
	/// Initial offset for first limit orders.
	/// </summary>
	public decimal StartOffset
	{
		get => _startOffset.Value;
		set => _startOffset.Value = value;
	}
	
	/// <summary>
	/// Take profit distance from average price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Base distance between grid levels.
	/// </summary>
	public decimal GridDistance
	{
		get => _gridDistance.Value;
		set => _gridDistance.Value = value;
	}
	
	/// <summary>
	/// Additional distance added for each new level.
	/// </summary>
	public decimal StepDistance
	{
		get => _stepDistance.Value;
		set => _stepDistance.Value = value;
	}
	
	/// <summary>
	/// Use volume multiplication after each filled order.
	/// </summary>
	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}
	
	/// <summary>
	/// Volume multiplier for martingale.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}
	
	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public VRSetka3Strategy()
	{
		_startOffset = Param(nameof(StartOffset), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Start Offset", "Offset for first limit orders", "Parameters");
		
		_takeProfit = Param(nameof(TakeProfit), 300m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Distance for profit taking", "Parameters");
		
		_gridDistance = Param(nameof(GridDistance), 300m)
		.SetGreaterThanZero()
		.SetDisplay("Grid Distance", "Base distance between grid levels", "Parameters");
		
		_stepDistance = Param(nameof(StepDistance), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Step Distance", "Additional distance for next levels", "Parameters");
		
		_useMartingale = Param(nameof(UseMartingale), true)
		.SetDisplay("Use Martingale", "Multiply volume after each level", "Parameters");
		
		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Martingale Multiplier", "Volume multiplier for martingale", "Parameters");
		
		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Base order volume", "Trading");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		ResetState();
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
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
		
		var price = candle.ClosePrice;
		
		if (_buyVolume > 0)
		{
			if (price >= _buyAvgPrice + TakeProfit)
			{
				ClosePosition();
				ResetState();
				return;
			}
			
			if (_buyOrder == null)
			{
				var level = _buyAvgPrice - (GridDistance + StepDistance * _buyCount);
				var volume = Volume * (_useMartingale.Value ? (decimal)Math.Pow((double)MartingaleMultiplier, _buyCount) : 1m);
				_buyOrder = BuyLimit(level, volume);
			}
		}
		else if (_sellVolume > 0)
		{
			if (price <= _sellAvgPrice - TakeProfit)
			{
				ClosePosition();
				ResetState();
				return;
			}
			
			if (_sellOrder == null)
			{
				var level = _sellAvgPrice + (GridDistance + StepDistance * _sellCount);
				var volume = Volume * (_useMartingale.Value ? (decimal)Math.Pow((double)MartingaleMultiplier, _sellCount) : 1m);
				_sellOrder = SellLimit(level, volume);
			}
		}
		else
		{
			if (_buyOrder == null)
			{
				var buyPrice = price - StartOffset;
				_buyOrder = BuyLimit(buyPrice, Volume);
			}
			
			if (_sellOrder == null)
			{
				var sellPrice = price + StartOffset;
				_sellOrder = SellLimit(sellPrice, Volume);
			}
		}
	}
	
	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);
		
		var t = trade.Trade;
		
		if (trade.Order == _buyOrder && t.Side == Sides.Buy)
		{
			_buyAvgPrice = (_buyAvgPrice * _buyVolume + t.Price * t.Volume) / (_buyVolume + t.Volume);
			_buyVolume += t.Volume;
			_buyCount++;
			_buyOrder = null;
			CancelPendingOrder(ref _sellOrder);
		}
		else if (trade.Order == _sellOrder && t.Side == Sides.Sell)
		{
			_sellAvgPrice = (_sellAvgPrice * _sellVolume + t.Price * t.Volume) / (_sellVolume + t.Volume);
			_sellVolume += t.Volume;
			_sellCount++;
			_sellOrder = null;
			CancelPendingOrder(ref _buyOrder);
		}
		else if (Position == 0)
		{
			ResetState();
		}
	}
	
	private void CancelPendingOrder(ref Order order)
	{
		if (order != null && order.State.IsActive())
		CancelOrder(order);
		
		order = null;
	}
	
	private void ResetState()
	{
		CancelPendingOrder(ref _buyOrder);
		CancelPendingOrder(ref _sellOrder);
		_buyAvgPrice = _sellAvgPrice = 0;
		_buyVolume = _sellVolume = 0;
		_buyCount = _sellCount = 0;
	}
}
