namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Closes all positions when profit exceeds a fixed amount or percentage.
/// </summary>
public class CatcherProfit10Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _maximumProfit;
	private readonly StrategyParam<bool> _percentage;
	private readonly StrategyParam<decimal> _maximumPercentage;

	private decimal _initialBalance;

	/// <summary>
	/// Candle type used for periodic profit checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Profit in currency units required to close positions.
	/// </summary>
	public decimal MaximumProfit
	{
		get => _maximumProfit.Value;
		set => _maximumProfit.Value = value;
	}

	/// <summary>
	/// Enable percentage based profit target.
	/// </summary>
	public bool Percentage
	{
		get => _percentage.Value;
		set => _percentage.Value = value;
	}

	/// <summary>
	/// Percentage of initial balance required to close positions.
	/// </summary>
	public decimal MaximumPercentage
	{
		get => _maximumPercentage.Value;
		set => _maximumPercentage.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public CatcherProfit10Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maximumProfit = Param(nameof(MaximumProfit), 200m)
			.SetDisplay("Maximum Profit", "Profit value to trigger close", "General")
			.SetNotNegative();

		_percentage = Param(nameof(Percentage), false)
			.SetDisplay("Use Percentage", "Use percentage instead of fixed profit", "General");

		_maximumPercentage = Param(nameof(MaximumPercentage), 2m)
			.SetDisplay("Maximum Percentage", "Percentage of balance to trigger close", "General")
			.SetNotNegative();
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
		_initialBalance = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_initialBalance = Portfolio?.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0)
			return;

		var currentBalance = Portfolio?.CurrentValue ?? _initialBalance;
		var profit = currentBalance - _initialBalance;

		if (!Percentage && profit > MaximumProfit)
		{
			CloseAll();
		}
		else if (Percentage && _initialBalance > 0 && (profit / _initialBalance) * 100m > MaximumPercentage)
		{
			CloseAll();
		}
	}

	private void CloseAll()
	{
		CancelActiveOrders();

		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}
