using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mechanical trading strategy - enters at a fixed hour with profit target and stop loss.
/// </summary>
public class MechanicalTradingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<bool> _isShort;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Profit target percentage from entry price.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Hour of day to enter trade.
	/// </summary>
	public int TradeHour
	{
		get => _tradeHour.Value;
		set => _tradeHour.Value = value;
	}

	/// <summary>
	/// Enter short instead of long.
	/// </summary>
	public bool IsShort
	{
		get => _isShort.Value;
		set => _isShort.Value = value;
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
	/// Initializes a new instance of <see cref="MechanicalTradingStrategy"/>.
	/// </summary>
	public MechanicalTradingStrategy()
	{
		_profitTarget = Param(nameof(ProfitTarget), 0.4m)
			.SetNotNegative()
			.SetDisplay("Profit Target (%)", "Take profit percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_stopLoss = Param(nameof(StopLoss), 0.2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_tradeHour = Param(nameof(TradeHour), 16)
			.SetRange(0, 23)
			.SetDisplay("Trade Hour", "Hour of the day to enter", "General");

		_isShort = Param(nameof(IsShort), false)
			.SetDisplay("Short Mode", "Enter short instead of long", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		StartProtection(
			takeProfit: new Unit(ProfitTarget, UnitTypes.Percent),
			stopLoss: new Unit(StopLoss, UnitTypes.Percent));

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

		var time = candle.OpenTime;

		if (time.Hour != TradeHour || time.Minute != 0)
			return;

		if (Position != 0)
			return;

		if (IsShort)
			SellMarket();
		else
			BuyMarket();
	}
}
