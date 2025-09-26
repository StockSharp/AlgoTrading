using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Slow stochastic strategy that supports multiple entry algorithms.
/// Converts the Exp_Slow-Stoch MQL5 expert to the StockSharp high-level API.
/// </summary>
public class SlowStochasticModeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<SlowStochasticSignalMode> _mode;

	private readonly List<decimal> _kHistory = new();
	private readonly List<decimal?> _dHistory = new();

	private StochasticOscillator _stochastic;

	/// <summary>
	/// Available signal calculation modes.
	/// </summary>
	public enum SlowStochasticSignalMode
	{
		/// <summary>
		/// Trigger on the %K line breaking through the 50 level.
		/// </summary>
		Breakdown,

		/// <summary>
		/// Trigger on a direction change of the %K line.
		/// </summary>
		Twist,

		/// <summary>
		/// Trigger on %K and %D crossing (signal cloud color change).
		/// </summary>
		CloudTwist,
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SlowStochasticModeStrategy"/> class.
	/// </summary>
	public SlowStochasticModeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for calculations", "General");

		_kPeriod = Param(nameof(KPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("%K Period", "Lookback period of the %K line", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("%D Period", "Smoothing period of the %D line", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(2, 15, 1);

		_slowing = Param(nameof(Slowing), 3)
		.SetGreaterThanZero()
		.SetDisplay("Slowing", "Additional smoothing applied to %K", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Number of closed bars back used for signals", "Trading Rules")
		.SetRange(0, 5);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss (ticks)", "Stop loss distance expressed in instrument steps", "Risk Management")
		.SetRange(0, 5000);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit (ticks)", "Take profit distance expressed in instrument steps", "Risk Management")
		.SetRange(0, 10000);

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading Rules");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading Rules");

		_enableLongExits = Param(nameof(EnableLongExits), true)
		.SetDisplay("Enable Long Exits", "Allow closing long positions", "Trading Rules");

		_enableShortExits = Param(nameof(EnableShortExits), true)
		.SetDisplay("Enable Short Exits", "Allow closing short positions", "Trading Rules");

		_mode = Param(nameof(Mode), SlowStochasticSignalMode.Twist)
		.SetDisplay("Signal Mode", "Algorithm used to generate orders", "Trading Rules");
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period of the %K line.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period of the %D line.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to %K.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Number of closed bars back used for signal calculation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in instrument steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in instrument steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool EnableLongExits
	{
		get => _enableLongExits.Value;
		set => _enableLongExits.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool EnableShortExits
	{
		get => _enableShortExits.Value;
		set => _enableShortExits.Value = value;
	}

	/// <summary>
	/// Selected signal calculation mode.
	/// </summary>
	public SlowStochasticSignalMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
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

		_kHistory.Clear();
		_dHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			KPeriod = KPeriod,
			DPeriod = DPeriod,
			Slowing = Slowing
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(_stochastic, ProcessCandle)
		.Start();

		var takeProfitUnit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Step) : null;
		var stopLossUnit = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Step) : null;

		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!indicatorValue.IsFinal)
		return;

		var stochasticValue = (StochasticOscillatorValue)indicatorValue;

		if (stochasticValue.K is not decimal kValue)
		return;

		UpdateHistory(kValue, stochasticValue.D as decimal?);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!HasRequiredHistory())
		return;

		var (openLong, openShort, closeLong, closeShort) = EvaluateSignals();

		if (closeLong && Position > 0 && EnableLongExits)
		{
			SellMarket(Position);
		}

		if (closeShort && Position < 0 && EnableShortExits)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (openLong && EnableLongEntries && Position <= 0)
		{
			var volume = Volume + Math.Max(0m, -Position);
			if (volume > 0)
			BuyMarket(volume);
		}
		else if (openShort && EnableShortEntries && Position >= 0)
		{
			var volume = Volume + Math.Max(0m, Position);
			if (volume > 0)
			SellMarket(volume);
		}
	}

	private void UpdateHistory(decimal kValue, decimal? dValue)
	{
		_kHistory.Add(kValue);
		_dHistory.Add(dValue);

		var limit = GetRequiredHistoryCount() + 2;
		TrimHistory(_kHistory, limit);
		TrimHistory(_dHistory, limit);
	}

	private static void TrimHistory<T>(IList<T> list, int limit)
	{
		while (list.Count > limit)
		{
			list.RemoveAt(0);
		}
	}

	private bool HasRequiredHistory()
	{
		var required = GetRequiredHistoryCount();
		if (_kHistory.Count < required)
		return false;

		if (Mode == SlowStochasticSignalMode.CloudTwist)
		{
			return _dHistory.Count >= required &&
			TryGetD(SignalBar, out _) &&
			TryGetD(SignalBar + 1, out _);
		}

		return true;
	}

	private int GetRequiredHistoryCount()
	{
		return Mode == SlowStochasticSignalMode.Twist ? SignalBar + 3 : SignalBar + 2;
	}

	private (bool openLong, bool openShort, bool closeLong, bool closeShort) EvaluateSignals()
	{
		var openLong = false;
		var openShort = false;
		var closeLong = false;
		var closeShort = false;

		switch (Mode)
		{
		case SlowStochasticSignalMode.Breakdown:
				{
					if (TryGetK(SignalBar, out var currentK) && TryGetK(SignalBar + 1, out var previousK))
					{
						if (previousK <= 50m && currentK > 50m)
						{
							openLong = true;
							closeShort = true;
						}

						if (previousK >= 50m && currentK < 50m)
						{
							openShort = true;
							closeLong = true;
						}
					}
					break;
			}

		case SlowStochasticSignalMode.Twist:
				{
					if (TryGetK(SignalBar, out var latestK) &&
					TryGetK(SignalBar + 1, out var prevK) &&
					TryGetK(SignalBar + 2, out var olderK))
					{
						if (prevK < olderK && latestK > prevK)
						{
							openLong = true;
							closeShort = true;
						}

						if (prevK > olderK && latestK < prevK)
						{
							openShort = true;
							closeLong = true;
						}
					}
					break;
			}

		case SlowStochasticSignalMode.CloudTwist:
				{
					if (TryGetK(SignalBar, out var kCurrent) &&
					TryGetK(SignalBar + 1, out var kPrev) &&
					TryGetD(SignalBar, out var dCurrent) &&
					TryGetD(SignalBar + 1, out var dPrev))
					{
						if (kPrev <= dPrev && kCurrent > dCurrent)
						{
							openLong = true;
							closeShort = true;
						}

						if (kPrev >= dPrev && kCurrent < dCurrent)
						{
							openShort = true;
							closeLong = true;
						}
					}
					break;
			}
		}

		if (!EnableLongEntries)
		openLong = false;

		if (!EnableShortEntries)
		openShort = false;

		if (!EnableLongExits)
		closeLong = false;

		if (!EnableShortExits)
		closeShort = false;

		return (openLong, openShort, closeLong, closeShort);
	}

	private bool TryGetK(int shift, out decimal value)
	{
		var index = _kHistory.Count - 1 - shift;
		if (index < 0 || index >= _kHistory.Count)
		{
			value = default;
			return false;
		}

		value = _kHistory[index];
		return true;
	}

	private bool TryGetD(int shift, out decimal value)
	{
		var index = _dHistory.Count - 1 - shift;
		if (index < 0 || index >= _dHistory.Count)
		{
			value = default;
			return false;
		}

		var stored = _dHistory[index];
		if (stored is not decimal result)
		{
			value = default;
			return false;
		}

		value = result;
		return true;
	}
}
