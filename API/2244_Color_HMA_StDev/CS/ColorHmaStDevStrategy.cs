using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Hull Moving Average with standard deviation filter.
/// It opens positions when price deviates from the HMA by a defined multiplier.
/// </summary>
public class ColorHmaStDevStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _stdPeriod;
	private readonly StrategyParam<decimal> _k1;
	private readonly StrategyParam<decimal> _k2;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Hull Moving Average period.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation period.
	/// </summary>
	public int StdPeriod
	{
		get => _stdPeriod.Value;
		set => _stdPeriod.Value = value;
	}

	/// <summary>
	/// Deviation multiplier for entries.
	/// </summary>
	public decimal K1
	{
		get => _k1.Value;
		set => _k1.Value = value;
	}

	/// <summary>
	/// Deviation multiplier for exits.
	/// </summary>
	public decimal K2
	{
		get => _k2.Value;
		set => _k2.Value = value;
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
	/// Initialize ColorHmaStDev strategy.
	/// </summary>
	public ColorHmaStDevStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 13)
			.SetDisplay("HMA Period", "Hull Moving Average period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 2);

		_stdPeriod = Param(nameof(StdPeriod), 9)
			.SetDisplay("StdDev Period", "Standard deviation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_k1 = Param(nameof(K1), 1.5m)
			.SetDisplay("Entry Multiplier", "Deviation multiplier for entry", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_k2 = Param(nameof(K2), 2.5m)
			.SetDisplay("Exit Multiplier", "Deviation multiplier for exit", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4)))
			.SetDisplay("Candle Type", "Type of candles to subscribe", "Common");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var hma = new HullMovingAverage { Length = HmaPeriod };
		var std = new StandardDeviation { Length = StdPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hma, std, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upperEntry = hmaValue + K1 * stdValue;
		var lowerEntry = hmaValue - K1 * stdValue;
		var upperExit = hmaValue + K2 * stdValue;
		var lowerExit = hmaValue - K2 * stdValue;

		// Entry rules based on deviation from HMA
		if (candle.ClosePrice > upperEntry && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal: close {candle.ClosePrice} > {upperEntry}");
		}
		else if (candle.ClosePrice < lowerEntry && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal: close {candle.ClosePrice} < {lowerEntry}");
		}

		// Exit rules when price returns inside wider band
		if (Position > 0 && candle.ClosePrice < lowerExit)
		{
			SellMarket(Position);
			LogInfo($"Exit long: close {candle.ClosePrice} < {lowerExit}");
		}
		else if (Position < 0 && candle.ClosePrice > upperExit)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short: close {candle.ClosePrice} > {upperExit}");
		}
	}
}
