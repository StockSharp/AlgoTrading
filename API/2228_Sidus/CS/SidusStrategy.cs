using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SIDUS strategy based on moving average crossovers.
/// Buys when fast LWMA crosses above slow LWMA or when slow LWMA crosses above slow EMA.
/// Sells on opposite crossovers. Includes optional stop-loss and take-profit.
/// </summary>
public class SidusStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEma;
	private readonly StrategyParam<int> _slowEma;
	private readonly StrategyParam<int> _fastLwma;
	private readonly StrategyParam<int> _slowLwma;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	
	private ExponentialMovingAverage _fastEmaIndicator;
	private ExponentialMovingAverage _slowEmaIndicator;
	private LinearWeightedMovingAverage _fastLwmaIndicator;
	private LinearWeightedMovingAverage _slowLwmaIndicator;
	
	private decimal _prevFastLwma;
	private decimal _prevSlowLwma;
	private decimal _prevSlowEma;
	private bool _isInitialized;
	
	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEma
	{
		get => _fastEma.Value;
		set => _fastEma.Value = value;
	}
	
	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEma
	{
		get => _slowEma.Value;
		set => _slowEma.Value = value;
	}
	
	/// <summary>
	/// Fast LWMA length.
	/// </summary>
	public int FastLwma
	{
		get => _fastLwma.Value;
		set => _fastLwma.Value = value;
	}
	
	/// <summary>
	/// Slow LWMA length.
	/// </summary>
	public int SlowLwma
	{
		get => _slowLwma.Value;
		set => _slowLwma.Value = value;
	}
	
	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}
	
	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public SidusStrategy()
	{
		_fastEma = Param(nameof(FastEma), 18)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Length of the fast EMA", "Sidus")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 2);
		
		_slowEma = Param(nameof(SlowEma), 28)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Length of the slow EMA", "Sidus")
		.SetCanOptimize(true)
		.SetOptimize(20, 50, 2);
		
		_fastLwma = Param(nameof(FastLwma), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Length of the fast LWMA", "Sidus")
		.SetCanOptimize(true)
		.SetOptimize(3, 10, 1);
		
		_slowLwma = Param(nameof(SlowLwma), 8)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Length of the slow LWMA", "Sidus")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit in percent", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);
		
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss in percent", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);
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
		_prevFastLwma = 0;
		_prevSlowLwma = 0;
		_prevSlowEma = 0;
		_isInitialized = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_fastEmaIndicator = new ExponentialMovingAverage { Length = FastEma };
		_slowEmaIndicator = new ExponentialMovingAverage { Length = SlowEma };
		_fastLwmaIndicator = new LinearWeightedMovingAverage { Length = FastLwma };
		_slowLwmaIndicator = new LinearWeightedMovingAverage { Length = SlowLwma };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastEmaIndicator, _slowEmaIndicator, _fastLwmaIndicator, _slowLwmaIndicator, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEmaIndicator);
			DrawIndicator(area, _slowEmaIndicator);
			DrawIndicator(area, _fastLwmaIndicator);
			DrawIndicator(area, _slowLwmaIndicator);
			DrawOwnTrades(area);
		}
		
		StartProtection(
		new Unit(TakeProfitPercent, UnitTypes.Percent),
		new Unit(StopLossPercent, UnitTypes.Percent)
		);
		
		base.OnStarted(time);
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue, decimal fastLwmaValue, decimal slowLwmaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (!_isInitialized && _fastEmaIndicator.IsFormed && _slowEmaIndicator.IsFormed && _fastLwmaIndicator.IsFormed && _slowLwmaIndicator.IsFormed)
		{
			_prevFastLwma = fastLwmaValue;
			_prevSlowLwma = slowLwmaValue;
			_prevSlowEma = slowEmaValue;
			_isInitialized = true;
			return;
		}
		
		if (!_isInitialized)
		return;
		
		var buySignal =
		(fastLwmaValue > slowLwmaValue && _prevFastLwma <= _prevSlowLwma) ||
		(slowLwmaValue > slowEmaValue && _prevSlowLwma <= _prevSlowEma);
		
		var sellSignal =
		(fastLwmaValue < slowLwmaValue && _prevFastLwma >= _prevSlowLwma) ||
		(slowLwmaValue < slowEmaValue && _prevSlowLwma >= _prevSlowEma);
		
		if (buySignal && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		else if (sellSignal && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));
		
		_prevFastLwma = fastLwmaValue;
		_prevSlowLwma = slowLwmaValue;
		_prevSlowEma = slowEmaValue;
	}
}
