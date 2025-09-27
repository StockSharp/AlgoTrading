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

public enum TokyoSignalMode
{
	ContraryTrend,
	AccordingTrend,
}

public class TokyoSessionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _brokerOffset;
	private readonly StrategyParam<TokyoSignalMode> _signalMode;
	private readonly StrategyParam<int> _timeSetLevels;
	private readonly StrategyParam<int> _timeCheckLevels;
	private readonly StrategyParam<int> _timeRecheckPrices;
	private readonly StrategyParam<int> _timeCloseOrders;
	private readonly StrategyParam<decimal> _minDistance;
	private readonly StrategyParam<decimal> _maxDistance;
	private readonly StrategyParam<bool> _recheckPrices;
	private readonly StrategyParam<bool> _checkAllBars;
	private readonly StrategyParam<bool> _closeInSignal;
	private readonly StrategyParam<bool> _closeOrdersOnTime;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEven;
	private readonly StrategyParam<decimal> _breakEvenAfter;
	private readonly StrategyParam<int> _maxOrders;

	private decimal? _levelHigh;
	private decimal? _levelLow;
	private decimal _averageSum;
	private int _averageCount;
	private bool _checkBarsBuy;
	private bool _checkBarsSell;
	private bool _checkPricesBuy;
	private bool _checkPricesSell;
	private bool _awaitingSignal;
	private bool _recheckPerformed;
	private DateTime? _targetSignalDate;
	private DateTimeOffset? _lastAccumulatedCandleTime;
	private decimal? _previousClose;
	private TimeSpan _timeFrame;
	private decimal _pipSize;
	private DateTime? _lastTimeCloseDate;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BrokerOffset
	{
		get => _brokerOffset.Value;
		set => _brokerOffset.Value = value;
	}

	public TokyoSignalMode TypeOfSignals
	{
		get => _signalMode.Value;
		set => _signalMode.Value = value;
	}

	public int TimeSetLevels
	{
		get => _timeSetLevels.Value;
		set => _timeSetLevels.Value = value;
	}

	public int TimeCheckLevels
	{
		get => _timeCheckLevels.Value;
		set => _timeCheckLevels.Value = value;
	}

	public int TimeRecheckPrices
	{
		get => _timeRecheckPrices.Value;
		set => _timeRecheckPrices.Value = value;
	}

	public int TimeCloseOrders
	{
		get => _timeCloseOrders.Value;
		set => _timeCloseOrders.Value = value;
	}

	public decimal MinDistanceOfLevel
	{
		get => _minDistance.Value;
		set => _minDistance.Value = value;
	}

	public decimal MaxDistanceOfLevel
	{
		get => _maxDistance.Value;
		set => _maxDistance.Value = value;
	}

	public bool ReCheckPrices
	{
		get => _recheckPrices.Value;
		set => _recheckPrices.Value = value;
	}

	public bool CheckAllBars
	{
		get => _checkAllBars.Value;
		set => _checkAllBars.Value = value;
	}

	public bool CloseInSignal
	{
		get => _closeInSignal.Value;
		set => _closeInSignal.Value = value;
	}

	public bool CloseOrdersOnTime
	{
		get => _closeOrdersOnTime.Value;
		set => _closeOrdersOnTime.Value = value;
	}

	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	public decimal BreakEven
	{
		get => _breakEven.Value;
		set => _breakEven.Value = value;
	}

	public decimal BreakEvenAfter
	{
		get => _breakEvenAfter.Value;
		set => _breakEvenAfter.Value = value;
	}


	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	public TokyoSessionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "General");

		_brokerOffset = Param(nameof(BrokerOffset), 0)
		.SetDisplay("Broker GMT Offset", "Difference between broker server and GMT time (hours)", "General")
		.SetRange(-24, 24);

		_signalMode = Param(nameof(TypeOfSignals), TokyoSignalMode.ContraryTrend)
		.SetDisplay("Signal Mode", "Choose between contrary or trend following entries", "Signals");

		_timeSetLevels = Param(nameof(TimeSetLevels), 21)
		.SetDisplay("Level Hour", "Hour when the reference candle is captured", "Signals")
		.SetRange(0, 23);

		_timeCheckLevels = Param(nameof(TimeCheckLevels), 6)
		.SetDisplay("Entry Hour", "Hour when breakout conditions are evaluated", "Signals")
		.SetRange(0, 23);

		_timeRecheckPrices = Param(nameof(TimeRecheckPrices), 3)
		.SetDisplay("Recheck Hour", "Hour used for additional momentum validation", "Signals")
		.SetRange(0, 23);

		_timeCloseOrders = Param(nameof(TimeCloseOrders), 8)
		.SetDisplay("Flat Hour", "Hour when all positions are closed", "Risk Management")
		.SetRange(0, 23);

		_minDistance = Param(nameof(MinDistanceOfLevel), 0m)
		.SetDisplay("Minimum Distance", "Minimal distance from the reference level in pips", "Signals")
		.SetNotNegative();

		_maxDistance = Param(nameof(MaxDistanceOfLevel), 46m)
		.SetDisplay("Maximum Distance", "Maximum distance from the reference level in pips", "Signals")
		.SetNotNegative();

		_recheckPrices = Param(nameof(ReCheckPrices), true)
		.SetDisplay("Recheck Prices", "Enable additional validation at the recheck hour", "Signals");

		_checkAllBars = Param(nameof(CheckAllBars), true)
		.SetDisplay("Check Intermediate Bars", "Require that prices stay inside the reference channel before entries", "Signals");

		_closeInSignal = Param(nameof(CloseInSignal), true)
		.SetDisplay("Close On Counter Signal", "Exit when price returns inside the channel", "Risk Management");

		_closeOrdersOnTime = Param(nameof(CloseOrdersOnTime), true)
		.SetDisplay("Close At Hour", "Exit all positions after the specified hour", "Risk Management");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use Take Profit", "Enable fixed take profit", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 10m)
		.SetDisplay("Take Profit", "Take profit distance in pips", "Risk Management")
		.SetNotNegative();

		_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use Stop Loss", "Enable protective stop loss", "Risk Management");

		_stopLoss = Param(nameof(StopLoss), 26m)
		.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk Management")
		.SetNotNegative();

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
		.SetDisplay("Use Trailing Stop", "Enable trailing stop management", "Risk Management");

		_trailingStop = Param(nameof(TrailingStop), 5m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk Management")
		.SetNotNegative();

		_trailingStep = Param(nameof(TrailingStep), 1m)
		.SetDisplay("Trailing Step", "Trailing step in pips", "Risk Management")
		.SetNotNegative();

		_useBreakEven = Param(nameof(UseBreakEven), false)
		.SetDisplay("Use Break Even", "Enable automatic break-even", "Risk Management");

		_breakEven = Param(nameof(BreakEven), 2.5m)
		.SetDisplay("Break Even", "Distance to move stop loss to break-even (pips)", "Risk Management")
		.SetNotNegative();

		_breakEvenAfter = Param(nameof(BreakEvenAfter), 0m)
		.SetDisplay("Break Even Activation", "Profit required before moving stop (pips)", "Risk Management")
		.SetNotNegative();


		_maxOrders = Param(nameof(MaxOrders), 1)
		.SetDisplay("Maximum Orders", "Maximum number of position multiples to hold", "Trading")
		.SetNotNegative();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_levelHigh = null;
		_levelLow = null;
		_averageSum = 0m;
		_averageCount = 0;
		_checkBarsBuy = true;
		_checkBarsSell = true;
		_checkPricesBuy = true;
		_checkPricesSell = true;
		_awaitingSignal = false;
		_recheckPerformed = false;
		_targetSignalDate = null;
		_lastAccumulatedCandleTime = null;
		_previousClose = null;
		_timeFrame = TimeSpan.Zero;
		_pipSize = 0m;
		_lastTimeCloseDate = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_timeFrame = CandleType.Arg switch
		{
			TimeSpan span => span,
			_ => TimeSpan.FromHours(1),
		};

		_pipSize = Security?.PriceStep ?? 0.0001m;
		if (_pipSize <= 0m)
			_pipSize = 0.0001m;

		var takeProfit = UseTakeProfit && TakeProfit > 0m ? new Unit(TakeProfit * _pipSize, UnitTypes.Absolute) : null;
		var stopLoss = UseStopLoss && StopLoss > 0m ? new Unit(StopLoss * _pipSize, UnitTypes.Absolute) : null;
		var trailing = UseTrailingStop && TrailingStop > 0m ? new Unit(TrailingStop * _pipSize, UnitTypes.Absolute) : null;
		var trailingStep = UseTrailingStop && TrailingStep > 0m ? new Unit(TrailingStep * _pipSize, UnitTypes.Absolute) : null;
		var breakEven = UseBreakEven && BreakEven > 0m ? new Unit(BreakEven * _pipSize, UnitTypes.Absolute) : null;
		var breakEvenAfter = UseBreakEven && BreakEvenAfter > 0m ? new Unit(BreakEvenAfter * _pipSize, UnitTypes.Absolute) : null;

		StartProtection(
			takeProfit: takeProfit,
			stopLoss: stopLoss,
			trailingStop: trailing,
			trailingStep: trailingStep,
			breakEven: breakEven,
			breakEvenActivate: breakEvenAfter,
			useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var brokerOpenTime = ApplyOffset(candle.OpenTime);
		var brokerNextTime = ApplyOffset(candle.OpenTime + _timeFrame);

		UpdateLevelsIfNeeded(candle, brokerOpenTime);
		UpdateSessionState(candle, brokerOpenTime);

		TryCloseBySignal(candle);
		TryCloseByTime(brokerNextTime);
		TryOpenPositions(candle, brokerNextTime);

		_lastAccumulatedCandleTime = candle.OpenTime;
		_previousClose = candle.ClosePrice;
	}

	private void UpdateLevelsIfNeeded(ICandleMessage candle, DateTime brokerOpenTime)
	{
		if (brokerOpenTime.Hour != TimeSetLevels)
			return;

		_levelHigh = candle.HighPrice;
		_levelLow = candle.LowPrice;
		_averageSum = candle.ClosePrice;
		_averageCount = 1;
		_checkBarsBuy = true;
		_checkBarsSell = true;
		_checkPricesBuy = true;
		_checkPricesSell = true;
		_awaitingSignal = true;
		_recheckPerformed = !ReCheckPrices;
		_lastAccumulatedCandleTime = candle.OpenTime;

		var signalDate = brokerOpenTime.Date;
		if (TimeSetLevels > TimeCheckLevels)
			signalDate = signalDate.AddDays(1);

		_targetSignalDate = signalDate;

		LogInfo($"Reference candle captured at {brokerOpenTime}: high {_levelHigh}, low {_levelLow}.");
	}

	private void UpdateSessionState(ICandleMessage candle, DateTime brokerOpenTime)
	{
		if (!_awaitingSignal || _levelHigh is null || _levelLow is null)
			return;

		if (_lastAccumulatedCandleTime == candle.OpenTime)
			return;

		_averageSum += candle.ClosePrice;
		_averageCount++;

		if (CheckAllBars)
		{
			if (candle.ClosePrice > _levelHigh)
			{
				_checkBarsBuy = false;
			}

			if (candle.ClosePrice < _levelLow)
			{
				_checkBarsSell = false;
			}
		}

		if (ReCheckPrices && !_recheckPerformed && brokerOpenTime.Hour == TimeRecheckPrices)
		{
			var average = _averageCount > 0 ? _averageSum / _averageCount : candle.ClosePrice;
			var previousClose = _previousClose;

			if (previousClose != null)
			{
				if (average > candle.ClosePrice && candle.ClosePrice < previousClose.Value)
				{
					_checkPricesBuy = false;
				}

				if (average < candle.ClosePrice && candle.ClosePrice > previousClose.Value)
				{
					_checkPricesSell = false;
				}
			}

			_recheckPerformed = true;
		}
	}

	private void TryOpenPositions(ICandleMessage candle, DateTime brokerNextTime)
	{
		if (!_awaitingSignal || _levelHigh is null || _levelLow is null)
			return;

		if (_targetSignalDate != null && brokerNextTime.Date != _targetSignalDate)
			return;

		if (brokerNextTime.Hour != TimeCheckLevels)
			return;

		var closePrice = candle.ClosePrice;
		var minDistance = MinDistanceOfLevel * _pipSize;
		var maxDistance = MaxDistanceOfLevel * _pipSize;

		var canBuy = false;
		var canSell = false;

		switch (TypeOfSignals)
		{
			case TokyoSignalMode.ContraryTrend:
				canBuy = closePrice < _levelLow.Value - minDistance && (MaxDistanceOfLevel <= 0m || closePrice > _levelLow.Value - maxDistance) && _checkBarsBuy && _checkPricesBuy;
				canSell = closePrice > _levelHigh.Value + minDistance && (MaxDistanceOfLevel <= 0m || closePrice < _levelHigh.Value + maxDistance) && _checkBarsSell && _checkPricesSell;
				break;
			case TokyoSignalMode.AccordingTrend:
				canBuy = closePrice > _levelHigh.Value + minDistance && (MaxDistanceOfLevel <= 0m || closePrice < _levelHigh.Value + maxDistance) && _checkBarsSell && _checkPricesSell;
				canSell = closePrice < _levelLow.Value - minDistance && (MaxDistanceOfLevel <= 0m || closePrice > _levelLow.Value - maxDistance) && _checkBarsBuy && _checkPricesBuy;
				break;
		}

		if (canBuy && CanEnterLong())
		{
			CancelActiveOrders();
			var volume = CalculateEntryVolume(true);

			if (volume > 0m)
			{
				BuyMarket(volume);
				LogInfo($"Opened long position at {closePrice} using level {_levelLow}.");
			}
		}

		if (canSell && CanEnterShort())
		{
			CancelActiveOrders();
			var volume = CalculateEntryVolume(false);

			if (volume > 0m)
			{
				SellMarket(volume);
				LogInfo($"Opened short position at {closePrice} using level {_levelHigh}.");
			}
		}

		_awaitingSignal = false;
	}

	private void TryCloseBySignal(ICandleMessage candle)
	{
		if (!CloseInSignal || Position == 0m || _levelHigh is null || _levelLow is null)
			return;

		var closePrice = candle.ClosePrice;
		var minDistance = MinDistanceOfLevel * _pipSize;

		switch (TypeOfSignals)
		{
			case TokyoSignalMode.ContraryTrend:
				if (Position > 0m && closePrice > _levelLow.Value + minDistance)
				{
					ExitPosition();
					LogInfo("Closed long position due to contrary signal.");
				}

				if (Position < 0m && closePrice < _levelHigh.Value - minDistance)
				{
					ExitPosition();
					LogInfo("Closed short position due to contrary signal.");
				}

				break;
			case TokyoSignalMode.AccordingTrend:
				if (Position > 0m && closePrice < _levelHigh.Value - minDistance)
				{
					ExitPosition();
					LogInfo("Closed long position due to retracement.");
				}

				if (Position < 0m && closePrice > _levelLow.Value + minDistance)
				{
					ExitPosition();
					LogInfo("Closed short position due to retracement.");
				}

				break;
		}
	}

	private void TryCloseByTime(DateTime brokerNextTime)
	{
		if (!CloseOrdersOnTime || Position == 0m)
			return;

		if (brokerNextTime.Hour < TimeCloseOrders)
			return;

		if (_lastTimeCloseDate == brokerNextTime.Date)
			return;

		ExitPosition();
		_lastTimeCloseDate = brokerNextTime.Date;
		LogInfo($"Closed position at scheduled time {TimeCloseOrders:00}:00.");
	}

	private bool CanEnterLong()
	{
		if (Volume <= 0m)
			return false;

		if (MaxOrders <= 0)
			return true;

		var maxPosition = Volume * MaxOrders;
		return Position < maxPosition;
	}

	private bool CanEnterShort()
	{
		if (Volume <= 0m)
			return false;

		if (MaxOrders <= 0)
			return true;

		var maxPosition = Volume * MaxOrders;
		return Position > -maxPosition;
	}

	private decimal CalculateEntryVolume(bool isLong)
	{
		var volume = Volume;

		if (isLong && Position < 0m)
		{
			volume += Math.Abs(Position);
		}
		else if (!isLong && Position > 0m)
		{
			volume += Math.Abs(Position);
		}

		if (MaxOrders > 0)
		{
			var maxPosition = Volume * MaxOrders;
			if (isLong)
			{
				volume = Math.Min(volume, maxPosition - Position);
			}
			else
			{
				volume = Math.Min(volume, Position + maxPosition);
			}
		}

		return Math.Max(0m, volume);
	}

	private void ExitPosition()
	{
		CancelActiveOrders();

		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}
	}

	private DateTime ApplyOffset(DateTimeOffset time)
	{
		return (time + TimeSpan.FromHours(BrokerOffset)).DateTime;
	}
}

