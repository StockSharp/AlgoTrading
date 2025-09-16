using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NRTR ATR Stop strategy converted from the original MetaTrader expert.
/// The strategy relies on an ATR-based trailing reversal level to determine trend changes.
/// </summary>
public class NrtrAtrStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<bool> _useTradingWindow;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _signalBarDelay;

	private ATR _atrIndicator = null!;
	private decimal? _previousUpLine;
	private decimal? _previousDownLine;
	private int _previousTrend;
	private ICandleMessage? _previousCandle;
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;
	private readonly Queue<NrtrSignal> _signalQueue = new();

	private readonly struct NrtrSignal
	{
		public NrtrSignal(decimal? upLine, decimal? downLine, bool buySignal, bool sellSignal)
		{
			UpLine = upLine;
			DownLine = downLine;
			BuySignal = buySignal;
			SellSignal = sellSignal;
		}

		public decimal? UpLine { get; }
		public decimal? DownLine { get; }
		public bool BuySignal { get; }
		public bool SellSignal { get; }
	}

	/// <summary>
	/// Volume used when opening new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on indicator signals.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on indicator signals.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Enable the trading window restriction.
	/// </summary>
	public bool UseTradingWindow
	{
		get => _useTradingWindow.Value;
		set => _useTradingWindow.Value = value;
	}

	/// <summary>
	/// Start hour for the trading window.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Start minute for the trading window.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// End hour for the trading window.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// End minute for the trading window.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
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
	/// Period of the ATR indicator.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the ATR value when building the NRTR levels.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Number of fully closed bars to delay signal execution.
	/// </summary>
	public int SignalBarDelay
	{
		get => _signalBarDelay.Value;
		set => _signalBarDelay.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults converted from MQL inputs.
	/// </summary>
	public NrtrAtrStopStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume used when opening positions", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (points)", "Stop-loss distance measured in price steps", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (points)", "Take-profit distance measured in price steps", "Risk Management");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Close Long Positions", "Allow long exits generated by the indicator", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Close Short Positions", "Allow short exits generated by the indicator", "Trading");

		_useTradingWindow = Param(nameof(UseTradingWindow), true)
		.SetDisplay("Use Trading Window", "Restrict trading to a specific intraday window", "Session");

		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Hour when trading becomes available", "Session")
		.SetOptimize(0, 23, 1);

		_startMinute = Param(nameof(StartMinute), 0)
		.SetDisplay("Start Minute", "Minute when trading becomes available", "Session")
		.SetOptimize(0, 59, 1);

		_endHour = Param(nameof(EndHour), 23)
		.SetDisplay("End Hour", "Hour when trading stops", "Session")
		.SetOptimize(0, 23, 1);

		_endMinute = Param(nameof(EndMinute), 59)
		.SetDisplay("End Minute", "Minute when trading stops", "Session")
		.SetOptimize(0, 59, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used by the NRTR ATR Stop indicator", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Number of bars used to calculate ATR", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Multiplier applied to the ATR value", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1m, 4m, 0.5m);

		_signalBarDelay = Param(nameof(SignalBarDelay), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Signal Bar", "Number of closed bars to wait before acting", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(0, 3, 1);
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

		_previousUpLine = null;
		_previousDownLine = null;
		_previousTrend = 0;
		_previousCandle = null;
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
		_signalQueue.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_atrIndicator = new ATR { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atrIndicator, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		HandleRiskManagement(candle);

		var inTradingWindow = !UseTradingWindow || IsWithinTradingWindow(candle.CloseTime);

		if (UseTradingWindow && !inTradingWindow && Position != 0)
		{
			ForceFlat();
		}

		if (!_atrIndicator.IsFormed)
		{
			_previousCandle = candle;
			return;
		}

		var nrtrSignal = CalculateNrtrSignal(candle, atrValue);
		if (nrtrSignal is null)
		return;

		_signalQueue.Enqueue(nrtrSignal.Value);

		if (_signalQueue.Count <= SignalBarDelay)
		return;

		var signalToUse = _signalQueue.Dequeue();

		if (Position > 0 && signalToUse.UpLine.HasValue)
		{
			var newStop = signalToUse.UpLine.Value;
			_longStop = _longStop.HasValue ? Math.Max(_longStop.Value, newStop) : newStop;
		}
		else if (Position < 0 && signalToUse.DownLine.HasValue)
		{
			var newStop = signalToUse.DownLine.Value;
			_shortStop = _shortStop.HasValue ? Math.Min(_shortStop.Value, newStop) : newStop;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (UseTradingWindow && !inTradingWindow)
		return;

		if (signalToUse.BuySignal)
		{
			if (SellPosClose)
			CloseShort();

			if (BuyPosOpen && Position <= 0)
			OpenLong(candle.ClosePrice, signalToUse.UpLine);
		}
		else if (signalToUse.SellSignal)
		{
			if (BuyPosClose)
			CloseLong();

			if (SellPosOpen && Position >= 0)
			OpenShort(candle.ClosePrice, signalToUse.DownLine);
		}
	}

	private NrtrSignal? CalculateNrtrSignal(ICandleMessage candle, decimal atrValue)
	{
		if (_previousCandle is null)
		{
			_previousCandle = candle;
			return null;
		}

		if (atrValue <= 0)
		{
			_previousCandle = candle;
			return null;
		}

		var prevCandle = _previousCandle;
		var rez = atrValue * AtrMultiplier;

		var trend = _previousTrend;
		var upPrev = NormalizeBuffer(_previousUpLine);
		var downPrev = NormalizeBuffer(_previousDownLine);

		if (trend <= 0)
		{
			if (downPrev is decimal downValue)
			{
				if (prevCandle.LowPrice > downValue)
				{
					upPrev = prevCandle.LowPrice - rez;
					trend = 1;
				}
			}
			else
			{
				upPrev = prevCandle.LowPrice - rez;
				trend = 1;
			}
		}

		if (trend >= 0)
		{
			if (upPrev is decimal upValue)
			{
				if (prevCandle.HighPrice < upValue)
				{
					downPrev = prevCandle.HighPrice + rez;
					trend = -1;
				}
			}
			else
			{
				downPrev = prevCandle.HighPrice + rez;
				trend = -1;
			}
		}

		decimal? currentUp = null;
		if (trend >= 0 && upPrev is decimal upLine)
		{
			currentUp = prevCandle.LowPrice > upLine + rez
			? prevCandle.LowPrice - rez
			: upLine;
		}

		decimal? currentDown = null;
		if (trend <= 0 && downPrev is decimal downLine)
		{
			currentDown = prevCandle.HighPrice < downLine - rez
			? prevCandle.HighPrice + rez
			: downLine;
		}

		var buySignal = trend > 0 && _previousTrend <= 0 && currentUp.HasValue;
		var sellSignal = trend < 0 && _previousTrend >= 0 && currentDown.HasValue;

		_previousTrend = trend;
		_previousUpLine = currentUp;
		_previousDownLine = currentDown;
		_previousCandle = candle;

		return new NrtrSignal(currentUp, currentDown, buySignal, sellSignal);
	}

	private static decimal? NormalizeBuffer(decimal? value)
	{
		if (value is null)
		return null;

		return value <= 0m ? null : value;
	}

	private void HandleRiskManagement(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				CloseLong();
			}
			else if (_longTarget.HasValue && candle.HighPrice >= _longTarget.Value)
			{
				CloseLong();
			}
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				CloseShort();
			}
			else if (_shortTarget.HasValue && candle.LowPrice <= _shortTarget.Value)
			{
				CloseShort();
			}
		}
	}

	private void OpenLong(decimal price, decimal? indicatorStop)
	{
		if (OrderVolume <= 0)
		return;

		BuyMarket(OrderVolume);

		var step = Security?.PriceStep ?? 0m;

		decimal? manualStop = null;
		if (step > 0m && StopLossPoints > 0m)
		manualStop = price - StopLossPoints * step;

		if (indicatorStop.HasValue && manualStop.HasValue)
		_longStop = Math.Max(indicatorStop.Value, manualStop.Value);
		else
		_longStop = indicatorStop ?? manualStop;

		if (step > 0m && TakeProfitPoints > 0m)
		_longTarget = price + TakeProfitPoints * step;
		else
		_longTarget = null;

		_shortStop = null;
		_shortTarget = null;
	}

	private void OpenShort(decimal price, decimal? indicatorStop)
	{
		if (OrderVolume <= 0)
		return;

		SellMarket(OrderVolume);

		var step = Security?.PriceStep ?? 0m;

		decimal? manualStop = null;
		if (step > 0m && StopLossPoints > 0m)
		manualStop = price + StopLossPoints * step;

		if (indicatorStop.HasValue && manualStop.HasValue)
		_shortStop = Math.Min(indicatorStop.Value, manualStop.Value);
		else
		_shortStop = indicatorStop ?? manualStop;

		if (step > 0m && TakeProfitPoints > 0m)
		_shortTarget = price - TakeProfitPoints * step;
		else
		_shortTarget = null;

		_longStop = null;
		_longTarget = null;
	}

	private void CloseLong()
	{
		if (Position <= 0)
		return;

		SellMarket(Math.Abs(Position));
		_longStop = null;
		_longTarget = null;
	}

	private void CloseShort()
	{
		if (Position >= 0)
		return;

		BuyMarket(Math.Abs(Position));
		_shortStop = null;
		_shortTarget = null;
	}

	private void ForceFlat()
	{
		if (Position > 0)
		CloseLong();
		else if (Position < 0)
		CloseShort();
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var current = time.LocalDateTime;
		var hour = current.Hour;
		var minute = current.Minute;

		if (StartHour < EndHour)
		{
			if (hour == StartHour && minute >= StartMinute)
			return true;
			if (hour > StartHour && hour < EndHour)
			return true;
			if (hour > StartHour && hour == EndHour && minute < EndMinute)
			return true;
		}
		else if (StartHour == EndHour)
		{
			if (hour == StartHour && minute >= StartMinute && minute < EndMinute)
			return true;
		}
		else
		{
			if (hour > StartHour || (hour == StartHour && minute >= StartMinute))
			return true;
			if (hour < EndHour)
			return true;
			if (hour == EndHour && minute < EndMinute)
			return true;
		}

		return false;
	}
}
