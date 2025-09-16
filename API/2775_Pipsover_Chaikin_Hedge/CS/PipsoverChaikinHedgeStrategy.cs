using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chaikin oscillator oversold/overbought strategy with optional reversal hedging and trailing management.
/// </summary>
public class PipsoverChaikinHedgeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _openLevel;
	private readonly StrategyParam<decimal> _closeLevel;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _breakevenPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageTypeOption> _maType;
	private readonly StrategyParam<int> _chaikinFastPeriod;
	private readonly StrategyParam<int> _chaikinSlowPeriod;
	private readonly StrategyParam<MovingAverageTypeOption> _chaikinMaType;
	private readonly StrategyParam<DataType> _candleType;

	private AccumulationDistributionLine _adLine = null!;
	private IIndicator _priceMa = null!;
	private IIndicator _chaikinFast = null!;
	private IIndicator _chaikinSlow = null!;

	private readonly Queue<decimal> _maValues = new();

	private decimal _pipSize;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrevCandle;
	private decimal _prevChaikin;
	private bool _hasPrevChaikin;

	/// <summary>
	/// Chaikin threshold for entries.
	/// </summary>
	public decimal OpenLevel
	{
		get => _openLevel.Value;
		set => _openLevel.Value = value;
	}

	/// <summary>
	/// Chaikin threshold for hedging reversals.
	/// </summary>
	public decimal CloseLevel
	{
		get => _closeLevel.Value;
		set => _closeLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Breakeven activation distance in pips.
	/// </summary>
	public decimal BreakevenPips
	{
		get => _breakevenPips.Value;
		set => _breakevenPips.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Moving average shift in bars.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average type for price filter.
	/// </summary>
	public MovingAverageTypeOption MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Fast Chaikin moving average length.
	/// </summary>
	public int ChaikinFastPeriod
	{
		get => _chaikinFastPeriod.Value;
		set => _chaikinFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow Chaikin moving average length.
	/// </summary>
	public int ChaikinSlowPeriod
	{
		get => _chaikinSlowPeriod.Value;
		set => _chaikinSlowPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type used in Chaikin oscillator.
	/// </summary>
	public MovingAverageTypeOption ChaikinMaType
	{
		get => _chaikinMaType.Value;
		set => _chaikinMaType.Value = value;
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
	/// Initializes a new instance of the <see cref="PipsoverChaikinHedgeStrategy"/> class.
	/// </summary>
	public PipsoverChaikinHedgeStrategy()
	{
		_openLevel = Param(nameof(OpenLevel), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Open Level", "Chaikin level for entries", "Chaikin");

		_closeLevel = Param(nameof(CloseLevel), 125m)
		.SetGreaterThanZero()
		.SetDisplay("Close Level", "Chaikin level for hedging", "Chaikin");

		_stopLossPips = Param(nameof(StopLossPips), 65m)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_breakevenPips = Param(nameof(BreakevenPips), 15m)
		.SetDisplay("Breakeven (pips)", "Breakeven activation distance", "Risk");

		_maPeriod = Param(nameof(MaPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Price moving average length", "Trend");

		_maShift = Param(nameof(MaShift), 0)
		.SetDisplay("MA Shift", "Price moving average shift", "Trend");

		_maType = Param(nameof(MaType), MovingAverageTypeOption.Simple)
		.SetDisplay("MA Type", "Price moving average type", "Trend");

		_chaikinFastPeriod = Param(nameof(ChaikinFastPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Chaikin Fast", "Fast Chaikin length", "Chaikin");

		_chaikinSlowPeriod = Param(nameof(ChaikinSlowPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Chaikin Slow", "Slow Chaikin length", "Chaikin");

		_chaikinMaType = Param(nameof(ChaikinMaType), MovingAverageTypeOption.Exponential)
		.SetDisplay("Chaikin MA Type", "Chaikin moving average type", "Chaikin");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "Data");
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

		_maValues.Clear();
		_pipSize = 0m;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_prevOpen = 0m;
		_prevClose = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_prevChaikin = 0m;
		_hasPrevCandle = false;
		_hasPrevChaikin = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_adLine = new AccumulationDistributionLine();
		_priceMa = CreateMovingAverage(MaType, MaPeriod);
		_chaikinFast = CreateMovingAverage(ChaikinMaType, ChaikinFastPeriod);
		_chaikinSlow = CreateMovingAverage(ChaikinMaType, ChaikinSlowPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_priceMa, _adLine, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal adValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var prevOpen = _prevOpen;
		var prevClose = _prevClose;
		var prevHigh = _prevHigh;
		var prevLow = _prevLow;
		var prevChaikin = _prevChaikin;
		var hasPrevCandle = _hasPrevCandle;
		var hasPrevChaikin = _hasPrevChaikin;

		var fastValue = _chaikinFast.Process(new DecimalIndicatorValue(_chaikinFast, adValue, candle.Time));
		var slowValue = _chaikinSlow.Process(new DecimalIndicatorValue(_chaikinSlow, adValue, candle.Time));

		if (!fastValue.IsFinal || !slowValue.IsFinal)
		{
			_prevChaikin = fastValue.ToDecimal() - slowValue.ToDecimal();
			_hasPrevChaikin = true;
			StorePreviousCandle(candle);
			return;
		}

		var chaikin = fastValue.ToDecimal() - slowValue.ToDecimal();
		var shiftedMa = UpdateShiftedMa(maValue);

		if (shiftedMa is null)
		{
			_prevChaikin = chaikin;
			_hasPrevChaikin = true;
			StorePreviousCandle(candle);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevChaikin = chaikin;
			_hasPrevChaikin = true;
			StorePreviousCandle(candle);
			return;
		}

		var hasPrevData = hasPrevCandle && hasPrevChaikin;
		var positionClosed = HandleStopsAndTargets(candle);
		var reversed = false;

		if (Position == 0m)
		{
			if (hasPrevData)
			{
				var bullishPrev = prevClose > prevOpen;
				var bearishPrev = prevClose < prevOpen;

				if (bullishPrev && prevLow < shiftedMa && prevChaikin < -OpenLevel)
				{
					BuyMarket(Volume);
					SetupLongTargets(candle.ClosePrice);
				}
				else if (bearishPrev && prevHigh > shiftedMa && prevChaikin > OpenLevel)
				{
					SellMarket(Volume);
					SetupShortTargets(candle.ClosePrice);
				}
			}
		}
		else if (!positionClosed)
		{
			if (hasPrevData)
			{
				var bearishPrev = prevClose < prevOpen;
				var bullishPrev = prevClose > prevOpen;

				if (Position > 0m && bearishPrev && prevHigh > shiftedMa && prevChaikin > CloseLevel)
				{
					var size = Math.Abs(Position) + Volume;
					SellMarket(size);
					SetupShortTargets(candle.ClosePrice);
					reversed = true;
				}
				else if (Position < 0m && bullishPrev && prevLow < shiftedMa && prevChaikin < -CloseLevel)
				{
					var size = Math.Abs(Position) + Volume;
					BuyMarket(size);
					SetupLongTargets(candle.ClosePrice);
					reversed = true;
				}
			}

			if (!reversed)
			UpdateTrailing(candle);
		}

		_prevChaikin = chaikin;
		_hasPrevChaikin = true;
		StorePreviousCandle(candle);
	}
	private decimal? UpdateShiftedMa(decimal maValue)
	{
		var shift = Math.Max(0, MaShift);
		_maValues.Enqueue(maValue);

		while (_maValues.Count > shift + 1)
		_maValues.Dequeue();

		if (_maValues.Count < shift + 1)
		return null;

		using var enumerator = _maValues.GetEnumerator();
		for (var i = 0; i <= _maValues.Count - shift - 1; i++)
		enumerator.MoveNext();

		return enumerator.Current;
	}

	private void StorePreviousCandle(ICandleMessage candle)
	{
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_hasPrevCandle = true;
	}

	private bool HandleStopsAndTargets(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0m)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}
		}

		return false;
	}

	private void SetupLongTargets(decimal price)
	{
		_entryPrice = price;

		if (StopLossPips > 0m)
		_stopPrice = price - StopLossPips * _pipSize;
		else
		_stopPrice = null;

		if (TakeProfitPips > 0m)
		_takeProfitPrice = price + TakeProfitPips * _pipSize;
		else
		_takeProfitPrice = null;
	}

	private void SetupShortTargets(decimal price)
	{
		_entryPrice = price;

		if (StopLossPips > 0m)
		_stopPrice = price + StopLossPips * _pipSize;
		else
		_stopPrice = null;

		if (TakeProfitPips > 0m)
		_takeProfitPrice = price - TakeProfitPips * _pipSize;
		else
		_takeProfitPrice = null;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
		return;

		var breakevenDist = BreakevenPips > 0m ? BreakevenPips * _pipSize : 0m;
		var trailingDist = TrailingStopPips > 0m ? TrailingStopPips * _pipSize : 0m;

		if (Position > 0m)
		{
			var move = candle.ClosePrice - entry;

			if (breakevenDist > 0m && move > breakevenDist)
			{
				if (_stopPrice is null || _stopPrice < entry)
				_stopPrice = entry;
			}

			if (trailingDist > 0m)
			{
				var activation = breakevenDist + trailingDist;
				if (move > activation)
				{
					var newStop = candle.ClosePrice - trailingDist;
					if (_stopPrice is null || newStop > _stopPrice)
					_stopPrice = newStop;
				}
			}
		}
		else if (Position < 0m)
		{
			var move = entry - candle.ClosePrice;

			if (breakevenDist > 0m && move > breakevenDist)
			{
				if (_stopPrice is null || _stopPrice > entry)
				_stopPrice = entry;
			}

			if (trailingDist > 0m)
			{
				var activation = breakevenDist + trailingDist;
				if (move > activation)
				{
					var newStop = candle.ClosePrice + trailingDist;
					if (_stopPrice is null || newStop < _stopPrice)
					_stopPrice = newStop;
				}
			}
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		if (step <= 0m)
		step = 0.0001m;

		var tmp = step;
		var decimals = 0;

		while (tmp < 1m && decimals < 10)
		{
			tmp *= 10m;
			decimals++;
		}

		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static IIndicator CreateMovingAverage(MovingAverageTypeOption type, int length)
	{
		return type switch
		{
			MovingAverageTypeOption.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypeOption.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeOption.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeOption.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	/// <summary>
	/// Moving average options matching the MetaTrader configuration.
	/// </summary>
	public enum MovingAverageTypeOption
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}
}
