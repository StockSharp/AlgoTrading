using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "MACD_signal" MetaTrader expert that trades MACD histogram breakouts filtered by ATR.
/// </summary>
public class MacdSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _thresholdMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _minimumTakeProfitPoints;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private AverageTrueRange _atr = null!;

	private decimal? _previousHistogram;
	private decimal? _previousAtr;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Fixed take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps. Set to zero to disable trailing.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Fast EMA length used by the MACD indicator.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length used by the MACD indicator.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the MACD signal line.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR to build the histogram breakout threshold.
	/// </summary>
	public decimal ThresholdMultiplier
	{
		get => _thresholdMultiplier.Value;
		set => _thresholdMultiplier.Value = value;
	}

	/// <summary>
	/// Averaging period for the ATR volatility filter.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Primary candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimal allowed take-profit distance in price steps.
	/// </summary>
	public int MinimumTakeProfitPoints
	{
		get => _minimumTakeProfitPoints.Value;
		set => _minimumTakeProfitPoints.Value = value;
	}


	/// Initializes a new instance of the <see cref="MacdSignalStrategy"/> class.
	/// </summary>
	public MacdSignalStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Distance for the fixed take-profit target in price steps.", "Risk");
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order size used for every market entry.", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 25)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (points)", "Trailing distance in price steps. Set to 0 to disable trailing.", "Risk")
			.SetCanOptimize(true);

		_fastPeriod = Param(nameof(FastPeriod), 9)
			.SetRange(1, 100)
			.SetDisplay("MACD Fast EMA", "Length of the fast EMA used by MACD.", "Indicators")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 15)
			.SetRange(2, 150)
			.SetDisplay("MACD Slow EMA", "Length of the slow EMA used by MACD.", "Indicators")
			.SetCanOptimize(true);

		_signalPeriod = Param(nameof(SignalPeriod), 8)
			.SetRange(1, 100)
			.SetDisplay("MACD Signal EMA", "Smoothing period for the MACD signal line.", "Indicators")
			.SetCanOptimize(true);

		_thresholdMultiplier = Param(nameof(ThresholdMultiplier), 0.004m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Multiplier applied to ATR to form the breakout threshold.", "Indicators")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 200)
			.SetRange(10, 500)
			.SetDisplay("ATR Period", "Length of the Average True Range filter.", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for MACD and ATR calculations.", "General");

		_minimumTakeProfitPoints = Param(nameof(MinimumTakeProfitPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Min Take Profit (points)", "Smallest allowed take-profit distance applied for safety checks.", "Risk");
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
		_previousAtr = null;
		ResetTrailing();

		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_macd, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue atrValue)
	{
		// Skip unfinished candles to avoid acting on partial data.
		if (candle.State != CandleStates.Finished)
			return;

		// Honor the original EA guard that required a minimum take-profit.
		if (TakeProfitPoints < MinimumTakeProfitPoints)
			return;

		// Ensure the strategy is allowed to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// MACD is a composite indicator, so BindEx returns a typed value.
		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
			return;

		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;

		var histogram = macdLine - signalLine;
		var atr = atrValue.ToDecimal();

		// Manage existing positions before evaluating fresh entries.
		if (HandleActivePosition(candle, histogram))
			return;

		var previousHistogram = _previousHistogram;
		var previousAtr = _previousAtr;

		_previousHistogram = histogram;
		_previousAtr = atr;

		if (previousHistogram is null || previousAtr is null)
			return;

		var threshold = previousAtr.Value * ThresholdMultiplier;
		if (threshold <= 0m)
			return;

		var allowLong = Direction is null || Direction == Sides.Buy;
		var allowShort = Direction is null || Direction == Sides.Sell;

		// Replicate the bullish breakout: histogram crosses above +threshold.
		if (allowLong && histogram > threshold && previousHistogram.Value < threshold && Position <= 0)
		{
			BuyMarket(TradeVolume + Math.Abs(Position));
			ResetTrailing();
			return;
		}

		// Replicate the bearish breakout: histogram crosses below -threshold.
		if (allowShort && histogram < -threshold && previousHistogram.Value > -threshold && Position >= 0)
		{
			SellMarket(TradeVolume + Math.Abs(Position));
			ResetTrailing();
		}
	}

	private bool HandleActivePosition(ICandleMessage candle, decimal histogram)
	{
		if (Position > 0)
		{
			// Exit long trades when the histogram turns negative.
			if (histogram < 0m)
			{
				ClosePosition();
				ResetTrailing();
				return true;
			}

			var entry = PositionPrice;
			if (entry <= 0m)
				return false;

			// Check the fixed take-profit target for long positions.
			if (TakeProfitPoints > 0)
			{
				var target = entry + GetPriceOffset(TakeProfitPoints);
				if (target > 0m && candle.HighPrice >= target)
				{
					ClosePosition();
					ResetTrailing();
					return true;
				}
			}

			// Emulate the trailing stop that follows the bid price.
			if (TrailingStopPoints > 0)
			{
				var distance = GetPriceOffset(TrailingStopPoints);
				if (distance > 0m)
				{
					if (candle.ClosePrice - entry > distance)
					{
						var desired = candle.ClosePrice - distance;
						if (_longTrailingStop is null || desired > _longTrailingStop.Value)
							_longTrailingStop = desired;
					}

					if (_longTrailingStop is decimal stop && candle.LowPrice <= stop)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}
		}
		else if (Position < 0)
		{
			// Exit short trades when the histogram turns positive.
			if (histogram > 0m)
			{
				ClosePosition();
				ResetTrailing();
				return true;
			}

			var entry = PositionPrice;
			if (entry <= 0m)
				return false;

			// Check the fixed take-profit target for short positions.
			if (TakeProfitPoints > 0)
			{
				var target = entry - GetPriceOffset(TakeProfitPoints);
				if (target > 0m && candle.LowPrice <= target)
				{
					ClosePosition();
					ResetTrailing();
					return true;
				}
			}

			// Emulate the ask-based trailing stop from the original EA.
			if (TrailingStopPoints > 0)
			{
				var distance = GetPriceOffset(TrailingStopPoints);
				if (distance > 0m)
				{
					if (entry - candle.ClosePrice > distance)
					{
						var desired = candle.ClosePrice + distance;
						if (_shortTrailingStop is null || desired < _shortTrailingStop.Value)
							_shortTrailingStop = desired;
					}

					if (_shortTrailingStop is decimal stop && candle.HighPrice >= stop)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}
		}

		return false;
	}

	private void ResetTrailing()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private decimal GetPriceOffset(int points)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m || points <= 0)
			return 0m;

		return step * points;
	}
}
