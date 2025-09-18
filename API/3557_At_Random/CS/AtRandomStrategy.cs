using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that randomly opens long or short positions on every completed candle.
/// The implementation mirrors the "At random" MetaTrader expert by generating a new
/// signal on each bar and optionally closing the previous position before acting on it.
/// </summary>
public class AtRandomStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _closeBeforeReversal;
	private readonly StrategyParam<bool> _logSignals;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _randomSeed;

	private Random _random = null!;
	private bool _waitForFlat;

	/// <summary>
	/// Initializes a new instance of <see cref="AtRandomStrategy"/>.
	/// </summary>
	public AtRandomStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base order size for every random entry", "Trading")
			.SetCanOptimize(true);

		_closeBeforeReversal = Param(nameof(CloseBeforeReversal), true)
			.SetDisplay("Close Before Reversal", "Whether the existing position must be closed before taking the next random signal", "Trading");

		_logSignals = Param(nameof(LogSignals), true)
			.SetDisplay("Log Signals", "Write an informational log entry every time a random direction is chosen", "Diagnostics");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe that triggers new random decisions", "Data");

		_randomSeed = Param(nameof(RandomSeed), 0)
			.SetDisplay("Random Seed", "Seed for the pseudo random generator (0 = system clock)", "Diagnostics");
	}

	/// <summary>
	/// Base order size used for each random market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Enables closing the current position before a new random entry is allowed.
	/// </summary>
	public bool CloseBeforeReversal
	{
		get => _closeBeforeReversal.Value;
		set => _closeBeforeReversal.Value = value;
	}

	/// <summary>
	/// Enables writing informational messages whenever a random direction is generated.
	/// </summary>
	public bool LogSignals
	{
		get => _logSignals.Value;
		set => _logSignals.Value = value;
	}

	/// <summary>
	/// Candle type used to schedule random decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Optional deterministic seed for the pseudo random number generator.
	/// </summary>
	public int RandomSeed
	{
		get => _randomSeed.Value;
		set => _randomSeed.Value = value;
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

		_waitForFlat = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TradeVolume <= 0)
			throw new InvalidOperationException("TradeVolume must be greater than zero.");

		_random = RandomSeed == 0 ? new Random() : new Random(RandomSeed);

		// Subscribe to the configured candle series and process signals when a bar completes.
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Wait until the previous position is fully closed when requested by the user.
		if (_waitForFlat)
		{
			if (Position == 0)
			{
				_waitForFlat = false;
			}
			else
			{
				return;
			}
		}

		// Close the existing exposure before acting on the next random signal.
		if (CloseBeforeReversal && Position != 0)
		{
			ClosePosition();
			_waitForFlat = true;
			return;
		}

		var side = _random.Next(0, 2) == 0 ? Sides.Buy : Sides.Sell;

		if (LogSignals)
			this.AddInfoLog($"Random signal: {side} at {candle.ClosePrice}.");

		if (side == Sides.Buy)
			BuyMarket(TradeVolume);
		else
			SellMarket(TradeVolume);
	}
}
