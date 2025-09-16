using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crosses a fast EMA and a slow TEMA to approximate the original 2pb Ideal MA logic.
/// </summary>
public class TwoPoleIdealMaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa = null!;
	private TripleExponentialMovingAverage _slowMa = null!;

	private decimal _prevFast;
	private decimal _prevSlow;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow TEMA length.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Candle timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TwoPoleIdealMaStrategy"/>.
	/// </summary>
	public TwoPoleIdealMaStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10).SetDisplay("Fast Period", "Fast MA length", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 30).SetDisplay("Slow Period", "Slow MA length", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators.
		_fastMa = new ExponentialMovingAverage { Length = FastPeriod };
		_slowMa = new TripleExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		// Enable default protective behavior.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		// Process only finished candles.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;
		_prevFast = fast;
		_prevSlow = slow;

		// Enter long on upward cross, short on downward cross.
		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
