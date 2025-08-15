// ValueMomentumAcrossAssetsStrategy.cs
// -----------------------------------------------------------------------------
// Value & Momentum across asset classes
// Rebalance frequency and data feeds stubbed; candle-trigger only.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Placeholder strategy combining value and momentum across assets.
/// </summary>
public class ValueMomentumAcrossAssetsStrategy : Strategy
{
	// Parameters
	private readonly StrategyParam<IEnumerable<Security>> _univ;
	private readonly StrategyParam<decimal> _min;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// List of securities to trade.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _univ.Value;
		set => _univ.Value = value;
	}

	/// <summary>
	/// Minimum trade value in USD.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _min.Value;
		set => _min.Value = value;
	}

	public ValueMomentumAcrossAssetsStrategy()
	{
		_univ = Param<IEnumerable<Security>>(nameof(Universe), [])
			.SetDisplay("Universe", "List of securities to trade", "Universe");

		_min = Param(nameof(MinTradeUsd), 200m)
			.SetDisplay("Min Trade USD", "Minimum notional value for orders", "Risk Management");
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return Universe.Select(s => (s, CandleType));
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset t)
	{
		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe empty");

		base.OnStarted(t);

		var trig = Universe.First();

		SubscribeCandles(CandleType, true, trig).Bind(c => OnDay(c.OpenTime.Date)).Start();
	}

	private void OnDay(DateTime d)
	{
		// TODO: implement factor logic. Placeholder keeps portfolio flat.
	}
}