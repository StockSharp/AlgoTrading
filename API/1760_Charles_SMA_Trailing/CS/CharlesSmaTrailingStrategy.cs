using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA crossover strategy with optional trailing stop management.
/// </summary>
public class CharlesSmaTrailingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailStart;
	private readonly StrategyParam<decimal> _trailingAmount;
	
	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;
	
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private bool _trailActive;
	
	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	
	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	
	/// <summary>
	/// Fixed stop loss in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	
	/// <summary>
	/// Fixed take profit in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	
	/// <summary>
	/// Profit to activate trailing stop.
	/// </summary>
	public decimal TrailStart { get => _trailStart.Value; set => _trailStart.Value = value; }
	
	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingAmount { get => _trailingAmount.Value; set => _trailingAmount.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="CharlesSmaTrailingStrategy"/> class.
	/// </summary>
	public CharlesSmaTrailingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Working candle timeframe", "General");
		
		_fastPeriod = Param(nameof(FastPeriod), 18)
		.SetGreaterThanZero()
		.SetDisplay("Fast Period", "Length of fast SMA", "Indicators")
		.SetCanOptimize(true);
		
		_slowPeriod = Param(nameof(SlowPeriod), 60)
		.SetGreaterThanZero()
		.SetDisplay("Slow Period", "Length of slow SMA", "Indicators")
		.SetCanOptimize(true);
		
		_stopLoss = Param(nameof(StopLoss), 0m)
		.SetDisplay("Stop Loss", "Fixed stop loss", "Risk");
		
		_takeProfit = Param(nameof(TakeProfit), 25m)
		.SetDisplay("Take Profit", "Fixed take profit", "Risk");
		
		_trailStart = Param(nameof(TrailStart), 25m)
		.SetDisplay("Trail Start", "Profit to activate trailing", "Risk");
		
		_trailingAmount = Param(nameof(TrailingAmount), 5m)
		.SetDisplay("Trailing Amount", "Trailing stop distance", "Risk");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_fastSma = new SimpleMovingAverage { Length = FastPeriod };
		_slowSma = new SimpleMovingAverage { Length = SlowPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastSma, _slowSma, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastSma, "Fast SMA");
			DrawIndicator(area, _slowSma, "Slow SMA");
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!_fastSma.IsFormed || !_slowSma.IsFormed)
		return;
		
		ManagePosition(candle);
		
		if (fast > slow && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = StopLoss > 0 ? _entryPrice - StopLoss : 0m;
			_targetPrice = TakeProfit > 0 ? _entryPrice + TakeProfit : 0m;
			_trailActive = false;
		}
		else if (fast < slow && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = StopLoss > 0 ? _entryPrice + StopLoss : 0m;
			_targetPrice = TakeProfit > 0 ? _entryPrice - TakeProfit : 0m;
			_trailActive = false;
		}
	}
	
	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (StopLoss > 0 && candle.LowPrice <= _stopPrice)
			{
				ClosePosition();
				return;
			}
			
			if (TakeProfit > 0 && candle.HighPrice >= _targetPrice)
			{
				ClosePosition();
				return;
			}
			
			if (TrailStart > 0 && TrailingAmount > 0)
			{
				var move = candle.ClosePrice - _entryPrice;
				if (!_trailActive && move >= TrailStart)
				_trailActive = true;
				
				if (_trailActive)
				{
					var newStop = candle.ClosePrice - TrailingAmount;
					if (_stopPrice == 0m || newStop > _stopPrice)
					_stopPrice = newStop;
					
					if (candle.LowPrice <= _stopPrice)
					ClosePosition();
				}
			}
		}
		else if (Position < 0)
		{
			if (StopLoss > 0 && candle.HighPrice >= _stopPrice)
			{
				ClosePosition();
				return;
			}
			
			if (TakeProfit > 0 && candle.LowPrice <= _targetPrice)
			{
				ClosePosition();
				return;
			}
			
			if (TrailStart > 0 && TrailingAmount > 0)
			{
				var move = _entryPrice - candle.ClosePrice;
				if (!_trailActive && move >= TrailStart)
				_trailActive = true;
				
				if (_trailActive)
				{
					var newStop = candle.ClosePrice + TrailingAmount;
					if (_stopPrice == 0m || newStop < _stopPrice)
					_stopPrice = newStop;
					
					if (candle.HighPrice >= _stopPrice)
					ClosePosition();
				}
			}
		}
	}
}