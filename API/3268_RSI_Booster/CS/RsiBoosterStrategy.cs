using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "RSI booster" expert advisor.
/// Opens a position when the fast RSI moves away from the delayed RSI by a configurable ratio.
/// Applies optional stop-loss, take-profit, trailing stop, and a limited reverse-recovery mechanism.
/// </summary>
public class RsiBoosterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _ratio;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _onlyOnePositionPerBar;
	private readonly StrategyParam<bool> _returnOrderEnabled;
	private readonly StrategyParam<int> _returnOrdersMax;
	private readonly StrategyParam<int> _firstRsiPeriod;
	private readonly StrategyParam<AppliedPriceType> _firstRsiPrice;
	private readonly StrategyParam<int> _secondRsiPeriod;
	private readonly StrategyParam<AppliedPriceType> _secondRsiPrice;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _firstRsi = null!;
	private RelativeStrengthIndex _secondRsi = null!;

	private decimal? _previousSecondRsi;
	private bool _hasSecondHistory;

	private DateTimeOffset? _lastLongSignalTime;
	private DateTimeOffset? _lastShortSignalTime;

	private decimal _previousPosition;
	private decimal _lastRealizedPnL;
	private int _currentReturnCount;

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private decimal _priceStep;

	/// <summary>
	/// Initializes a new instance of <see cref="RsiBoosterStrategy"/>.
	/// </summary>
	public RsiBoosterStrategy()
	{
		_volume = Param(nameof(Volume), 0.01m)
		.SetDisplay("Volume", "Trading volume in lots", LocalizedStrings.StrGeneral);

		_ratio = Param(nameof(Ratio), 10m)
		.SetDisplay("RSI Difference", "Threshold between fast and delayed RSI", LocalizedStrings.StrGeneral);

		_stopLossPips = Param(nameof(StopLossPips), 9m)
		.SetDisplay("Stop Loss", "Fixed stop-loss in points", LocalizedStrings.StrGeneral)
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 5m)
		.SetDisplay("Take Profit", "Fixed take-profit in points", LocalizedStrings.StrGeneral)
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 25m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in points", LocalizedStrings.StrGeneral)
		.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step", "Minimum improvement before moving the trailing stop", LocalizedStrings.StrGeneral)
		.SetCanOptimize(true);

		_onlyOnePositionPerBar = Param(nameof(OnlyOnePositionPerBar), true)
		.SetDisplay("One Trade Per Bar", "Allow only a single entry per bar and direction", LocalizedStrings.StrGeneral);

		_returnOrderEnabled = Param(nameof(ReturnOrderEnabled), false)
		.SetDisplay("Enable Return Order", "Open an opposite recovery order after a loss", LocalizedStrings.StrGeneral);

		_returnOrdersMax = Param(nameof(ReturnOrdersMax), 2)
		.SetDisplay("Return Order Limit", "Maximum number of chained recovery orders", LocalizedStrings.StrGeneral)
		.SetCanOptimize(true);

		_firstRsiPeriod = Param(nameof(FirstRsiPeriod), 14)
		.SetDisplay("Fast RSI Period", "Calculation period for the fast RSI", LocalizedStrings.StrIndicators);

		_firstRsiPrice = Param(nameof(FirstRsiPrice), AppliedPriceType.Close)
		.SetDisplay("Fast RSI Price", "Price source for the fast RSI", LocalizedStrings.StrIndicators);

		_secondRsiPeriod = Param(nameof(SecondRsiPeriod), 14)
		.SetDisplay("Delayed RSI Period", "Calculation period for the delayed RSI", LocalizedStrings.StrIndicators);

		_secondRsiPrice = Param(nameof(SecondRsiPrice), AppliedPriceType.Close)
		.SetDisplay("Delayed RSI Price", "Price source for the delayed RSI", LocalizedStrings.StrIndicators);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used for calculations", LocalizedStrings.StrGeneral);
	}

	/// <summary>
	/// Trading volume used for market orders.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Minimum RSI difference required to open a new position.
	/// </summary>
	public decimal Ratio
	{
		get => _ratio.Value;
		set => _ratio.Value = value;
	}

	/// <summary>
	/// Fixed stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Fixed take-profit distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in instrument points.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum improvement before shifting the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Controls whether multiple entries per bar are allowed.
	/// </summary>
	public bool OnlyOnePositionPerBar
	{
		get => _onlyOnePositionPerBar.Value;
		set => _onlyOnePositionPerBar.Value = value;
	}

	/// <summary>
	/// Enables the loss-recovery reverse order logic.
	/// </summary>
	public bool ReturnOrderEnabled
	{
		get => _returnOrderEnabled.Value;
		set => _returnOrderEnabled.Value = value;
	}

	/// <summary>
	/// Maximum number of chained recovery orders.
	/// </summary>
	public int ReturnOrdersMax
	{
		get => _returnOrdersMax.Value;
		set => _returnOrdersMax.Value = value;
	}

	/// <summary>
	/// Period of the fast RSI indicator.
	/// </summary>
	public int FirstRsiPeriod
	{
		get => _firstRsiPeriod.Value;
		set => _firstRsiPeriod.Value = value;
	}

	/// <summary>
	/// Price source for the fast RSI.
	/// </summary>
	public AppliedPriceType FirstRsiPrice
	{
		get => _firstRsiPrice.Value;
		set => _firstRsiPrice.Value = value;
	}

	/// <summary>
	/// Period of the delayed RSI indicator.
	/// </summary>
	public int SecondRsiPeriod
	{
		get => _secondRsiPeriod.Value;
		set => _secondRsiPeriod.Value = value;
	}

	/// <summary>
	/// Price source for the delayed RSI.
	/// </summary>
	public AppliedPriceType SecondRsiPrice
	{
		get => _secondRsiPrice.Value;
		set => _secondRsiPrice.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_firstRsi = null!;
		_secondRsi = null!;
		_previousSecondRsi = null;
		_hasSecondHistory = false;
		_lastLongSignalTime = null;
		_lastShortSignalTime = null;
		_previousPosition = 0m;
		_lastRealizedPnL = 0m;
		_currentReturnCount = 0;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_firstRsi = new RelativeStrengthIndex { Length = FirstRsiPeriod };
		_secondRsi = new RelativeStrengthIndex { Length = SecondRsiPeriod };

		_priceStep = GetPriceStep();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _firstRsi);
			DrawIndicator(area, _secondRsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var fastInput = GetPrice(candle, FirstRsiPrice);
		var fastValue = _firstRsi.Process(fastInput, candle.OpenTime, true);
		if (!fastValue.IsFinal)
		return;

		var fastRsi = fastValue.ToDecimal();

		var slowInput = GetPrice(candle, SecondRsiPrice);
		var slowValue = _secondRsi.Process(slowInput, candle.OpenTime, true);
		if (!slowValue.IsFinal)
		return;

		var slowRsi = slowValue.ToDecimal();

		var positionClosed = UpdateRiskManagement(candle);
		if (positionClosed)
		{
			_previousSecondRsi = slowRsi;
			return;
		}

		if (!_hasSecondHistory)
		{
			_previousSecondRsi = slowRsi;
			_hasSecondHistory = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousSecondRsi = slowRsi;
			return;
		}

		var difference = fastRsi - _previousSecondRsi.Value;

		if (difference > Ratio && Position <= 0m && CanEnterLong(candle.OpenTime))
		{
			var volume = Volume + Math.Abs(Position);
			ResetTrailing();
			_currentReturnCount = 0;
			BuyMarket(volume);
			_lastLongSignalTime = candle.OpenTime;
		}
		else if (difference < -Ratio && Position >= 0m && CanEnterShort(candle.OpenTime))
		{
			var volume = Volume + Math.Abs(Position);
			ResetTrailing();
			_currentReturnCount = 0;
			SellMarket(volume);
			_lastShortSignalTime = candle.OpenTime;
		}

		_previousSecondRsi = slowRsi;
	}

	private bool UpdateRiskManagement(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var entry = PositionPrice;
			if (entry <= 0m)
			return false;

			var stopOffset = StopLossPips > 0m ? GetPriceOffset(StopLossPips) : 0m;
			if (stopOffset > 0m)
			{
				var stopPrice = entry - stopOffset;
				if (candle.LowPrice <= stopPrice)
				{
					ClosePosition();
					ResetTrailing();
					return true;
				}
			}

			var takeOffset = TakeProfitPips > 0m ? GetPriceOffset(TakeProfitPips) : 0m;
			if (takeOffset > 0m)
			{
				var takePrice = entry + takeOffset;
				if (candle.HighPrice >= takePrice)
				{
					ClosePosition();
					ResetTrailing();
					return true;
				}
			}

			var trailingDistance = TrailingStopPips > 0m ? GetPriceOffset(TrailingStopPips) : 0m;
			if (trailingDistance > 0m)
			{
				var trailingStep = TrailingStepPips > 0m ? GetPriceOffset(TrailingStepPips) : 0m;
				var profit = candle.ClosePrice - entry;

				if (profit > trailingDistance)
				{
					var desiredStop = candle.ClosePrice - trailingDistance;
					var minimalImprovement = trailingStep > 0m ? trailingStep : (Security?.PriceStep ?? 0m);

					if (_longTrailingStop is null || desiredStop - _longTrailingStop.Value >= minimalImprovement)
					_longTrailingStop = desiredStop;
				}

				if (_longTrailingStop is decimal trail && candle.LowPrice <= trail)
				{
					ClosePosition();
					ResetTrailing();
					return true;
				}
			}
		}
		else if (Position < 0m)
		{
			var entry = PositionPrice;
			if (entry <= 0m)
			return false;

			var stopOffset = StopLossPips > 0m ? GetPriceOffset(StopLossPips) : 0m;
			if (stopOffset > 0m)
			{
				var stopPrice = entry + stopOffset;
				if (candle.HighPrice >= stopPrice)
				{
					ClosePosition();
					ResetTrailing();
					return true;
				}
			}

			var takeOffset = TakeProfitPips > 0m ? GetPriceOffset(TakeProfitPips) : 0m;
			if (takeOffset > 0m)
			{
				var takePrice = entry - takeOffset;
				if (candle.LowPrice <= takePrice)
				{
					ClosePosition();
					ResetTrailing();
					return true;
				}
			}

			var trailingDistance = TrailingStopPips > 0m ? GetPriceOffset(TrailingStopPips) : 0m;
			if (trailingDistance > 0m)
			{
				var trailingStep = TrailingStepPips > 0m ? GetPriceOffset(TrailingStepPips) : 0m;
				var profit = entry - candle.ClosePrice;

				if (profit > trailingDistance)
				{
					var desiredStop = candle.ClosePrice + trailingDistance;
					var minimalImprovement = trailingStep > 0m ? trailingStep : (Security?.PriceStep ?? 0m);

					if (_shortTrailingStop is null || _shortTrailingStop.Value - desiredStop >= minimalImprovement)
					_shortTrailingStop = desiredStop;
				}

				if (_shortTrailingStop is decimal trail && candle.HighPrice >= trail)
				{
					ClosePosition();
					ResetTrailing();
					return true;
				}
			}
		}
		else
		{
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}

		return false;
	}

	private bool CanEnterLong(DateTimeOffset candleTime)
	{
		if (!OnlyOnePositionPerBar)
		return true;

		return _lastLongSignalTime is null || _lastLongSignalTime.Value != candleTime;
	}

	private bool CanEnterShort(DateTimeOffset candleTime)
	{
		if (!OnlyOnePositionPerBar)
		return true;

		return _lastShortSignalTime is null || _lastShortSignalTime.Value != candleTime;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (_previousPosition == 0m && Position != 0m)
		{
			_lastRealizedPnL = PnLManager?.RealizedPnL ?? PnL;
		}
		else if (_previousPosition != 0m && Position == 0m)
		{
			var realized = PnLManager?.RealizedPnL ?? PnL;
			var tradePnL = realized - _lastRealizedPnL;
			_lastRealizedPnL = realized;

			if (ReturnOrderEnabled)
			{
				if (tradePnL < 0m)
				{
					if (_currentReturnCount < ReturnOrdersMax)
					{
						_currentReturnCount++;

						if (IsFormedAndOnlineAndAllowTrading())
						{
							ResetTrailing();

							if (_previousPosition > 0m)
							{
								SellMarket(Volume);
							}
							else if (_previousPosition < 0m)
							{
								BuyMarket(Volume);
							}
						}
					}
					else
					{
						_currentReturnCount = 0;
					}
				}
				else
				{
					_currentReturnCount = 0;
				}
			}
			else
			{
				_currentReturnCount = 0;
			}

			ResetTrailing();
		}

		_previousPosition = Position;
	}

	private void ResetTrailing()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private decimal GetPriceOffset(decimal points)
	{
		if (points <= 0m)
		return 0m;

		var step = _priceStep;
		if (step <= 0m)
		step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
		step = 0.0001m;

		return points * step;
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m)
		return step;

		return 0.0001m;
	}

	private static decimal GetPrice(ICandleMessage candle, AppliedPriceType type)
	{
		return type switch
		{
			AppliedPriceType.Close => candle.ClosePrice,
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	/// <summary>
	/// Price sources compatible with MetaTrader applied price modes.
	/// </summary>
	public enum AppliedPriceType
	{
		/// <summary>
		/// Close price of the candle.
		/// </summary>
		Close,

		/// <summary>
		/// Open price of the candle.
		/// </summary>
		Open,

		/// <summary>
		/// High price of the candle.
		/// </summary>
		High,

		/// <summary>
		/// Low price of the candle.
		/// </summary>
		Low,

		/// <summary>
		/// Average of the high and low prices.
		/// </summary>
		Median,

		/// <summary>
		/// Average of high, low, and close prices.
		/// </summary>
		Typical,

		/// <summary>
		/// Weighted price where the close has double weight.
		/// </summary>
		Weighted
	}
}
