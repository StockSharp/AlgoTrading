using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the T3MA-ALARM expert logic.
/// The algorithm trades when a double-smoothed EMA changes its slope direction.
/// </summary>
public class T3MaDirectionChangeStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<int> _signalBarOffset;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage? _emaPrice;
	private ExponentialMovingAverage? _emaSmooth;
	private readonly Queue<decimal> _recentSmoothed = new();
	private readonly Queue<SignalInfo> _pendingSignals = new();
	private int _previousDirection;

	/// <summary>
	/// Length of the EMA used for both smoothing passes.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Number of bars by which the smoothed series is shifted.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Number of finished candles to delay signal execution.
	/// </summary>
	public int SignalBarOffset
	{
		get => _signalBarOffset.Value;
		set => _signalBarOffset.Value = value;
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
	/// Take profit distance measured in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Base order volume used when opening a new position.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public T3MaDirectionChangeStrategy()
	{
		_maLength = Param(nameof(MaLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length of the EMA used for the double smoothing", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 2);

		_maShift = Param(nameof(MaShift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("EMA Shift", "Shift applied to the smoothed EMA when evaluating slope changes", "Indicator");

		_signalBarOffset = Param(nameof(SignalBarOffset), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Delay", "How many completed candles to wait before acting on a signal", "Trading rules");

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (steps)", "Stop loss distance expressed in price steps", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 60m, 10m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 125m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (steps)", "Take profit distance expressed in price steps", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 25m);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base volume used for entries", "Trading rules");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
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

		_emaPrice = null;
		_emaSmooth = null;
		_recentSmoothed.Clear();
		_pendingSignals.Clear();
		_previousDirection = 0;

		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		// Create EMA instances used for the double smoothing chain.
		_emaPrice = new ExponentialMovingAverage { Length = MaLength };
		_emaSmooth = new ExponentialMovingAverage { Length = MaLength };

		// Subscribe to candle updates and process them sequentially.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Optional visualization: candles and executed trades.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		// Initialize protective orders based on configured distances.
		StartProtection(
			takeProfit: TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Step) : null,
			stopLoss: StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Step) : null);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_emaPrice is null || _emaSmooth is null)
			return;

		// Run the two-stage EMA smoothing chain.
		var emaPriceValue = _emaPrice.Process(candle.ClosePrice);
		var emaSmoothValue = _emaSmooth.Process(emaPriceValue);

		var smoothed = emaSmoothValue.ToDecimal();
		_recentSmoothed.Enqueue(smoothed);

		var shift = MaShift;
		var requiredCount = shift + 2;
		while (_recentSmoothed.Count > requiredCount)
		{
			_recentSmoothed.Dequeue();
		}

		if (!_emaSmooth.IsFormed || _recentSmoothed.Count < requiredCount)
		{
			EnqueueSignal(new SignalInfo(0, candle.OpenTime, candle.ClosePrice));
			return;
		}

		var snapshot = _recentSmoothed.ToArray();
		var current = snapshot[^1 - shift];
		var previous = snapshot[^2 - shift];

		var direction = _previousDirection;
		if (current > previous)
		{
			direction = 1;
		}
		else if (current < previous)
		{
			direction = -1;
		}

		var signal = 0;
		if (_previousDirection == -1 && direction == 1)
		{
			signal = 1;
		}
		else if (_previousDirection == 1 && direction == -1)
		{
			signal = -1;
		}

		_previousDirection = direction;

		var referencePrice = signal > 0
			? candle.LowPrice
			: signal < 0
				? candle.HighPrice
				: candle.ClosePrice;

		EnqueueSignal(new SignalInfo(signal, candle.OpenTime, referencePrice));
	}

	private void EnqueueSignal(SignalInfo signal)
	{
		// Store the signal so that it can be executed after the configured delay.
		_pendingSignals.Enqueue(signal);

		while (_pendingSignals.Count > SignalBarOffset)
		{
			var readySignal = _pendingSignals.Dequeue();
			ExecuteSignal(readySignal);
		}
	}

	private void ExecuteSignal(SignalInfo signal)
	{
		if (signal.Direction == 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var baseVolume = Volume;
		var reversalVolume = Math.Abs(Position) + baseVolume;

		if (signal.Direction > 0)
		{
			// Direction flipped upward: switch to a long position.
			if (Position < 0)
			{
				BuyMarket(reversalVolume);
				LogInfo($"Reversed short into long after upward slope change at {signal.Time:O}. Reference price: {signal.ReferencePrice}");
			}
			else if (Position == 0)
			{
				BuyMarket(baseVolume);
				LogInfo($"Entered long after upward slope change at {signal.Time:O}. Reference price: {signal.ReferencePrice}");
			}
		}
		else
		{
			// Direction flipped downward: switch to a short position.
			if (Position > 0)
			{
				SellMarket(reversalVolume);
				LogInfo($"Reversed long into short after downward slope change at {signal.Time:O}. Reference price: {signal.ReferencePrice}");
			}
			else if (Position == 0)
			{
				SellMarket(baseVolume);
				LogInfo($"Entered short after downward slope change at {signal.Time:O}. Reference price: {signal.ReferencePrice}");
			}
		}
	}

	private readonly record struct SignalInfo(int Direction, DateTimeOffset Time, decimal ReferencePrice);
}
