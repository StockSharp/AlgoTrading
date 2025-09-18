using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crossover strategy based on two exponential moving averages.
/// Opens long positions when the fast EMA crosses above the slow EMA and opens short positions on the opposite crossover.
/// </summary>
public class Crossover2EmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private decimal? _previousSpread;

	/// <summary>
	/// Candle type used for EMA calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the fast EMA.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow EMA.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="Crossover2EmaStrategy"/>.
	/// </summary>
	public Crossover2EmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for EMA calculations", "General");

		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Period of the fast EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(4, 30, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Period", "Period of the slow EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 1);
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

		_fastEma = null;
		_slowEma = null;
		_previousSpread = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SlowPeriod <= FastPeriod)
			throw new InvalidOperationException("Slow EMA period must be greater than fast EMA period.");

		_fastEma = new ExponentialMovingAverage
		{
			Length = FastPeriod,
			CandlePrice = CandlePrice.Close,
		};

		_slowEma = new ExponentialMovingAverage
		{
			Length = SlowPeriod,
			CandlePrice = CandlePrice.Close,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, ProcessCandle)
			.Start();

		// Enable built-in protective mechanisms once the strategy starts.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate the spread between the fast and slow EMAs to detect crossovers.
		var spread = fastValue - slowValue;

		if (_previousSpread is decimal previousSpread)
		{
			var crossedUp = previousSpread <= 0m && spread > 0m;
			var crossedDown = previousSpread >= 0m && spread < 0m;

			if (crossedUp && Position <= 0)
			{
				// Reverse a short position and establish a new long position when the fast EMA crosses above the slow EMA.
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (crossedDown && Position >= 0)
			{
				// Reverse a long position and establish a new short position when the fast EMA crosses below the slow EMA.
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_previousSpread = spread;
	}
}
