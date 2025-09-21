using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor "escape".
/// Buys when the close price dips below a 5-period SMA of opens and sells when it rises above a 4-period SMA.
/// Reproduces the original fixed take-profit and stop-loss distances measured in MetaTrader points.
/// </summary>
public class EscapeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _longTakeProfitPoints;
	private readonly StrategyParam<decimal> _shortTakeProfitPoints;
	private readonly StrategyParam<decimal> _longStopLossPoints;
	private readonly StrategyParam<decimal> _shortStopLossPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _minimumMarginPerLot;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage? _longAverage;
	private SimpleMovingAverage? _shortAverage;

	private decimal? _previousLongAverage;
	private decimal? _previousShortAverage;

	private decimal? _longStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfitPrice;

	private bool _isLongExitRequested;
	private bool _isShortExitRequested;

	private decimal _pointSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="EscapeStrategy"/> class.
	/// </summary>
	public EscapeStrategy()
	{
		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 10m)
			.SetDisplay("Long Take Profit (points)", "Take-profit distance for long positions expressed in MetaTrader points.", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 10m)
			.SetDisplay("Short Take Profit (points)", "Take-profit distance for short positions expressed in MetaTrader points.", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000m)
			.SetDisplay("Long Stop Loss (points)", "Stop-loss distance for long positions expressed in MetaTrader points.", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000m)
			.SetDisplay("Short Stop Loss (points)", "Stop-loss distance for short positions expressed in MetaTrader points.", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 0.2m)
			.SetDisplay("Trade Volume", "Lot size used for market orders.", "Trading")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_minimumMarginPerLot = Param(nameof(MinimumMarginPerLot), 500m)
			.SetDisplay("Minimum Margin Per Lot", "Approximate capital requirement per lot before opening a trade.", "Risk Management")
			.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for indicator calculations.", "General");

		_pointSize = 0m;
	}

	/// <summary>
	/// Long take-profit distance in MetaTrader points.
	/// </summary>
	public decimal LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Short take-profit distance in MetaTrader points.
	/// </summary>
	public decimal ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Long stop-loss distance in MetaTrader points.
	/// </summary>
	public decimal LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Short stop-loss distance in MetaTrader points.
	/// </summary>
	public decimal ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Lot size used for every entry order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Minimum capital requirement per lot before submitting a new order.
	/// </summary>
	public decimal MinimumMarginPerLot
	{
		get => _minimumMarginPerLot.Value;
		set => _minimumMarginPerLot.Value = value;
	}

	/// <summary>
	/// Candle type used to drive indicator updates.
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

		_longAverage = null;
		_shortAverage = null;

		_previousLongAverage = null;
		_previousShortAverage = null;

		ResetPositionState();
		_pointSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = CalculatePointSize();

		_longAverage = new SimpleMovingAverage { Length = 5 };
		_shortAverage = new SimpleMovingAverage { Length = 4 };

		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenCandlesFinished(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _longAverage);
			DrawIndicator(area, _shortAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var longAverage = _longAverage;
		var shortAverage = _shortAverage;

		if (longAverage == null || shortAverage == null)
			return;

		EvaluateActivePosition(candle);

		var previousLong = _previousLongAverage;
		var previousShort = _previousShortAverage;

		if (IsFormedAndOnlineAndAllowTrading()
			&& Position == 0m
			&& previousLong.HasValue
			&& previousShort.HasValue)
		{
			var closePrice = candle.ClosePrice;
			var volume = TradeVolume;

			if (volume > 0m && HasSufficientMargin(volume))
			{
				if (closePrice < previousLong.Value)
				{
					PrepareLongLevels(closePrice);
					BuyMarket(volume);
					return;
				}

				if (closePrice > previousShort.Value)
				{
					PrepareShortLevels(closePrice);
					SellMarket(volume);
				}
			}
		}

		var longValue = longAverage.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();
		if (longAverage.IsFormed)
			_previousLongAverage = longValue;

		var shortValue = shortAverage.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();
		if (shortAverage.IsFormed)
			_previousShortAverage = shortValue;
	}

	private void EvaluateActivePosition(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var position = Position;

		if (position > 0m)
		{
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				if (!_isLongExitRequested)
				{
					SellMarket(position);
					_isLongExitRequested = true;
				}
				return;
			}

			if (_longTakeProfitPrice.HasValue && candle.HighPrice >= _longTakeProfitPrice.Value)
			{
				if (!_isLongExitRequested)
				{
					SellMarket(position);
					_isLongExitRequested = true;
				}
			}
		}
		else if (position < 0m)
		{
			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				if (!_isShortExitRequested)
				{
					BuyMarket(Math.Abs(position));
					_isShortExitRequested = true;
				}
				return;
			}

			if (_shortTakeProfitPrice.HasValue && candle.LowPrice <= _shortTakeProfitPrice.Value)
			{
				if (!_isShortExitRequested)
				{
					BuyMarket(Math.Abs(position));
					_isShortExitRequested = true;
				}
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetPositionState();
			return;
		}

		if (Position > 0m && delta > 0m)
		{
			_isLongExitRequested = false;
			_isShortExitRequested = false;
		}
		else if (Position < 0m && delta < 0m)
		{
			_isLongExitRequested = false;
			_isShortExitRequested = false;
		}
	}

	private void PrepareLongLevels(decimal entryPrice)
	{
		var point = CalculatePointSize();
		_pointSize = point;

		_longStopPrice = LongStopLossPoints > 0m ? entryPrice - (LongStopLossPoints * point) : null;
		_longTakeProfitPrice = LongTakeProfitPoints > 0m ? entryPrice + (LongTakeProfitPoints * point) : null;
		_isLongExitRequested = false;
		_isShortExitRequested = false;
	}

	private void PrepareShortLevels(decimal entryPrice)
	{
		var point = CalculatePointSize();
		_pointSize = point;

		_shortStopPrice = ShortStopLossPoints > 0m ? entryPrice + (ShortStopLossPoints * point) : null;
		_shortTakeProfitPrice = ShortTakeProfitPoints > 0m ? entryPrice - (ShortTakeProfitPoints * point) : null;
		_isLongExitRequested = false;
		_isShortExitRequested = false;
	}

	private void ResetPositionState()
	{
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_isLongExitRequested = false;
		_isShortExitRequested = false;
	}

	private decimal CalculatePointSize()
	{
		var security = Security;
		var priceStep = security?.PriceStep ?? 0m;
		return priceStep > 0m ? priceStep : 1m;
	}

	private bool HasSufficientMargin(decimal volume)
	{
		var portfolioValue = Portfolio?.CurrentValue ?? 0m;

		if (portfolioValue <= 0m)
			return true;

		var required = MinimumMarginPerLot * volume;

		if (portfolioValue < required)
		{
			LogInfo($"Insufficient capital. Portfolio value: {portfolioValue:F2}, required: {required:F2}.");
			return false;
		}

		return true;
	}
}
