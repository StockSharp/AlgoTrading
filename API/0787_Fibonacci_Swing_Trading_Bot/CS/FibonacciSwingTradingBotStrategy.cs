using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci swing trading strategy.
/// Calculates Fibonacci retracement levels over Donchian channel and trades on bullish/bearish candles.
/// </summary>
public class FibonacciSwingTradingBotStrategy : Strategy
{
	private const int SwingLength = 50;

	private readonly StrategyParam<decimal> _fiboLevel1;
	private readonly StrategyParam<decimal> _fiboLevel2;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// First Fibonacci retracement level.
	/// </summary>
	public decimal FiboLevel1
	{
		get => _fiboLevel1.Value;
		set => _fiboLevel1.Value = value;
	}

	/// <summary>
	/// Second Fibonacci retracement level.
	/// </summary>
	public decimal FiboLevel2
	{
		get => _fiboLevel2.Value;
		set => _fiboLevel2.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
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
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public FibonacciSwingTradingBotStrategy()
	{
		_fiboLevel1 = Param(nameof(FiboLevel1), 0.618m)
			.SetDisplay("Fibonacci Level 1", "First retracement level", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 0.8m, 0.01m);

		_fiboLevel2 = Param(nameof(FiboLevel2), 0.786m)
			.SetDisplay("Fibonacci Level 2", "Second retracement level", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.7m, 0.9m, 0.01m);

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward Ratio", "Target ratio between profit and loss", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent from entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_stopLoss = default;
		_takeProfit = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var donchian = new DonchianChannels { Length = SwingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var dc = (DonchianChannelsValue)donchianValue;

		if (dc.UpperBand is not decimal highestHigh ||
			dc.LowerBand is not decimal lowestLow)
			return;

		var range = highestHigh - lowestLow;
		var fib618 = highestHigh - range * FiboLevel1;
		var fib786 = highestHigh - range * FiboLevel2;

		var bullishCandle = candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > fib618 && candle.ClosePrice < highestHigh;
		var bearishCandle = candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < fib786 && candle.ClosePrice > lowestLow;

		var stopLoss = bullishCandle
			? candle.ClosePrice * (1m - StopLossPercent / 100m)
			: candle.ClosePrice * (1m + StopLossPercent / 100m);

		var takeProfit = bullishCandle
			? candle.ClosePrice + (candle.ClosePrice - stopLoss) * RiskRewardRatio
			: candle.ClosePrice - (stopLoss - candle.ClosePrice) * RiskRewardRatio;

		if (bullishCandle && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopLoss = stopLoss;
			_takeProfit = takeProfit;
		}
		else if (bearishCandle && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopLoss = stopLoss;
			_takeProfit = takeProfit;
		}

		if (Position > 0 && (candle.ClosePrice <= _stopLoss || candle.ClosePrice >= _takeProfit))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (candle.ClosePrice >= _stopLoss || candle.ClosePrice <= _takeProfit))
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}

