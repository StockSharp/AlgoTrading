using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Randomly enters a market position and keeps it open for a fixed duration.
/// </summary>
public class RndTradeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<TimeSpan> _holdDuration;

	private readonly Random _random = new();
	private DateTimeOffset? _entryTime;

	/// <summary>
	/// Candle type that drives random decision making.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume used when placing market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Holding duration for an opened position.
	/// </summary>
	public TimeSpan HoldDuration
	{
		get => _holdDuration.Value;
		set => _holdDuration.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RndTradeStrategy"/>.
	/// </summary>
	public RndTradeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles that trigger random entries", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume used for each random trade", "Parameters");

		_holdDuration = Param(nameof(HoldDuration), TimeSpan.FromHours(4))
			.SetDisplay("Hold Duration", "Time to keep a random trade open", "Risk");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_random.Next();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var candleTime = candle.CloseTime;

		if (Position == 0)
		{
			TryOpenPosition(candleTime);
		}
		else
		{
			TryClosePosition(candleTime);
		}
	}

	private void TryOpenPosition(DateTimeOffset candleTime)
	{
		var direction = _random.NextDouble();

		// Place a random buy or sell trade using the configured volume.
		var order = direction > 0.5
			? BuyMarket(TradeVolume)
			: SellMarket(TradeVolume);

		if (order == null)
			return;

		_entryTime = candleTime;
	}

	private void TryClosePosition(DateTimeOffset candleTime)
	{
		if (_entryTime is null)
			return;

		var elapsed = candleTime - _entryTime.Value;
		if (elapsed < HoldDuration)
			return;

		// Close the open position after the holding period expires.
		if (Position > 0)
		{
			if (SellMarket(Position) != null)
				_entryTime = null;
		}
		else if (Position < 0)
		{
			if (BuyMarket(-Position) != null)
				_entryTime = null;
		}
	}
}
