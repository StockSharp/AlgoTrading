using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Anomaly counter-trend strategy.
/// Measures the percentage change of the close over the last LookbackMinutes. A rise of at least PercentageThreshold sells and
/// a drop of at least PercentageThreshold buys, reversing an opposite position. Positions are closed by a stop-loss and a
/// take-profit set in ticks.
/// </summary>
public class AnomalyCounterTrendStrategy : Strategy
{
	private readonly StrategyParam<decimal> _percentageThreshold;
	private readonly StrategyParam<int> _lookbackMinutes;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Percentage move that counts as an anomaly.
	/// </summary>
	public decimal PercentageThreshold
	{
		get => _percentageThreshold.Value;
		set => _percentageThreshold.Value = value;
	}

	/// <summary>
	/// Window of the percentage change in minutes.
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AnomalyCounterTrendStrategy()
	{
		_percentageThreshold = Param(nameof(PercentageThreshold), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Percentage Threshold", "Percentage move that counts as an anomaly", "Anomaly Detection");

		_lookbackMinutes = Param(nameof(LookbackMinutes), 30)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Minutes", "Window of the percentage change in minutes", "Anomaly Detection");

		_stopLossTicks = Param(nameof(StopLossTicks), 100)
			.SetNotNegative()
			.SetDisplay("Stop Loss Ticks", "Stop-loss distance in ticks", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 200)
			.SetNotNegative()
			.SetDisplay("Take Profit Ticks", "Take-profit distance in ticks", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var frameMinutes = CandleType.Arg is TimeSpan tf && tf > TimeSpan.Zero ? tf.TotalMinutes : 1d;
		var lookbackBars = Math.Max(1, (int)Math.Round(LookbackMinutes / frameMinutes));
		var roc = new RateOfChange { Length = lookbackBars };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			TakeProfitTicks > 0 ? new Unit(TakeProfitTicks * step, UnitTypes.Absolute) : new Unit(),
			StopLossTicks > 0 ? new Unit(StopLossTicks * step, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, roc);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal change)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (change >= PercentageThreshold && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (change <= -PercentageThreshold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
	}
}
