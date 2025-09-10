using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades against sharp price moves.
/// Short when price rises above threshold, long when price falls below threshold.
/// </summary>
public class AnomalyCounterTrendStrategy : Strategy
{
	private readonly StrategyParam<decimal> _percentageThreshold;
	private readonly StrategyParam<int> _lookbackMinutes;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _prices = new();

	/// <summary>
	/// Minimum percentage move to detect anomaly.
	/// </summary>
	public decimal PercentageThreshold
	{
		get => _percentageThreshold.Value;
		set => _percentageThreshold.Value = value;
	}

	/// <summary>
	/// Lookback period in minutes.
	/// </summary>
	public int LookbackMinutes
	{
		get => _lookbackMinutes.Value;
		set => _lookbackMinutes.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take-profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AnomalyCounterTrendStrategy"/>.
	/// </summary>
	public AnomalyCounterTrendStrategy()
	{
		_percentageThreshold = Param(nameof(PercentageThreshold), 1m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Percentage Threshold", "Minimum percentage change to trigger", "Anomaly Detection")
			.SetCanOptimize(true);

		_lookbackMinutes = Param(nameof(LookbackMinutes), 30)
			.SetRange(5, 120)
			.SetDisplay("Lookback Minutes", "Window size in minutes", "Anomaly Detection")
			.SetCanOptimize(true);

		_stopLossTicks = Param(nameof(StopLossTicks), 100)
			.SetRange(10, 300)
			.SetDisplay("Stop Loss Ticks", "Stop-loss distance in ticks", "Risk Management");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 200)
			.SetRange(20, 600)
			.SetDisplay("Take Profit Ticks", "Take-profit distance in ticks", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prices.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var priceStep = Security?.PriceStep ?? 1m;
		StartProtection(
			new Unit(TakeProfitTicks * priceStep, UnitTypes.Absolute),
			new Unit(StopLossTicks * priceStep, UnitTypes.Absolute),
			false);

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

		_prices.Enqueue(candle.ClosePrice);
		if (_prices.Count <= LookbackMinutes)
			return;

		var priceNMinutesAgo = _prices.Dequeue();
		if (priceNMinutesAgo == 0)
			return;

		var change = (candle.ClosePrice - priceNMinutesAgo) / priceNMinutesAgo * 100m;

		var volume = Volume + Math.Abs(Position);

		if (change >= PercentageThreshold && Position >= 0)
		{
			SellMarket(volume);
			LogInfo($"Sell: rise anomaly {change:F2}%");
		}
		else if (change <= -PercentageThreshold && Position <= 0)
		{
			BuyMarket(volume);
			LogInfo($"Buy: fall anomaly {change:F2}%");
		}
	}
}
