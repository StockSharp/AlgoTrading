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
/// Day-opening strategy filtered by the MACD histogram slope and protected with fixed stops.
/// </summary>
public class DayOpeningMacdHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal? _previousHistogram;
	private decimal? _previousPreviousHistogram;
	private DateTime? _lastProcessedDay;

	private decimal? _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// MACD fast EMA period.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal line period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
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
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DayOpeningMacdHistogramStrategy"/> class.
	/// </summary>
	public DayOpeningMacdHistogramStrategy()
	{
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 58)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 195)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 183)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period for MACD", "Indicators");

		_stopLossPoints = Param(nameof(StopLossPoints), 875m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 510m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take-profit distance in points", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 2172m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for analysis", "General");
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

		_previousHistogram = null;
		_previousPreviousHistogram = null;
		_lastProcessedDay = null;
		_entryPrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
			return;

		if (macdTyped.Macd is not decimal macdMain || macdTyped.Signal is not decimal macdSignal)
			return;

		var histogram = macdMain - macdSignal;

		if (Position == 0 && _entryPrice.HasValue)
		{
			ResetPositionState();
		}

		if (Position > 0 && _entryPrice is decimal longEntry)
		{
			UpdateHighLow(candle);
			var stopPrice = Math.Max(longEntry - PointsToPrice(StopLossPoints), _highestPrice - PointsToPrice(TrailingStopPoints));
			var takePrice = longEntry + PointsToPrice(TakeProfitPoints);

			if (candle.LowPrice <= stopPrice)
			{
				SellMarket(Position);
				ResetPositionState();
			}
			else if (candle.HighPrice >= takePrice)
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else if (Position < 0 && _entryPrice is decimal shortEntry)
		{
			UpdateHighLow(candle);
			var stopPrice = Math.Min(shortEntry + PointsToPrice(StopLossPoints), _lowestPrice + PointsToPrice(TrailingStopPoints));
			var takePrice = shortEntry - PointsToPrice(TakeProfitPoints);

			if (candle.HighPrice >= stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
			else if (candle.LowPrice <= takePrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}

		var candleDay = candle.OpenTime.Date;
		var isNewDay = !_lastProcessedDay.HasValue || _lastProcessedDay.Value != candleDay;

		if (isNewDay)
		{
			TryOpenAtDayStart(candle);
			_lastProcessedDay = candleDay;
		}

		_previousPreviousHistogram = _previousHistogram;
		_previousHistogram = histogram;
	}

	private void TryOpenAtDayStart(ICandleMessage candle)
	{
		if (!_previousHistogram.HasValue || !_previousPreviousHistogram.HasValue)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var histogramFalling = _previousPreviousHistogram.Value > _previousHistogram.Value;
		var histogramRising = _previousPreviousHistogram.Value < _previousHistogram.Value;

		if (histogramFalling)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}

			if (Position <= 0)
			{
				BuyMarket();
				InitializePositionState(candle.OpenPrice);
				UpdateHighLow(candle);
			}
		}
		else if (histogramRising)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				ResetPositionState();
			}

			if (Position >= 0)
			{
				SellMarket();
				InitializePositionState(candle.OpenPrice);
				UpdateHighLow(candle);
			}
		}
	}

	private void InitializePositionState(decimal price)
	{
		_entryPrice = price;
		_highestPrice = price;
		_lowestPrice = price;
	}

	private void UpdateHighLow(ICandleMessage candle)
	{
		if (_entryPrice is null)
			return;

		_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
		_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}

	private decimal PointsToPrice(decimal points)
	{
		var step = Security?.PriceStep ?? 1m;
		return points * step;
	}
}

