using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Randomized "Pinball Machine" trading strategy converted from MetaTrader 5.
/// </summary>
public class PinballMachineStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _minOffsetPoints;
	private readonly StrategyParam<int> _maxOffsetPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Random _random = new();

	private decimal _stopLossPrice;
	private decimal _takeProfitPrice;
	private decimal _entryPrice;

	/// <summary>
	/// Percentage of capital risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Minimum random offset in price steps for stop-loss and take-profit.
	/// </summary>
	public int MinOffsetPoints
	{
		get => _minOffsetPoints.Value;
		set => _minOffsetPoints.Value = value;
	}

	/// <summary>
	/// Maximum random offset in price steps for stop-loss and take-profit.
	/// </summary>
	public int MaxOffsetPoints
	{
		get => _maxOffsetPoints.Value;
		set => _maxOffsetPoints.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the random decision process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PinballMachineStrategy"/>.
	/// </summary>
	public PinballMachineStrategy()
	{
		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetDisplay("Risk Percent", "Percentage of capital risked per trade", "Money Management")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_minOffsetPoints = Param(nameof(MinOffsetPoints), 10)
			.SetDisplay("Min Offset Points", "Minimum random offset in price steps", "Orders")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_maxOffsetPoints = Param(nameof(MaxOffsetPoints), 100)
			.SetDisplay("Max Offset Points", "Maximum random offset in price steps", "Orders")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe that triggers the lottery", "Data");
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
		ResetTargets();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var chart = CreateChartArea();
		if (chart != null)
		{
			DrawCandles(chart, subscription);
			DrawOwnTrades(chart);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageOpenPosition(candle);

		if (Position != 0)
			return;

		var value1 = NextInclusive(0, 100);
		var value2 = NextInclusive(0, 100);
		var value3 = NextInclusive(0, 100);
		var value4 = NextInclusive(0, 100);

		if (value1 == value2)
		{
			if (TryOpenLong(candle))
				return;
		}

		if (value3 == value4)
		{
			TryOpenShort(candle);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopLossPrice > 0m && candle.LowPrice <= _stopLossPrice)
			{
				SellMarket(Position);
				ResetTargets();
				return;
			}

			if (_takeProfitPrice > 0m && candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(Position);
				ResetTargets();
			}
		}
		else if (Position < 0)
		{
			if (_stopLossPrice > 0m && candle.HighPrice >= _stopLossPrice)
			{
				BuyMarket(-Position);
				ResetTargets();
				return;
			}

			if (_takeProfitPrice > 0m && candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(-Position);
				ResetTargets();
			}
		}
	}

	private bool TryOpenLong(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var (minPoints, maxPoints) = NormalizePointRange();

		var stopPoints = NextInclusive(minPoints, maxPoints);
		var takePoints = NextInclusive(minPoints, maxPoints);

		var entryPrice = candle.ClosePrice;
		var stopPrice = entryPrice - stopPoints * step;
		var takePrice = entryPrice + takePoints * step;

		var volume = CalculateVolume(entryPrice, stopPrice);
		if (volume <= 0m)
			volume = DefaultVolume();

		if (volume <= 0m || !IsFormedAndOnlineAndAllowTrading())
			return false;

		BuyMarket(volume);

		_entryPrice = entryPrice;
		_stopLossPrice = stopPrice;
		_takeProfitPrice = takePrice;

		return true;
	}

	private bool TryOpenShort(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var (minPoints, maxPoints) = NormalizePointRange();

		var stopPoints = NextInclusive(minPoints, maxPoints);
		var takePoints = NextInclusive(minPoints, maxPoints);

		var entryPrice = candle.ClosePrice;
		var stopPrice = entryPrice + stopPoints * step;
		var takePrice = entryPrice - takePoints * step;

		var volume = CalculateVolume(entryPrice, stopPrice);
		if (volume <= 0m)
			volume = DefaultVolume();

		if (volume <= 0m || !IsFormedAndOnlineAndAllowTrading())
			return false;

		SellMarket(volume);

		_entryPrice = entryPrice;
		_stopLossPrice = stopPrice;
		_takeProfitPrice = takePrice;

		return true;
	}

	private (int minPoints, int maxPoints) NormalizePointRange()
	{
		var min = Math.Min(MinOffsetPoints, MaxOffsetPoints);
		var max = Math.Max(MinOffsetPoints, MaxOffsetPoints);

		if (min <= 0)
			min = 1;

		if (max < min)
			max = min;

		return (min, max);
	}

	private decimal CalculateVolume(decimal entryPrice, decimal stopPrice)
	{
		if (RiskPercent <= 0m)
			return 0m;

		var riskPerUnit = Math.Abs(entryPrice - stopPrice);
		if (riskPerUnit <= 0m)
			return 0m;

		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.CurrentBalance ?? Portfolio?.BeginValue ?? 0m;
		if (portfolioValue <= 0m)
			return 0m;

		var riskAmount = portfolioValue * (RiskPercent / 100m);
		if (riskAmount <= 0m)
			return 0m;

		return riskAmount / riskPerUnit;
	}

	private decimal DefaultVolume()
	{
		if (Volume > 0m)
			return Volume;

		return 1m;
	}

	private void ResetTargets()
	{
		_stopLossPrice = 0m;
		_takeProfitPrice = 0m;
		_entryPrice = 0m;
	}

	private int NextInclusive(int min, int max)
	{
		var low = Math.Min(min, max);
		var high = Math.Max(min, max);
		return _random.Next(low, high + 1);
	}
}
