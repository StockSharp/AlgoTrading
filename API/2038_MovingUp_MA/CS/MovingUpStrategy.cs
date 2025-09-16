using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with optional risk management.
/// Implements take profit, stop loss and trailing stop.
/// </summary>
public class MovingUpStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _entryPrice;
	private decimal _maxPrice;
	private decimal _minPrice;
	private bool _isLong;
	
	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}
	
	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}
	
	/// <summary>
	/// Use take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}
	
	/// <summary>
	/// Take profit price distance.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Use stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}
	
	/// <summary>
	/// Stop loss price distance.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Use trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}
	
	/// <summary>
	/// Trailing stop price distance.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}
	
	/// <summary>
	/// The type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="MovingUpStrategy"/>.
	/// </summary>
	public MovingUpStrategy()
	{
		_fastLength = Param(nameof(FastLength), 13)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Fast MA period", "MA")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);
		
		_slowLength = Param(nameof(SlowLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Slow MA period", "MA")
		.SetCanOptimize(true)
		.SetOptimize(20, 60, 5);
		
		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use TP", "Enable take profit", "Risk");
		
		_takeProfit = Param(nameof(TakeProfit), 500m)
		.SetGreaterThanZero()
		.SetDisplay("TP", "Take profit distance", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 1000m, 100m);
		
		_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use SL", "Enable stop loss", "Risk");
		
		_stopLoss = Param(nameof(StopLoss), 250m)
		.SetGreaterThanZero()
		.SetDisplay("SL", "Stop loss distance", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(50m, 500m, 50m);
		
		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use TS", "Enable trailing stop", "Risk");
		
		_trailingStop = Param(nameof(TrailingStop), 250m)
		.SetGreaterThanZero()
		.SetDisplay("TS", "Trailing stop distance", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(50m, 500m, 50m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle", "Candle type", "General");
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
		_maxPrice = 0m;
		_minPrice = 0m;
		_isLong = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };
		
		var subscription = SubscribeCandles(CandleType);
		
		var previousFast = 0m;
		var previousSlow = 0m;
		var wasFastBelowSlow = false;
		var isInitialized = false;
		
		subscription
		.Bind(fastMa, slowMa, (candle, fast, slow) =>
		{
			if (candle.State != CandleStates.Finished)
			return;
			
			if (!IsFormedAndOnlineAndAllowTrading())
			return;
			
			if (!isInitialized && fastMa.IsFormed && slowMa.IsFormed)
			{
				previousFast = fast;
				previousSlow = slow;
				wasFastBelowSlow = fast < slow;
				isInitialized = true;
				return;
			}
			
			if (!isInitialized)
			return;
			
			var isFastBelowSlow = fast < slow;
			
			if (wasFastBelowSlow != isFastBelowSlow)
			{
				if (!isFastBelowSlow)
				{
					if (Position <= 0)
					{
						_entryPrice = candle.ClosePrice;
						_maxPrice = _entryPrice;
						_isLong = true;
						BuyMarket(Volume + Math.Abs(Position));
					}
				}
				else
				{
					if (Position >= 0)
					{
						_entryPrice = candle.ClosePrice;
						_minPrice = _entryPrice;
						_isLong = false;
						SellMarket(Volume + Math.Abs(Position));
					}
				}
				
				wasFastBelowSlow = isFastBelowSlow;
			}
			
			ManageRisk(candle.ClosePrice);
			
			previousFast = fast;
			previousSlow = slow;
		})
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}
	
	private void ManageRisk(decimal price)
	{
		if (Position > 0)
		{
			if (UseTakeProfit && price - _entryPrice >= TakeProfit)
			SellMarket(Position);
			
			if (UseStopLoss && _entryPrice - price >= StopLoss)
			SellMarket(Position);
			
			if (UseTrailingStop)
			{
				if (price > _maxPrice)
				_maxPrice = price;
				else if (_maxPrice - price >= TrailingStop)
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			var pos = Math.Abs(Position);
			
			if (UseTakeProfit && _entryPrice - price >= TakeProfit)
			BuyMarket(pos);
			
			if (UseStopLoss && price - _entryPrice >= StopLoss)
			BuyMarket(pos);
			
			if (UseTrailingStop)
			{
				if (price < _minPrice)
				_minPrice = price;
				else if (price - _minPrice >= TrailingStop)
				BuyMarket(pos);
			}
		}
	}
}
