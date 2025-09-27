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
/// Long-only strategy translated from the Billy expert advisor.
/// It looks for four consecutive descending highs and opens while two stochastic oscillators confirm upward momentum.
/// </summary>
public class BillyExpertReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<DataType> _tradingCandleType;
	private readonly StrategyParam<DataType> _slowStochasticCandleType;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;

	private StochasticOscillator _fastStochastic = null!;
	private StochasticOscillator _slowStochastic = null!;
	private decimal? _fastKCurrent;
	private decimal? _fastDCurrent;
	private decimal? _fastKPrevious;
	private decimal? _fastDPrevious;
	private decimal? _slowKCurrent;
	private decimal? _slowDCurrent;
	private decimal? _slowKPrevious;
	private decimal? _slowDPrevious;

	private decimal? _previousHigh1;
	private decimal? _previousHigh2;
	private decimal? _previousHigh3;
	private decimal? _previousOpen1;
	private decimal? _previousOpen2;
	private decimal? _previousOpen3;

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _accumulatedLongVolume;
	private decimal _priceStep;

	/// <summary>
	/// Initializes a new instance of the <see cref="BillyExpertReversalStrategy"/> class.
	/// </summary>
	public BillyExpertReversalStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss (pts)", "Stop loss distance in price points", "Risk");
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 12m)
			.SetDisplay("Take Profit (pts)", "Take profit distance in price points", "Risk");
		_maxOrders = Param(nameof(MaxOrders), 1)
			.SetDisplay("Max Orders", "Maximum number of simultaneous long entries", "Risk");
		_tradingCandleType = Param(nameof(TradingCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Trading Candle", "Primary timeframe used for signals", "General");
		_slowStochasticCandleType = Param(nameof(SlowStochasticCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Slow Stochastic Candle", "Higher timeframe used for confirmation", "General");
		_stochasticLength = Param(nameof(StochasticLength), 5)
			.SetDisplay("Stochastic Length", "Lookback period for %K", "Indicators");
		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 3)
			.SetDisplay("%K Smoothing", "Smoothing period applied to %K", "Indicators");
		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetDisplay("%D Period", "Smoothing period applied to %D", "Indicators");
		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
			.SetDisplay("Slowing", "Additional smoothing for %K", "Indicators");
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
	/// Maximum number of simultaneous long entries.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Primary trading timeframe.
	/// </summary>
	public DataType TradingCandleType
	{
		get => _tradingCandleType.Value;
		set => _tradingCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for the slower stochastic oscillator.
	/// </summary>
	public DataType SlowStochasticCandleType
	{
		get => _slowStochasticCandleType.Value;
		set => _slowStochasticCandleType.Value = value;
	}

	/// <summary>
	/// Lookback length for the stochastic oscillators.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %K line.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %D line.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Additional slowing applied to %K.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TradingCandleType);

		if (SlowStochasticCandleType != TradingCandleType)
		{
			yield return (Security, SlowStochasticCandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastKCurrent = null;
		_fastDCurrent = null;
		_fastKPrevious = null;
		_fastDPrevious = null;
		_slowKCurrent = null;
		_slowDCurrent = null;
		_slowKPrevious = null;
		_slowDPrevious = null;

		_previousHigh1 = null;
		_previousHigh2 = null;
		_previousHigh3 = null;
		_previousOpen1 = null;
		_previousOpen2 = null;
		_previousOpen3 = null;

		ResetTradeLevels();
		_accumulatedLongVolume = 0m;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? Security?.Step ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 1m;
			LogWarning("Price step is not specified. Using 1 as a fallback value.");
		}

		_fastStochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = StochasticKPeriod },
			D = { Length = StochasticDPeriod },
			Slowing = StochasticSlowing
		};

		_slowStochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = StochasticKPeriod },
			D = { Length = StochasticDPeriod },
			Slowing = StochasticSlowing
		};

		var tradingSubscription = SubscribeCandles(TradingCandleType);

		if (SlowStochasticCandleType == TradingCandleType)
		{
			tradingSubscription
				.BindEx(_fastStochastic, _slowStochastic, ProcessTradingCandle)
				.Start();
		}
		else
		{
			tradingSubscription
				.BindEx(_fastStochastic, ProcessTradingCandle)
				.Start();

			var slowSubscription = SubscribeCandles(SlowStochasticCandleType);
			slowSubscription
				.BindEx(_slowStochastic, ProcessSlowStochastic)
				.Start();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			_accumulatedLongVolume = Position;

			if (delta > 0m)
			{
				var entryPrice = PositionPrice;
				_stopLossPrice = StopLossPoints > 0m ? entryPrice - StopLossPoints * _priceStep : null;
				_takeProfitPrice = TakeProfitPoints > 0m ? entryPrice + TakeProfitPoints * _priceStep : null;
			}
		}
		else
		{
			_accumulatedLongVolume = 0m;
			ResetTradeLevels();
		}
	}

	private void ProcessTradingCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hasFast = UpdateFastStochastic(fastValue);
		var hasSlow = UpdateSlowStochastic(slowValue);

		if (!hasFast || !hasSlow)
			return;

		ProcessTradingLogic(candle);
	}

	private void ProcessTradingCandle(ICandleMessage candle, IIndicatorValue fastValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!UpdateFastStochastic(fastValue))
			return;

		ProcessTradingLogic(candle);
	}

	private void ProcessSlowStochastic(ICandleMessage candle, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateSlowStochastic(slowValue);
	}

	private void ProcessTradingLogic(ICandleMessage candle)
	{
		var descendingPattern = UpdateAndCheckPattern(candle);

		if (TryCloseLongPosition(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!descendingPattern)
			return;

		if (!HasBullishStochasticSignal())
			return;

		if (MaxOrders > 0 && GetActiveLongCount() >= MaxOrders)
			return;

		var volumeToBuy = Volume;

		if (Position < 0m)
			volumeToBuy += Math.Abs(Position);

		if (volumeToBuy <= 0m)
			return;

		BuyMarket(volumeToBuy);
	}

	private bool UpdateAndCheckPattern(ICandleMessage candle)
	{
		var hasHistory = _previousHigh1.HasValue && _previousHigh2.HasValue && _previousHigh3.HasValue &&
			_previousOpen1.HasValue && _previousOpen2.HasValue && _previousOpen3.HasValue;

		var descendingHighs = hasHistory &&
			candle.HighPrice < _previousHigh1!.Value &&
			_previousHigh1.Value < _previousHigh2!.Value &&
			_previousHigh2.Value < _previousHigh3!.Value;

		var descendingOpens = hasHistory &&
			candle.OpenPrice < _previousOpen1!.Value &&
			_previousOpen1.Value < _previousOpen2!.Value &&
			_previousOpen2.Value < _previousOpen3!.Value;

		_previousHigh3 = _previousHigh2;
		_previousHigh2 = _previousHigh1;
		_previousHigh1 = candle.HighPrice;

		_previousOpen3 = _previousOpen2;
		_previousOpen2 = _previousOpen1;
		_previousOpen1 = candle.OpenPrice;

		return descendingHighs && descendingOpens;
	}

	private bool TryCloseLongPosition(ICandleMessage candle)
	{
		if (Position <= 0m)
			return false;

		if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
		{
			SellMarket(Position);
			return true;
		}

		if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
		{
			SellMarket(Position);
			return true;
		}

		return false;
	}

	private bool HasBullishStochasticSignal()
	{
		if (!_fastKCurrent.HasValue || !_fastDCurrent.HasValue || !_fastKPrevious.HasValue || !_fastDPrevious.HasValue)
			return false;

		if (_fastKPrevious.Value <= _fastDPrevious.Value || _fastKCurrent.Value <= _fastDCurrent.Value)
			return false;

		if (!_slowKCurrent.HasValue || !_slowDCurrent.HasValue || !_slowKPrevious.HasValue || !_slowDPrevious.HasValue)
			return false;

		return _slowKPrevious.Value > _slowDPrevious.Value && _slowKCurrent.Value > _slowDCurrent.Value;
	}

	private bool UpdateFastStochastic(IIndicatorValue indicatorValue)
	{
		if (!indicatorValue.IsFinal)
			return false;

		if (indicatorValue is not StochasticOscillatorValue stoch)
			return false;

		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return false;

		_fastKPrevious = _fastKCurrent;
		_fastDPrevious = _fastDCurrent;
		_fastKCurrent = k;
		_fastDCurrent = d;
		return true;
	}

	private bool UpdateSlowStochastic(IIndicatorValue indicatorValue)
	{
		if (!indicatorValue.IsFinal)
			return false;

		if (indicatorValue is not StochasticOscillatorValue stoch)
			return false;

		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return false;

		_slowKPrevious = _slowKCurrent;
		_slowDPrevious = _slowDCurrent;
		_slowKCurrent = k;
		_slowDCurrent = d;
		return true;
	}

	private int GetActiveLongCount()
	{
		if (Position <= 0m)
			return 0;

		if (Volume <= 0m)
			return 1;

		var ratio = _accumulatedLongVolume / Volume;
		return (int)Math.Ceiling(ratio);
	}

	private void ResetTradeLevels()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}
}
