using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku Kijun-sen robot converted from MetaTrader.
/// The strategy looks for price crossing the Kijun line while a 20-period LWMA confirms the trend.
/// It manages risk with time filters, configurable stop-loss, break-even and trailing rules.
/// </summary>
public class KijunSenRobotStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _lwmaPeriod;
	private readonly StrategyParam<decimal> _maFilterPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _breakEvenPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<DataType> _candleType;

	private Ichimoku _ichimoku = null!;
	private WeightedMovingAverage _lwma = null!;

	private decimal? _previousClose;
	private decimal? _previousMa;
	private decimal? _previousPrevMa;
	private decimal? _previousKijun;
	private decimal? _previousPrevKijun;
	private decimal? _pendingLongLevel;
	private decimal? _pendingShortLevel;

	private bool? _isLongPosition;
	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _breakEvenDistance;
	private decimal _breakEvenStep;
	private decimal _trailingDistance;
	private bool _breakEvenApplied;

	/// <summary>
	/// Tenkan-sen calculation period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen calculation period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B calculation period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// Weighted moving average period used for slope confirmation.
	/// </summary>
	public int LwmaPeriod
	{
		get => _lwmaPeriod.Value;
		set => _lwmaPeriod.Value = value;
	}

	/// <summary>
	/// Minimum distance in pips between price and Kijun required by the LWMA filter.
	/// </summary>
	public decimal MaFilterPips
	{
		get => _maFilterPips.Value;
		set => _maFilterPips.Value = value;
	}

	/// <summary>
	/// Initial stop-loss in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Profit distance in pips required to move the stop-loss to break-even.
	/// </summary>
	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
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
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// First trading hour (inclusive) in exchange time.
	/// </summary>
	public int TradingStartHour
	{
		get => _tradingStartHour.Value;
		set => _tradingStartHour.Value = value;
	}

	/// <summary>
	/// Last trading hour (exclusive) in exchange time.
	/// </summary>
	public int TradingEndHour
	{
		get => _tradingEndHour.Value;
		set => _tradingEndHour.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="KijunSenRobotStrategy"/>.
	/// </summary>
	public KijunSenRobotStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Tenkan Period", "Period for Ichimoku Tenkan line", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(4, 12, 1);

		_kijunPeriod = Param(nameof(KijunPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("Kijun Period", "Period for Ichimoku Kijun line", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(8, 20, 1);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 24)
		.SetGreaterThanZero()
		.SetDisplay("Senkou Span B Period", "Period for Ichimoku Senkou Span B", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(18, 30, 1);

		_lwmaPeriod = Param(nameof(LwmaPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("LWMA Period", "Length of the confirmation LWMA", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 2);

		_maFilterPips = Param(nameof(MaFilterPips), 6m)
		.SetNotNegative()
		.SetDisplay("LWMA Filter (pips)", "Minimum distance between price and Kijun required by the LWMA", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(0m, 20m, 1m);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Initial protective stop distance", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(20m, 100m, 5m);

		_breakEvenPips = Param(nameof(BreakEvenPips), 9m)
		.SetNotNegative()
		.SetDisplay("Break-even Trigger (pips)", "Profit distance required to protect the position", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(5m, 20m, 1m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Distance for the trailing stop after the position moves in profit", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(5m, 30m, 1m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 120m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Optional fixed profit target", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(40m, 200m, 10m);

		_tradingStartHour = Param(nameof(TradingStartHour), 7)
		.SetRange(0, 23)
		.SetDisplay("Start Hour", "First trading hour (inclusive)", "Scheduling");

		_tradingEndHour = Param(nameof(TradingEndHour), 19)
		.SetRange(1, 24)
		.SetDisplay("End Hour", "Last trading hour (exclusive)", "Scheduling");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for signal generation", "General");
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

		_ichimoku = null!;
		_lwma = null!;

		_previousClose = null;
		_previousMa = null;
		_previousPrevMa = null;
		_previousKijun = null;
		_previousPrevKijun = null;
		_pendingLongLevel = null;
		_pendingShortLevel = null;

		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		_lwma = new WeightedMovingAverage
		{
			Length = LwmaPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_ichimoku, _lwma, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawIndicator(area, _lwma, "LWMA");
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(0), useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!maValue.IsFinal)
		return;

		var ichimokuTyped = (IchimokuValue)ichimokuValue;
		if (ichimokuTyped.Kijun is not decimal kijun)
		return;

		var maCurrent = maValue.ToDecimal();

		ManageOpenPosition(candle, maCurrent);

		if (IsFormedAndOnlineAndAllowTrading() && IsWithinTradingHours(candle.OpenTime))
		{
			EvaluateEntrySignals(candle, kijun, maCurrent);
		}

		UpdateHistory(candle, kijun, maCurrent);
	}

	private void ManageOpenPosition(ICandleMessage candle, decimal maCurrent)
	{
		if (Position == 0)
		{
			if (_isLongPosition != null || _entryPrice != null)
			ResetPositionState();
			return;
		}

		var isLong = Position > 0;
		var actualEntry = PositionPrice;
		if (_isLongPosition is null || _isLongPosition.Value != isLong || _entryPrice is null)
		{
			var entry = actualEntry != 0m ? actualEntry : candle.ClosePrice;
			SetupPositionState(isLong, entry);
		}
		else if (actualEntry != 0m && _entryPrice.Value != actualEntry)
		{
			_entryPrice = actualEntry;
			if (_isLongPosition.Value)
			{
				_stopLossPrice = _stopLossDistance > 0m ? _entryPrice - _stopLossDistance : null;
				_takeProfitPrice = _takeProfitDistance > 0m ? _entryPrice + _takeProfitDistance : null;
			}
			else
			{
				_stopLossPrice = _stopLossDistance > 0m ? _entryPrice + _stopLossDistance : null;
				_takeProfitPrice = _takeProfitDistance > 0m ? _entryPrice - _takeProfitDistance : null;
			}
		}

		if (_entryPrice is not decimal entryPrice)
		return;

		_isLongPosition = isLong;

		if (_previousMa.HasValue && _previousPrevMa.HasValue && _stopLossPrice.HasValue)
		{
			if (isLong && _stopLossPrice.Value < entryPrice && _previousMa.Value < _previousPrevMa.Value)
			{
				ClosePositionAndReset();
				return;
			}

			if (!isLong && _stopLossPrice.Value > entryPrice && _previousMa.Value > _previousPrevMa.Value)
			{
				ClosePositionAndReset();
				return;
			}
		}

		if (isLong)
		{
			ApplyBreakEvenAndTrailingForLong(candle, entryPrice);

			if (CheckStopLossHit(candle.LowPrice, _stopLossPrice))
			{
				ClosePositionAndReset();
				return;
			}

			if (CheckTakeProfitHit(candle.HighPrice, _takeProfitPrice))
			{
				ClosePositionAndReset();
				return;
			}
		}
		else
		{
			ApplyBreakEvenAndTrailingForShort(candle, entryPrice);

			if (CheckStopLossHitForShort(candle.HighPrice, _stopLossPrice))
			{
				ClosePositionAndReset();
				return;
			}

			if (CheckTakeProfitHitForShort(candle.LowPrice, _takeProfitPrice))
			{
				ClosePositionAndReset();
				return;
			}
		}
	}

	private void ApplyBreakEvenAndTrailingForLong(ICandleMessage candle, decimal entryPrice)
	{
		if (!_breakEvenApplied && _breakEvenDistance > 0m)
		{
			if (candle.ClosePrice - entryPrice >= _breakEvenDistance)
			{
				var newStop = entryPrice + (_breakEvenStep > 0m ? _breakEvenStep : 0m);
				if (_stopLossPrice is not decimal currentStop || newStop > currentStop)
				_stopLossPrice = newStop;
				_breakEvenApplied = true;
			}
		}

		if (_trailingDistance > 0m && candle.ClosePrice - entryPrice >= _trailingDistance)
		{
			var newStop = candle.ClosePrice - _trailingDistance;
			if (_stopLossPrice is not decimal currentStop || newStop > currentStop)
			_stopLossPrice = newStop;
		}
	}

	private void ApplyBreakEvenAndTrailingForShort(ICandleMessage candle, decimal entryPrice)
	{
		if (!_breakEvenApplied && _breakEvenDistance > 0m)
		{
			if (entryPrice - candle.ClosePrice >= _breakEvenDistance)
			{
				var newStop = entryPrice - (_breakEvenStep > 0m ? _breakEvenStep : 0m);
				if (_stopLossPrice is not decimal currentStop || newStop < currentStop)
				_stopLossPrice = newStop;
				_breakEvenApplied = true;
			}
		}

		if (_trailingDistance > 0m && entryPrice - candle.ClosePrice >= _trailingDistance)
		{
			var newStop = candle.ClosePrice + _trailingDistance;
			if (_stopLossPrice is not decimal currentStop || newStop < currentStop)
			_stopLossPrice = newStop;
		}
	}

	private static bool CheckStopLossHit(decimal lowPrice, decimal? stopPrice)
	{
		return stopPrice.HasValue && lowPrice <= stopPrice.Value;
	}

	private static bool CheckTakeProfitHit(decimal highPrice, decimal? takeProfitPrice)
	{
		return takeProfitPrice.HasValue && highPrice >= takeProfitPrice.Value;
	}

	private static bool CheckStopLossHitForShort(decimal highPrice, decimal? stopPrice)
	{
		return stopPrice.HasValue && highPrice >= stopPrice.Value;
	}

	private static bool CheckTakeProfitHitForShort(decimal lowPrice, decimal? takeProfitPrice)
	{
		return takeProfitPrice.HasValue && lowPrice <= takeProfitPrice.Value;
	}

	private void EvaluateEntrySignals(ICandleMessage candle, decimal kijun, decimal maCurrent)
	{
		if (_previousClose is not decimal previousClose ||
		_previousMa is not decimal previousMa ||
		_previousKijun is not decimal previousKijun)
		{
			return;
		}

		var maTrendUp = maCurrent > previousMa;
		var maTrendDown = maCurrent < previousMa;
		var filterOffset = ConvertPips(MaFilterPips);

		var priceOpenedBelow = candle.OpenPrice < kijun;
		var priceOpenedAbove = candle.OpenPrice > kijun;
		var priceClosedAbove = candle.ClosePrice > kijun;
		var priceClosedBelow = candle.ClosePrice < kijun;
		var priceTouchedBelow = candle.LowPrice <= kijun;
		var priceTouchedAbove = candle.HighPrice >= kijun;
		var priceWasBelow = previousClose < previousKijun;
		var priceWasAbove = previousClose > previousKijun;
		var kijunNotFalling = !_previousPrevKijun.HasValue || kijun >= _previousPrevKijun.Value;
		var kijunNotRising = !_previousPrevKijun.HasValue || kijun <= _previousPrevKijun.Value;

		if (_pendingLongLevel is null)
		{
			if (priceClosedAbove && (priceOpenedBelow || priceWasBelow || priceTouchedBelow) && kijunNotFalling)
			{
				if (filterOffset <= 0m || maCurrent < kijun - filterOffset)
				{
					_pendingLongLevel = kijun;
					_pendingShortLevel = null;
				}
			}
		}

		if (_pendingShortLevel is null)
		{
			if (priceClosedBelow && (priceOpenedAbove || priceWasAbove || priceTouchedAbove) && kijunNotRising)
			{
				if (filterOffset <= 0m || maCurrent > kijun + filterOffset)
				{
					_pendingShortLevel = kijun;
					_pendingLongLevel = null;
				}
			}
		}

		var volume = Volume + Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (_pendingLongLevel.HasValue && maTrendUp && Position <= 0)
		{
			BuyMarket(volume);
			SetupPositionState(true, candle.ClosePrice);
			_pendingLongLevel = null;
			_pendingShortLevel = null;
			return;
		}

		if (_pendingShortLevel.HasValue && maTrendDown && Position >= 0)
		{
			SellMarket(volume);
			SetupPositionState(false, candle.ClosePrice);
			_pendingLongLevel = null;
			_pendingShortLevel = null;
		}
	}

	private void UpdateHistory(ICandleMessage candle, decimal kijun, decimal maCurrent)
	{
		_previousPrevKijun = _previousKijun;
		_previousKijun = kijun;

		_previousPrevMa = _previousMa;
		_previousMa = maCurrent;

		_previousClose = candle.ClosePrice;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.Hour;
		return hour >= TradingStartHour && hour < TradingEndHour;
	}

	private void SetupPositionState(bool isLong, decimal entryPrice)
	{
		_isLongPosition = isLong;
		_entryPrice = entryPrice;
		_breakEvenApplied = false;

		_stopLossDistance = ConvertPips(StopLossPips);
		_takeProfitDistance = ConvertPips(TakeProfitPips);
		_breakEvenDistance = ConvertPips(BreakEvenPips);
		_breakEvenStep = ConvertPips(1m);
		_trailingDistance = ConvertPips(TrailingStopPips);

		_stopLossPrice = _stopLossDistance > 0m ? (isLong ? entryPrice - _stopLossDistance : entryPrice + _stopLossDistance) : null;
		_takeProfitPrice = _takeProfitDistance > 0m ? (isLong ? entryPrice + _takeProfitDistance : entryPrice - _takeProfitDistance) : null;
	}

	private void ClosePositionAndReset()
	{
		if (Position != 0)
		ClosePosition();

		ResetPositionState();
	}

	private void ResetPositionState()
	{
		_isLongPosition = null;
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_breakEvenDistance = 0m;
		_breakEvenStep = 0m;
		_trailingDistance = 0m;
		_breakEvenApplied = false;
	}

	private decimal ConvertPips(decimal value)
	{
		if (value <= 0m)
		return 0m;

		var step = GetPipStep();
		return value * step;
	}

	private decimal GetPipStep()
	{
		var priceStep = Security?.PriceStep ?? Security?.MinPriceStep;
		if (priceStep is null || priceStep <= 0m)
		return 1m;

		var stepValue = priceStep.Value;
		var decimals = GetDecimalPlaces(stepValue);
		if (decimals == 3 || decimals == 5)
		return stepValue * 10m;

		return stepValue;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);
		var decimals = 0;
		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
