namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Basic crossover template with stop loss and take profit.
/// </summary>
public class UltimateStrategyTemplateStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirst;
	
	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}
	
	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}
	
	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	
	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="UltimateStrategyTemplateStrategy"/> class.
	/// </summary>
	public UltimateStrategyTemplateStrategy()
	{
		_fastLength = Param(nameof(FastLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Length", "Period of the fast moving average", "General")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_slowLength = Param(nameof(SlowLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Length", "Period of the slow moving average", "General")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 5);
		
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
		.SetRange(0m, 50m)
		.SetDisplay("Stop Loss %", "Percentage stop loss", "Risk")
		.SetCanOptimize(true);
		
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 3m)
		.SetRange(0m, 100m)
		.SetDisplay("Take Profit %", "Percentage take profit", "Risk")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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
		_prevFast = 0m;
		_prevSlow = 0m;
		_isFirst = true;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastMa, slowMa, ProcessCandle).Start();
		
		StartProtection(
		stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
		takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
		useMarketOrders: true);
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (_isFirst)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isFirst = false;
			return;
		}
		
		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;
		
		if (crossUp && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));
		
		_prevFast = fast;
		_prevSlow = slow;
	}
}
