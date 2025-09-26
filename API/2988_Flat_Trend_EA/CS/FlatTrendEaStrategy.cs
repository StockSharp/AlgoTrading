namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the MQL5 "Flat Trend EA" that combines Parabolic SAR with ADX directional movement.
/// </summary>
public class FlatTrendEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _sarStart;
	private readonly StrategyParam<decimal> _sarIncrement;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;
	private ParabolicSar _parabolicSar;

	private decimal _pipSize;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

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
	/// Trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Whether trading is limited to a time window.
	/// </summary>
	public bool UseTradingHours
	{
		get => _useTradingHours.Value;
		set => _useTradingHours.Value = value;
	}

	/// <summary>
	/// Session start hour in exchange time.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session end hour in exchange time (exclusive).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// ADX period that controls the +DI and -DI smoothing.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Initial acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarStart
	{
		get => _sarStart.Value;
		set => _sarStart.Value = value;
	}

	/// <summary>
	/// Acceleration step for Parabolic SAR.
	/// </summary>
	public decimal SarIncrement
	{
		get => _sarIncrement.Value;
		set => _sarIncrement.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Candle type that controls indicator timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FlatTrendEaStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Step required to advance trailing stop", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_useTradingHours = Param(nameof(UseTradingHours), true)
			.SetDisplay("Use Trading Hours", "Limit trading to specific hours", "Session");

		_startHour = Param(nameof(StartHour), 10)
			.SetDisplay("Start Hour", "Session start hour (0-23)", "Session")
			.SetRange(0, 23)
			.SetCanOptimize(true);

		_endHour = Param(nameof(EndHour), 19)
			.SetDisplay("End Hour", "Session end hour (exclusive, 1-24)", "Session")
			.SetRange(1, 24)
			.SetCanOptimize(true);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Smoothing period for ADX", "Indicators")
			.SetRange(2, 100)
			.SetCanOptimize(true);

		_sarStart = Param(nameof(SarStart), 0.02m)
			.SetDisplay("SAR Start", "Initial acceleration factor", "Indicators")
			.SetCanOptimize(true);

		_sarIncrement = Param(nameof(SarIncrement), 0.02m)
			.SetDisplay("SAR Step", "Acceleration increment", "Indicators")
			.SetCanOptimize(true);

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetDisplay("SAR Maximum", "Maximum acceleration factor", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security security, DataType dataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetRiskState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		if (UseTradingHours && StartHour >= EndHour)
			throw new InvalidOperationException("Start hour must be less than end hour when trading hours are enabled.");

		_pipSize = CalculatePipSize();
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_parabolicSar = new ParabolicSar
		{
			Acceleration = SarStart,
			AccelerationStep = SarIncrement,
			AccelerationMax = SarMaximum
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, _parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);

			var adxArea = CreateChartArea();
			if (adxArea != null)
			{
				DrawIndicator(adxArea, _adx);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_adx is null || _parabolicSar is null)
			return;

		// Evaluate indicator state for the current candle.
		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		var plusDi = adxTyped.Dx.Plus;
		var minusDi = adxTyped.Dx.Minus;
		var sar = sarValue.ToDecimal();
		var closePrice = candle.ClosePrice;

		var isPriceAboveSar = closePrice > sar;
		var buySignal = isPriceAboveSar && plusDi > minusDi;
		var sellSignal = !isPriceAboveSar && plusDi <= minusDi;
		var endSellSignal = !isPriceAboveSar && plusDi > minusDi;
		var endBuySignal = isPriceAboveSar && plusDi <= minusDi;

		// Close any opposite exposure once signals flip.
		ClosePositionsOnSignals(buySignal, sellSignal, endSellSignal, endBuySignal);

		if (Position != 0)
		{
			// Refresh trailing logic before checking price barriers.
			UpdateTrailingStop(candle);
			if (CheckRiskExit(candle))
				return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseTradingHours)
		{
			var hour = candle.OpenTime.Hour;
			if (hour < StartHour || hour >= EndHour)
				return;
		}

		if (Position != 0)
			return;

		if (buySignal)
		{
			// Define risk levels for the new long position.
			PrepareRiskLevels(closePrice, true);
			BuyMarket(Volume);
		}
		else if (sellSignal)
		{
			// Define risk levels for the new short position.
			PrepareRiskLevels(closePrice, false);
			SellMarket(Volume);
		}
	}

	private void ClosePositionsOnSignals(bool buySignal, bool sellSignal, bool endSellSignal, bool endBuySignal)
	{
		if (Position < 0 && (buySignal || endSellSignal || endBuySignal))
		{
			BuyMarket(Math.Abs(Position));
			ResetRiskState();
		}

		if (Position > 0 && (sellSignal || endSellSignal || endBuySignal))
		{
			SellMarket(Position);
			ResetRiskState();
		}
	}

	private void PrepareRiskLevels(decimal entryPrice, bool isLong)
	{
		// Remember the entry to drive trailing-stop math.
		_entryPrice = entryPrice;

		if (StopLossPips > 0m)
		{
			var distance = StopLossPips * _pipSize;
			var rawPrice = isLong ? entryPrice - distance : entryPrice + distance;
			_stopPrice = Security?.ShrinkPrice(rawPrice) ?? rawPrice;
		}
		else
		{
			_stopPrice = null;
		}

		if (TakeProfitPips > 0m)
		{
			var distance = TakeProfitPips * _pipSize;
			var rawPrice = isLong ? entryPrice + distance : entryPrice - distance;
			_takeProfitPrice = Security?.ShrinkPrice(rawPrice) ?? rawPrice;
		}
		else
		{
			_takeProfitPrice = null;
		}
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || _entryPrice is null)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var closePrice = candle.ClosePrice;

		if (Position > 0)
		{
			// Trail long stop once price advances enough.
			var moved = closePrice - _entryPrice.Value;
			if (moved > trailingDistance + trailingStep)
			{
				var threshold = closePrice - (trailingDistance + trailingStep);
				if (!_stopPrice.HasValue || _stopPrice.Value < threshold)
				{
					var rawPrice = closePrice - trailingDistance;
					_stopPrice = Security?.ShrinkPrice(rawPrice) ?? rawPrice;
				}
			}
		}
		else if (Position < 0)
		{
			// Trail short stop when market falls in our favor.
			var moved = _entryPrice.Value - closePrice;
			if (moved > trailingDistance + trailingStep)
			{
				var threshold = closePrice + trailingDistance + trailingStep;
				if (!_stopPrice.HasValue || _stopPrice.Value > threshold)
				{
					var rawPrice = closePrice + trailingDistance;
					_stopPrice = Security?.ShrinkPrice(rawPrice) ?? rawPrice;
				}
			}
		}
	}

	private bool CheckRiskExit(ICandleMessage candle)
	{
		if (_entryPrice is null)
			return false;

		if (Position > 0)
		{
			// Exit long if stop-loss is breached.
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Position);
				ResetRiskState();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Position);
				ResetRiskState();
				return true;
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);
			// Cover short if the protective stop is touched.
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(volume);
				ResetRiskState();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(volume);
				ResetRiskState();
				return true;
			}
		}

		return false;
	}

	private void ResetRiskState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? 4;
		var multiplier = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * multiplier;
	}
}
