using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "_HPCS_PosNegDIsCrossOver_Mt4_EA_V01_WE".
/// Trades +DI/-DI crossovers of the ADX indicator and applies a martingale re-entry loop after losing trades.
/// </summary>
public class PosNegDiCrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _stopTime;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<int> _martingaleCycleLimit;

	private decimal _previousPlusDi;
	private decimal _previousMinusDi;
	private bool _diInitialized;

	private bool _cycleActive;
	private Sides? _cycleSide;
	private decimal _currentVolume;
	private int _currentCycle;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	private bool _awaitingCycleResolution;
	private bool _lastExitWasLoss;

	private DateTimeOffset? _lastSignalTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="PosNegDiCrossoverStrategy"/> class.
	/// </summary>
	public PosNegDiCrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Length of the Average Directional Index", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 50, 1);

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Restrict entries to a daily time window", "Schedule");

		_startTime = Param(nameof(StartTime), new TimeSpan(0, 0, 0))
			.SetDisplay("Start Time", "Daily time when trading becomes available", "Schedule");

		_stopTime = Param(nameof(StopTime), new TimeSpan(23, 59, 0))
			.SetDisplay("Stop Time", "Daily time after which new entries are blocked", "Schedule");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Baseline market order volume", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
			.SetNotNegative()
			.SetDisplay("Take-Profit (pips)", "Distance to the profit target expressed in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 10m)
			.SetNotNegative()
			.SetDisplay("Stop-Loss (pips)", "Distance to the protective stop expressed in pips", "Risk");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Multiplier", "Volume multiplier applied after a loss", "Money Management");

		_martingaleCycleLimit = Param(nameof(MartingaleCycleLimit), 5)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Cycle Limit", "Maximum number of martingale steps per signal", "Money Management");
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the Average Directional Index indicator.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Enable or disable the trading time window.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Daily start time of the trading window.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Daily end time of the trading window.
	/// </summary>
	public TimeSpan StopTime
	{
		get => _stopTime.Value;
		set => _stopTime.Value = value;
	}

	/// <summary>
	/// Base market order volume used to open a new cycle.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied after a losing trade.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum number of martingale steps executed per signal.
	/// </summary>
	public int MartingaleCycleLimit
	{
		get => _martingaleCycleLimit.Value;
		set => _martingaleCycleLimit.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_diInitialized = false;
		_previousPlusDi = 0m;
		_previousMinusDi = 0m;

		ResetCycle();
		_lastSignalTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetCycle();

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		HandleOpenPosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		var value = (AverageDirectionalIndexValue)adxValue;
		if (value.Dx.Plus is not decimal plusDi || value.Dx.Minus is not decimal minusDi)
		{
			return;
		}

		if (!_diInitialized)
		{
			_previousPlusDi = plusDi;
			_previousMinusDi = minusDi;
			_diInitialized = true;
			return;
		}

		var bullishCross = plusDi > minusDi && _previousPlusDi <= _previousMinusDi;
		var bearishCross = plusDi < minusDi && _previousPlusDi >= _previousMinusDi;

		var time = candle.CloseTime;
		var withinWindow = !UseTimeFilter || IsWithinTradingWindow(time.TimeOfDay);

		if (withinWindow && !_cycleActive && !_awaitingCycleResolution)
		{
			if (bullishCross && Position <= 0m && !IsSameSignalBar(candle.OpenTime))
			{
				StartNewCycle(Sides.Buy);
				_lastSignalTime = candle.OpenTime;
			}
			else if (bearishCross && Position >= 0m && !IsSameSignalBar(candle.OpenTime))
			{
				StartNewCycle(Sides.Sell);
				_lastSignalTime = candle.OpenTime;
			}
		}

		_previousPlusDi = plusDi;
		_previousMinusDi = minusDi;
	}

	private void HandleOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_awaitingCycleResolution)
			{
				return;
			}

			var exitVolume = Math.Abs(Position);

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				// Long stop-loss reached inside the finished bar range.
				ExecuteExit(Sides.Sell, exitVolume, true);
				return;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				// Long take-profit reached.
				ExecuteExit(Sides.Sell, exitVolume, false);
			}
		}
		else if (Position < 0m)
		{
			if (_awaitingCycleResolution)
			{
				return;
			}

			var exitVolume = Math.Abs(Position);

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				// Short stop-loss reached inside the finished bar range.
				ExecuteExit(Sides.Buy, exitVolume, true);
				return;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				// Short take-profit reached.
				ExecuteExit(Sides.Buy, exitVolume, false);
			}
		}
	}

	private void ExecuteExit(Sides exitSide, decimal volume, bool isLoss)
	{
		if (volume <= 0m)
		{
			return;
		}

		if (exitSide == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_stopPrice = null;
		_takePrice = null;
		_entryPrice = null;

		_awaitingCycleResolution = true;
		_lastExitWasLoss = isLoss;
	}

	private void StartNewCycle(Sides side)
	{
		var volume = OrderVolume;
		if (volume <= 0m)
		{
			return;
		}

		_cycleActive = true;
		_cycleSide = side;
		_currentCycle = 1;
		_currentVolume = volume;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_awaitingCycleResolution = false;
		_lastExitWasLoss = false;

		if (side == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}
	}

	private void ContinueMartingale()
	{
		if (_cycleSide is not Sides side)
		{
			ResetCycle();
			return;
		}

		var volume = _currentVolume;
		if (volume <= 0m)
		{
			ResetCycle();
			return;
		}

		if (UseTimeFilter && !IsWithinTradingWindow(CurrentTime.TimeOfDay))
		{
			ResetCycle();
			return;
		}

		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_awaitingCycleResolution = false;
		_lastExitWasLoss = false;

		if (side == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order.Security != Security)
		{
			return;
		}

		if (_cycleSide is not Sides side)
		{
			return;
		}

		var direction = trade.Order.Side;
		if ((side == Sides.Buy && direction != Sides.Buy) || (side == Sides.Sell && direction != Sides.Sell))
		{
			return;
		}

		// Store the most recent entry price to recalculate protective levels.
		_entryPrice = trade.Order.AveragePrice ?? trade.Trade.Price;
		UpdateProtectionLevels();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position != 0m)
		{
			return;
		}

		if (_awaitingCycleResolution)
		{
			if (_lastExitWasLoss && _cycleActive && _currentCycle < MartingaleCycleLimit)
			{
				_currentCycle++;
				_currentVolume *= MartingaleMultiplier;
				ContinueMartingale();
			}
			else
			{
				ResetCycle();
			}

			_awaitingCycleResolution = false;
			_lastExitWasLoss = false;
		}
		else if (_cycleActive)
		{
			// Position was closed externally; stop the martingale loop.
			ResetCycle();
		}
	}

	private void UpdateProtectionLevels()
	{
		if (_entryPrice is not decimal entry || _cycleSide is not Sides side)
		{
			return;
		}

		var pip = GetPipSize();
		if (pip <= 0m)
		{
			return;
		}

		_stopPrice = StopLossPips > 0m
			? side == Sides.Buy ? entry - StopLossPips * pip : entry + StopLossPips * pip
			: null;

		_takePrice = TakeProfitPips > 0m
			? side == Sides.Buy ? entry + TakeProfitPips * pip : entry - TakeProfitPips * pip
			: null;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
		{
			return 0.0001m;
		}

		var step = security.PriceStep ?? 0.0001m;
		if (step <= 0m)
		{
			step = 0.0001m;
		}

		var decimals = security.Decimals;
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private bool IsWithinTradingWindow(TimeSpan timeOfDay)
	{
		var start = StartTime;
		var stop = StopTime;

		if (start == stop)
		{
			return true;
		}

		return start <= stop
			? timeOfDay >= start && timeOfDay <= stop
			: timeOfDay >= start || timeOfDay <= stop;
	}

	private bool IsSameSignalBar(DateTimeOffset candleOpenTime)
	{
		return _lastSignalTime != null && _lastSignalTime.Value == candleOpenTime;
	}

	private void ResetCycle()
	{
		_cycleActive = false;
		_cycleSide = null;
		_currentVolume = OrderVolume;
		_currentCycle = 0;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_awaitingCycleResolution = false;
		_lastExitWasLoss = false;
	}
}

