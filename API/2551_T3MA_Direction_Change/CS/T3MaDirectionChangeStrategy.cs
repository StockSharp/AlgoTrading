namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that trades when a double-smoothed EMA changes its slope direction.
/// </summary>
public class T3MaDirectionChangeStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<int> _signalBarOffset;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _recentSmoothed = new();
	private readonly Queue<SignalInfo> _pendingSignals = new();
	private ExponentialMovingAverage _emaPrice;
	private ExponentialMovingAverage _emaSmooth;
	private int _previousDirection;
	private int _cooldownRemaining;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int MaShift { get => _maShift.Value; set => _maShift.Value = value; }
	public int SignalBarOffset { get => _signalBarOffset.Value; set => _signalBarOffset.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public T3MaDirectionChangeStrategy()
	{
		_maLength = Param(nameof(MaLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length of the EMA used for the double smoothing", "Indicator");

		_maShift = Param(nameof(MaShift), 0)
			.SetNotNegative()
			.SetDisplay("EMA Shift", "Shift applied to the smoothed EMA when evaluating slope changes", "Indicator");

		_signalBarOffset = Param(nameof(SignalBarOffset), 1)
			.SetNotNegative()
			.SetDisplay("Signal Delay", "How many completed candles to wait before acting on a signal", "Trading rules");

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (steps)", "Stop loss distance expressed in price steps", "Risk management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 125m)
			.SetNotNegative()
			.SetDisplay("Take Profit (steps)", "Take profit distance expressed in price steps", "Risk management");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base volume used for entries", "Trading rules");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait after entries and exits", "Trading rules");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		_cooldownRemaining = 0;
		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		Volume = TradeVolume;
		_emaPrice = new EMA { Length = MaLength };
		_emaSmooth = new EMA { Length = MaLength };
		_recentSmoothed.Clear();
		_pendingSignals.Clear();
		_previousDirection = 0;
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		var slUnit = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Absolute) : null;
		var tpUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null;
		StartProtection(slUnit, tpUnit);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var emaPriceValue = _emaPrice.Process(new DecimalIndicatorValue(_emaPrice, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		var emaSmoothValue = _emaSmooth.Process(emaPriceValue);
		if (!emaSmoothValue.IsFormed)
			return;

		AddSmoothedValue(emaSmoothValue.ToDecimal(), MaShift + 2);
		if (_recentSmoothed.Count < MaShift + 2)
		{
			EnqueueSignal(new SignalInfo(0));
			return;
		}

		var currentIndex = _recentSmoothed.Count - 1 - MaShift;
		var previousIndex = _recentSmoothed.Count - 2 - MaShift;
		var current = _recentSmoothed[currentIndex];
		var previous = _recentSmoothed[previousIndex];
		var direction = _previousDirection;

		if (current > previous)
			direction = 1;
		else if (current < previous)
			direction = -1;

		var signal = 0;
		if (_previousDirection == -1 && direction == 1)
			signal = 1;
		else if (_previousDirection == 1 && direction == -1)
			signal = -1;

		_previousDirection = direction;
		EnqueueSignal(new SignalInfo(signal));
	}

	private void AddSmoothedValue(decimal value, int limit)
	{
		_recentSmoothed.Add(value);
		if (_recentSmoothed.Count > limit)
			_recentSmoothed.RemoveAt(0);
	}

	private void EnqueueSignal(SignalInfo signal)
	{
		_pendingSignals.Enqueue(signal);

		while (_pendingSignals.Count > SignalBarOffset)
		{
			var readySignal = _pendingSignals.Dequeue();
			ExecuteSignal(readySignal);
		}
	}

	private void ExecuteSignal(SignalInfo signal)
	{
		if (signal.Direction == 0 || _cooldownRemaining > 0)
			return;

		if (signal.Direction > 0 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (signal.Direction < 0 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldownRemaining = SignalCooldownBars;
		}
	}

	private readonly record struct SignalInfo(int Direction);
}
