using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Country value factor strategy based on CAPE ratio.
/// </summary>
public class CountryValueFactorStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Securities to trade.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _universe.Value;
		set => _universe.Value = value;
	}

	/// <summary>
	/// Minimum trade size in USD.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _minTradeUsd.Value;
		set => _minTradeUsd.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CountryValueFactorStrategy"/>.
	/// </summary>
	public CountryValueFactorStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe), [])
			.SetDisplay("Universe", "Trading securities collection", "General");

		_minTradeUsd = Param(nameof(MinTradeUsd), 200m)
			.SetDisplay("Min Trade USD", "Minimal trade size in USD", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return Universe?.Select(s => (s, CandleType)) ?? [];
	}

	/// <inheritdoc />
	
	protected override void OnReseted()
	{
		base.OnReseted();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe is empty.");

		var trigger = Universe.First();

		SubscribeCandles(CandleType, true, trigger)
			.Bind(c => OnDay(c.OpenTime.Date))
			.Start();
	}

	private void OnDay(DateTime date)
	{
		// TODO: implement factor logic. Placeholder keeps portfolio flat.
	}
}
