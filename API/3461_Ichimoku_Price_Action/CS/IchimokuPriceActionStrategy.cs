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

public class IchimokuPriceActionStrategy : Strategy
{
	private readonly StrategyParam<bool> _buyMode;
	private readonly StrategyParam<bool> _sellMode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<bool> _useMacdFilter;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<StopLossModes> _stopLossMode;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _atrCandleType;
	private readonly StrategyParam<int> _swingBars;
	private readonly StrategyParam<DataType> _swingCandleType;
	private readonly StrategyParam<TakeProfitModes> _takeProfitMode;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _takeProfitRatio;
	private readonly StrategyParam<bool> _closeOnReverse;
	private readonly StrategyParam<decimal> _moveToBreakEven;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingTrigger;
	private readonly StrategyParam<decimal> _trailingStep;

	// Technical indicators required by the strategy.
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private AverageTrueRange _atr;
	private Highest _swingHigh;
	private Lowest _swingLow;

	// Cached indicator values used when calculating risk controls.
	private decimal? _lastAtrValue;
	private decimal? _lastSwingHigh;
	private decimal? _lastSwingLow;

	// Internal state that tracks the active position.
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	public bool BuyMode
	{
		get => _buyMode.Value;
		set => _buyMode.Value = value;
	}

	public bool SellMode
	{
		get => _sellMode.Value;
		set => _sellMode.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	public bool UseMacdFilter
	{
		get => _useMacdFilter.Value;
		set => _useMacdFilter.Value = value;
	}

	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	public StopLossModes StopLossMode
	{
		get => _stopLossMode.Value;
		set => _stopLossMode.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public DataType AtrCandleType
	{
		get => _atrCandleType.Value;
		set => _atrCandleType.Value = value;
	}

	public int SwingBars
	{
		get => _swingBars.Value;
		set => _swingBars.Value = value;
	}

	public DataType SwingCandleType
	{
		get => _swingCandleType.Value;
		set => _swingCandleType.Value = value;
	}

	public TakeProfitModes TakeProfitMode
	{
		get => _takeProfitMode.Value;
		set => _takeProfitMode.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TakeProfitRatio
	{
		get => _takeProfitRatio.Value;
		set => _takeProfitRatio.Value = value;
	}

	public bool CloseOnReverse
	{
		get => _closeOnReverse.Value;
		set => _closeOnReverse.Value = value;
	}


	public decimal MoveToBreakEven
	{
		get => _moveToBreakEven.Value;
		set => _moveToBreakEven.Value = value;
	}

	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	public decimal TrailingTrigger
	{
		get => _trailingTrigger.Value;
		set => _trailingTrigger.Value = value;
	}

	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	public IchimokuPriceActionStrategy()
	{
		_buyMode = Param(nameof(BuyMode), true)
		.SetDisplay("Enable Longs", "Allow the strategy to open long positions", "General");
		_sellMode = Param(nameof(SellMode), true)
		.SetDisplay("Enable Shorts", "Allow the strategy to open short positions", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Trading Candle", "Primary timeframe for signals", "General");
		_startTime = Param(nameof(StartTime), TimeSpan.Zero)
		.SetDisplay("Start Time", "Session start time (00:00 disables filter)", "Schedule");
		_endTime = Param(nameof(EndTime), TimeSpan.Zero)
		.SetDisplay("End Time", "Session end time (00:00 disables filter)", "Schedule");
		_useMacdFilter = Param(nameof(UseMacdFilter), true)
		.SetDisplay("Use MACD", "Require MACD confirmation for entries", "Filters");
		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA period", "Filters");
		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA period", "Filters");
		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA period", "Filters");
		_stopLossMode = Param(nameof(StopLossModes), StopLossModes.FixedPips)
		.SetDisplay("Stop-Loss Mode", "How the protective stop is calculated", "Risk");
		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Stop-Loss (pips)", "Stop distance in pips when fixed mode is selected", "Risk");
		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "ATR multiple for stop placement", "Risk");
		_atrPeriod = Param(nameof(AtrPeriod), 28)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR lookback length", "Risk");
		_atrCandleType = Param(nameof(AtrCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("ATR Candle", "Timeframe used for ATR calculation", "Risk");
		_swingBars = Param(nameof(SwingBars), 10)
		.SetGreaterThanZero()
		.SetDisplay("Swing Lookback", "Bars to evaluate swing highs and lows", "Risk");
		_swingCandleType = Param(nameof(SwingCandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Swing Candle", "Timeframe for swing analysis", "Risk");
		_takeProfitMode = Param(nameof(TakeProfitModes), TakeProfitModes.FixedPips)
		.SetDisplay("Take-Profit Mode", "How exits are measured", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Take-Profit (pips)", "Take-profit distance in pips", "Risk");
		_takeProfitRatio = Param(nameof(TakeProfitRatio), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Risk/Reward", "Reward-to-risk ratio when ratio mode is selected", "Risk");
		_closeOnReverse = Param(nameof(CloseOnReverse), true)
		.SetDisplay("Close On Reverse", "Exit when the opposite signal appears", "Risk");
		_moveToBreakEven = Param(nameof(MoveToBreakEven), 0m)
		.SetDisplay("Break-Even Trigger", "Profit in pips required to lock entry price", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 50m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");
		_trailingTrigger = Param(nameof(TrailingTrigger), 25m)
		.SetDisplay("Trailing Trigger", "Profit in pips required to activate trailing", "Risk");
		_trailingStep = Param(nameof(TrailingStep), 5m)
		.SetDisplay("Trailing Step", "Minimum improvement in pips before stop is moved", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		if (AtrCandleType != CandleType)
			yield return (Security, AtrCandleType);
		if (SwingCandleType != CandleType && SwingCandleType != AtrCandleType)
			yield return (Security, SwingCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastAtrValue = null;
		_lastSwingHigh = null;
		_lastSwingLow = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Ensure that the built-in protection module is armed once per lifetime.
		StartProtection();

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Fast = MacdFast,
			Slow = MacdSlow,
			Signal = MacdSignal
		};

		var tradeSubscription = SubscribeCandles(CandleType);
		tradeSubscription
		.Bind(_macd, ProcessTradingCandle)
		.Start();

		if (StopLossMode == StopLossModes.AtrMultiplier || TakeProfitMode == TakeProfitModes.RiskReward)
		{
			_atr = new AverageTrueRange { Length = AtrPeriod };
			var atrSubscription = SubscribeCandles(AtrCandleType);
			atrSubscription
			.Bind(_atr!, UpdateAtr)
			.Start();
		}

		if (StopLossMode == StopLossModes.SwingHighLow)
		{
			_swingHigh = new Highest
			{
				Length = SwingBars,
				CandlePrice = CandlePrice.High
			};
			_swingLow = new Lowest
			{
				Length = SwingBars,
				CandlePrice = CandlePrice.Low
			};

			var swingSubscription = SubscribeCandles(SwingCandleType);
			swingSubscription
			.Bind(_swingHigh, _swingLow, UpdateSwing)
			.Start();
		}
	}

	private void UpdateAtr(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastAtrValue = atrValue;
	}

	private void UpdateSwing(ICandleMessage candle, decimal swingHigh, decimal swingLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastSwingHigh = swingHigh;
		_lastSwingLow = swingLow;
	}

	private void ProcessTradingCandle(ICandleMessage candle, decimal macdValue, decimal macdSignal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var currentTime = candle.CloseTime.TimeOfDay;
		if (!IsWithinTradingWindow(currentTime))
			return;

		var priceStep = Security.PriceStep ?? 0.0001m;
		var longSignal = ShouldEnterLong(candle, macdValue, macdSignal);
		var shortSignal = ShouldEnterShort(candle, macdValue, macdSignal);

		if (Position == 0)
		{
			if (longSignal)
			{
				// Open a fresh long position and initialise tracking variables.
				BuyMarket(Volume);
				_longEntryPrice = candle.ClosePrice;
				_longStopPrice = CalculateStopPrice(true, candle);
			}
			else if (shortSignal)
			{
				// Open a fresh short position and initialise tracking variables.
				SellMarket(Volume);
				_shortEntryPrice = candle.ClosePrice;
				_shortStopPrice = CalculateStopPrice(false, candle);
			}
			return;
		}

		if (Position > 0)
		{
			var stopPrice = CalculateStopPrice(true, candle);
			if (stopPrice != null)
			{
				// Keep the most conservative long stop among the calculated values.
				_longStopPrice = _longStopPrice == null ? stopPrice : Math.Max(stopPrice.Value, _longStopPrice.Value);
			}

			ApplyBreakEvenAndTrailing(true, candle, priceStep);

			if (_longStopPrice != null && candle.LowPrice <= _longStopPrice)
			{
				// Protective stop hit - close the long exposure.
				SellMarket(Position);
				ResetLong();
				return;
			}

			var targetPrice = CalculateTargetPrice(true, candle);
			if (targetPrice != null && candle.HighPrice >= targetPrice)
			{
				// Profit target reached - realise the gains.
				SellMarket(Position);
				ResetLong();
				return;
			}

			if (CloseOnReverse && shortSignal)
			{
				// Flip the bias when the opposite entry signal appears.
				SellMarket(Position);
				ResetLong();
			}
		}
		else if (Position < 0)
		{
			var stopPrice = CalculateStopPrice(false, candle);
			if (stopPrice != null)
			{
				// Keep the most conservative short stop among the calculated values.
				_shortStopPrice = _shortStopPrice == null ? stopPrice : Math.Min(stopPrice.Value, _shortStopPrice.Value);
			}

			ApplyBreakEvenAndTrailing(false, candle, priceStep);

			if (_shortStopPrice != null && candle.HighPrice >= _shortStopPrice)
			{
				// Protective stop hit - close the short exposure.
				BuyMarket(Math.Abs(Position));
				ResetShort();
				return;
			}

			var targetPrice = CalculateTargetPrice(false, candle);
			if (targetPrice != null && candle.LowPrice <= targetPrice)
			{
				// Profit target reached - realise the gains.
				BuyMarket(Math.Abs(Position));
				ResetShort();
				return;
			}

			if (CloseOnReverse && longSignal)
			{
				// Flip the bias when the opposite entry signal appears.
				BuyMarket(Math.Abs(Position));
				ResetShort();
			}
		}
	}

	private bool ShouldEnterLong(ICandleMessage candle, decimal macdValue, decimal macdSignal)
	{
		if (!BuyMode)
			return false;

		if (UseMacdFilter && macdValue <= macdSignal)
			return false;

		return true;
	}

	private bool ShouldEnterShort(ICandleMessage candle, decimal macdValue, decimal macdSignal)
	{
		if (!SellMode)
			return false;

		if (UseMacdFilter && macdValue >= macdSignal)
			return false;

		return true;
	}

	private decimal? CalculateStopPrice(bool isLong, ICandleMessage candle)
	{
		var entryPrice = isLong ? _longEntryPrice : _shortEntryPrice;
		if (entryPrice == null)
			entryPrice = candle.ClosePrice;

		var priceStep = Security.PriceStep ?? 0.0001m;

		switch (StopLossMode)
		{
			case StopLossModes.FixedPips:
			{
				var distance = StopLossPips * priceStep;
				return isLong ? entryPrice.Value - distance : entryPrice.Value + distance;
			}
			case StopLossModes.AtrMultiplier when _lastAtrValue != null:
			{
				var distance = AtrMultiplier * _lastAtrValue.Value;
				return isLong ? entryPrice.Value - distance : entryPrice.Value + distance;
			}
			case StopLossModes.SwingHighLow when _lastSwingHigh != null && _lastSwingLow != null:
			{
				return isLong ? _lastSwingLow : _lastSwingHigh;
			}
			default:
				return null;
		}
	}

	private decimal? CalculateTargetPrice(bool isLong, ICandleMessage candle)
	{
		var entryPrice = isLong ? _longEntryPrice : _shortEntryPrice;
		if (entryPrice == null)
			return null;

		var priceStep = Security.PriceStep ?? 0.0001m;

		switch (TakeProfitMode)
		{
			case TakeProfitModes.FixedPips:
			{
				var distance = TakeProfitPips * priceStep;
				return isLong ? entryPrice.Value + distance : entryPrice.Value - distance;
			}
			case TakeProfitModes.RiskReward:
			{
				var stopPrice = isLong ? _longStopPrice : _shortStopPrice;
				if (stopPrice == null)
					return null;

				var risk = Math.Abs(entryPrice.Value - stopPrice.Value);
				if (risk <= 0)
					return null;

				var distance = risk * TakeProfitRatio;
				return isLong ? entryPrice.Value + distance : entryPrice.Value - distance;
			}
			default:
				return null;
		}
	}

	private void ApplyBreakEvenAndTrailing(bool isLong, ICandleMessage candle, decimal priceStep)
	{
		var entryPrice = isLong ? _longEntryPrice : _shortEntryPrice;
		if (entryPrice == null)
			return;

		var currentPrice = candle.ClosePrice;
		var profit = isLong ? currentPrice - entryPrice.Value : entryPrice.Value - currentPrice;
		var pipProfit = profit / priceStep;

		if (MoveToBreakEven > 0 && pipProfit >= MoveToBreakEven)
		{
			var breakEvenPrice = entryPrice.Value;
			if (isLong)
			{
				// Once the break-even trigger is achieved, never allow the stop below entry.
				_longStopPrice = _longStopPrice == null ? breakEvenPrice : Math.Max(_longStopPrice.Value, breakEvenPrice);
			}
			else
			{
				// Once the break-even trigger is achieved, never allow the stop above entry.
				_shortStopPrice = _shortStopPrice == null ? breakEvenPrice : Math.Min(_shortStopPrice.Value, breakEvenPrice);
			}
		}

		if (TrailingStop <= 0 || TrailingTrigger <= 0)
			return;

		if (pipProfit < TrailingTrigger)
			return;

		var desiredStop = isLong
		? currentPrice - TrailingStop * priceStep
		: currentPrice + TrailingStop * priceStep;

		if (isLong)
		{
			if (_longStopPrice == null || desiredStop - _longStopPrice.Value >= TrailingStep * priceStep)
				_longStopPrice = _longStopPrice == null ? desiredStop : Math.Max(_longStopPrice.Value, desiredStop);
		}
		else
		{
			if (_shortStopPrice == null || _shortStopPrice.Value - desiredStop >= TrailingStep * priceStep)
				_shortStopPrice = _shortStopPrice == null ? desiredStop : Math.Min(_shortStopPrice.Value, desiredStop);
		}
	}

	private bool IsWithinTradingWindow(TimeSpan currentTime)
	{
		if (StartTime == TimeSpan.Zero && EndTime == TimeSpan.Zero)
			return true;

		if (StartTime <= EndTime)
			return currentTime >= StartTime && currentTime <= EndTime;

		return currentTime >= StartTime || currentTime <= EndTime;
	}

	private void ResetLong()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
	}

	private void ResetShort()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
	}

	public enum StopLossModes
	{
		FixedPips,
		AtrMultiplier,
		SwingHighLow
	}

	public enum TakeProfitModes
	{
		FixedPips,
		RiskReward
	}
}
