using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on CCI (Commodity Channel Index) breakout.
/// </summary>
public class CciBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CciBreakoutStrategy"/>.
	/// </summary>
	public CciBreakoutStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("CCI Period", "Period for CCI calculation", "Indicators")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 5m)
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
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create CCI indicator
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		// Subscribe to candles and bind the indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(cci, ProcessCandle)
			.Start();

		// Enable stop loss protection
		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true
		);

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var cciValue = value.ToDecimal();

		// Entry logic
		if (cciValue > 100 && Position <= 0)
		{
			// CCI breakout above +100 - Strong upward momentum
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal: CCI breakout above +100. Value: {cciValue:F2}");
		}
		else if (cciValue < -100 && Position >= 0)
		{
			// CCI breakout below -100 - Strong downward momentum
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal: CCI breakout below -100. Value: {cciValue:F2}");
		}

		// Exit logic
		if (Position > 0 && cciValue < 0)
		{
			// Exit long position when CCI crosses back below zero
			SellMarket(Math.Abs(Position));
			LogInfo($"Exiting long position: CCI crossed below zero. Value: {cciValue:F2}");
		}
		else if (Position < 0 && cciValue > 0)
		{
			// Exit short position when CCI crosses back above zero
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exiting short position: CCI crossed above zero. Value: {cciValue:F2}");
		}
	}
}