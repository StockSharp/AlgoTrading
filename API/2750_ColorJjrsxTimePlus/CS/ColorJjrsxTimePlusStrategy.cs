using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy inspired by the Color JJRSX TM Plus Expert Advisor.
/// Uses a smoothed RSI oscillator to detect slope reversals and optional time-based exits.
/// </summary>
public class ColorJjrsxTimePlusStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExit;
	private readonly StrategyParam<bool> _enableSellExit;
	private readonly StrategyParam<bool> _enableTimeExit;
	private readonly StrategyParam<int> _holdingMinutes;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private readonly Queue<decimal> _smoothedValues = new();

	private RelativeStrengthIndex? _rsi;
	private JurikMovingAverage? _smoother;
	private DateTimeOffset? _entryTime;

	/// <summary>
	/// Candle type used for generating signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI length before Jurik smoothing.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Length of the Jurik moving average.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Number of completed candles to shift before calculating signals.
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Enable or disable long entries.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enable or disable short entries.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on oscillator downturns.
	/// </summary>
	public bool EnableBuyExit
	{
		get => _enableBuyExit.Value;
		set => _enableBuyExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on oscillator upturns.
	/// </summary>
	public bool EnableSellExit
	{
		get => _enableSellExit.Value;
		set => _enableSellExit.Value = value;
	}

	/// <summary>
	/// Enable the maximum holding time exit.
	/// </summary>
	public bool EnableTimeExit
	{
		get => _enableTimeExit.Value;
		set => _enableTimeExit.Value = value;
	}

	/// <summary>
	/// Maximum minutes to keep an open position.
	/// </summary>
	public int HoldingMinutes
	{
		get => _holdingMinutes.Value;
		set => _holdingMinutes.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="ColorJjrsxTimePlusStrategy"/>.
	/// </summary>
	public ColorJjrsxTimePlusStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General");

		_rsiLength = Param(nameof(RsiLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period for the RSI calculation", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(4, 20, 1);

		_smoothingLength = Param(nameof(SmoothingLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Jurik moving average length", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_signalShift = Param(nameof(SignalShift), 1)
			.SetDisplay("Signal Shift", "Completed candles to skip before evaluating signals", "Indicator");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Execution");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Execution");

		_enableBuyExit = Param(nameof(EnableBuyExit), true)
			.SetDisplay("Exit Long on Downturn", "Close longs when the oscillator turns down", "Execution");

		_enableSellExit = Param(nameof(EnableSellExit), true)
			.SetDisplay("Exit Short on Upturn", "Close shorts when the oscillator turns up", "Execution");

		_enableTimeExit = Param(nameof(EnableTimeExit), true)
			.SetDisplay("Enable Time Exit", "Close positions after the holding period expires", "Risk");

		_holdingMinutes = Param(nameof(HoldingMinutes), 240)
			.SetGreaterThanZero()
			.SetDisplay("Holding Minutes", "Maximum time in minutes to keep a position", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(60, 720, 60);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop Loss (pts)", "Stop loss distance expressed in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetDisplay("Take Profit (pts)", "Take profit distance expressed in price steps", "Risk");
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

		_smoothedValues.Clear();
		_entryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiLength
		};

		_smoother = new JurikMovingAverage
		{
			Length = SmoothingLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.PriceStep) : null,
			takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.PriceStep) : null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_smoother is null)
			return;

		HandleTimeExit(candle.CloseTime);

		var smoothValue = _smoother.Process(new DecimalIndicatorValue(_smoother, rsiValue, candle.CloseTime));

		if (!smoothValue.IsFinal || smoothValue is not DecimalIndicatorValue smoothDecimal)
			return;

		_smoothedValues.Enqueue(smoothDecimal.Value);

		var required = SignalShift + 3;

		if (_smoothedValues.Count < required)
			return;

		while (_smoothedValues.Count > required)
		{
			_smoothedValues.Dequeue();
		}

		var values = _smoothedValues.ToArray();

		var currentIndex = values.Length - SignalShift - 1;
		var previousIndex = values.Length - SignalShift - 2;
		var olderIndex = values.Length - SignalShift - 3;

		if (currentIndex < 0 || previousIndex < 0 || olderIndex < 0)
			return;

		var current = values[currentIndex];
		var previous = values[previousIndex];
		var older = values[olderIndex];

		var slopeUp = previous < older;
		var slopeDown = previous > older;

		if (EnableSellExit && slopeUp && Position < 0)
		{
			BuyMarket();
			_entryTime = null;
		}

		if (EnableBuyExit && slopeDown && Position > 0)
		{
			SellMarket();
			_entryTime = null;
		}

		if (EnableBuyEntries && slopeUp && current > previous && Position <= 0)
		{
			BuyMarket();
			_entryTime = candle.CloseTime;
		}
		else if (EnableSellEntries && slopeDown && current < previous && Position >= 0)
		{
			SellMarket();
			_entryTime = candle.CloseTime;
		}
	}

	private void HandleTimeExit(DateTimeOffset candleTime)
	{
		if (!EnableTimeExit || Position == 0 || _entryTime is null)
			return;

		var minutesInPosition = (candleTime - _entryTime.Value).TotalMinutes;

		if (minutesInPosition < HoldingMinutes)
			return;

		if (Position > 0)
		{
			SellMarket();
		}
		else if (Position < 0)
		{
			BuyMarket();
		}

		_entryTime = null;
	}
}
