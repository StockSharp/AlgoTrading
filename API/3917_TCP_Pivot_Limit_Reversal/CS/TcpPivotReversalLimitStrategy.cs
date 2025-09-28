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
/// Pivot based breakout-reversion strategy converted from the gpfTCPivotLimit MQL expert.
/// Calculates classic daily pivot levels and trades hourly candles around them.
/// </summary>
public class TcpPivotReversalLimitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _targetMode;
	private readonly StrategyParam<decimal> _trailingPoints;
	private readonly StrategyParam<bool> _intradayTrading;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pivot;
	private decimal _resistance1;
	private decimal _resistance2;
	private decimal _resistance3;
	private decimal _support1;
	private decimal _support2;
	private decimal _support3;

	private bool _pivotReady;
	private DateTime? _currentDay;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _dayClose;
	private decimal _previousDayHigh;
	private decimal _previousDayLow;
	private decimal _previousDayClose;

	private ICandleMessage _previousCandle;
	private ICandleMessage _previousPreviousCandle;

	private PositionSides _positionSide = PositionSides.Flat;
	private decimal? _currentStopPrice;
	private decimal? _currentTargetPrice;
	private decimal? _trailingExtreme;
	private decimal _priceStep;
	private decimal _trailingDistance;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TcpPivotReversalLimitStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Base order volume for entries", "General");

		_targetMode = Param(nameof(TargetMode), 1)
		.SetDisplay("Target Mode", "Selects which pivot levels are used for entries and exits", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1, 5, 1);

		_trailingPoints = Param(nameof(TrailingPoints), 30m)
		.SetNotNegative()
		.SetDisplay("Trailing Points", "Trailing stop distance in price points", "Risk");

		_intradayTrading = Param(nameof(IntradayTrading), false)
		.SetDisplay("Intraday Mode", "Close positions at the end of each trading day", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time-frame used for calculations (hourly in the original script)", "General");
	}

	/// <summary>
	/// Order volume used for new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Selects the pivot combination (1 to 5) from the original script.
	/// </summary>
	public int TargetMode
	{
		get => _targetMode.Value;
		set => _targetMode.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points (multiples of the security price step).
	/// </summary>
	public decimal TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// Enables closing positions at the end of each day (23:00 candle close).
	/// </summary>
	public bool IntradayTrading
	{
		get => _intradayTrading.Value;
		set => _intradayTrading.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy (hourly by default).
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_pivot = 0m;
		_resistance1 = 0m;
		_resistance2 = 0m;
		_resistance3 = 0m;
		_support1 = 0m;
		_support2 = 0m;
		_support3 = 0m;
		_pivotReady = false;
		_currentDay = null;
		_dayHigh = 0m;
		_dayLow = 0m;
		_dayClose = 0m;
		_previousDayHigh = 0m;
		_previousDayLow = 0m;
		_previousDayClose = 0m;
		_previousCandle = null;
		_previousPreviousCandle = null;
		_positionSide = PositionSides.Flat;
		_currentStopPrice = null;
		_currentTargetPrice = null;
		_trailingExtreme = null;
		_priceStep = 0m;
		_trailingDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		_priceStep = Security?.PriceStep ?? 0m;
		_trailingDistance = _priceStep > 0m ? TrailingPoints * _priceStep : 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateDailyStatistics(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			ShiftCandles(candle);
			return;
		}

		ManageExistingPosition(candle);

		if (Position == 0m)
		TryEnter(candle);

		ShiftCandles(candle);
	}

	private void UpdateDailyStatistics(ICandleMessage candle)
	{
		var candleDate = candle.OpenTime.Date;

		if (_currentDay == null)
		{
			_currentDay = candleDate;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_dayClose = candle.ClosePrice;
			return;
		}

		if (_currentDay.Value != candleDate)
		{
			_previousDayHigh = _dayHigh;
			_previousDayLow = _dayLow;
			_previousDayClose = _dayClose;
			CalculatePivotLevels();

			_currentDay = candleDate;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_dayClose = candle.ClosePrice;
			return;
		}

		_dayHigh = Math.Max(_dayHigh, candle.HighPrice);
		_dayLow = Math.Min(_dayLow, candle.LowPrice);
		_dayClose = candle.ClosePrice;
	}

	private void CalculatePivotLevels()
	{
		if (_previousDayHigh <= 0m && _previousDayLow <= 0m && _previousDayClose <= 0m)
		{
			_pivotReady = false;
			return;
		}

		_pivot = (_previousDayHigh + _previousDayLow + _previousDayClose) / 3m;
		_resistance1 = 2m * _pivot - _previousDayLow;
		_support1 = 2m * _pivot - _previousDayHigh;
		_resistance2 = _pivot + (_resistance1 - _support1);
		_support2 = _pivot - (_resistance1 - _support1);
		_resistance3 = _previousDayHigh + 2m * (_pivot - _previousDayLow);
		_support3 = _previousDayLow - 2m * (_previousDayHigh - _pivot);

		_pivotReady = true;

		LogInfo($"New pivot calculated. Pivot={_pivot:F5}, R1={_resistance1:F5}, R2={_resistance2:F5}, R3={_resistance3:F5}, S1={_support1:F5}, S2={_support2:F5}, S3={_support3:F5}");
	}

	private void ManageExistingPosition(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			ResetPositionState();
			return;
		}

		if (_positionSide == PositionSides.Flat)
		_positionSide = Position > 0m ? PositionSides.Long : PositionSides.Short;

		if (IntradayTrading && candle.CloseTime.Hour == 23)
		{
			LogInfo("Intraday close triggered.");
			ClosePosition();
			ResetPositionState();
			return;
		}

		if (_positionSide == PositionSides.Long)
		{
			if (_currentStopPrice.HasValue && candle.LowPrice <= _currentStopPrice.Value)
			{
				LogInfo($"Long stop reached at {_currentStopPrice.Value:F5}.");
				ClosePosition();
				ResetPositionState();
				return;
			}

			if (_currentTargetPrice.HasValue && candle.HighPrice >= _currentTargetPrice.Value)
			{
				LogInfo($"Long take-profit reached at {_currentTargetPrice.Value:F5}.");
				ClosePosition();
				ResetPositionState();
				return;
			}

			UpdateLongTrailing(candle);
		}
		else if (_positionSide == PositionSides.Short)
		{
			if (_currentStopPrice.HasValue && candle.HighPrice >= _currentStopPrice.Value)
			{
				LogInfo($"Short stop reached at {_currentStopPrice.Value:F5}.");
				ClosePosition();
				ResetPositionState();
				return;
			}

			if (_currentTargetPrice.HasValue && candle.LowPrice <= _currentTargetPrice.Value)
			{
				LogInfo($"Short take-profit reached at {_currentTargetPrice.Value:F5}.");
				ClosePosition();
				ResetPositionState();
				return;
			}

			UpdateShortTrailing(candle);
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_trailingDistance <= 0m || Position <= 0m)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
		return;

		if (!_trailingExtreme.HasValue)
		{
			if (candle.HighPrice - entryPrice >= _trailingDistance)
			{
				_trailingExtreme = candle.HighPrice;
				LogInfo($"Long trailing activated at {_trailingExtreme.Value:F5}.");
			}
			return;
		}

		if (candle.HighPrice > _trailingExtreme.Value)
		_trailingExtreme = candle.HighPrice;

		var exitLevel = _trailingExtreme.Value - _trailingDistance;
		if (candle.LowPrice <= exitLevel)
		{
			LogInfo($"Long trailing stop triggered at {exitLevel:F5}.");
			ClosePosition();
			ResetPositionState();
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_trailingDistance <= 0m || Position >= 0m)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
		return;

		if (!_trailingExtreme.HasValue)
		{
			if (entryPrice - candle.LowPrice >= _trailingDistance)
			{
				_trailingExtreme = candle.LowPrice;
				LogInfo($"Short trailing activated at {_trailingExtreme.Value:F5}.");
			}
			return;
		}

		if (candle.LowPrice < _trailingExtreme.Value)
		_trailingExtreme = candle.LowPrice;

		var exitLevel = _trailingExtreme.Value + _trailingDistance;
		if (candle.HighPrice >= exitLevel)
		{
			LogInfo($"Short trailing stop triggered at {exitLevel:F5}.");
			ClosePosition();
			ResetPositionState();
		}
	}

	private void TryEnter(ICandleMessage candle)
	{
		if (!_pivotReady || _previousCandle == null || _previousPreviousCandle == null)
		return;

		if (Volume <= 0m)
		return;

		var (sellLevel, sellStop, sellTarget, buyLevel, buyStop, buyTarget) = GetLevelsForMode();

		if (ShouldEnterShort(_previousPreviousCandle, _previousCandle, sellLevel))
		{
			LogInfo($"Opening short. Level={sellLevel:F5}, Stop={sellStop:F5}, Target={sellTarget:F5}");
			SellMarket(Volume);
			_positionSide = PositionSides.Short;
			_currentStopPrice = sellStop;
			_currentTargetPrice = sellTarget;
			_trailingExtreme = null;
			return;
		}

		if (ShouldEnterLong(_previousPreviousCandle, _previousCandle, buyLevel))
		{
			LogInfo($"Opening long. Level={buyLevel:F5}, Stop={buyStop:F5}, Target={buyTarget:F5}");
			BuyMarket(Volume);
			_positionSide = PositionSides.Long;
			_currentStopPrice = buyStop;
			_currentTargetPrice = buyTarget;
			_trailingExtreme = null;
		}
	}

	private (decimal sellLevel, decimal sellStop, decimal sellTarget, decimal buyLevel, decimal buyStop, decimal buyTarget) GetLevelsForMode()
	{
		var mode = Math.Clamp(TargetMode, 1, 5);

		return mode switch
		{
			1 => (_resistance1, _resistance2, _support1, _support1, _support2, _resistance1),
			2 => (_resistance1, _resistance2, _support2, _support1, _support2, _resistance2),
			3 => (_resistance2, _resistance3, _support1, _support2, _support3, _resistance1),
			4 => (_resistance2, _resistance3, _support2, _support2, _support3, _resistance2),
			_ => (_resistance2, _resistance3, _support3, _support2, _support3, _resistance3),
		};
	}

	private static bool ShouldEnterShort(ICandleMessage twoAgo, ICandleMessage oneAgo, decimal level)
	{
		if (level == 0m)
		return false;

		return (twoAgo.HighPrice > level || twoAgo.ClosePrice >= level)
		&& twoAgo.OpenPrice < level
		&& oneAgo.ClosePrice <= level;
	}

	private static bool ShouldEnterLong(ICandleMessage twoAgo, ICandleMessage oneAgo, decimal level)
	{
		if (level == 0m)
		return false;

		return (twoAgo.LowPrice < level || twoAgo.ClosePrice <= level)
		&& twoAgo.OpenPrice > level
		&& oneAgo.ClosePrice >= level;
	}

	private void ShiftCandles(ICandleMessage candle)
	{
		_previousPreviousCandle = _previousCandle;
		_previousCandle = candle;
	}

	private void ResetPositionState()
	{
		if (Position == 0m)
		{
			_positionSide = PositionSides.Flat;
			_currentStopPrice = null;
			_currentTargetPrice = null;
			_trailingExtreme = null;
		}
	}

	private enum PositionSides
	{
		Flat,
		Long,
		Short
	}
}

