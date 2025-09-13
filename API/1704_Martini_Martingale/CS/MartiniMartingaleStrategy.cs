using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martini martingale strategy that hedges losing positions by doubling volume in the opposite direction.
/// Places initial stop orders on both sides and closes all trades once profit target is reached.
/// </summary>
public class MartiniMartingaleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _profitClose;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<DataType> _candleType;
	
	private Order _buyStop;
	private Order _sellStop;
	private int _orderCount;
	private decimal _lastOrderPrice;
	private decimal _currentOrderVolume;
	
	/// <summary>
	/// Price step in absolute units.
	/// </summary>
	public decimal Step { get => _step.Value; set => _step.Value = value; }
	
	/// <summary>
	/// Profit threshold to close all positions.
	/// </summary>
	public decimal ProfitClose { get => _profitClose.Value; set => _profitClose.Value = value; }
	
	/// <summary>
	/// Starting trade volume.
	/// </summary>
	public decimal InitialVolume { get => _initialVolume.Value; set => _initialVolume.Value = value; }
	
	/// <summary>
	/// Candle type used for price updates.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public MartiniMartingaleStrategy()
	{
		_step = Param(nameof(Step), 10m)
			.SetDisplay("Step", "Price step size", "General")
			.SetCanOptimize(true);
		_profitClose = Param(nameof(ProfitClose), 1m)
			.SetDisplay("Profit Close", "Close positions when profit reached", "General")
			.SetCanOptimize(true);
		_initialVolume = Param(nameof(InitialVolume), 1m)
			.SetDisplay("Initial Volume", "Initial trade volume", "General")
			.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle", "Candle type", "General");
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		ResetState();
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var price = candle.ClosePrice;
		
		// Check profit target
		var profit = CalculateProfit(price);
		if (Position != 0 && profit >= ProfitClose)
		{
			ClosePosition();
			CancelStops();
			ResetState();
			return;
		}
		
		if (Position == 0)
		{
			// Place initial pending orders when flat
			if (_buyStop == null && _sellStop == null)
			{
				_buyStop = BuyStop(_currentOrderVolume, price + Step);
				_sellStop = SellStop(_currentOrderVolume, price - Step);
			}
			
			return;
		}
		
		// Remove pending orders once a position exists
		CancelStops();
		
		if (_orderCount == 0)
		{
			// First executed order
			_orderCount = 1;
			_lastOrderPrice = price;
			return;
		}
		
		var direction = Math.Sign(Position);
		var distance = direction > 0 ? _lastOrderPrice - price : price - _lastOrderPrice;
		
		// Open opposite order if price moved against us by the defined step * order count
		if (distance >= Step * _orderCount)
		{
			var volume = _currentOrderVolume * 2m;
			
			if (direction > 0)
			SellMarket(volume);
			else
			BuyMarket(volume);
			
			_currentOrderVolume = volume;
			_orderCount++;
			_lastOrderPrice = price;
		}
	}
	
	private void CancelStops()
	{
		if (_buyStop != null)
		{
			CancelOrder(_buyStop);
			_buyStop = null;
		}
		
		if (_sellStop != null)
		{
			CancelOrder(_sellStop);
			_sellStop = null;
		}
	}
	
	private decimal CalculateProfit(decimal currentPrice)
	{
		if (Position == 0)
		return 0m;
		
		var entry = PositionAvgPrice;
		var dir = Math.Sign(Position);
		return dir > 0
		? (currentPrice - entry) * Math.Abs(Position)
		: (entry - currentPrice) * Math.Abs(Position);
	}
	
	private void ResetState()
	{
		_orderCount = 0;
		_currentOrderVolume = InitialVolume;
		_lastOrderPrice = 0m;
	}
}
