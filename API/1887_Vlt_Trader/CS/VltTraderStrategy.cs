namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Places breakout stop orders after detecting low volatility periods.
/// </summary>
public class VltTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _pendingLevel;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private Lowest _lowest = null!;
	private decimal _prevRange;
	private decimal _prevMinRange;

	/// <summary>
	/// Indicator period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Offset for stop orders in ticks.
	/// </summary>
	public int PendingLevel
	{
		get => _pendingLevel.Value;
		set => _pendingLevel.Value = value;
	}

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
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
	/// Initializes a new instance of the <see cref="VltTraderStrategy"/> class.
	/// </summary>
	public VltTraderStrategy()
	{
		_period = Param(nameof(Period), 9)
			.SetDisplay("Period", "Indicator period", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 50, 1);

		_pendingLevel = Param(nameof(PendingLevel), 100)
			.SetDisplay("Pending level", "Offset for stop orders in ticks", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_stopLoss = Param(nameof(StopLoss), 550)
			.SetDisplay("Stop loss", "Stop loss in ticks", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 2000, 50);

		_takeProfit = Param(nameof(TakeProfit), 550)
			.SetDisplay("Take profit", "Take profit in ticks", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 2000, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candles for calculation", "General");
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

		_lowest = new Lowest { Length = Period };
		_prevRange = 0m;
		_prevMinRange = decimal.MaxValue;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Do(ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfit, UnitTypes.Points), new Unit(StopLoss, UnitTypes.Points));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;

		var lowestValue = _lowest.Process(range);
		if (!lowestValue.IsFinal || !lowestValue.TryGetValue(out var minRange))
			return;

		var isSignal = range < minRange && _prevRange >= _prevMinRange;

		_prevRange = range;
		_prevMinRange = minRange;

		if (!isSignal || Position != 0)
			return;

		CancelActiveOrders();

		var step = Security.PriceStep ?? 1m;
		var buyPrice = candle.HighPrice + PendingLevel * step;
		var sellPrice = candle.LowPrice - PendingLevel * step;

		BuyStop(Volume, buyPrice);
		SellStop(Volume, sellPrice);
	}
}
