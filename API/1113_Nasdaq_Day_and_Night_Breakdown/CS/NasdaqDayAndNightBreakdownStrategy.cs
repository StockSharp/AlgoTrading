using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Compares pre-market and regular session moves of Nasdaq futures and index.
/// </summary>
public class NasdaqDayAndNightBreakdownStrategy : Strategy
{
	private readonly StrategyParam<Security> _index;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<DateTimeOffset, (decimal open, decimal close)> _indexCandles = [];

	/// <summary>
	/// Nasdaq index security.
	/// </summary>
	public Security IndexSecurity
	{
		get => _index.Value;
		set => _index.Value = value;
	}

	/// <summary>
	/// Candle time frame.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NasdaqDayAndNightBreakdownStrategy"/>.
	/// </summary>
	public NasdaqDayAndNightBreakdownStrategy()
	{
		_index = Param<Security>(nameof(IndexSecurity), null)
			.SetDisplay("Index", "Reference index security", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles time frame", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (IndexSecurity, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_indexCandles.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		if (IndexSecurity == null)
			throw new InvalidOperationException("IndexSecurity must be set.");

		base.OnStarted(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessFutureCandle)
			.Start();

		SubscribeCandles(CandleType, true, IndexSecurity)
			.Bind(ProcessIndexCandle)
			.Start();
	}

	private void ProcessIndexCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_indexCandles[candle.OpenTime] = (candle.OpenPrice, candle.ClosePrice);
	}

	private void ProcessFutureCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_indexCandles.TryGetValue(candle.OpenTime, out var idx))
			return;

		var beforeMarketOpen = idx.open - candle.OpenPrice;
		var afterMarketOpen = idx.close - idx.open;
		var directionContinues = beforeMarketOpen * afterMarketOpen;
		var betterDisplay = directionContinues > 0 ? beforeMarketOpen + afterMarketOpen : afterMarketOpen;
		var upSwing = candle.HighPrice - candle.OpenPrice;
		var downSwing = candle.LowPrice - candle.OpenPrice;

		LogInfo($"Before={beforeMarketOpen} After={afterMarketOpen} Better={betterDisplay} UpSwing={upSwing} DownSwing={downSwing}");
	}
}
