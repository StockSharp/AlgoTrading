using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the UP3x1 Premium expert advisor that relies on EMA momentum with daily context.
/// </summary>
public class Up3x1PremiumStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _dailyEmaLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _rangeThreshold;
	private readonly StrategyParam<decimal> _bodyThreshold;
	private readonly StrategyParam<decimal> _dailyReversalThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private decimal? _fastPrev;
	private decimal? _fastPrev2;
	private decimal? _slowPrev;
	private decimal? _slowPrev2;
	private ICandleMessage _prevCandle;
	private ICandleMessage _prevPrevCandle;

	private decimal? _dailyEmaValue;
	private decimal? _prevDailyOpen;
	private decimal? _prevDailyClose;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _trailingStopPrice;

	public Up3x1PremiumStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume for each trade", "Trading")
		.SetCanOptimize();

		_fastEmaLength = Param(nameof(FastEmaLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Length", "Length of the fast EMA", "Indicators")
		.SetCanOptimize();

		_slowEmaLength = Param(nameof(SlowEmaLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA Length", "Length of the slow EMA", "Indicators")
		.SetCanOptimize();

		_dailyEmaLength = Param(nameof(DailyEmaLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Daily EMA Length", "EMA length for the daily trend filter", "Indicators")
		.SetCanOptimize();

		_takeProfit = Param(nameof(TakeProfit), 0.015m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit", "Absolute take profit distance", "Risk")
		.SetCanOptimize();

		_stopLoss = Param(nameof(StopLoss), 0.01m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss", "Absolute stop loss distance", "Risk")
		.SetCanOptimize();

		_trailingStop = Param(nameof(TrailingStop), 0.001m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop", "Distance for trailing stop updates", "Risk")
		.SetCanOptimize();

		_rangeThreshold = Param(nameof(RangeThreshold), 0.006m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Range Threshold", "Minimum candle range to qualify as wide", "Filters")
		.SetCanOptimize();

		_bodyThreshold = Param(nameof(BodyThreshold), 0.005m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Body Threshold", "Minimum candle body for momentum", "Filters")
		.SetCanOptimize();

		_dailyReversalThreshold = Param(nameof(DailyReversalThreshold), 0.006m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Daily Reversal Threshold", "Minimum prior day reversal size", "Filters")
		.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary working timeframe", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Daily Candle Type", "Higher timeframe for daily context", "General");
	}

	/// <summary>
	/// Trade volume expressed in security lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Length of the fast EMA on the working timeframe.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow EMA on the working timeframe.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the EMA used on the daily candles.
	/// </summary>
	public int DailyEmaLength
	{
		get => _dailyEmaLength.Value;
		set => _dailyEmaLength.Value = value;
	}

	/// <summary>
	/// Absolute take profit expressed in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Absolute stop loss expressed in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Distance used for trailing stop updates.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Minimum candle range that activates the momentum filter.
	/// </summary>
	public decimal RangeThreshold
	{
		get => _rangeThreshold.Value;
		set => _rangeThreshold.Value = value;
	}

	/// <summary>
	/// Minimum candle body needed to qualify as a thrust.
	/// </summary>
	public decimal BodyThreshold
	{
		get => _bodyThreshold.Value;
		set => _bodyThreshold.Value = value;
	}

	/// <summary>
	/// Size of the prior daily reversal required during the midnight check.
	/// </summary>
	public decimal DailyReversalThreshold
	{
		get => _dailyReversalThreshold.Value;
		set => _dailyReversalThreshold.Value = value;
	}

	/// <summary>
	/// Working timeframe for the main signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for the daily EMA filter.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DailyCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastPrev = null;
		_fastPrev2 = null;
		_slowPrev = null;
		_slowPrev2 = null;
		_prevCandle = null;
		_prevPrevCandle = null;

		_dailyEmaValue = null;
		_prevDailyOpen = null;
		_prevDailyClose = null;

		ClearTradeLevels();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		// Create EMA indicators for the working timeframe.
		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(fastEma, slowEma, ProcessCandle)
		.Start();

		// Daily subscription provides the higher timeframe confirmation.
		var dailyEma = new ExponentialMovingAverage { Length = DailyEmaLength };
		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription
		.Bind(dailyEma, ProcessDailyCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Store the latest completed daily information for intraday decisions.
		_dailyEmaValue = emaValue;
		_prevDailyOpen = candle.OpenPrice;
		_prevDailyClose = candle.ClosePrice;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Manage an existing position before looking for fresh entries.
		ManageOpenPosition(candle);

		var haveHistory = _prevCandle != null && _prevPrevCandle != null &&
		_fastPrev.HasValue && _fastPrev2.HasValue && _slowPrev.HasValue && _slowPrev2.HasValue;

		if (Position == 0m && haveHistory && IsFormedAndOnlineAndAllowTrading())
		{
			var bullishCross = _fastPrev2.Value < _slowPrev2.Value && _fastPrev.Value > _slowPrev.Value &&
			_prevPrevCandle.OpenPrice < _prevCandle.OpenPrice;

			var wideBullish = (_prevCandle.HighPrice - _prevCandle.LowPrice) > RangeThreshold &&
			_prevCandle.ClosePrice > _prevCandle.OpenPrice &&
			(_prevCandle.ClosePrice - _prevCandle.OpenPrice) > BodyThreshold;

			var midnight = candle.OpenTime.Hour == 0;
			var dailyBounce = midnight &&
			_prevDailyOpen is decimal dayOpen &&
			_prevDailyClose is decimal dayClose &&
			dayOpen > dayClose &&
			(dayOpen - dayClose) > DailyReversalThreshold;

			var priceAboveDaily = _dailyEmaValue is decimal daily && candle.ClosePrice >= daily;

			var longSignal = bullishCross || wideBullish || dailyBounce || priceAboveDaily;

			var bearishCross = _fastPrev2.Value > _slowPrev2.Value && _fastPrev.Value < _slowPrev.Value &&
			_prevPrevCandle.OpenPrice > _prevCandle.OpenPrice;

			var wideBearish = (_prevCandle.HighPrice - _prevCandle.LowPrice) > RangeThreshold &&
			_prevCandle.OpenPrice > _prevCandle.ClosePrice &&
			(_prevCandle.OpenPrice - _prevCandle.ClosePrice) > BodyThreshold;

			var midnightSell = midnight &&
			_prevDailyOpen is decimal dayOpenSell &&
			_prevDailyClose is decimal dayCloseSell &&
			dayOpenSell < dayCloseSell &&
			(dayCloseSell - dayOpenSell) > DailyReversalThreshold;

			var shortSignal = bearishCross || wideBearish || midnightSell;

			if (longSignal && shortSignal)
			{
				// Break ties with the latest EMA relationship.
				if (_fastPrev.Value >= _slowPrev.Value)
				shortSignal = false;
				else
				longSignal = false;
			}

			if (longSignal && OrderVolume > 0m)
			{
				BuyMarket(OrderVolume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = StopLoss > 0m ? _entryPrice - StopLoss : null;
				_takeProfitPrice = TakeProfit > 0m ? _entryPrice + TakeProfit : null;
				_trailingStopPrice = TrailingStop > 0m ? _entryPrice - TrailingStop : null;
			}
			else if (shortSignal && OrderVolume > 0m)
			{
				SellMarket(OrderVolume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = StopLoss > 0m ? _entryPrice + StopLoss : null;
				_takeProfitPrice = TakeProfit > 0m ? _entryPrice - TakeProfit : null;
				_trailingStopPrice = TrailingStop > 0m ? _entryPrice + TrailingStop : null;
			}
		}

		// Preserve history to mimic the MQL index-based access pattern.
		_prevPrevCandle = _prevCandle;
		_prevCandle = candle;

		_fastPrev2 = _fastPrev;
		_fastPrev = fastEma;
		_slowPrev2 = _slowPrev;
		_slowPrev = slowEma;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			UpdateTrailingStopForLong(candle);

			var exit = AreEmaNear(_fastPrev, _slowPrev);

			if (!exit && _takeProfitPrice is decimal tp && candle.HighPrice >= tp)
			exit = true;

			if (!exit && _stopPrice is decimal sl && candle.LowPrice <= sl)
			exit = true;

			if (!exit && _trailingStopPrice is decimal trail && candle.LowPrice <= trail)
			exit = true;

			if (exit)
			{
				SellMarket(Math.Abs(Position));
				ClearTradeLevels();
			}
		}
		else if (Position < 0m)
		{
			UpdateTrailingStopForShort(candle);

			var exit = AreEmaNear(_fastPrev, _slowPrev);

			if (!exit && _takeProfitPrice is decimal tp && candle.LowPrice <= tp)
			exit = true;

			if (!exit && _stopPrice is decimal sl && candle.HighPrice >= sl)
			exit = true;

			if (!exit && _trailingStopPrice is decimal trail && candle.HighPrice >= trail)
			exit = true;

			if (exit)
			{
				BuyMarket(Math.Abs(Position));
				ClearTradeLevels();
			}
		}
	}

	private void UpdateTrailingStopForLong(ICandleMessage candle)
	{
		if (TrailingStop <= 0m || _entryPrice is not decimal entry)
		return;

		var move = candle.HighPrice - entry;
		if (move < TrailingStop)
		return;

		var newStop = candle.HighPrice - TrailingStop;
		if (_trailingStopPrice is null || newStop > _trailingStopPrice)
		_trailingStopPrice = newStop;
	}

	private void UpdateTrailingStopForShort(ICandleMessage candle)
	{
		if (TrailingStop <= 0m || _entryPrice is not decimal entry)
		return;

		var move = entry - candle.LowPrice;
		if (move < TrailingStop)
		return;

		var newStop = candle.LowPrice + TrailingStop;
		if (_trailingStopPrice is null || newStop < _trailingStopPrice)
		_trailingStopPrice = newStop;
	}

	private static bool AreEmaNear(decimal? fast, decimal? slow)
	{
		if (fast is not decimal fastValue || slow is not decimal slowValue)
		return false;

		if (slowValue == 0m)
		return false;

		var diff = Math.Abs(fastValue - slowValue);
		return diff <= Math.Abs(slowValue) * 0.001m;
	}

	private void ClearTradeLevels()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_trailingStopPrice = null;
	}
}
