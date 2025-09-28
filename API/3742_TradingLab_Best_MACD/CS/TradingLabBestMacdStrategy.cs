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

public class TradingLabBestMacdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _signalValidity;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _boxPeriod;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _stopDistancePoints;
	private readonly StrategyParam<decimal> _riskRewardMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private int _resistanceCounter;
	private int _supportCounter;
	private int _macdDownCounter;
	private int _macdUpCounter;

	private decimal? _prevMacdMain;
	private decimal? _prevMacdSignal;

	private decimal? _plannedStop;
	private decimal? _plannedTake;
	private Sides? _plannedSide;

	private decimal? _activeStop;
	private decimal? _activeTake;
	private Sides? _activeSide;

	private decimal? _previousHigh;
	private decimal? _previousLow;
	private bool _hasPreviousCandle;

	/// <summary>
	/// Initializes a new instance of <see cref="TradingLabBestMacdStrategy"/>.
	/// </summary>
	public TradingLabBestMacdStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
			.SetCanOptimize(true);

		_signalValidity = Param(nameof(SignalValidity), 7)
			.SetGreaterThanZero()
			.SetDisplay("Signal Validity", "Number of candles a MACD or box trigger remains active", "Filters")
			.SetCanOptimize(true);

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Simple moving average period used as the trend filter", "Filters")
			.SetCanOptimize(true);

		_boxPeriod = Param(nameof(BoxPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Box Period", "Lookback length for the support/resistance box", "Filters")
			.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Length", "Fast EMA length for MACD", "Indicators")
			.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Length", "Slow EMA length for MACD", "Indicators")
			.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Length", "Signal line length for MACD", "Indicators")
			.SetCanOptimize(true);

		_stopDistancePoints = Param(nameof(StopDistancePoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Distance (points)", "Protective stop distance from the moving average expressed in points", "Risk")
			.SetCanOptimize(true);

		_riskRewardMultiplier = Param(nameof(RiskRewardMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Risk-Reward Multiplier", "Multiplier applied to derive the take-profit distance", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Data type used to subscribe for candles", "General")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Fixed volume sent with every market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Number of candles that keep MACD and support/resistance triggers active.
	/// </summary>
	public int SignalValidity
	{
		get => _signalValidity.Value;
		set => _signalValidity.Value = value;
	}

	/// <summary>
	/// Period for the simple moving average trend filter.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Lookback length for the support/resistance box.
	/// </summary>
	public int BoxPeriod
	{
		get => _boxPeriod.Value;
		set => _boxPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA length used by MACD.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length used by MACD.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal line length used by MACD.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Protective stop distance measured in MetaTrader points.
	/// </summary>
	public decimal StopDistancePoints
	{
		get => _stopDistancePoints.Value;
		set => _stopDistancePoints.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the adjusted moving-average distance to build the take-profit target.
	/// </summary>
	public decimal RiskRewardMultiplier
	{
		get => _riskRewardMultiplier.Value;
		set => _riskRewardMultiplier.Value = value;
	}

	/// <summary>
	/// Candle data type used to subscribe for historical bars.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Configure the indicators that replicate the MetaTrader calculations.
		_sma = new SimpleMovingAverage { Length = MaLength };
		_highest = new Highest { Length = BoxPeriod };
		_lowest = new Lowest { Length = BoxPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Fast = MacdFastLength,
			Slow = MacdSlowLength,
			Signal = MacdSignalLength
		};

		// Subscribe to the configured candle stream and bind indicator outputs to the handler.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { _sma, _highest, _lowest, _macd }, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage trailing exits before analysing new signals.
		CheckProtectiveLevels(candle);

		if (values.Length != 4 || !values[0].IsFinal || !values[1].IsFinal || !values[2].IsFinal || !values[3].IsFinal)
		{
			// Wait until every indicator provides a confirmed value.
			UpdatePreviousCandle(candle, null, null);
			return;
		}

		var smaValue = values[0].ToDecimal();
		var resistanceValue = values[1].ToDecimal();
		var supportValue = values[2].ToDecimal();
		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)values[3];
		var macdMain = macdValue.Macd;
		var macdSignal = macdValue.Signal;

		if (!IsFormedAndOnlineAndAllowTrading() || !_sma.IsFormed || !_highest.IsFormed || !_lowest.IsFormed || !_macd.IsFormed)
		{
			UpdatePreviousCandle(candle, macdMain, macdSignal);
			return;
		}

		// Decrease counters that track how many candles each signal remains active.
		if (_resistanceCounter > 0)
			_resistanceCounter--;
		if (_supportCounter > 0)
			_supportCounter--;
		if (_macdDownCounter > 0)
			_macdDownCounter--;
		if (_macdUpCounter > 0)
			_macdUpCounter--;

		var point = GetPointValue();
		var triggeredResistance = false;
		var triggeredSupport = false;

		if (_hasPreviousCandle)
		{
			// Detect fresh touches of the synthetic resistance/support levels.
			if (_previousHigh.HasValue && resistanceValue > 0m && _previousHigh.Value > resistanceValue)
			{
				_resistanceCounter = SignalValidity;
				triggeredResistance = true;
			}

			if (_previousLow.HasValue && supportValue > 0m && _previousLow.Value < supportValue)
			{
				_supportCounter = SignalValidity;
				triggeredSupport = true;
			}
		}

		if (_prevMacdMain.HasValue && _prevMacdSignal.HasValue)
		{
			// Track MACD crossovers relative to the zero line.
			if (macdMain < macdSignal && _prevMacdMain.Value > _prevMacdSignal.Value && macdMain > 0m)
			{
				_macdDownCounter = SignalValidity;
			}

			if (macdMain > macdSignal && _prevMacdMain.Value < _prevMacdSignal.Value && macdMain < 0m)
			{
				_macdUpCounter = SignalValidity;
			}
		}

		var volume = OrderVolume;

		if (volume > 0m)
		{
			// Evaluate entry conditions once both the MACD and box counters are armed.
			var longSignalActive = _macdUpCounter > 0 && _supportCounter > 0 && candle.ClosePrice > smaValue;
			var longTriggeredNow = triggeredSupport || (_macdUpCounter == SignalValidity);
			if (longSignalActive && longTriggeredNow && Position <= 0)
			{
				var stopOffset = StopDistancePoints * point;
				var adjustedDistance = candle.ClosePrice - smaValue + stopOffset;
				if (adjustedDistance > 0m)
				{
					_plannedStop = smaValue - stopOffset;
					_plannedTake = candle.ClosePrice + adjustedDistance * RiskRewardMultiplier;
					_plannedSide = Sides.Buy;
					BuyMarket(volume);
				}
			}

			var shortSignalActive = _macdDownCounter > 0 && _resistanceCounter > 0 && candle.ClosePrice < smaValue;
			var shortTriggeredNow = triggeredResistance || (_macdDownCounter == SignalValidity);
			if (shortSignalActive && shortTriggeredNow && Position >= 0)
			{
				var stopOffset = StopDistancePoints * point;
				var adjustedDistance = smaValue - candle.ClosePrice + stopOffset;
				if (adjustedDistance > 0m)
				{
					_plannedStop = smaValue + stopOffset;
					_plannedTake = candle.ClosePrice - adjustedDistance * RiskRewardMultiplier;
					_plannedSide = Sides.Sell;
					SellMarket(volume);
				}
			}
		}

		UpdatePreviousCandle(candle, macdMain, macdSignal);
	}

	private void CheckProtectiveLevels(ICandleMessage candle)
	{
		if (_activeSide == null || Position == 0)
			return;

		// Close the position if price violates the stored stop-loss or take-profit.
		if (_activeSide == Sides.Buy && Position > 0)
		{
			if (_activeStop.HasValue && candle.LowPrice <= _activeStop.Value)
			{
				ClearPlannedLevels();
				ClearActiveLevels();
				ClosePosition();
				return;
			}

			if (_activeTake.HasValue && candle.HighPrice >= _activeTake.Value)
			{
				ClearPlannedLevels();
				ClearActiveLevels();
				ClosePosition();
				return;
			}
		}
		else if (_activeSide == Sides.Sell && Position < 0)
		{
			if (_activeStop.HasValue && candle.HighPrice >= _activeStop.Value)
			{
				ClearPlannedLevels();
				ClearActiveLevels();
				ClosePosition();
				return;
			}

			if (_activeTake.HasValue && candle.LowPrice <= _activeTake.Value)
			{
				ClearPlannedLevels();
				ClearActiveLevels();
				ClosePosition();
			}
		}
	}

	private void UpdatePreviousCandle(ICandleMessage candle, decimal? macdMain, decimal? macdSignal)
	{
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
		_hasPreviousCandle = true;
		_prevMacdMain = macdMain;
		_prevMacdSignal = macdSignal;
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		return step;
	}

	private void ClearPlannedLevels()
	{
		_plannedStop = null;
		_plannedTake = null;
		_plannedSide = null;
	}

	private void ClearActiveLevels()
	{
		_activeStop = null;
		_activeTake = null;
		_activeSide = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Trade.Security != Security)
			return;

		if (Position == 0)
		{
			ClearActiveLevels();
			ClearPlannedLevels();
			return;
		}

		if (Position > 0)
		{
			if (_plannedSide == Sides.Buy)
			{
				_activeStop = _plannedStop;
				_activeTake = _plannedTake;
				_activeSide = Sides.Buy;
				ClearPlannedLevels();
			}
			else
			{
				_activeSide = Sides.Buy;
			}
		}
		else if (Position < 0)
		{
			if (_plannedSide == Sides.Sell)
			{
				_activeStop = _plannedStop;
				_activeTake = _plannedTake;
				_activeSide = Sides.Sell;
				ClearPlannedLevels();
			}
			else
			{
				_activeSide = Sides.Sell;
			}
		}
	}
}

