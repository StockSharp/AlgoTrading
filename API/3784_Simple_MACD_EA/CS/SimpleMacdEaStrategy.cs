using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that follows the Simple MACD EA logic ported from MQL.
/// Uses EMA differences to detect trend reversals and manages a trailing exit.
/// </summary>
public class SimpleMacdEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _macdLevel;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<int> _trailingIterationsLimit;
	private readonly StrategyParam<int> _waitTimeBeforeStopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousDirectionSignal;
	private decimal? _previousBestSignal;
	private int _trend;
	private int _previousTrend;
	private decimal _macdStrength;
	private int _pendingTime;
	private int _pace;
	private int _trailingUpdates;
	private bool _findHighest;
	private bool _findLowest;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// EMA length used in MACD calculations.
	/// </summary>
	public int MacdLevel
	{
		get => _macdLevel.Value;
		set => _macdLevel.Value = value;
	}

	/// <summary>
	/// Trailing stop in points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Maximum number of trailing adjustments.
	/// </summary>
	public int TrailingIterationsLimit
	{
		get => _trailingIterationsLimit.Value;
		set => _trailingIterationsLimit.Value = value;
	}

	/// <summary>
	/// Number of cycles to wait before activating the soft stop logic.
	/// </summary>
	public int WaitTimeBeforeStopLoss
	{
		get => _waitTimeBeforeStopLoss.Value;
		set => _waitTimeBeforeStopLoss.Value = value;
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
	/// Initializes a new instance of the <see cref="SimpleMacdEaStrategy"/> class.
	/// </summary>
	public SimpleMacdEaStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_macdLevel = Param(nameof(MacdLevel), 500)
			.SetGreaterThanZero()
			.SetDisplay("MACD Level", "EMA length for MACD difference", "Parameters");

		_trailingStop = Param(nameof(TrailingStop), 55m)
			.SetMinValue(0m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk");

		_trailingIterationsLimit = Param(nameof(TrailingIterationsLimit), 100)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Updates", "Maximum trailing stop updates", "Risk");

		_waitTimeBeforeStopLoss = Param(nameof(WaitTimeBeforeStopLoss), 10000)
			.SetMinMax(0, 1000000)
			.SetDisplay("Wait Cycles", "Cycles before enabling soft stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Data type for strategy", "General");
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

		_previousDirectionSignal = null;
		_previousBestSignal = null;
		_trend = 0;
		_previousTrend = 0;
		_macdStrength = 0m;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		base.Volume = Volume;

		var emaFast = new ExponentialMovingAverage
		{
			Length = 100
		};

		var emaSlow = new ExponentialMovingAverage
		{
			Length = MacdLevel
		};

		var emaSlowPlus = new ExponentialMovingAverage
		{
			Length = MacdLevel + 1
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaFast, emaSlow, emaSlowPlus, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal emaSlowPlus)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security?.PriceStep ?? 1m;
		var close = candle.ClosePrice;

		var directionSignal = emaFast - emaSlow;
		var bestSignal = emaSlow - emaSlowPlus;

		_macdStrength = _previousDirectionSignal is decimal prevDirection
			? Math.Abs(directionSignal - prevDirection)
			: 0m;

		var previousTrend = _trend;
		var currentTrend = directionSignal > 0m ? 1 : directionSignal < 0m ? -1 : 0;

		_previousTrend = previousTrend;
		_trend = currentTrend;

		if (Position != 0m)
		{
			ManageOpenPosition(candle, close, step, bestSignal, _previousBestSignal, previousTrend, currentTrend);

			_previousDirectionSignal = directionSignal;
			_previousBestSignal = bestSignal;
			return;
		}

		ResetPositionState();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousDirectionSignal = directionSignal;
			_previousBestSignal = bestSignal;
			return;
		}

		if (currentTrend > 0 && previousTrend < 0 && Position <= 0m)
		{
			EnterLong();
		}
		else if (currentTrend < 0 && previousTrend > 0 && Position >= 0m)
		{
			EnterShort();
		}

		_previousDirectionSignal = directionSignal;
		_previousBestSignal = bestSignal;
	}

	private void ManageOpenPosition(ICandleMessage candle, decimal close, decimal step, decimal currentBestSignal, decimal? previousBestSignal, int previousTrend, int currentTrend)
	{
		_pendingTime++;
		_pace++;

		var entryPrice = Position.AveragePrice ?? candle.ClosePrice;

		if (Position > 0m)
		{
			if (!_findHighest)
			{
				_findHighest = true;
				_findLowest = false;
			}

			if (TrailingStop > 0m && _pace > TrailingIterationsLimit && _trailingUpdates < TrailingIterationsLimit)
			{
				if (close - entryPrice > step * TrailingStop)
				{
					var newStop = close - step * TrailingStop;
					if (_longTrailingStop is null || newStop > _longTrailingStop.Value)
					{
						_longTrailingStop = newStop;
						_pace = 0;
						_trailingUpdates++;
						_pendingTime = 0;
					}
				}
			}

			if (previousBestSignal is decimal prevBest && _findHighest && close > entryPrice + step * 5m && currentBestSignal < prevBest)
			{
				ClosePositionAndReset();
				return;
			}

			if (_findHighest && _pendingTime > WaitTimeBeforeStopLoss)
			{
				var dynamicOffset = step * (decimal)(_pendingTime - WaitTimeBeforeStopLoss);
				if (close <= entryPrice + dynamicOffset)
				{
					ClosePositionAndReset();
					return;
				}
			}

			if (currentTrend < 0 && previousTrend > 0 && close > entryPrice + step * 5m)
			{
				ClosePositionAndReset();
				return;
			}

			if (TrailingStop > 0m)
			{
				if (_trailingUpdates >= TrailingIterationsLimit)
				{
					ClosePositionAndReset();
					return;
				}

				if (_longTrailingStop is decimal trail && close <= trail)
				{
					ClosePositionAndReset();
					return;
				}
			}
		}
		else if (Position < 0m)
		{
			if (!_findLowest)
			{
				_findLowest = true;
				_findHighest = false;
			}

			if (TrailingStop > 0m && _pace > TrailingIterationsLimit && _trailingUpdates < TrailingIterationsLimit)
			{
				if (entryPrice - close > step * TrailingStop)
				{
					var newStop = close + step * TrailingStop;
					if (_shortTrailingStop is null || newStop < _shortTrailingStop.Value)
					{
						_shortTrailingStop = newStop;
						_pace = 0;
						_trailingUpdates++;
						_pendingTime = 0;
					}
				}
			}

			if (previousBestSignal is decimal prevBest && _findLowest && close < entryPrice - step * 5m && currentBestSignal > prevBest)
			{
				ClosePositionAndReset();
				return;
			}

			if (_findLowest && _pendingTime > WaitTimeBeforeStopLoss)
			{
				var dynamicOffset = step * (decimal)(_pendingTime - WaitTimeBeforeStopLoss);
				if (close >= entryPrice - dynamicOffset)
				{
					ClosePositionAndReset();
					return;
				}
			}

			if (currentTrend > 0 && previousTrend < 0 && close < entryPrice - step * 5m)
			{
				ClosePositionAndReset();
				return;
			}

			if (TrailingStop > 0m)
			{
				if (_trailingUpdates >= TrailingIterationsLimit)
				{
					ClosePositionAndReset();
					return;
				}

				if (_shortTrailingStop is decimal trail && close >= trail)
				{
					ClosePositionAndReset();
					return;
				}
			}
		}
	}

	private void EnterLong()
	{
		var volume = Volume + (Position < 0m ? Math.Abs(Position) : 0m);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_findHighest = true;
		_findLowest = false;
		_pendingTime = 0;
		_pace = TrailingIterationsLimit;
		_trailingUpdates = 0;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private void EnterShort()
	{
		var volume = Volume + (Position > 0m ? Math.Abs(Position) : 0m);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_findHighest = false;
		_findLowest = true;
		_pendingTime = 0;
		_pace = TrailingIterationsLimit;
		_trailingUpdates = 0;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private void ClosePositionAndReset()
	{
		ClosePosition();
		ResetPositionState();
	}

	private void ResetPositionState()
	{
		_pendingTime = 0;
		_pace = 0;
		_trailingUpdates = 0;
		_findHighest = false;
		_findLowest = false;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}
}
