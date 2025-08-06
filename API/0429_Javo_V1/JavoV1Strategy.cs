namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Javo v1 Strategy
/// </summary>
public class JavoV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;

	public JavoV1Strategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle type", "Heikin Ashi candle timeframe", "Heikin Ashi Candle");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 1)
			.SetDisplay("Fast EMA Period", "Fast EMA period", "Heikin Ashi EMA");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 30)
			.SetDisplay("Slow EMA Period", "Slow EMA period", "Slow EMA");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		// Subscribe to candles
		var subscription = this.SubscribeCandles(CandleType);
		subscription
			.WhenCandlesFinished()
			.Do(ProcessCandle)
			.Apply(this);

		subscription.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma, System.Drawing.Color.Lime);
			DrawIndicator(area, _slowEma, System.Drawing.Color.Red);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Skip if strategy is not ready
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate Heikin-Ashi values
		decimal haOpen, haClose;

		if (_prevHaOpen == 0)
		{
			// First candle
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2;
			haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
		}
		else
		{
			// Calculate based on previous HA candle
			haOpen = (_prevHaOpen + _prevHaClose) / 2;
			haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
		}

		// Process EMAs with HA close
		var fastEmaValue = _fastEma.Process(haClose);
		var slowEmaValue = _slowEma.Process(haClose);

		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
		{
			// Store current HA values for next candle
			_prevHaOpen = haOpen;
			_prevHaClose = haClose;
			return;
		}

		// Get numeric values
		var fma = fastEmaValue.GetValue<decimal>();
		var sma = slowEmaValue.GetValue<decimal>();

		// Get previous values for crossover detection
		var prevFma = _fastEma.GetValue(1);
		var prevSma = _slowEma.GetValue(1);

		// Check for crossovers
		var goLong = fma > sma && prevFma <= prevSma;
		var goShort = fma < sma && prevFma >= prevSma;

		// Execute trades
		if (goLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (goShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		// Store current HA values for next candle
		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
	}
}