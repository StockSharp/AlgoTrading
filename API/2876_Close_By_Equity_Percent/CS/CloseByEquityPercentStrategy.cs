using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes all open positions once portfolio equity reaches
/// a configurable multiple of the last flat balance.
/// </summary>
public class CloseByEquityPercentStrategy : Strategy
{
	private readonly StrategyParam<decimal> _equityPercentFromBalance;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _balanceSnapshot;

	/// <summary>
	/// Equity multiple required before closing all positions.
	/// </summary>
	public decimal EquityPercentFromBalance
	{
		get => _equityPercentFromBalance.Value;
		set => _equityPercentFromBalance.Value = value;
	}

	/// <summary>
	/// Candle type used to trigger equity checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CloseByEquityPercentStrategy"/> class.
	/// </summary>
	public CloseByEquityPercentStrategy()
	{
		_equityPercentFromBalance = Param(nameof(EquityPercentFromBalance), 1.2m)
			.SetRange(1m, 3m)
			.SetDisplay("Equity Multiple", "Equity multiple required before closing all positions", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used to trigger equity checks", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			return [];

		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio cannot be null.");

		if (Security == null)
			throw new InvalidOperationException("Security must be set to subscribe for updates.");

		_balanceSnapshot = Portfolio.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateBalanceSnapshot();

		var equity = Portfolio?.CurrentValue ?? 0m;

		if (_balanceSnapshot <= 0m)
			return;

		var target = _balanceSnapshot * EquityPercentFromBalance;

		if (equity < target)
			return;

		foreach (var position in Positions.ToArray())
		{
			var volume = GetPositionValue(position.Security, Portfolio) ?? 0m;

			if (volume == 0m)
				continue;

			// Close each active position once the equity target is reached.
			ClosePosition(position.Security);
		}
	}

	private void UpdateBalanceSnapshot()
	{
		if (Portfolio == null)
			return;

		// Balance is updated only after all positions are closed.
		var hasOpenPositions = Positions.Any(p => (GetPositionValue(p.Security, Portfolio) ?? 0m) != 0m);

		if (!hasOpenPositions)
			_balanceSnapshot = Portfolio.CurrentValue ?? _balanceSnapshot;
	}
}
