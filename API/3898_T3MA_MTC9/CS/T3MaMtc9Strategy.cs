using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reproduces the T3MA(MTC) MetaTrader expert.
/// It trades on slope reversals of a double-smoothed exponential moving average.
/// </summary>
public class T3MaMtc9Strategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<int> _calculationBarOffset;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _allowMultiplePositions;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage? _emaPrimary;
	private ExponentialMovingAverage? _emaSmooth;
	private readonly Queue<decimal> _smoothedHistory = new();
	private readonly Queue<SignalInfo> _pendingSignals = new();
	private int _previousDirection;
	private decimal? _lastSignalMarker;

	/// <summary>
	/// Length of both exponential moving averages used for smoothing.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// How many bars to shift the smoothed series when checking slope changes.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Number of completed candles to delay order execution.
	/// </summary>
	public int CalculationBarOffset
	{
		get => _calculationBarOffset.Value;
		set => _calculationBarOffset.Value = value;
	}

	/// <summary>
	/// Base trading volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Whether the strategy should attach a stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Whether the strategy should attach a take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allow adding new positions even if an opposite one already exists.
	/// </summary>
	public bool AllowMultiplePositions
	{
		get => _allowMultiplePositions.Value;
		set => _allowMultiplePositions.Value = value;
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
	/// Configure parameters matching the MetaTrader inputs.
	/// </summary>
	public T3MaMtc9Strategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period of the exponential moving averages", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 30, 1);

		_maShift = Param(nameof(MaShift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("EMA Shift", "Shift applied to the smoothed series when evaluating slope direction", "Indicator");

		_calculationBarOffset = Param(nameof(CalculationBarOffset), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Delay", "How many finished candles to wait before acting on a signal", "Trading rules");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume expressed in lots", "Trading rules");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Attach a stop loss when opening new positions", "Risk management");

		_stopLossPoints = Param(nameof(StopLossPoints), 40m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (steps)", "Stop loss distance in price steps", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 120m, 10m);

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Attach a take profit when opening new positions", "Risk management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 11m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (steps)", "Take profit distance in price steps", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 5m);

		_allowMultiplePositions = Param(nameof(AllowMultiplePositions), true)
			.SetDisplay("Allow Multiple Positions", "Permit new trades even if an opposite position exists", "Trading rules");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General");
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

		_emaPrimary = null;
		_emaSmooth = null;
		_smoothedHistory.Clear();
		_pendingSignals.Clear();
		_previousDirection = 0;
		_lastSignalMarker = null;
		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_emaPrimary = new ExponentialMovingAverage { Length = MaPeriod };
		_emaSmooth = new ExponentialMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var takeProfit = UseTakeProfit && TakeProfitPoints > 0m
			? new Unit(TakeProfitPoints, UnitTypes.Step)
			: null;

		var stopLoss = UseStopLoss && StopLossPoints > 0m
			? new Unit(StopLossPoints, UnitTypes.Step)
			: null;

		StartProtection(takeProfit, stopLoss);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_emaPrimary is null || _emaSmooth is null)
			return;

		var emaPrimaryValue = _emaPrimary.Process(candle.ClosePrice);
		var emaSmoothValue = _emaSmooth.Process(emaPrimaryValue);
		var smoothed = emaSmoothValue.ToDecimal();

		_smoothedHistory.Enqueue(smoothed);

		var requiredHistory = MaShift + 2;
		while (_smoothedHistory.Count > requiredHistory)
		{
			_smoothedHistory.Dequeue();
		}

		if (!_emaSmooth.IsFormed || _smoothedHistory.Count < requiredHistory)
		{
			EnqueueSignal(0, candle.ClosePrice, candle.OpenTime);
			return;
		}

		var snapshot = _smoothedHistory.ToArray();
		var current = snapshot[^1 - MaShift];
		var previous = snapshot[^2 - MaShift];

		var direction = _previousDirection;
		if (current > previous)
		{
			direction = 1;
		}
		else if (current < previous)
		{
			direction = -1;
		}

		var signalDirection = 0;
		if (_previousDirection == -1 && direction == 1)
		{
			signalDirection = 1;
		}
		else if (_previousDirection == 1 && direction == -1)
		{
			signalDirection = -1;
		}

		_previousDirection = direction;

		var markerPrice = signalDirection > 0
			? candle.LowPrice
			: signalDirection < 0
				? candle.HighPrice
				: candle.ClosePrice;

		EnqueueSignal(signalDirection, markerPrice, candle.OpenTime);
	}

	private void EnqueueSignal(int direction, decimal markerPrice, DateTimeOffset time)
	{
		_pendingSignals.Enqueue(new SignalInfo(direction, markerPrice, time));

		while (_pendingSignals.Count > CalculationBarOffset)
		{
			var ready = _pendingSignals.Dequeue();
			ExecuteSignal(ready);
		}
	}

	private void ExecuteSignal(SignalInfo signal)
	{
		if (signal.Direction == 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!AllowMultiplePositions && Position != 0)
			return;

		var marker = signal.MarkerPrice;
		if (_lastSignalMarker.HasValue && ArePricesEqual(marker, _lastSignalMarker.Value))
			return;

		var volume = TradeVolume;

		if (signal.Direction > 0)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_lastSignalMarker = marker;
	}

	private static bool ArePricesEqual(decimal left, decimal right)
	{
		return Math.Abs(left - right) < 1e-8m;
	}

	private readonly record struct SignalInfo(int Direction, decimal MarkerPrice, DateTimeOffset Time);
}
