using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy based on distance from recent extremes.
/// Buys when price drops far below recent high and sells when price rises far above recent low.
/// Places additional limit orders forming a martingale grid and exits on total profit.
/// </summary>
public class VeryBlondeSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _countBars;
	private readonly StrategyParam<decimal> _limit;
	private readonly StrategyParam<decimal> _grid;
	private readonly StrategyParam<decimal> _amount;
	private readonly StrategyParam<decimal> _lockDown;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _entryPrice;
	private bool _isLong;
	private bool _lockActivated;
	private decimal _lockPrice;
	
	/// <summary>
	/// Number of candles to search extremes.
	/// </summary>
	public int CountBars
	{
		get => _countBars.Value;
		set => _countBars.Value = value;
	}
	
	/// <summary>
	/// Minimum distance from recent extreme in ticks to trigger entry.
	/// </summary>
	public decimal Limit
	{
		get => _limit.Value;
		set => _limit.Value = value;
	}
	
	/// <summary>
	/// Grid distance in ticks between additional orders.
	/// </summary>
	public decimal Grid
	{
		get => _grid.Value;
		set => _grid.Value = value;
	}
	
	/// <summary>
	/// Target profit to close all positions.
	/// </summary>
	public decimal Amount
	{
		get => _amount.Value;
		set => _amount.Value = value;
	}
	
	/// <summary>
	/// Breakeven activation distance in ticks.
	/// </summary>
	public decimal LockDown
	{
		get => _lockDown.Value;
		set => _lockDown.Value = value;
	}
	
	/// <summary>
	/// Type of candles for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public VeryBlondeSystemStrategy()
	{
		_countBars = Param(nameof(CountBars), 10)
		.SetDisplay("Count Bars", "Number of candles to search extremes", "General")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);
		
		_limit = Param(nameof(Limit), 240m)
		.SetDisplay("Limit", "Minimum distance from extreme in ticks", "Trading")
		.SetGreaterThanZero();
		
		_grid = Param(nameof(Grid), 35m)
		.SetDisplay("Grid", "Grid distance in ticks", "Trading")
		.SetGreaterThanZero();
		
		_amount = Param(nameof(Amount), 40m)
		.SetDisplay("Amount", "Target profit to close all positions", "Risk")
		.SetGreaterThanZero();
		
		_lockDown = Param(nameof(LockDown), 0m)
		.SetDisplay("Lock Down", "Breakeven activation distance in ticks", "Risk");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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
		_isLong = false;
		_lockActivated = false;
		_lockPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var highest = new Highest { Length = CountBars };
		var lowest = new Lowest { Length = CountBars };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(highest, lowest, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var step = Security.PriceStep ?? 1m;
		
		if (Position == 0)
		{
			CheckForOpen(candle, high, low, step);
		}
		else
		{
			CheckForClose(candle, step);
		}
	}
	
	private void CheckForOpen(ICandleMessage candle, decimal high, decimal low, decimal step)
	{
		var close = candle.ClosePrice;
		
		if (high - close > Limit * step)
		{
			OpenPosition(true, close, step);
		}
		else if (close - low > Limit * step)
		{
			OpenPosition(false, close, step);
		}
	}
	
	private void OpenPosition(bool isBuy, decimal price, decimal step)
	{
		var volume = Volume;
		if (isBuy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}
		
		_entryPrice = price;
		_isLong = isBuy;
		_lockActivated = false;
		_lockPrice = 0m;
		
		for (var i = 1; i < 5; i++)
		{
			var gridPrice = isBuy
			? price - Grid * step * i
			: price + Grid * step * i;
			
			var gridVolume = volume * (decimal)Math.Pow(2, i);
			
			if (isBuy)
			BuyLimit(gridVolume, gridPrice);
			else
			SellLimit(gridVolume, gridPrice);
		}
	}
	
	private void CheckForClose(ICandleMessage candle, decimal step)
	{
		var currentProfit = Position * (candle.ClosePrice - _entryPrice);
		
		if (currentProfit >= Amount)
		{
			CloseAll();
			return;
		}
		
		if (LockDown <= 0m)
		return;
		
		if (_isLong)
		{
			if (!_lockActivated && candle.ClosePrice - _entryPrice > LockDown * step)
			{
				_lockActivated = true;
				_lockPrice = _entryPrice;
			}
			else if (_lockActivated && candle.ClosePrice <= _lockPrice)
			{
				CloseAll();
			}
		}
		else
		{
			if (!_lockActivated && _entryPrice - candle.ClosePrice > LockDown * step)
			{
				_lockActivated = true;
				_lockPrice = _entryPrice;
			}
			else if (_lockActivated && candle.ClosePrice >= _lockPrice)
			{
				CloseAll();
			}
		}
	}
	
	private void CloseAll()
	{
		if (Position != 0)
		ClosePosition();
		
		CancelActiveOrders();
		
		_entryPrice = 0m;
		_lockActivated = false;
		_lockPrice = 0m;
	}
}
