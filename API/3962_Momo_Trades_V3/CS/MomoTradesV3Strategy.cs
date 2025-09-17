using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum-based strategy converted from the "Momo Trades V3" MetaTrader expert.
/// The system combines MACD momentum patterns with a displaced EMA filter and optional breakeven management.
/// </summary>
public class MomoTradesV3Strategy : Strategy
{
	private const decimal MacdZeroTolerance = 1e-8m;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _emaShift;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _macdShift;
	private readonly StrategyParam<decimal> _priceShiftPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _riskFraction;
	private readonly StrategyParam<bool> _useAutoVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _closeEndDay;
	private readonly StrategyParam<bool> _useBreakeven;
	private readonly StrategyParam<decimal> _breakevenOffsetPoints;

	private readonly List<decimal> _macdHistory = new();
	private readonly List<decimal> _emaHistory = new();
	private readonly List<decimal> _closeHistory = new();

	private decimal _pointValue;
	private decimal? _breakevenPrice;
	private Sides? _breakevenSide;

	/// <summary>
	/// Primary candle type for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EMA period used for the directional filter.
	/// </summary>
	public int MaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Number of finished bars used when sampling the EMA and close prices.
	/// </summary>
	public int MaShift
	{
		get => _emaShift.Value;
		set => _emaShift.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the MACD indicator.
	/// </summary>
	public int FastPeriod
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the MACD indicator.
	/// </summary>
	public int SlowPeriod
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal EMA length for the MACD indicator.
	/// </summary>
	public int SignalPeriod
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Additional bar displacement applied when reading MACD values.
	/// </summary>
	public int MacdShift
	{
		get => _macdShift.Value;
		set => _macdShift.Value = value;
	}

	/// <summary>
	/// Minimal distance between price and EMA expressed in MetaTrader points.
	/// </summary>
	public decimal PriceShiftPoints
	{
		get => _priceShiftPoints.Value;
		set => _priceShiftPoints.Value = value;
	}

	/// <summary>
	/// Base trading volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Fraction of portfolio equity allocated when auto volume is enabled.
	/// </summary>
	public decimal RiskFraction
	{
		get => _riskFraction.Value;
		set => _riskFraction.Value = value;
	}

	/// <summary>
	/// Enable automatic position sizing.
	/// </summary>
	public bool UseAutoVolume
	{
		get => _useAutoVolume.Value;
		set => _useAutoVolume.Value = value;
	}

	/// <summary>
	/// Initial protective stop distance in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Initial take-profit distance in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Close any open position at the end of the trading day.
	/// </summary>
	public bool CloseEndDay
	{
		get => _closeEndDay.Value;
		set => _closeEndDay.Value = value;
	}

	/// <summary>
	/// Enable breakeven stop handling.
	/// </summary>
	public bool UseBreakeven
	{
		get => _useBreakeven.Value;
		set => _useBreakeven.Value = value;
	}

	/// <summary>
	/// Offset applied to the breakeven trigger in MetaTrader points.
	/// </summary>
	public decimal BreakevenOffsetPoints
	{
		get => _breakevenOffsetPoints.Value;
		set => _breakevenOffsetPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MomoTradesV3Strategy"/>.
	/// </summary>
	public MomoTradesV3Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");

		_emaPeriod = Param(nameof(MaPeriod), 22)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "Length of the EMA filter", "Indicators");

		_emaShift = Param(nameof(MaShift), 1)
		.SetGreaterThanZero()
		.SetDisplay("EMA Shift", "Number of closed bars used for EMA comparison", "Indicators");

		_macdFast = Param(nameof(FastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA period", "Indicators");

		_macdSlow = Param(nameof(SlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA period", "Indicators");

		_macdSignal = Param(nameof(SignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA period", "Indicators");

		_macdShift = Param(nameof(MacdShift), 1)
		.SetGreaterThanZero()
		.SetDisplay("MACD Shift", "Extra displacement for MACD history", "Indicators");

		_priceShiftPoints = Param(nameof(PriceShiftPoints), 10m)
		.SetDisplay("Price Shift", "Required price offset from EMA in MetaTrader points", "Signals");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Default trade volume", "Trading");

		_riskFraction = Param(nameof(RiskFraction), 0.1m)
		.SetDisplay("Risk Fraction", "Equity fraction for auto volume", "Risk Management");

		_useAutoVolume = Param(nameof(UseAutoVolume), false)
		.SetDisplay("Auto Volume", "Enable risk-based volume sizing", "Risk Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
		.SetDisplay("Stop-Loss Points", "Distance to the initial stop in MetaTrader points", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
		.SetDisplay("Take-Profit Points", "Distance to the initial take-profit in MetaTrader points", "Risk Management");

		_closeEndDay = Param(nameof(CloseEndDay), true)
		.SetDisplay("Close End of Day", "Exit positions near the session close", "Risk Management");

		_useBreakeven = Param(nameof(UseBreakeven), false)
		.SetDisplay("Use Breakeven", "Move the stop to breakeven after profit", "Risk Management");

		_breakevenOffsetPoints = Param(nameof(BreakevenOffsetPoints), 0m)
		.SetDisplay("Breakeven Offset", "Additional points added to the breakeven level", "Risk Management");
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

		_macdHistory.Clear();
		_emaHistory.Clear();
		_closeHistory.Clear();
		_breakevenPrice = null;
		_breakevenSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 0m;
		if (_pointValue <= 0m)
		{
			var decimals = Security?.Decimals;
			if (decimals != null)
			{
				_pointValue = (decimal)Math.Pow(10, -decimals.Value);
			}
			if (_pointValue <= 0m)
				_pointValue = 0.0001m;
		}

		Volume = TradeVolume;

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = FastPeriod,
			LongPeriod = SlowPeriod,
			SignalPeriod = SignalPeriod
		};

		var ema = new ExponentialMovingAverage
		{
			Length = MaPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(macd, ema, ProcessCandle)
		.Start();

		var stopUnit = StopLossPoints > 0m && _pointValue > 0m
		? new Unit(StopLossPoints * _pointValue, UnitTypes.Absolute)
		: null;

		var takeUnit = TakeProfitPoints > 0m && _pointValue > 0m
		? new Unit(TakeProfitPoints * _pointValue, UnitTypes.Absolute)
		: null;

		StartProtection(takeProfit: takeUnit, stopLoss: stopUnit);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal signalLine, decimal histogram, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_macdHistory.Insert(0, macdLine);
		TrimHistory(_macdHistory);

		_emaHistory.Insert(0, emaValue);
		TrimHistory(_emaHistory);

		_closeHistory.Insert(0, candle.ClosePrice);
		TrimHistory(_closeHistory);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		HandleEndOfDayExit(candle);
		HandleBreakeven(candle);

		if (Position != 0)
			return;

		var priceShift = PriceShiftPoints * _pointValue;

		var canBuy = EvaluateMacdBuy() && EvaluateEmaBuy(priceShift);
		var canSell = EvaluateMacdSell() && EvaluateEmaSell(priceShift);

		if (canBuy)
		{
			var volume = CalculateOrderVolume(candle.ClosePrice);
			volume = NormalizeVolume(volume);
			if (volume > 0m)
			{
				BuyMarket(volume);
			}
		}
		else if (canSell)
		{
			var volume = CalculateOrderVolume(candle.ClosePrice);
			volume = NormalizeVolume(volume);
			if (volume > 0m)
			{
				SellMarket(volume);
			}
		}
	}

	private void HandleEndOfDayExit(ICandleMessage candle)
	{
		if (!CloseEndDay || Position == 0)
			return;

		var endHour = candle.OpenTime.DayOfWeek == DayOfWeek.Friday ? 21 : 23;
		if (candle.OpenTime.Hour != endHour)
			return;

		if (Position > 0)
		{
			SellMarket(Position.Abs());
		}
		else if (Position < 0)
		{
			BuyMarket(Position.Abs());
		}

		_breakevenPrice = null;
		_breakevenSide = null;
	}

	private void HandleBreakeven(ICandleMessage candle)
	{
		if (!UseBreakeven || Position == 0)
		{
			_breakevenPrice = null;
			_breakevenSide = null;
			return;
		}

		var entryPrice = PositionPrice ?? candle.ClosePrice;
		var offset = BreakevenOffsetPoints * _pointValue;

		if (Position > 0)
		{
			if (_breakevenSide != Sides.Buy)
			{
				_breakevenSide = Sides.Buy;
				_breakevenPrice = null;
			}

			var desired = entryPrice + offset;
			if (_breakevenPrice == null)
			{
				if (candle.LowPrice > desired)
				{
					_breakevenPrice = desired;
				}
			}
			else if (candle.LowPrice <= _breakevenPrice.Value)
			{
				SellMarket(Position.Abs());
				_breakevenPrice = null;
				_breakevenSide = null;
			}
		}
		else if (Position < 0)
		{
			if (_breakevenSide != Sides.Sell)
			{
				_breakevenSide = Sides.Sell;
				_breakevenPrice = null;
			}

			var desired = entryPrice - offset;
			if (_breakevenPrice == null)
			{
				if (candle.HighPrice < desired)
				{
					_breakevenPrice = desired;
				}
			}
			else if (candle.HighPrice >= _breakevenPrice.Value)
			{
				BuyMarket(Position.Abs());
				_breakevenPrice = null;
				_breakevenSide = null;
			}
		}
	}

	private bool EvaluateMacdBuy()
	{
		if (!TryGetMacd(MacdShift + 3, out var macd3) ||
			!TryGetMacd(MacdShift + 4, out var macd4) ||
		!TryGetMacd(MacdShift + 5, out var macd5) ||
		!TryGetMacd(MacdShift + 6, out var macd6) ||
		!TryGetMacd(MacdShift + 7, out var macd7) ||
		!TryGetMacd(MacdShift + 8, out var macd8))
		{
			return false;
		}

		var macd5IsZero = macd5.Abs() <= MacdZeroTolerance;

		var pattern1 = macd3 > macd4 &&
		macd4 > macd5 &&
		macd5IsZero &&
		macd5 > macd6 &&
		macd6 > macd7;

		var pattern2 = macd3 > macd4 &&
		macd4 > macd5 &&
		macd5 >= 0m &&
		macd6 <= 0m &&
		macd6 > macd7 &&
		macd7 > macd8;

		return pattern1 || pattern2;
	}

	private bool EvaluateMacdSell()
	{
		if (!TryGetMacd(MacdShift + 3, out var macd3) ||
			!TryGetMacd(MacdShift + 4, out var macd4) ||
		!TryGetMacd(MacdShift + 5, out var macd5) ||
		!TryGetMacd(MacdShift + 6, out var macd6) ||
		!TryGetMacd(MacdShift + 7, out var macd7) ||
		!TryGetMacd(MacdShift + 8, out var macd8))
		{
			return false;
		}

		var macd5IsZero = macd5.Abs() <= MacdZeroTolerance;

		var pattern1 = macd3 < macd4 &&
		macd4 < macd5 &&
		macd5IsZero &&
		macd5 < macd6 &&
		macd6 < macd7;

		var pattern2 = macd3 < macd4 &&
		macd4 < macd5 &&
		macd5 <= 0m &&
		macd6 >= 0m &&
		macd6 < macd7 &&
		macd7 < macd8;

		return pattern1 || pattern2;
	}

	private bool EvaluateEmaBuy(decimal priceShift)
	{
		if (!TryGetShiftedValue(_closeHistory, MaShift, out var close) ||
			!TryGetShiftedValue(_emaHistory, MaShift, out var ema))
		{
			return false;
		}

		return close - ema > priceShift;
	}

	private bool EvaluateEmaSell(decimal priceShift)
	{
		if (!TryGetShiftedValue(_closeHistory, MaShift, out var close) ||
			!TryGetShiftedValue(_emaHistory, MaShift, out var ema))
		{
			return false;
		}

		return ema - close > priceShift;
	}

	private bool TryGetMacd(int index, out decimal value)
	{
		if (index < 0 || index >= _macdHistory.Count)
		{
			value = 0m;
			return false;
		}

		value = _macdHistory[index];
		return true;
	}

	private static bool TryGetShiftedValue(List<decimal> list, int shift, out decimal value)
	{
		if (shift < 0 || shift >= list.Count)
		{
			value = 0m;
			return false;
		}

		value = list[shift];
		return true;
	}

	private static void TrimHistory(List<decimal> list)
	{
		const int maxItems = 64;
		if (list.Count > maxItems)
			list.RemoveRange(maxItems, list.Count - maxItems);
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		if (!UseAutoVolume)
			return TradeVolume;

		var portfolio = Portfolio;
		var security = Security;

		if (portfolio?.CurrentValue == null || security == null || price <= 0m || RiskFraction <= 0m)
			return TradeVolume;

		var equity = portfolio.CurrentValue.Value;
		if (equity <= 0m)
			return TradeVolume;

		var lotSize = security.LotSize ?? 1m;
		if (lotSize <= 0m)
			lotSize = 1m;

		var contractValue = price * lotSize;
		if (contractValue <= 0m)
			return TradeVolume;

		var desired = equity * RiskFraction / contractValue;
		var normalized = NormalizeVolume(desired);
		return normalized > 0m ? normalized : TradeVolume;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var sec = Security;
		if (sec == null)
			return volume;

		var step = sec.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var steps = Math.Floor(volume / step);
		var normalized = (decimal)steps * step;

		var min = sec.VolumeMin ?? step;
		if (normalized < min)
			normalized = min;

		var max = sec.VolumeMax;
		if (max != null && normalized > max.Value)
			normalized = max.Value;

		return normalized;
	}
}
