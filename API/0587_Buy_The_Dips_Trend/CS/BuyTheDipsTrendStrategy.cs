using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover trend strategy with fixed take profit and stop loss.
/// </summary>
public class BuyTheDipsTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirstCandle;
	
	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}
	
	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
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
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="BuyTheDipsTrendStrategy"/>.
	/// </summary>
	public BuyTheDipsTrendStrategy()
	{
		_fastLength = Param(nameof(FastLength), 50)
		.SetRange(10, 100)
		.SetDisplay("Fast EMA", "Length of fast EMA", "Indicators")
		.SetCanOptimize(true);
		
		_slowLength = Param(nameof(SlowLength), 200)
		.SetRange(50, 400)
		.SetDisplay("Slow EMA", "Length of slow EMA", "Indicators")
		.SetCanOptimize(true);
		
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.7m)
		.SetRange(0.1m, 5m)
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
		.SetCanOptimize(true);
		
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
		.SetRange(0.5m, 10m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		
		_prevFast = 0;
		_prevSlow = 0;
		_isFirstCandle = true;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(fastEma, slowEma, ProcessCandle)
		.Start();
		
		StartProtection(
		takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
		stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
		useMarketOrders: true);
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (_isFirstCandle)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_isFirstCandle = false;
			return;
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var crossedUp = _prevFast <= _prevSlow && fastValue > slowValue;
		var crossedDown = _prevFast >= _prevSlow && fastValue < slowValue;
		
		if (crossedUp && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (crossedDown && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		
		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
