using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alli Heik strategy converted from the MQL5 "AlliHeik" expert advisor.
/// Uses a smoothed Heikin Ashi oscillator with optional reverse mode and trailing stops.
/// </summary>
public class AlliHeikStrategy : Strategy
{
	/// <summary>
	/// Available moving average types for smoothing.
	/// </summary>
	public enum MaType
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Sma,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Ema,

		/// <summary>
		/// Smoothed moving average (SMMA/RMA).
		/// </summary>
		Smma,

		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		Lwma,
	}

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<int> _preSmoothPeriod;
	private readonly StrategyParam<MaType> _preSmoothMethod;
	private readonly StrategyParam<int> _postSmoothPeriod;
	private readonly StrategyParam<MaType> _postSmoothMethod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<MaType> _signalMethod;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _preOpenMa = null!;
	private LengthIndicator<decimal> _preCloseMa = null!;
	private LengthIndicator<decimal> _preHighMa = null!;
	private LengthIndicator<decimal> _preLowMa = null!;
	private LengthIndicator<decimal> _postSmoothMa = null!;
	private LengthIndicator<decimal> _signalMa = null!;

	private bool _hasHaState;
	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private decimal? _prevSmoothed;
	private decimal? _prevOscillator;
	private decimal? _prevSignal;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Trading volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips. Set to zero to disable.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips. Set to zero to disable.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Set to zero to disable.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum progress in pips before the trailing stop is moved.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Reverse trading signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Close opposite positions before opening a new one.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Pre-smoothing period for Heikin Ashi inputs.
	/// </summary>
	public int PreSmoothPeriod
	{
		get => _preSmoothPeriod.Value;
		set => _preSmoothPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type used for pre-smoothing.
	/// </summary>
	public MaType PreSmoothMethod
	{
		get => _preSmoothMethod.Value;
		set => _preSmoothMethod.Value = value;
	}

	/// <summary>
	/// Post-smoothing period applied to the Heikin Ashi midpoint.
	/// </summary>
	public int PostSmoothPeriod
	{
		get => _postSmoothPeriod.Value;
		set => _postSmoothPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type used for post-smoothing.
	/// </summary>
	public MaType PostSmoothMethod
	{
		get => _postSmoothMethod.Value;
		set => _postSmoothMethod.Value = value;
	}

	/// <summary>
	/// Signal line period for the oscillator.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type used for the signal line.
	/// </summary>
	public MaType SignalMethod
	{
		get => _signalMethod.Value;
		set => _signalMethod.Value = value;
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
	/// Initializes a new instance of the <see cref="AlliHeikStrategy"/> class.
	/// </summary>
	public AlliHeikStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trading volume in lots", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetRange(0m, 1000m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetRange(0m, 1000m)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetRange(0m, 1000m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetRange(0m, 1000m)
			.SetDisplay("Trailing Step (pips)", "Price advance before moving the trailing stop", "Risk Management");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert oscillator crossover directions", "Strategy");

		_closeOpposite = Param(nameof(CloseOpposite), false)
			.SetDisplay("Close Opposite", "Close opposite positions before entering", "Strategy");

		_preSmoothPeriod = Param(nameof(PreSmoothPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Pre Smooth Period", "Period for pre-smoothing open/high/low/close", "Indicator");

		_preSmoothMethod = Param(nameof(PreSmoothMethod), MaType.Lwma)
			.SetDisplay("Pre Smooth Method", "Moving average type for pre-smoothing", "Indicator");

		_postSmoothPeriod = Param(nameof(PostSmoothPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Post Smooth Period", "Period for smoothing Heikin Ashi midpoint", "Indicator");

		_postSmoothMethod = Param(nameof(PostSmoothMethod), MaType.Lwma)
			.SetDisplay("Post Smooth Method", "Moving average type for post-smoothing", "Indicator");

		_signalPeriod = Param(nameof(SignalPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Period of the oscillator signal line", "Indicator");

		_signalMethod = Param(nameof(SignalMethod), MaType.Smma)
			.SetDisplay("Signal Method", "Moving average type for the signal line", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Source candles for the strategy", "General");
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

		_preOpenMa = null!;
		_preCloseMa = null!;
		_preHighMa = null!;
		_preLowMa = null!;
		_postSmoothMa = null!;
		_signalMa = null!;

		_hasHaState = false;
		_prevHaOpen = 0m;
		_prevHaClose = 0m;
		_prevSmoothed = null;
		_prevOscillator = null;
		_prevSignal = null;

		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		_preOpenMa = CreateMovingAverage(PreSmoothMethod, PreSmoothPeriod);
		_preCloseMa = CreateMovingAverage(PreSmoothMethod, PreSmoothPeriod);
		_preHighMa = CreateMovingAverage(PreSmoothMethod, PreSmoothPeriod);
		_preLowMa = CreateMovingAverage(PreSmoothMethod, PreSmoothPeriod);
		_postSmoothMa = CreateMovingAverage(PostSmoothMethod, PostSmoothPeriod);
		_signalMa = CreateMovingAverage(SignalMethod, SignalPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		UpdateStops(candle);

		var (oscillator, signal) = CalculateOscillator(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var previousOscillator = _prevOscillator;
		var previousSignal = _prevSignal;

		_prevOscillator = oscillator;
		_prevSignal = signal;

		if (previousOscillator is null || previousSignal is null)
			return;

		var crossUp = oscillator > signal && previousOscillator <= previousSignal;
		var crossDown = oscillator < signal && previousOscillator >= previousSignal;

		var longSignal = ReverseSignals ? crossUp : crossDown;
		var shortSignal = ReverseSignals ? crossDown : crossUp;

		if (longSignal)
		{
			EnterPosition(true, candle.ClosePrice);
		}
		else if (shortSignal)
		{
			EnterPosition(false, candle.ClosePrice);
		}
	}

	private (decimal oscillator, decimal signal) CalculateOscillator(ICandleMessage candle)
	{
		var openValue = _preOpenMa.Process(candle.OpenPrice, candle.OpenTime, true).GetValue<decimal>();
		var closeValue = _preCloseMa.Process(candle.ClosePrice, candle.OpenTime, true).GetValue<decimal>();
		var highValue = _preHighMa.Process(candle.HighPrice, candle.OpenTime, true).GetValue<decimal>();
		var lowValue = _preLowMa.Process(candle.LowPrice, candle.OpenTime, true).GetValue<decimal>();

		decimal haOpen;
		if (_hasHaState)
		{
			haOpen = (_prevHaOpen + _prevHaClose) / 2m;
		}
		else
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			_hasHaState = true;
		}

		var haClose = (openValue + highValue + lowValue + closeValue) / 4m;
		var midpoint = (haOpen + haClose) / 2m;

		var previousSmoothed = _prevSmoothed;
		var smoothed = _postSmoothMa.Process(midpoint, candle.OpenTime, true).GetValue<decimal>();

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevSmoothed = smoothed;

		var oscillator = previousSmoothed.HasValue ? smoothed - previousSmoothed.Value : 0m;
		var signal = _signalMa.Process(oscillator, candle.OpenTime, true).GetValue<decimal>();

		return (oscillator, signal);
	}

	private void EnterPosition(bool isLong, decimal price)
	{
		if (CloseOpposite)
		{
			if (isLong && Position < 0)
			{
				ClosePosition();
				ResetPositionState();
			}
			else if (!isLong && Position > 0)
			{
				ClosePosition();
				ResetPositionState();
			}
		}

		if (isLong && Position > 0)
			return;

		if (!isLong && Position < 0)
			return;

		var volume = Volume + Math.Abs(Position);

		if (isLong)
		{
			BuyMarket(volume);
			_entryPrice = price;
			_stopPrice = StopLossPips > 0m ? price - GetPriceOffset(StopLossPips) : null;
			_takePrice = TakeProfitPips > 0m ? price + GetPriceOffset(TakeProfitPips) : null;
		}
		else
		{
			SellMarket(volume);
			_entryPrice = price;
			_stopPrice = StopLossPips > 0m ? price + GetPriceOffset(StopLossPips) : null;
			_takePrice = TakeProfitPips > 0m ? price - GetPriceOffset(TakeProfitPips) : null;
		}
	}

	private void UpdateStops(ICandleMessage candle)
	{
		if (Position == 0)
			return;

		if (_entryPrice is null)
			_entryPrice = candle.ClosePrice;

		var trailingStop = TrailingStopPips > 0m ? GetPriceOffset(TrailingStopPips) : 0m;
		var trailingStep = TrailingStepPips > 0m ? GetPriceOffset(TrailingStepPips) : 0m;

		if (Position > 0)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (trailingStop > 0m && _entryPrice.HasValue)
			{
				var currentPrice = candle.ClosePrice;
				if (currentPrice - _entryPrice.Value > trailingStop + trailingStep)
				{
					var newStop = currentPrice - trailingStop;
					if (!_stopPrice.HasValue || newStop > _stopPrice.Value + trailingStep)
						_stopPrice = newStop;
				}
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (trailingStop > 0m && _entryPrice.HasValue)
			{
				var currentPrice = candle.ClosePrice;
				if (_entryPrice.Value - currentPrice > trailingStop + trailingStep)
				{
					var newStop = currentPrice + trailingStop;
					if (!_stopPrice.HasValue || newStop < _stopPrice.Value - trailingStep || _stopPrice.Value == 0m)
						_stopPrice = newStop;
				}
			}
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private decimal GetPriceOffset(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return pips * 0.0001m;

		var priceStep = security.PriceStep ?? 0.0001m;
		var decimals = security.Decimals ?? 4;
		var multiplier = decimals >= 3 ? 10m : 1m;
		return pips * priceStep * multiplier;
	}

	private LengthIndicator<decimal> CreateMovingAverage(MaType type, int length)
	{
		return type switch
		{
			MaType.Sma => new SMA { Length = length },
			MaType.Ema => new EMA { Length = length },
			MaType.Smma => new SmoothedMovingAverage { Length = length },
			MaType.Lwma => new WeightedMovingAverage { Length = length },
			_ => new SMA { Length = length },
		};
	}
}
