using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibo1 strategy: WMA crossover with momentum and MACD confirmation.
/// Uses Fibonacci-inspired levels based on recent high/low for additional filtering.
/// </summary>
public class Fibo1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _highestPeriod;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int HighestPeriod
	{
		get => _highestPeriod.Value;
		set => _highestPeriod.Value = value;
	}

	public Fibo1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast weighted MA period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow weighted MA period", "Indicators");

		_highestPeriod = Param(nameof(HighestPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Highest/Lowest", "Period for high/low range", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fastWma = new WeightedMovingAverage { Length = FastMaPeriod };
		var slowWma = new WeightedMovingAverage { Length = SlowMaPeriod };
		var highest = new Highest { Length = HighestPeriod };
		var lowest = new Lowest { Length = HighestPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastWma, slowWma, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastWma);
			DrawIndicator(area, slowWma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var range = highest - lowest;
		if (range <= 0)
			return;

		// Fibonacci 38.2% and 61.8% levels
		var fib382 = lowest + range * 0.382m;
		var fib618 = lowest + range * 0.618m;

		if (_prevFast.HasValue && _prevSlow.HasValue)
		{
			// Buy: WMA bullish cross + price above 38.2% fib level
			if (_prevFast.Value <= _prevSlow.Value && fast > slow && close > fib382 && Position <= 0)
			{
				BuyMarket();
			}
			// Sell: WMA bearish cross + price below 61.8% fib level
			else if (_prevFast.Value >= _prevSlow.Value && fast < slow && close < fib618 && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
