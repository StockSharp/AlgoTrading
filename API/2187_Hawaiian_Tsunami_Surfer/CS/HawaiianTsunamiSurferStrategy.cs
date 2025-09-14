using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian strategy based on strong momentum spikes.
/// Sells when momentum percentage rises above a small threshold.
/// Buys when momentum drops below the negative threshold.
/// Uses protective stop-loss and take-profit in price steps.
/// </summary>
public class HawaiianTsunamiSurferStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _tsunamiStrength;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Momentum calculation period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Momentum deviation from 0% to trigger trades (in percent).
	/// </summary>
	public decimal TsunamiStrength
	{
		get => _tsunamiStrength.Value;
		set => _tsunamiStrength.Value = value;
	}

	/// <summary>
	/// Take-profit in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HawaiianTsunamiSurferStrategy"/>.
	/// </summary>
	public HawaiianTsunamiSurferStrategy()
	{
		_momentumPeriod = Param(nameof(MomentumPeriod), 1)
		.SetDisplay("Momentum Period", "Period of the momentum indicator", "Indicators")
		.SetGreaterThanZero();

		_tsunamiStrength = Param(nameof(TsunamiStrength), 0.24m)
		.SetDisplay("Threshold", "Momentum percentage deviation from 0%", "Parameters")
		.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500)
		.SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk Management")
		.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 700)
		.SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk Management")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		// Create momentum indicator
		var momentum = new Momentum { Length = MomentumPeriod };

		// Subscribe to candles and bind the indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(momentum, ProcessCandle)
		.Start();

		// Configure protective stop-loss and take-profit
		var priceStep = Security?.PriceStep ?? 1m;
		StartProtection(
		new Unit(TakeProfitPoints * priceStep, UnitTypes.Price),
		new Unit(StopLossPoints * priceStep, UnitTypes.Price),
		useMarketOrders: true);

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, momentum);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Convert momentum difference to percentage change around zero
		var closePrice = candle.ClosePrice;
		var previousPrice = closePrice - momentumValue;
		if (previousPrice == 0m)
		return;

		var percentChange = (momentumValue / previousPrice) * 100m;

		if (Position != 0)
		return;

		if (percentChange > TsunamiStrength)
		{
			// Strong upward move -> open short
			SellMarket(Volume);
		}
		else if (percentChange < -TsunamiStrength)
		{
			// Strong downward move -> open long
			BuyMarket(Volume);
		}
	}
}

