namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Simple multiple time frame moving average strategy.
/// Buys when both H1 and H4 SMAs are rising, sells when both are falling.
/// </summary>
public class SimpleMultipleTimeFrameMovingAverageStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<DataType> _longCandleType;

	private decimal _prevHourSma;
	private decimal _prevPrevHourSma;
	private decimal _prevLongSma;
	private decimal _prevPrevLongSma;
	private bool _hasHour;
	private bool _hasLong;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType ShortCandleType { get => _shortCandleType.Value; set => _shortCandleType.Value = value; }
	public DataType LongCandleType { get => _longCandleType.Value; set => _longCandleType.Value = value; }

	public SimpleMultipleTimeFrameMovingAverageStrategy()
	{
		_length = Param(nameof(Length), 5)
			.SetDisplay("MA Length", "Moving average period", "General")
			.SetCanOptimize(true);

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Short Time Frame", "First time frame", "General");

		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Time Frame", "Second time frame", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var shortSma = new SimpleMovingAverage { Length = Length };
		var longSma = new SimpleMovingAverage { Length = Length };

		SubscribeCandles(ShortCandleType)
			.Bind(shortSma, ProcessShort)
			.Start();

		SubscribeCandles(LongCandleType)
			.Bind(longSma, ProcessLong)
			.Start();
	}

	private void ProcessShort(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevPrevHourSma = _prevHourSma;
		_prevHourSma = sma;
		_hasHour = _prevPrevHourSma != default;

		TryTrade();
	}

	private void ProcessLong(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevPrevLongSma = _prevLongSma;
		_prevLongSma = sma;
		_hasLong = _prevPrevLongSma != default;

		TryTrade();
	}

	private void TryTrade()
	{
		if (!_hasHour || !_hasLong)
			return;

		var hourUp = _prevHourSma > _prevPrevHourSma;
		var hourDown = _prevHourSma < _prevPrevHourSma;
		var longUp = _prevLongSma > _prevPrevLongSma;
		var longDown = _prevLongSma < _prevPrevLongSma;

		if (Position == 0)
		{
			if (hourUp && longUp)
				BuyMarket();
			else if (hourDown && longDown)
				SellMarket();
		}
		else if (Position > 0)
		{
			if (hourDown || longDown)
				ClosePosition();
		}
		else // Position < 0
		{
			if (hourUp || longUp)
				ClosePosition();
		}
	}
}
