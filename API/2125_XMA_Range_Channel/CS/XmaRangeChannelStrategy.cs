using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades breakouts of a moving average channel built from highs and lows.
/// </summary>
public class XmaRangeChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;

	private SimpleMovingAverage _highMa = null!;
	private SimpleMovingAverage _lowMa = null!;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period for channel construction.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public XmaRangeChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for analysis", "General");

		_length = Param(nameof(Length), 7)
			.SetGreaterThanZero()
			.SetDisplay("Channel Length", "Period for high and low moving averages", "Indicator")
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

		// Prepare moving averages for high and low prices.
		_highMa = new SimpleMovingAverage { Length = Length };
		_lowMa = new SimpleMovingAverage { Length = Length };

		// Subscribe to candles and process each one.
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		// Enable built-in position protection.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only finished candles are used.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Update moving averages with high and low prices.
		var upper = _highMa.Process(candle.HighPrice, candle.ServerTime, true).ToDecimal();
		var lower = _lowMa.Process(candle.LowPrice, candle.ServerTime, true).ToDecimal();

		// Wait until both indicators have enough data.
		if (!_highMa.IsFormed || !_lowMa.IsFormed)
			return;

		// Breakout above the upper band - go long.
		if (candle.ClosePrice > upper && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		// Breakout below the lower band - go short.
		else if (candle.ClosePrice < lower && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
