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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD strategy with stochastic confirmation and EMA trend filter.
/// Recreates the behaviour of the original MetaTrader expert advisor using StockSharp high-level API.
/// </summary>
public class MacdStochasticFilterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _macdOpenLevel;
	private readonly StrategyParam<decimal> _macdCloseLevel;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private Stochastic _stochastic = null!;
	private ExponentialMovingAverage _ema = null!;

	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _prevEma;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _tickSize;

	/// <summary>
	/// Take profit distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in instrument points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Required MACD distance from zero line for entries.
	/// </summary>
	public decimal MacdOpenLevel
	{
		get => _macdOpenLevel.Value;
		set => _macdOpenLevel.Value = value;
	}

	/// <summary>
	/// Required MACD distance from zero line for exits.
	/// </summary>
	public decimal MacdCloseLevel
	{
		get => _macdCloseLevel.Value;
		set => _macdCloseLevel.Value = value;
	}

	/// <summary>
	/// Fast EMA period inside MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period inside MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Trend EMA period used as directional filter.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic look-back length.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Smoothing applied to stochastic %K line.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Period used to form stochastic %D line.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults from the original expert advisor.
	/// </summary>
	public MacdStochasticFilterStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetNotNegative();

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 5m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk")
			.SetNotNegative();

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetNotNegative();

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Volume", "Order volume in lots", "Orders")
			.SetGreaterThanZero();

		_macdOpenLevel = Param(nameof(MacdOpenLevel), 3m)
			.SetDisplay("MACD Open Level", "Minimum MACD distance from zero for entries", "Indicators")
			.SetNotNegative();

		_macdCloseLevel = Param(nameof(MacdCloseLevel), 2m)
			.SetDisplay("MACD Close Level", "Minimum MACD distance from zero for exits", "Indicators")
			.SetNotNegative();

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
			.SetGreaterThanZero();

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators")
			.SetGreaterThanZero();

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal EMA period for MACD", "Indicators")
			.SetGreaterThanZero();

		_emaPeriod = Param(nameof(EmaPeriod), 26)
			.SetDisplay("Trend EMA", "EMA period for trend filter", "Indicators")
			.SetGreaterThanZero();

		_stochasticLength = Param(nameof(StochasticLength), 15)
			.SetDisplay("Stochastic Length", "Look-back length for stochastic", "Indicators")
			.SetGreaterThanZero();

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 3)
			.SetDisplay("Stochastic %K", "Smoothing for %K line", "Indicators")
			.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetDisplay("Stochastic %D", "Smoothing for %D line", "Indicators")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for price data", "General");
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

		_prevMacd = null;
		_prevSignal = null;
		_prevEma = null;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_tickSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		_stochastic = new Stochastic
		{
			Length = StochasticLength,
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod
		};

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };

		_tickSize = Security?.PriceStep ?? 0m;
		if (_tickSize == 0m)
		{
			_tickSize = 1m;
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _stochastic, _ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue stochasticValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (!macdValue.IsFinal || !stochasticValue.IsFinal || !emaValue.IsFinal)
		{
			return;
		}

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdData)
		{
			return;
		}

		if (macdData.Macd is not decimal macd || macdData.Signal is not decimal signal)
		{
			return;
		}

		if (stochasticValue is not StochasticValue stochasticData)
		{
			return;
		}

		if (stochasticData.K is not decimal kValue || stochasticData.D is not decimal dValue)
		{
			return;
		}

		var emaCurrent = emaValue.ToDecimal();
		if (_prevEma is null)
		{
			_prevEma = emaCurrent;
		}

		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		var prevEma = _prevEma;

		ManageActivePosition(candle, macd, signal, kValue, dValue, prevMacd, prevSignal);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_prevEma = emaCurrent;
			return;
		}

		if (Position == 0 && TradeVolume > 0m && prevMacd is decimal prevMacdValue && prevSignal is decimal prevSignalValue && prevEma is decimal prevEmaValue)
		{
			if (ShouldEnterLong(macd, signal, prevMacdValue, prevSignalValue, emaCurrent, prevEmaValue, kValue, dValue))
			{
				EnterLong(candle);
			}
			else if (ShouldEnterShort(macd, signal, prevMacdValue, prevSignalValue, emaCurrent, prevEmaValue, kValue, dValue))
			{
				EnterShort(candle);
			}
		}

		_prevMacd = macd;
		_prevSignal = signal;
		_prevEma = emaCurrent;
	}

	private void ManageActivePosition(ICandleMessage candle, decimal macd, decimal signal, decimal kValue, decimal dValue, decimal? prevMacd, decimal? prevSignal)
	{
		if (Position > 0)
		{
			if (ShouldExitLong(macd, signal, prevMacd, prevSignal, kValue, dValue))
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_stopPrice > 0m && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_takePrice > 0m && candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			UpdateLongTrailing(candle.ClosePrice);
		}
		else if (Position < 0)
		{
			if (ShouldExitShort(macd, signal, prevMacd, prevSignal, kValue, dValue))
			{
				BuyMarket(-Position);
				ResetPositionState();
				return;
			}

			if (_stopPrice > 0m && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return;
			}

			if (_takePrice > 0m && candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return;
			}

			UpdateShortTrailing(candle.ClosePrice);
		}
	}

	private bool ShouldEnterLong(decimal macd, decimal signal, decimal prevMacd, decimal prevSignal, decimal emaCurrent, decimal emaPrevious, decimal kValue, decimal dValue)
	{
		if (kValue <= dValue)
		{
			return false;
		}

		if (!(macd < 0m && macd > signal && prevMacd < prevSignal))
		{
			return false;
		}

		if (Math.Abs(macd) <= MacdOpenLevel * _tickSize)
		{
			return false;
		}

		return emaCurrent > emaPrevious;
	}

	private bool ShouldEnterShort(decimal macd, decimal signal, decimal prevMacd, decimal prevSignal, decimal emaCurrent, decimal emaPrevious, decimal kValue, decimal dValue)
	{
		if (kValue >= dValue)
		{
			return false;
		}

		if (!(macd > 0m && macd < signal && prevMacd > prevSignal))
		{
			return false;
		}

		if (macd <= MacdOpenLevel * _tickSize)
		{
			return false;
		}

		return emaCurrent < emaPrevious;
	}

	private bool ShouldExitLong(decimal macd, decimal signal, decimal? prevMacd, decimal? prevSignal, decimal kValue, decimal dValue)
	{
		if (kValue >= dValue)
		{
			return false;
		}

		if (!(macd > 0m && macd < signal))
		{
			return false;
		}

		if (prevMacd is not decimal prevMacdValue || prevSignal is not decimal prevSignalValue || prevMacdValue <= prevSignalValue)
		{
			return false;
		}

		return macd > MacdCloseLevel * _tickSize;
	}

	private bool ShouldExitShort(decimal macd, decimal signal, decimal? prevMacd, decimal? prevSignal, decimal kValue, decimal dValue)
	{
		if (kValue <= dValue)
		{
			return false;
		}

		if (!(macd < 0m && macd > signal))
		{
			return false;
		}

		if (prevMacd is not decimal prevMacdValue || prevSignal is not decimal prevSignalValue || prevMacdValue >= prevSignalValue)
		{
			return false;
		}

		return Math.Abs(macd) > MacdCloseLevel * _tickSize;
	}

	private void EnterLong(ICandleMessage candle)
	{
		BuyMarket(TradeVolume);
		InitializePositionState(candle.ClosePrice, true);
	}

	private void EnterShort(ICandleMessage candle)
	{
		SellMarket(TradeVolume);
		InitializePositionState(candle.ClosePrice, false);
	}

	private void InitializePositionState(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		if (_tickSize <= 0m)
		{
			_tickSize = Security?.PriceStep ?? 1m;
		}

		var stopDistance = StopLossPoints > 0m ? StopLossPoints * _tickSize : 0m;
		var takeDistance = TakeProfitPoints > 0m ? TakeProfitPoints * _tickSize : 0m;
		var trailingDistance = TrailingStopPoints > 0m ? TrailingStopPoints * _tickSize : 0m;

		if (isLong)
		{
			_stopPrice = stopDistance > 0m ? entryPrice - stopDistance : 0m;
			_takePrice = takeDistance > 0m ? entryPrice + takeDistance : 0m;

			if (trailingDistance > 0m)
			{
				var trailingStop = entryPrice - trailingDistance;
				if (_stopPrice == 0m || trailingStop > _stopPrice)
				{
					_stopPrice = trailingStop;
				}
			}
		}
		else
		{
			_stopPrice = stopDistance > 0m ? entryPrice + stopDistance : 0m;
			_takePrice = takeDistance > 0m ? entryPrice - takeDistance : 0m;

			if (trailingDistance > 0m)
			{
				var trailingStop = entryPrice + trailingDistance;
				if (_stopPrice == 0m || trailingStop < _stopPrice)
				{
					_stopPrice = trailingStop;
				}
			}
		}
	}

	private void UpdateLongTrailing(decimal currentPrice)
	{
		if (TrailingStopPoints <= 0m)
		{
			return;
		}

		var trailingDistance = TrailingStopPoints * _tickSize;
		if (trailingDistance <= 0m)
		{
			return;
		}

		if (currentPrice - _entryPrice <= trailingDistance)
		{
			return;
		}

		var candidate = currentPrice - trailingDistance;
		if (candidate > _stopPrice)
		{
			_stopPrice = candidate;
		}
	}

	private void UpdateShortTrailing(decimal currentPrice)
	{
		if (TrailingStopPoints <= 0m)
		{
			return;
		}

		var trailingDistance = TrailingStopPoints * _tickSize;
		if (trailingDistance <= 0m)
		{
			return;
		}

		if (_entryPrice - currentPrice <= trailingDistance)
		{
			return;
		}

		var candidate = currentPrice + trailingDistance;
		if (_stopPrice == 0m || candidate < _stopPrice)
		{
			_stopPrice = candidate;
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}
}

