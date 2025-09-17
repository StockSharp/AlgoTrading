using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// First Friday detection strategy converted from the MetaTrader 4 FirstFriday expert advisor.
/// The strategy watches daily candles and writes an informational log when the first Friday candle of a month appears.
/// </summary>
public class FirstFridayStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset? _lastCandleTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="FirstFridayStrategy"/> class.
	/// </summary>
	public FirstFridayStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for first Friday detection", "General");
	}

	/// <summary>
	/// Candle type that is processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_lastCandleTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to the configured timeframe and listen for new candles.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Ignore duplicate callbacks for the same candle open time.
		if (_lastCandleTime == candle.OpenTime)
			return;

		_lastCandleTime = candle.OpenTime;

		// Check whether the completed candle corresponds to the first Friday of the month.
		if (!IsFirstFriday(candle.OpenTime))
			return;

		AddInfoLog("Detected the first Friday daily candle for {0:D}.", candle.OpenTime.Date);
	}

	private static bool IsFirstFriday(DateTimeOffset candleTime)
	{
		if (candleTime.DayOfWeek != DayOfWeek.Friday)
			return false;

		return candleTime.Day is >= 1 and <= 7;
	}
}
