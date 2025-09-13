using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demo strategy that logs candle data and configured signal parameters.
/// It does not execute trades.
/// </summary>
public class SignalsDemoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _equityLimit;
	private readonly StrategyParam<decimal> _slippage;
	private readonly StrategyParam<decimal> _depositPercent;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum allowed equity when copying signals.
	/// </summary>
	public decimal EquityLimit
	{
		get => _equityLimit.Value;
		set => _equityLimit.Value = value;
	}

	/// <summary>
	/// Allowed slippage in ticks.
	/// </summary>
	public decimal Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <summary>
	/// Percentage of deposit to allocate for signal copying.
	/// </summary>
	public decimal DepositPercent
	{
		get => _depositPercent.Value;
		set => _depositPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref=\"SignalsDemoStrategy\"/>.
	/// </summary>
	public SignalsDemoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_equityLimit = Param(nameof(EquityLimit), 0m)
		.SetDisplay("Equity Limit", "Max equity for signal copy", "General")
		.SetCanOptimize(true);

		_slippage = Param(nameof(Slippage), 0m)
		.SetDisplay("Slippage", "Allowed slippage in ticks", "General")
		.SetCanOptimize(true);

		_depositPercent = Param(nameof(DepositPercent), 100m)
		.SetDisplay("Deposit Percent", "Percent of deposit to use", "General")
		.SetCanOptimize(true);
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

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		// Log configured signal parameters.
		AddInfoLog($"Equity limit: {EquityLimit}, Slippage: {Slippage}, Deposit percent: {DepositPercent}");
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Print basic information about the finished candle.
		AddInfoLog($"Candle {candle.OpenTime:yyyy-MM-dd HH:mm} Close {candle.ClosePrice}");
	}
}
