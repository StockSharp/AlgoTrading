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
/// Logs the current position on every candle. Simplified from the original multi-position listing.
/// When position changes direction based on candle close, trades accordingly.
/// </summary>
public class ListPositionsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _logInterval;

	private int _candleCount;
	private decimal? _prevClose;

	/// <summary>
	/// Candle type for monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles between position log entries.
	/// </summary>
	public int LogInterval
	{
		get => _logInterval.Value;
		set => _logInterval.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ListPositionsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe for monitoring", "General");

		_logInterval = Param(nameof(LogInterval), 10)
			.SetGreaterThanZero()
			.SetDisplay("Log Interval", "Log position every N candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_candleCount = 0;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed)
			return;

		_candleCount++;

		if (_prevClose != null)
		{
			if (candle.ClosePrice > _prevClose.Value && Position <= 0)
				BuyMarket();
			else if (candle.ClosePrice < _prevClose.Value && Position >= 0)
				SellMarket();
		}

		if (_candleCount % LogInterval == 0)
		{
			LogInfo($"Position: {Position}, Price: {candle.ClosePrice:0.#####}, Equity: {Portfolio?.CurrentValue:0.##}");
		}

		_prevClose = candle.ClosePrice;
	}
}
