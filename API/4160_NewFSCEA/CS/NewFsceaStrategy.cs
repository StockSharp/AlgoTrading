using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD trend-following strategy with EMA slope filter, trailing stop, and take profit.
/// </summary>
public class NewFsceaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _openLevelPoints;
	private readonly StrategyParam<decimal> _closeLevelPoints;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<int> _trendShift;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private ExponentialMovingAverage _ema = null!;

	private decimal? _previousMacd;
	private decimal? _previousSignal;

	private decimal?[]? _emaHistory;
	private int _emaIndex;
	private int _emaCount;

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _lastLongEntryPrice;
	private decimal? _lastShortEntryPrice;

	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum MACD magnitude required to enter a trade.
	/// </summary>
	public decimal OpenLevelPoints
	{
		get => _openLevelPoints.Value;
		set => _openLevelPoints.Value = value;
	}

	/// <summary>
	/// MACD magnitude required to exit an open position.
	/// </summary>
	public decimal CloseLevelPoints
	{
		get => _closeLevelPoints.Value;
		set => _closeLevelPoints.Value = value;
	}

	/// <summary>
	/// EMA length used for the trend filter.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars used to shift the EMA when computing the slope.
	/// </summary>
	public int TrendShift
	{
		get => _trendShift.Value;
		set => _trendShift.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NewFsceaStrategy"/>.
	/// </summary>
	public NewFsceaStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 300m)
			.SetDisplay("Take Profit (pts)", "Target profit distance expressed in points", "Risk")
			.SetGreaterThanZero();

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 20m)
			.SetDisplay("Trailing Stop (pts)", "Trailing stop distance expressed in points", "Risk")
			.SetGreaterThanZero();

		_openLevelPoints = Param(nameof(OpenLevelPoints), 3m)
			.SetDisplay("Open Threshold", "MACD threshold that must be exceeded to enter", "Signals")
			.SetGreaterThanZero();

		_closeLevelPoints = Param(nameof(CloseLevelPoints), 2m)
			.SetDisplay("Close Threshold", "MACD threshold that triggers exits", "Signals")
			.SetGreaterThanZero();

		_trendPeriod = Param(nameof(TrendPeriod), 10)
			.SetDisplay("EMA Period", "Length of the EMA used for the trend filter", "Indicators")
			.SetGreaterThanZero();

		_trendShift = Param(nameof(TrendShift), 2)
			.SetDisplay("EMA Shift", "Horizontal shift applied to the EMA (bars)", "Indicators")
			.SetNotNegative();

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Volume", "Base order volume", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for analysis", "Data");
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

		_previousMacd = null;
		_previousSignal = null;
		_emaHistory = null;
		_emaIndex = 0;
		_emaCount = 0;

		ResetRiskState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 },
		};

		_ema = new ExponentialMovingAverage
		{
			Length = TrendPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !emaValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdCurrent = macdTyped.Macd;
		var signalCurrent = macdTyped.Signal;

		if (!TryGetShiftedEma(emaValue.ToDecimal(), out var emaCurrent, out var emaPrevious))
		{
			StoreMacd(macdCurrent, signalCurrent);
			return;
		}

		if (_previousMacd is not decimal macdPrevious || _previousSignal is not decimal signalPrevious)
		{
			StoreMacd(macdCurrent, signalCurrent);
			return;
		}

		var priceStep = GetPriceStep();
		var openThreshold = OpenLevelPoints * priceStep;
		var closeThreshold = CloseLevelPoints * priceStep;

		var longSignal = macdCurrent < 0m &&
			macdCurrent > signalCurrent &&
			macdPrevious < signalPrevious &&
			Math.Abs(macdCurrent) > openThreshold &&
			emaCurrent > emaPrevious;

		var shortSignal = macdCurrent > 0m &&
			macdCurrent < signalCurrent &&
			macdPrevious > signalPrevious &&
			macdCurrent > openThreshold &&
			emaCurrent < emaPrevious;

		if (Position == 0)
		{
			if (longSignal)
			{
				EnterLong(candle, priceStep);
			}
			else if (shortSignal)
			{
				EnterShort(candle, priceStep);
			}
		}
		else if (Position > 0)
		{
			ProcessLongPosition(candle, macdCurrent, signalCurrent, macdPrevious, signalPrevious, closeThreshold, priceStep);
		}
		else
		{
			ProcessShortPosition(candle, macdCurrent, signalCurrent, macdPrevious, signalPrevious, closeThreshold, priceStep);
		}

		StoreMacd(macdCurrent, signalCurrent);
	}

	private void ProcessLongPosition(ICandleMessage candle, decimal macdCurrent, decimal signalCurrent, decimal macdPrevious, decimal signalPrevious, decimal closeThreshold, decimal priceStep)
	{
		var exitSignal = macdCurrent > 0m &&
			macdCurrent < signalCurrent &&
			macdPrevious > signalPrevious &&
			macdCurrent > closeThreshold;

		UpdateLongTrailing(candle, priceStep);

		var shouldExit = exitSignal;

		if (_longTakeProfitPrice is decimal longTp && candle.HighPrice >= longTp)
			shouldExit = true;

		if (_longStopPrice is decimal longSl && candle.LowPrice <= longSl)
			shouldExit = true;

		if (shouldExit)
			ExitLong();
	}

	private void ProcessShortPosition(ICandleMessage candle, decimal macdCurrent, decimal signalCurrent, decimal macdPrevious, decimal signalPrevious, decimal closeThreshold, decimal priceStep)
	{
		var exitSignal = macdCurrent < 0m &&
			macdCurrent > signalCurrent &&
			macdPrevious < signalPrevious &&
			Math.Abs(macdCurrent) > closeThreshold;

		UpdateShortTrailing(candle, priceStep);

		var shouldExit = exitSignal;

		if (_shortTakeProfitPrice is decimal shortTp && candle.LowPrice <= shortTp)
			shouldExit = true;

		if (_shortStopPrice is decimal shortSl && candle.HighPrice >= shortSl)
			shouldExit = true;

		if (shouldExit)
			ExitShort();
	}

	private void EnterLong(ICandleMessage candle, decimal priceStep)
	{
		var volume = Math.Max(TradeVolume, 0m);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		var entryPrice = candle.ClosePrice;
		_lastLongEntryPrice = entryPrice;
		_longTakeProfitPrice = TakeProfitPoints > 0m ? entryPrice + priceStep * TakeProfitPoints : null;
		_longStopPrice = null;
	}

	private void EnterShort(ICandleMessage candle, decimal priceStep)
	{
		var volume = Math.Max(TradeVolume, 0m);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		var entryPrice = candle.ClosePrice;
		_lastShortEntryPrice = entryPrice;
		_shortTakeProfitPrice = TakeProfitPoints > 0m ? entryPrice - priceStep * TakeProfitPoints : null;
		_shortStopPrice = null;
	}

	private void ExitLong()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		ResetLongState();
	}

	private void ExitShort()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		ResetShortState();
	}

	private void UpdateLongTrailing(ICandleMessage candle, decimal priceStep)
	{
		if (TrailingStopPoints <= 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice == 0m)
			entryPrice = _lastLongEntryPrice ?? 0m;

		if (entryPrice <= 0m)
			return;

		var trailDistance = priceStep * TrailingStopPoints;
		var profit = candle.ClosePrice - entryPrice;
		if (profit <= trailDistance)
			return;

		var newStop = candle.ClosePrice - trailDistance;
		if (_longStopPrice is null || newStop > _longStopPrice)
			_longStopPrice = newStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle, decimal priceStep)
	{
		if (TrailingStopPoints <= 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice == 0m)
			entryPrice = _lastShortEntryPrice ?? 0m;

		if (entryPrice <= 0m)
			return;

		var trailDistance = priceStep * TrailingStopPoints;
		var profit = entryPrice - candle.ClosePrice;
		if (profit <= trailDistance)
			return;

		var newStop = candle.ClosePrice + trailDistance;
		if (_shortStopPrice is null || newStop < _shortStopPrice)
			_shortStopPrice = newStop;
	}

	private decimal GetPriceStep()
	{
		var priceStep = Security?.PriceStep ?? 1m;
		return priceStep > 0m ? priceStep : 1m;
	}

	private bool TryGetShiftedEma(decimal emaValue, out decimal current, out decimal previous)
	{
		var shift = Math.Max(0, TrendShift);
		var length = shift + 2;

		if (_emaHistory == null || _emaHistory.Length != length)
		{
			_emaHistory = new decimal?[length];
			_emaIndex = 0;
			_emaCount = 0;
		}

		_emaHistory[_emaIndex] = emaValue;
		_emaIndex = (_emaIndex + 1) % length;

		if (_emaCount < length)
			_emaCount++;

		if (_emaCount < length)
		{
			current = default;
			previous = default;
			return false;
		}

		var lastIndex = (_emaIndex - 1 + length) % length;
		var currentIndex = (lastIndex - shift + length) % length;
		var previousIndex = (lastIndex - shift - 1 + length) % length;

		if (_emaHistory[currentIndex] is decimal curr && _emaHistory[previousIndex] is decimal prev)
		{
			current = curr;
			previous = prev;
			return true;
		}

		current = default;
		previous = default;
		return false;
	}

	private void StoreMacd(decimal macdCurrent, decimal signalCurrent)
	{
		_previousMacd = macdCurrent;
		_previousSignal = signalCurrent;
	}

	private void ResetLongState()
	{
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_lastLongEntryPrice = null;
	}

	private void ResetShortState()
	{
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_lastShortEntryPrice = null;
	}

	private void ResetRiskState()
	{
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade?.Order;
		var myTrade = trade?.Trade;
		if (order == null || myTrade == null)
			return;

		var priceStep = GetPriceStep();

		if (order.Direction == Sides.Buy)
		{
			if (Position > 0)
			{
				var price = PositionPrice != 0m ? PositionPrice : myTrade.Price;
				_lastLongEntryPrice = price;
				_longTakeProfitPrice = TakeProfitPoints > 0m ? price + priceStep * TakeProfitPoints : null;
			}
			else if (Position <= 0)
			{
				_lastShortEntryPrice = Position < 0 ? PositionPrice : null;
			}
		}
		else if (order.Direction == Sides.Sell)
		{
			if (Position < 0)
			{
				var price = PositionPrice != 0m ? PositionPrice : myTrade.Price;
				_lastShortEntryPrice = price;
				_shortTakeProfitPrice = TakeProfitPoints > 0m ? price - priceStep * TakeProfitPoints : null;
			}
			else if (Position >= 0)
			{
				_lastLongEntryPrice = Position > 0 ? PositionPrice : null;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			ResetShortState();
		}
		else if (Position < 0)
		{
			ResetLongState();
		}
		else
		{
			ResetRiskState();
		}
	}
}
