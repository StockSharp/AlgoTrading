using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that places buy and sell stop orders around current price and manages trailing stops.
/// Designed for volatile news releases.
/// </summary>
public class NewsPendingOrdersStrategy : Strategy
{
	private readonly StrategyParam<int> _step;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _trailingStart;
	private readonly StrategyParam<int> _stepTrail;
	private readonly StrategyParam<bool> _breakEven;
	private readonly StrategyParam<int> _minProfitBreakEven;
	private readonly StrategyParam<int> _timeModify;
	
	private decimal _tickSize;
	private Order _buyPending;
	private Order _sellPending;
	private Order _stopOrder;
	private Order _takeOrder;
	private DateTimeOffset _lastBuyAdjust;
	private DateTimeOffset _lastSellAdjust;
	private decimal _entryPrice;
	private bool _breakEvenDone;
	
	/// <summary>
	/// Distance in ticks to place pending orders.
	/// </summary>
	public int Step
	{
		get => _step.Value;
		set => _step.Value = value;
	}
	
	/// <summary>
	/// Stop-loss in ticks.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Take-profit in ticks. 0 disables.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Trailing stop distance in ticks.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}
	
	/// <summary>
	/// Profit in ticks before trailing is activated.
	/// </summary>
	public int TrailingStart
	{
		get => _trailingStart.Value;
		set => _trailingStart.Value = value;
	}
	
	/// <summary>
	/// Minimum change in stop price in ticks to send a new stop order.
	/// </summary>
	public int StepTrail
	{
		get => _stepTrail.Value;
		set => _stepTrail.Value = value;
	}
	
	/// <summary>
	/// Move stop to entry price after reaching profit.
	/// </summary>
	public bool BreakEven
	{
		get => _breakEven.Value;
		set => _breakEven.Value = value;
	}
	
	/// <summary>
	/// Profit in ticks required to move stop to break-even.
	/// </summary>
	public int MinProfitBreakEven
	{
		get => _minProfitBreakEven.Value;
		set => _minProfitBreakEven.Value = value;
	}
	
	/// <summary>
	/// Seconds between pending order updates.
	/// </summary>
	public int TimeModify
	{
		get => _timeModify.Value;
		set => _timeModify.Value = value;
	}
	
	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public NewsPendingOrdersStrategy()
	{
		_step = Param(nameof(Step), 10)
		.SetGreaterThanZero()
		.SetDisplay("Step", "Distance in ticks", "General");
		
		_stopLoss = Param(nameof(StopLoss), 10)
		.SetGreaterThanZero()
		.SetDisplay("Stop-loss", "Stop-loss in ticks", "Risk");
		
		_takeProfit = Param(nameof(TakeProfit), 50)
		.SetDisplay("Take-profit", "Take-profit in ticks", "Risk");
		
		_trailingStop = Param(nameof(TrailingStop), 10)
		.SetDisplay("Trailing stop", "Trailing stop in ticks", "Risk");
		
		_trailingStart = Param(nameof(TrailingStart), 0)
		.SetDisplay("Trailing start", "Profit before trailing", "Risk");
		
		_stepTrail = Param(nameof(StepTrail), 2)
		.SetDisplay("Step trail", "Minimum stop change", "Risk");
		
		_breakEven = Param(nameof(BreakEven), false)
		.SetDisplay("Break even", "Move stop to entry", "Risk");
		
		_minProfitBreakEven = Param(nameof(MinProfitBreakEven), 0)
		.SetDisplay("Break even profit", "Profit to move stop", "Risk");
		
		_timeModify = Param(nameof(TimeModify), 30)
		.SetGreaterThanZero()
		.SetDisplay("Reprice secs", "Seconds between repricing", "General");
	}
	
	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_buyPending = null;
		_sellPending = null;
		_stopOrder = null;
		_takeOrder = null;
		_entryPrice = 0;
		_breakEvenDone = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_tickSize = Security?.PriceStep ?? 1m;
		
		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}
	
	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var bid = level1.TryGetDecimal(Level1Fields.BestBidPrice);
		var ask = level1.TryGetDecimal(Level1Fields.BestAskPrice);
		if (bid == null || ask == null)
			return;
		
		var now = level1.ServerTime;
		var stepPrice = Step * _tickSize;
		var trailStep = StepTrail * _tickSize;
		
		if (Position == 0)
			{
			// place or reprice buy stop
			var buyPrice = ask.Value + stepPrice;
			if (_buyPending == null)
				{
				_buyPending = BuyStop(Volume, buyPrice);
				_lastBuyAdjust = now;
			}
			else if ((now - _lastBuyAdjust).TotalSeconds >= TimeModify && Math.Abs(_buyPending.Price - buyPrice) >= trailStep)
			{
				CancelOrder(_buyPending);
				_buyPending = BuyStop(Volume, buyPrice);
				_lastBuyAdjust = now;
			}
			
			// place or reprice sell stop
			var sellPrice = bid.Value - stepPrice;
			if (_sellPending == null)
				{
				_sellPending = SellStop(Volume, sellPrice);
				_lastSellAdjust = now;
			}
			else if ((now - _lastSellAdjust).TotalSeconds >= TimeModify && Math.Abs(_sellPending.Price - sellPrice) >= trailStep)
			{
				CancelOrder(_sellPending);
				_sellPending = SellStop(Volume, sellPrice);
				_lastSellAdjust = now;
			}
		}
		else
		{
			// cancel opposite pending orders when in position
			if (Position > 0 && _sellPending != null)
				{
				CancelOrder(_sellPending);
				_sellPending = null;
			}
			if (Position < 0 && _buyPending != null)
				{
				CancelOrder(_buyPending);
				_buyPending = null;
			}
			
			AdjustStop(ask.Value, bid.Value);
		}
	}
	
	private void AdjustStop(decimal ask, decimal bid)
	{
		if (_stopOrder == null)
			return;
		
		var trailStep = StepTrail * _tickSize;
		decimal? newPrice = null;
		
		if (Position > 0)
			{
			var profit = (bid - _entryPrice) / _tickSize;
			if (BreakEven && !_breakEvenDone && profit >= MinProfitBreakEven)
				{
				var be = _entryPrice + MinProfitBreakEven * _tickSize;
				if (be > _stopOrder.Price)
					{
					newPrice = be;
					_breakEvenDone = true;
				}
			}
			
			if (TrailingStop > 0 && profit >= TrailingStart)
				{
				var trail = bid - TrailingStop * _tickSize;
				if (trail > _stopOrder.Price + trailStep)
					newPrice = newPrice == null ? trail : Math.Max(newPrice.Value, trail);
			}
		}
		else if (Position < 0)
		{
			var profit = (_entryPrice - ask) / _tickSize;
			if (BreakEven && !_breakEvenDone && profit >= MinProfitBreakEven)
				{
				var be = _entryPrice - MinProfitBreakEven * _tickSize;
				if (be < _stopOrder.Price)
					{
					newPrice = be;
					_breakEvenDone = true;
				}
			}
			
			if (TrailingStop > 0 && profit >= TrailingStart)
				{
				var trail = ask + TrailingStop * _tickSize;
				if (trail < _stopOrder.Price - trailStep)
					newPrice = newPrice == null ? trail : Math.Min(newPrice.Value, trail);
			}
		}
		
		if (newPrice != null)
			{
			CancelOrder(_stopOrder);
			_stopOrder = Position > 0
			? SellStop(Volume, newPrice.Value)
			: BuyStop(Volume, newPrice.Value);
		}
	}
	
	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		
		if (trade.Order == null)
			return;
		
		if (Position != 0)
			{
			_entryPrice = trade.Trade.Price;
			_breakEvenDone = false;
			
			if (Position > 0 && _sellPending != null)
				{
				CancelOrder(_sellPending);
				_sellPending = null;
			}
			if (Position < 0 && _buyPending != null)
				{
				CancelOrder(_buyPending);
				_buyPending = null;
			}
			
			RegisterProtection(Position > 0);
		}
	}
	
	private void RegisterProtection(bool isLong)
	{
		var stopOffset = StopLoss * _tickSize;
		var takeOffset = TakeProfit * _tickSize;
		
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);
		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);
		
		_stopOrder = isLong
		? SellStop(Volume, _entryPrice - stopOffset)
		: BuyStop(Volume, _entryPrice + stopOffset);
		
		_takeOrder = TakeProfit > 0
		? (isLong ? SellLimit(Volume, _entryPrice + takeOffset) : BuyLimit(Volume, _entryPrice - takeOffset))
		: null;
	}
	
	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);
		
		if (Position != 0)
			return;
		
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);
		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);
		
		_stopOrder = null;
		_takeOrder = null;
		_entryPrice = 0;
		_breakEvenDone = false;
	}
}
