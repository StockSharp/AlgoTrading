using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Converted Anubis strategy that combines higher timeframe CCI and standard deviation with MACD signals.
/// </summary>
public class AnubisStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _cciThreshold;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _breakevenPips;
	private readonly StrategyParam<decimal> _thresholdPips;
	private readonly StrategyParam<decimal> _takeStdMultiplier;
	private readonly StrategyParam<decimal> _closeAtrMultiplier;
	private readonly StrategyParam<decimal> _spacingPips;
	private readonly StrategyParam<int> _maxLongPositions;
	private readonly StrategyParam<int> _maxShortPositions;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _stdFastLength;
	private readonly StrategyParam<int> _stdSlowLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly DataType _higherTimeFrame = TimeSpan.FromHours(4).TimeFrame();

	private AverageTrueRange _atrIndicator = null!;
	private CommodityChannelIndex _cciIndicator = null!;
	private StandardDeviation _fastStdDev = null!;
	private StandardDeviation _slowStdDev = null!;
	private MovingAverageConvergenceDivergenceSignal _macdIndicator = null!;

	private decimal _lastAtr;
	private bool _atrReady;
	private decimal _cciValue;
	private decimal _stdFastValue;
	private decimal _stdSlowValue;
	private bool _higherReady;

	private decimal _macdMainPrev1;
	private decimal _macdMainPrev2;
	private decimal _macdSignalPrev1;
	private decimal _macdSignalPrev2;
	private int _macdSamples;

	private decimal _prevCandleOpen;
	private decimal _prevCandleClose;
	private bool _hasPrevCandle;

	private decimal _adjustedPoint;
	private decimal _stopLossDistance;
	private decimal _breakevenDistance;
	private decimal _thresholdDistance;
	private decimal _spacingDistance;

	private decimal _longStopPrice;
	private decimal _longTakePrice;
	private bool _longBreakevenActivated;
	private int _longEntries;
	private DateTimeOffset? _lastLongSignalTime;
	private decimal _lastLongPrice;

	private decimal _shortStopPrice;
	private decimal _shortTakePrice;
	private bool _shortBreakevenActivated;
	private int _shortEntries;
	private DateTimeOffset? _lastShortSignalTime;
	private decimal _lastShortPrice;

	/// <summary>
	/// Trade volume for each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// CCI threshold for overbought/oversold detection.
	/// </summary>
	public decimal CciThreshold
	{
		get => _cciThreshold.Value;
		set => _cciThreshold.Value = value;
	}

	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
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
	/// Breakeven activation distance in pips.
	/// </summary>
	public decimal BreakevenPips
	{
		get => _breakevenPips.Value;
		set => _breakevenPips.Value = value;
	}

	/// <summary>
	/// Profit locking threshold in pips for MACD exit.
	/// </summary>
	public decimal ThresholdPips
	{
		get => _thresholdPips.Value;
		set => _thresholdPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the slow standard deviation for take-profit.
	/// </summary>
	public decimal TakeStdMultiplier
	{
		get => _takeStdMultiplier.Value;
		set => _takeStdMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier used for candle range exit.
	/// </summary>
	public decimal CloseAtrMultiplier
	{
		get => _closeAtrMultiplier.Value;
		set => _closeAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum spacing between sequential entries in pips.
	/// </summary>
	public decimal SpacingPips
	{
		get => _spacingPips.Value;
		set => _spacingPips.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous long entries.
	/// </summary>
	public int MaxLongPositions
	{
		get => _maxLongPositions.Value;
		set => _maxLongPositions.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous short entries.
	/// </summary>
	public int MaxShortPositions
	{
		get => _maxShortPositions.Value;
		set => _maxShortPositions.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal smoothing length for MACD.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// ATR calculation length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Fast standard deviation length.
	/// </summary>
	public int StdFastLength
	{
		get => _stdFastLength.Value;
		set => _stdFastLength.Value = value;
	}

	/// <summary>
	/// Slow standard deviation length.
	/// </summary>
	public int StdSlowLength
	{
		get => _stdSlowLength.Value;
		set => _stdSlowLength.Value = value;
	}

	/// <summary>
	/// Main candle type used for MACD and ATR.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="AnubisStrategy"/> with default parameters.
	/// </summary>
	public AnubisStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order size used for entries", "Trading");

		_cciThreshold = Param(nameof(CciThreshold), 80m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Threshold", "Absolute CCI level used to detect extremes", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI lookback on the higher timeframe", "Indicators");

		_stopLossPips = Param(nameof(StopLossPips), 100m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance measured in pips", "Risk");

		_breakevenPips = Param(nameof(BreakevenPips), 65m)
			.SetDisplay("Breakeven (pips)", "Distance to move stop to entry", "Risk");

		_thresholdPips = Param(nameof(ThresholdPips), 28m)
			.SetDisplay("MACD Exit Threshold (pips)", "Extra profit required before MACD exit", "Risk");

		_takeStdMultiplier = Param(nameof(TakeStdMultiplier), 2.9m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiplier", "Multiplier for higher timeframe standard deviation", "Risk");

		_closeAtrMultiplier = Param(nameof(CloseAtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Previous candle range multiplier for exits", "Risk");

		_spacingPips = Param(nameof(SpacingPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Spacing (pips)", "Minimum distance between consecutive entries", "Trading");

		_maxLongPositions = Param(nameof(MaxLongPositions), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Long Entries", "Maximum stacked long positions", "Trading");

		_maxShortPositions = Param(nameof(MaxShortPositions), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Short Entries", "Maximum stacked short positions", "Trading");

		_macdFastLength = Param(nameof(MacdFastLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Length", "Fast EMA period for MACD", "Indicators");

		_macdSlowLength = Param(nameof(MacdSlowLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Length", "Slow EMA period for MACD", "Indicators");

		_macdSignalLength = Param(nameof(MacdSignalLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Length", "Signal smoothing for MACD", "Indicators");

		_atrLength = Param(nameof(AtrLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR lookback on the main timeframe", "Indicators");

		_stdFastLength = Param(nameof(StdFastLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast StdDev Length", "SMA based standard deviation period", "Indicators");

		_stdSlowLength = Param(nameof(StdSlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow StdDev Length", "Secondary standard deviation period used for take-profit", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Signal Candle Type", "Timeframe used for MACD and ATR", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, _higherTimeFrame);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastAtr = 0m;
		_atrReady = false;
		_cciValue = 0m;
		_stdFastValue = 0m;
		_stdSlowValue = 0m;
		_higherReady = false;

		_macdMainPrev1 = 0m;
		_macdMainPrev2 = 0m;
		_macdSignalPrev1 = 0m;
		_macdSignalPrev2 = 0m;
		_macdSamples = 0;

		_prevCandleOpen = 0m;
		_prevCandleClose = 0m;
		_hasPrevCandle = false;

		_adjustedPoint = 0m;
		_stopLossDistance = 0m;
		_breakevenDistance = 0m;
		_thresholdDistance = 0m;
		_spacingDistance = 0m;

		_longStopPrice = 0m;
		_longTakePrice = 0m;
		_longBreakevenActivated = false;
		_longEntries = 0;
		_lastLongSignalTime = null;
		_lastLongPrice = 0m;

		_shortStopPrice = 0m;
		_shortTakePrice = 0m;
		_shortBreakevenActivated = false;
		_shortEntries = 0;
		_lastShortSignalTime = null;
		_lastShortPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		StartProtection();

		InitializeIndicators();
		InitializeDistances();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atrIndicator, (candle, atrValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				_lastAtr = atrValue;
				_atrReady = _atrIndicator.IsFormed;
			})
			.BindEx(_macdIndicator, ProcessMainCandle)
			.Start();

		SubscribeCandles(_higherTimeFrame)
			.Bind(_fastStdDev, _slowStdDev, _cciIndicator, ProcessHigherCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macdIndicator);
			DrawIndicator(area, _cciIndicator);
			DrawOwnTrades(area);
		}
	}

	private void InitializeIndicators()
	{
		// Create indicator instances based on the latest parameters.
		_atrIndicator = new AverageTrueRange { Length = AtrLength };
		_cciIndicator = new CommodityChannelIndex { Length = CciPeriod };
		_fastStdDev = new StandardDeviation { Length = StdFastLength };
		_slowStdDev = new StandardDeviation { Length = StdSlowLength };
		_macdIndicator = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength }
		};
	}

	private void InitializeDistances()
	{
		// Normalize pip-based settings into absolute price distances.
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 0.0001m;

		_adjustedPoint = step;
		if (step > 0m && step < 0.01m)
			_adjustedPoint = step * 10m;

		_stopLossDistance = StopLossPips * _adjustedPoint;
		_breakevenDistance = BreakevenPips * _adjustedPoint;
		_thresholdDistance = ThresholdPips * _adjustedPoint;
		_spacingDistance = SpacingPips * _adjustedPoint;
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal fastStd, decimal slowStd, decimal cci)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store higher timeframe indicator values for the main signal evaluation.
		_stdFastValue = fastStd;
		_stdSlowValue = slowStd;
		_cciValue = cci;
		_higherReady = _fastStdDev.IsFormed && _slowStdDev.IsFormed && _cciIndicator.IsFormed;
	}

	private void ProcessMainCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
			return;

		if (macdTyped.Macd is not decimal macdCurrent || macdTyped.Signal is not decimal signalCurrent)
			return;

		// Reset cached targets whenever the strategy becomes flat.
		if (Position <= 0m && _longEntries > 0)
			ResetLongTargets();

		if (Position >= 0m && _shortEntries > 0)
			ResetShortTargets();

		var macd1 = _macdMainPrev1;
		var macd2 = _macdMainPrev2;
		var signal1 = _macdSignalPrev1;
		var signal2 = _macdSignalPrev2;
		var hasMacdHistory = _macdSamples >= 2;

		var price = candle.ClosePrice;

		// Wait until all indicators provide valid data before trading.
		if (!IsFormedAndOnlineAndAllowTrading() || !_higherReady || !_atrReady || !hasMacdHistory || _stdSlowValue <= 0m)
		{
			UpdateStateAfterProcess(macdCurrent, signalCurrent, candle);
			return;
		}

		var cci = _cciValue;
		var takeDistance = TakeStdMultiplier * _stdSlowValue;

		// Evaluate entry signals on MACD crosses and higher timeframe CCI extremes.
		var openBuy = cci < -CciThreshold && macd2 <= signal2 && macd1 > signal1 && macd1 < 0m;
		var openSell = cci > CciThreshold && macd2 >= signal2 && macd1 < signal1 && macd1 > 0m;

		if (openBuy)
		{
			// Close opposite exposure before opening a new long.
			if (Position < 0m)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortTargets();
			}

			// Apply stacking rules and spacing filters.
			var allowEntry = Position >= 0m && _longEntries < MaxLongPositions && takeDistance > 0m;
			var spacedEnough = _lastLongPrice == 0m || Math.Abs(price - _lastLongPrice) > _spacingDistance;
			var newBar = _lastLongSignalTime != candle.OpenTime;

			if (allowEntry && spacedEnough && newBar)
			{
				BuyMarket(TradeVolume);
				_longEntries++;
				_lastLongPrice = price;
				_lastLongSignalTime = candle.OpenTime;
				_longStopPrice = _stopLossDistance > 0m ? price - _stopLossDistance : 0m;
				_longTakePrice = takeDistance > 0m ? price + takeDistance : 0m;
				_longBreakevenActivated = false;
			}
		}
		else if (openSell)
		{
			// Close opposite exposure before opening a new short.
			if (Position > 0m)
			{
				SellMarket(Math.Abs(Position));
				ResetLongTargets();
			}

			// Apply stacking rules and spacing filters.
			var allowEntry = Position <= 0m && _shortEntries < MaxShortPositions && takeDistance > 0m;
			var spacedEnough = _lastShortPrice == 0m || Math.Abs(price - _lastShortPrice) > _spacingDistance;
			var newBar = _lastShortSignalTime != candle.OpenTime;

			if (allowEntry && spacedEnough && newBar)
			{
				SellMarket(TradeVolume);
				_shortEntries++;
				_lastShortPrice = price;
				_lastShortSignalTime = candle.OpenTime;
				_shortStopPrice = _stopLossDistance > 0m ? price + _stopLossDistance : 0m;
				_shortTakePrice = takeDistance > 0m ? price - takeDistance : 0m;
				_shortBreakevenActivated = false;
			}
		}

		UpdateBreakeven(price);

		if (Position > 0m)
		{
			var prevRange = _hasPrevCandle ? _prevCandleClose - _prevCandleOpen : 0m;
			var exitByRange = _hasPrevCandle && prevRange > CloseAtrMultiplier * _lastAtr;
			var exitByMacd = macd1 < macd2 && price - PositionPrice > _thresholdDistance;

			// Check range-based and MACD-based exit conditions.
			if (exitByRange || exitByMacd)
			{
				SellMarket(Math.Abs(Position));
				ResetLongTargets();
			}
			else
			{
				CheckLongStops(price);
			}
		}
		else if (Position < 0m)
		{
			var prevRange = _hasPrevCandle ? _prevCandleOpen - _prevCandleClose : 0m;
			var exitByRange = _hasPrevCandle && prevRange > CloseAtrMultiplier * _lastAtr;
			var exitByMacd = macd1 > macd2 && PositionPrice - price > _thresholdDistance;

			if (exitByRange || exitByMacd)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortTargets();
			}
			else
			{
				CheckShortStops(price);
			}
		}
		else
		{
			// Clear cached targets when no positions are open.
			ResetLongTargets();
			ResetShortTargets();
		}

		UpdateStateAfterProcess(macdCurrent, signalCurrent, candle);
	}

	private void UpdateBreakeven(decimal price)
	{
		// Long side breakeven management.
		if (Position > 0m && !_longBreakevenActivated && _breakevenDistance > 0m && price - _breakevenDistance > PositionPrice && _longStopPrice > 0m)
		{
			_longBreakevenActivated = true;
			_longStopPrice = PositionPrice;
		}
		else if (Position <= 0m)
		{
			_longBreakevenActivated = false;
		}

		// Short side breakeven management.
		if (Position < 0m && !_shortBreakevenActivated && _breakevenDistance > 0m && price + _breakevenDistance < PositionPrice && _shortStopPrice > 0m)
		{
			_shortBreakevenActivated = true;
			_shortStopPrice = PositionPrice;
		}
		else if (Position >= 0m)
		{
			_shortBreakevenActivated = false;
		}
	}

	private void CheckLongStops(decimal price)
	{
		if (Position <= 0m)
			return;

		// Exit long positions when price hits the take-profit level.
		if (_longTakePrice > 0m && price >= _longTakePrice)
		{
			SellMarket(Math.Abs(Position));
			ResetLongTargets();
			return;
		}

		// Exit long positions when price returns to the protective stop.
		if (_longStopPrice > 0m && price <= _longStopPrice)
		{
			SellMarket(Math.Abs(Position));
			ResetLongTargets();
		}
	}

	private void CheckShortStops(decimal price)
	{
		if (Position >= 0m)
			return;

		// Exit short positions when price hits the take-profit level.
		if (_shortTakePrice > 0m && price <= _shortTakePrice)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortTargets();
			return;
		}

		// Exit short positions when price returns to the protective stop.
		if (_shortStopPrice > 0m && price >= _shortStopPrice)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortTargets();
		}
	}

	private void ResetLongTargets()
	{
		if (Position > 0m)
			return;

		// Clear long-specific cached values once the position is closed.
		_longStopPrice = 0m;
		_longTakePrice = 0m;
		_longBreakevenActivated = false;
		_longEntries = 0;
		_lastLongPrice = 0m;
		_lastLongSignalTime = null;
	}

	private void ResetShortTargets()
	{
		if (Position < 0m)
			return;

		// Clear short-specific cached values once the position is closed.
		_shortStopPrice = 0m;
		_shortTakePrice = 0m;
		_shortBreakevenActivated = false;
		_shortEntries = 0;
		_lastShortPrice = 0m;
		_lastShortSignalTime = null;
	}

	private void UpdateStateAfterProcess(decimal macdCurrent, decimal signalCurrent, ICandleMessage candle)
	{
		// Shift stored MACD values and remember the candle data for the next iteration.
		_macdMainPrev2 = _macdMainPrev1;
		_macdMainPrev1 = macdCurrent;
		_macdSignalPrev2 = _macdSignalPrev1;
		_macdSignalPrev1 = signalCurrent;
		if (_macdSamples < 2)
			_macdSamples++;

		_prevCandleOpen = candle.OpenPrice;
		_prevCandleClose = candle.ClosePrice;
		_hasPrevCandle = true;
	}
}
