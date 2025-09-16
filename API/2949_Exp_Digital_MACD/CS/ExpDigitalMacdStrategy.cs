using System;
using System.Collections.Generic;

using StockSharp;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the Exp_Digital_MACD expert advisor using StockSharp high level API.
/// The system observes MACD values on finished candles and trades according to four selectable modes.
/// </summary>
public class ExpDigitalMacdStrategy : Strategy
{
	private readonly StrategyParam<ExpDigitalMacdMode> _mode;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal? _macdPrev;
	private decimal? _macdPrev2;
	private decimal? _macdPrev3;
	private decimal? _signalPrev;
	private decimal? _signalPrev2;
	private decimal? _signalPrev3;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpDigitalMacdStrategy"/> class.
	/// </summary>
	public ExpDigitalMacdStrategy()
	{
		_mode = Param(nameof(Mode), ExpDigitalMacdMode.MacdTwist)
		.SetDisplay("Mode", "Algorithm used to analyse MACD", "General");

		_fastPeriod = Param(nameof(FastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Length of the fast EMA used inside MACD", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(8, 18, 2);

		_slowPeriod = Param(nameof(SlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Length of the slow EMA used inside MACD", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(20, 34, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Signal EMA", "Length of the MACD signal smoothing", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(3, 9, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for MACD evaluation", "General");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume for market entries", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pts)", "Protective stop distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pts)", "Protective target distance in price steps", "Risk");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
		.SetDisplay("Enable Long Entry", "Allow opening of long positions", "Permissions");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
		.SetDisplay("Enable Short Entry", "Allow opening of short positions", "Permissions");

		_enableLongExit = Param(nameof(EnableLongExit), true)
		.SetDisplay("Enable Long Exit", "Allow closing of existing long positions", "Permissions");

		_enableShortExit = Param(nameof(EnableShortExit), true)
		.SetDisplay("Enable Short Exit", "Allow closing of existing short positions", "Permissions");
	}

	/// <summary>
	/// Selected trading mode.
	/// </summary>
	public ExpDigitalMacdMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Candle data type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume used for each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Whether long entries are allowed.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Whether short entries are allowed.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Whether long exits are allowed.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Whether short exits are allowed.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
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

		_macdPrev = null;
		_macdPrev2 = null;
		_macdPrev3 = null;
		_signalPrev = null;
		_signalPrev2 = null;
		_signalPrev3 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Apply the configured volume so helper methods use the latest value.
		Volume = OrderVolume;

		// Create MACD indicator that exposes both the line and the signal buffer.
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

	// Subscribe to candles and bind the indicator via high level API.
	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(_macd, ProcessCandle)
	.Start();

	// Optional chart output for visual inspection of MACD behaviour.
	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, _macd);
		DrawOwnTrades(area);
	}

	var step = Security?.Step ?? 0m;
	Unit? stopLossUnit = null;
	Unit? takeProfitUnit = null;

	// Convert point based risk controls to absolute prices when possible.
	if (StopLossPoints > 0m && step > 0m)
	stopLossUnit = new Unit(StopLossPoints * step, UnitTypes.Absolute);

	if (TakeProfitPoints > 0m && step > 0m)
	takeProfitUnit = new Unit(TakeProfitPoints * step, UnitTypes.Absolute);

	StartProtection(
		takeProfit: takeProfitUnit,
		stopLoss: stopLossUnit,
		useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		// React only on closed candles to mirror the MQL implementation.
		if (candle.State != CandleStates.Finished)
		return;

		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)indicatorValue;
		var macdCurrent = macdValue.Macd;
		var signalCurrent = macdValue.Signal;

		// Ensure we have enough history for the selected mode before trading.
		if (_macdPrev is null || _macdPrev2 is null)
		{
			UpdateState(macdCurrent, signalCurrent);
			return;
		}

		if (Mode == ExpDigitalMacdMode.MacdTwist && _macdPrev3 is null)
		{
			UpdateState(macdCurrent, signalCurrent);
			return;
		}

		if (Mode == ExpDigitalMacdMode.SignalTwist && (_signalPrev is null || _signalPrev2 is null || _signalPrev3 is null))
		{
			UpdateState(macdCurrent, signalCurrent);
			return;
		}

		if (Mode == ExpDigitalMacdMode.MacdDisposition && (_signalPrev is null || _signalPrev2 is null))
		{
			UpdateState(macdCurrent, signalCurrent);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateState(macdCurrent, signalCurrent);
			return;
		}

		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;

		switch (Mode)
		{
			case ExpDigitalMacdMode.Breakdown:
			{
				var prev = _macdPrev.Value;
				var prev2 = _macdPrev2.Value;

				// MACD above zero two bars ago -> close shorts and potentially buy on drop back.
				if (prev2 > 0m)
				{
					if (EnableLongEntry && prev <= 0m)
					buyOpen = true;

					if (EnableShortExit)
					sellClose = true;
				}

				// MACD below zero two bars ago -> close longs and potentially sell on bounce above.
				if (prev2 < 0m)
				{
					if (EnableShortEntry && prev >= 0m)
					sellOpen = true;

					if (EnableLongExit)
					buyClose = true;
				}

				break;
			}
			case ExpDigitalMacdMode.MacdTwist:
			{
				var prev = _macdPrev.Value;
				var prev2 = _macdPrev2.Value;
				var prev3 = _macdPrev3!.Value;

				// Detect a local trough (twist upwards).
				if (prev2 < prev3)
				{
					if (EnableLongEntry && prev > prev2)
					buyOpen = true;

					if (EnableShortExit)
					sellClose = true;
				}

				// Detect a local peak (twist downwards).
				if (prev2 > prev3)
				{
					if (EnableShortEntry && prev < prev2)
					sellOpen = true;

					if (EnableLongExit)
					buyClose = true;
				}

				break;
			}
			case ExpDigitalMacdMode.SignalTwist:
			{
				var prev = _signalPrev!.Value;
				var prev2 = _signalPrev2!.Value;
				var prev3 = _signalPrev3!.Value;

				// Signal line turning up.
				if (prev2 < prev3)
				{
					if (EnableLongEntry && prev > prev2)
					buyOpen = true;

					if (EnableShortExit)
					sellClose = true;
				}

				// Signal line turning down.
				if (prev2 > prev3)
				{
					if (EnableShortEntry && prev < prev2)
					sellOpen = true;

					if (EnableLongExit)
					buyClose = true;
				}

				break;
			}
			case ExpDigitalMacdMode.MacdDisposition:
			{
				var macdPrev = _macdPrev.Value;
				var macdPrev2 = _macdPrev2.Value;
				var signalPrev = _signalPrev!.Value;
				var signalPrev2 = _signalPrev2!.Value;

				// MACD crossing back below the signal line.
				if (macdPrev2 > signalPrev2)
				{
					if (EnableLongEntry && macdPrev <= signalPrev)
					buyOpen = true;

					if (EnableShortExit)
					sellClose = true;
				}

				// MACD crossing back above the signal line.
				if (macdPrev2 < signalPrev2)
				{
					if (EnableShortEntry && macdPrev >= signalPrev)
					sellOpen = true;

					if (EnableLongExit)
					buyClose = true;
				}

				break;
			}
		}

		// Close existing positions when allowed by the original toggles.
		if (buyClose && Position > 0m)
		{
			SellMarket(Position);
		}

		if (sellClose && Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		// Open or reverse positions using market orders sized to flatten the previous exposure.
		if (buyOpen && Position <= 0m)
		{
			if (Position < 0m && !EnableShortExit)
			{
				// Respect the flag that forbids closing short positions.
			}
			else
			{
				var volume = OrderVolume + Math.Abs(Position);
				BuyMarket(volume);
			}
		}

		if (sellOpen && Position >= 0m)
		{
			if (Position > 0m && !EnableLongExit)
			{
				// Respect the flag that forbids closing long positions.
			}
			else
			{
				var volume = OrderVolume + Math.Abs(Position);
				SellMarket(volume);
			}
		}

		UpdateState(macdCurrent, signalCurrent);
	}

	private void UpdateState(decimal macd, decimal signal)
	{
		// Shift stored values so that previous readings remain accessible for the next candle.
		_macdPrev3 = _macdPrev2;
		_macdPrev2 = _macdPrev;
		_macdPrev = macd;

		_signalPrev3 = _signalPrev2;
		_signalPrev2 = _signalPrev;
		_signalPrev = signal;
	}
}

/// <summary>
/// Modes that replicate the original Exp_Digital_MACD decision rules.
/// </summary>
public enum ExpDigitalMacdMode
{
	/// <summary>
	/// Use zero line breakdown logic.
	/// </summary>
	Breakdown,

	/// <summary>
	/// Use MACD slope twists.
	/// </summary>
	MacdTwist,

	/// <summary>
	/// Use signal line twists.
	/// </summary>
	SignalTwist,

	/// <summary>
	/// Compare MACD disposition relative to the signal line.
	/// </summary>
	MacdDisposition,
}
