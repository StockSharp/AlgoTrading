using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on the Vortex indicator crossover system.
/// Replicates the logic of the original MQL expert by arming entry triggers
/// on the candle where VI+ and VI- lines cross and executing when price breaks the trigger.
/// </summary>
public class VortexIndicatorSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private VortexIndicator _vortex = null!;
	private decimal _previousPlus;
	private decimal _previousMinus;
	private bool _hasPrevious;
	private decimal? _pendingBuyTrigger;
	private decimal? _pendingSellTrigger;

	/// <summary>
	/// Length of the Vortex indicator.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters for the strategy.
	/// </summary>
	public VortexIndicatorSystemStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetDisplay("Vortex Length", "Period for the Vortex indicator", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for analysis", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_vortex = new VortexIndicator
		{
			Length = Length
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_vortex, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal viPlus, decimal viMinus)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_vortex.IsFormed)
			return;

		if (_pendingBuyTrigger is decimal buyTrigger && candle.HighPrice > buyTrigger)
		{
			if (Position <= 0)
			{
				// Reverse existing short if present and open a new long position when price breaks the trigger.
				BuyMarket(Volume + Math.Abs(Position));
			}

			_pendingBuyTrigger = null;
		}
		else if (_pendingSellTrigger is decimal sellTrigger && candle.LowPrice < sellTrigger)
		{
			if (Position >= 0)
			{
				// Reverse existing long if present and open a new short position when price breaks the trigger.
				SellMarket(Volume + Math.Abs(Position));
			}

			_pendingSellTrigger = null;
		}

		if (!_hasPrevious)
		{
			_previousPlus = viPlus;
			_previousMinus = viMinus;
			_hasPrevious = true;
			return;
		}

		var crossedUp = _previousPlus <= _previousMinus && viPlus > viMinus;
		var crossedDown = _previousPlus >= _previousMinus && viPlus < viMinus;

		if (crossedUp)
		{
			if (Position < 0)
			{
				// Flatten existing short positions when a bullish crossover appears.
				ClosePosition();
			}

			_pendingBuyTrigger = candle.HighPrice;
			_pendingSellTrigger = null;
		}
		else if (crossedDown)
		{
			if (Position > 0)
			{
				// Flatten existing long positions when a bearish crossover appears.
				ClosePosition();
			}

			_pendingSellTrigger = candle.LowPrice;
			_pendingBuyTrigger = null;
		}

		_previousPlus = viPlus;
		_previousMinus = viMinus;
	}
}
