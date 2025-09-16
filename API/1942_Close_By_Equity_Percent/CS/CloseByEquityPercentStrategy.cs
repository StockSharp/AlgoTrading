using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes position when equity exceeds balance by a given multiplier.
/// </summary>
public class CloseByEquityPercentStrategy : Strategy
{
	private readonly StrategyParam<decimal> _equityPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _currentBalance;

	/// <summary>
	/// Equity to balance multiplier.
	/// </summary>
	public decimal EquityPercentFromBalance
	{
		get => _equityPercent.Value;
		set => _equityPercent.Value = value;
	}

	/// <summary>
	/// Candle type for periodic checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CloseByEquityPercentStrategy"/>.
	/// </summary>
	public CloseByEquityPercentStrategy()
	{
		_equityPercent = Param(nameof(EquityPercentFromBalance), 1.2m)
			.SetDisplay("Equity/Bal Multiplier", "Threshold multiplier for equity relative to balance", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1.1m, 2m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for periodic checks", "General");
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
		_currentBalance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentBalance = Portfolio?.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Do(ProcessCandle)
			.Start();

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

		var equity = Portfolio?.CurrentValue ?? 0m;

		if (equity > _currentBalance * EquityPercentFromBalance)
		{
			if (Position != 0)
				ClosePosition();

			LogInfo($"Equity {equity:0.##} exceeded balance {_currentBalance:0.##} * {EquityPercentFromBalance:0.##}");
			_currentBalance = equity;
			return;
		}

		if (Position == 0)
			_currentBalance = equity;
	}
}
