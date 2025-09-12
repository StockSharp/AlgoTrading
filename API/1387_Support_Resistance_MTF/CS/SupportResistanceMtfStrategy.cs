using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Utility strategy that logs support and resistance levels from a higher timeframe.
/// </summary>
public class SupportResistanceMtfStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<bool> _useHighLow;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal _currentResistance;
	private decimal _currentSupport;

	/// <summary>
	/// Lookback period on the higher timeframe.
	/// </summary>
	public int Period { get => _period.Value; set => _period.Value = value; }

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HigherCandleType { get => _higherCandleType.Value; set => _higherCandleType.Value = value; }

	/// <summary>
	/// Use high/low prices instead of close/open.
	/// </summary>
	public bool UseHighLow { get => _useHighLow.Value; set => _useHighLow.Value = value; }

	/// <summary>
	/// Initialize <see cref="SupportResistanceMtfStrategy"/>.
	/// </summary>
	public SupportResistanceMtfStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Lookback length for levels", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Higher Candle Type", "Timeframe for level calculation", "General");

		_useHighLow = Param(nameof(UseHighLow), true)
			.SetDisplay("Use High/Low", "Use high/low or close/open", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, HigherCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_currentResistance = _currentSupport = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Period };
		_lowest = new Lowest { Length = Period };

		var subscription = SubscribeCandles(HigherCandleType);
		subscription
			.Bind(ProcessHigherCandle)
			.Start();
	}

	private void ProcessHigherCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highPrice = UseHighLow ? candle.HighPrice : Math.Max(candle.ClosePrice, candle.OpenPrice);
		var lowPrice = UseHighLow ? candle.LowPrice : Math.Min(candle.ClosePrice, candle.OpenPrice);

		var highVal = _highest.Process(highPrice);
		var lowVal = _lowest.Process(lowPrice);

		if (highVal.IsFinal)
			_currentResistance = highVal.ToDecimal();

		if (lowVal.IsFinal)
			_currentSupport = lowVal.ToDecimal();

		LogInfo($"Support={_currentSupport:F2} Resistance={_currentResistance:F2}");
	}
}
