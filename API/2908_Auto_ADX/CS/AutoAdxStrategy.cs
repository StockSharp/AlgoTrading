using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Auto ADX strategy converted from MQL implementation.
/// It trades based on ADX strength and directional movement lines with optional trailing exits.
/// </summary>
public class AutoAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxLevel;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal? _previousAdx;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal step in pips required before moving the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Averaging period for the ADX indicator.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX level required for entry conditions.
	/// </summary>
	public decimal AdxLevel
	{
		get => _adxLevel.Value;
		set => _adxLevel.Value = value;
	}

	/// <summary>
	/// Determines whether open positions should be closed when signals reverse.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
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
	/// Initializes a new instance of the <see cref="AutoAdxStrategy"/> class.
	/// </summary>
	public AutoAdxStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Distance for stop loss in pips.", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Distance for take profit in pips.", "Risk")
			.SetNotNegative();

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips.", "Risk")
			.SetNotNegative();

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimal profit advance before adjusting trailing stop.", "Risk")
			.SetNotNegative();

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Averaging period for ADX calculation.", "Indicators")
			.SetGreaterThanZero();

		_adxLevel = Param(nameof(AdxLevel), 30m)
			.SetDisplay("ADX Level", "ADX threshold used to filter trades.", "Indicators")
			.SetRange(10m, 60m);

		_reverseSignals = Param(nameof(ReverseSignals), true)
			.SetDisplay("Reverse Signals", "Close positions when conditions flip.", "Behaviour");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles processed by the strategy.", "General");
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

		_previousAdx = null;
		ResetTrailingStops();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		_pipSize = CalculatePipSize();

		var stopLossUnit = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null;
		var takeProfitUnit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null;

		// Use built-in protection for initial stop-loss and take-profit.
		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);

		var adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		// Subscribe to candles and bind the ADX indicator.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		// Optional chart visualization.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
			return 1m;

		var ratio = 1m / step;
		var digits = (int)Math.Round(Math.Log10((double)ratio), MidpointRounding.AwayFromZero);
		var adjust = digits == 3 || digits == 5 ? 10m : 1m;

		return step * adjust;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		// Only finished candles are processed to avoid intrabar noise.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var typedAdx = (AverageDirectionalIndexValue)adxValue;

		if (typedAdx.MovingAverage is not decimal currentAdx ||
			typedAdx.Dx.Plus is not decimal plusDi ||
			typedAdx.Dx.Minus is not decimal minusDi)
		{
			return;
		}

		// Update trailing stops before evaluating new signals.
		UpdateTrailingStops(candle);

		var previousAdx = _previousAdx;

		// Optional reverse logic closes positions when trend conditions flip.
		if (ReverseSignals && Position != 0 && previousAdx.HasValue)
		{
			if (Position > 0 && (plusDi < minusDi || currentAdx < previousAdx.Value))
			{
				ClosePosition();
				ResetTrailingStops();
				_previousAdx = currentAdx;
				return;
			}

			if (Position < 0 && (plusDi > minusDi || currentAdx > previousAdx.Value))
			{
				ClosePosition();
				ResetTrailingStops();
				_previousAdx = currentAdx;
				return;
			}
		}

		if (Position == 0 && previousAdx.HasValue)
		{
			// Long entry requires +DI dominance, strong ADX, and rising trend strength.
			if (plusDi > minusDi && currentAdx > AdxLevel && currentAdx > previousAdx.Value)
			{
				BuyMarket(Volume);
				ResetTrailingStops();
			}
			// Short entry requires -DI dominance, weak ADX, and decreasing trend strength.
			else if (plusDi < minusDi && currentAdx < AdxLevel && currentAdx < previousAdx.Value)
			{
				SellMarket(Volume);
				ResetTrailingStops();
			}
		}

		_previousAdx = currentAdx;

		if (Position == 0)
			ResetTrailingStops();
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0)
		{
			ResetTrailingStops();
			return;
		}

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0)
		{
			var entryPrice = PositionPrice;
			if (entryPrice == 0m)
				return;

			var profit = candle.ClosePrice - entryPrice;
			if (profit <= trailingDistance)
				return;

			var candidate = candle.ClosePrice - trailingDistance;

			if (_longTrailingStop is null || candidate - _longTrailingStop.Value >= trailingStep)
				_longTrailingStop = candidate;

			if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			{
				ClosePosition();
				ResetTrailingStops();
			}
		}
		else if (Position < 0)
		{
			var entryPrice = PositionPrice;
			if (entryPrice == 0m)
				return;

			var profit = entryPrice - candle.ClosePrice;
			if (profit <= trailingDistance)
				return;

			var candidate = candle.ClosePrice + trailingDistance;

			if (_shortTrailingStop is null || _shortTrailingStop.Value - candidate >= trailingStep)
				_shortTrailingStop = candidate;

			if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			{
				ClosePosition();
				ResetTrailingStops();
			}
		}
		else
		{
			ResetTrailingStops();
		}
	}

	private void ResetTrailingStops()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}
}
