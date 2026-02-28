using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Suffic369 breakout strategy based on fast/slow SMA crossover.
/// Buy when fast SMA crosses above slow SMA, sell when fast crosses below slow.
/// </summary>
public class Suffic369Strategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Suffic369Strategy"/> class.
	/// </summary>
	public Suffic369Strategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 3)
			.SetDisplay("Fast SMA Length", "Fast moving average period", "Indicators");

		_slowMaLength = Param(nameof(SlowMaLength), 6)
			.SetDisplay("Slow SMA Length", "Slow moving average period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle source", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var fastSma = new SimpleMovingAverage { Length = FastMaLength };
		var slowSma = new SimpleMovingAverage { Length = SlowMaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		// Buy signal: fast SMA crosses above slow SMA
		var longSignal = _prevFast <= _prevSlow && fast > slow;
		// Sell signal: fast SMA crosses below slow SMA
		var shortSignal = _prevFast >= _prevSlow && fast < slow;

		if (Position > 0 && shortSignal)
		{
			SellMarket();
		}
		else if (Position < 0 && longSignal)
		{
			BuyMarket();
		}
		else if (Position == 0)
		{
			if (longSignal)
				BuyMarket();
			else if (shortSignal)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
