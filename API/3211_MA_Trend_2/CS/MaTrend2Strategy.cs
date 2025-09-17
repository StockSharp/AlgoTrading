using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Logging;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the "MA Trend 2" MetaTrader strategy using the high-level StockSharp API.
/// The system compares the previous moving average value with the current price to determine entries.
/// </summary>
public class MaTrend2Strategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<LotManagementMode> _lotMode;
	private readonly StrategyParam<decimal> _lotOrRiskValue;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<CandlePrice> _maPrice;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TradingDirection> _tradingDirection;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;

	private readonly List<decimal> _maHistory = new();

	private decimal _pipSize;
	private decimal _longTrailingStop;
	private decimal _shortTrailingStop;

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
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
	/// Minimal price improvement required to move the trailing stop, measured in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Lot sizing mode used by the strategy.
	/// </summary>
	public LotManagementMode LotMode
	{
		get => _lotMode.Value;
		set => _lotMode.Value = value;
	}

	/// <summary>
	/// Fixed volume or risk percent depending on <see cref="LotMode"/>.
	/// </summary>
	public decimal LotOrRiskValue
	{
		get => _lotOrRiskValue.Value;
		set => _lotOrRiskValue.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Number of completed candles between the current bar and the moving average sample used for signals.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Candle price used by the moving average calculation.
	/// </summary>
	public CandlePrice MaPrice
	{
		get => _maPrice.Value;
		set => _maPrice.Value = value;
	}

	/// <summary>
	/// Candle series consumed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public TradingDirection Direction
	{
		get => _tradingDirection.Value;
		set => _tradingDirection.Value = value;
	}

	/// <summary>
	/// When <c>true</c> only a single position can be open at any time.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Inverts the relation between price and moving average when enabled.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Determines whether opposite exposure is closed before opening a new trade.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MaTrend2Strategy"/>.
	/// </summary>
	public MaTrend2Strategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			.SetGreaterOrEqual(0);

		_takeProfitPips = Param(nameof(TakeProfitPips), 140)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			.SetGreaterOrEqual(0);

		_trailingStopPips = Param(nameof(TrailingStopPips), 15)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetGreaterOrEqual(0);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimal improvement before adjusting the trailing stop", "Risk")
			.SetGreaterOrEqual(0);

		_lotMode = Param(nameof(LotMode), LotManagementMode.RiskPercent)
			.SetDisplay("Lot Mode", "Fixed lot or percent risk sizing", "Risk");

		_lotOrRiskValue = Param(nameof(LotOrRiskValue), 3m)
			.SetDisplay("Lot / Risk", "Fixed volume or risk percent depending on the lot mode", "Risk")
			.SetGreaterThanZero();

		_maPeriod = Param(nameof(MaPeriod), 12)
			.SetDisplay("MA Period", "Moving average length", "Indicators")
			.SetGreaterThanZero();

		_maShift = Param(nameof(MaShift), 3)
			.SetDisplay("MA Shift", "Bars between the current candle and the MA sample", "Indicators")
			.SetGreaterOrEqual(0);

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.LinearWeighted)
			.SetDisplay("MA Method", "Moving average smoothing method", "Indicators");

		_maPrice = Param(nameof(MaPrice), CandlePrice.Weighted)
			.SetDisplay("MA Price", "Candle price used by the moving average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");

		_tradingDirection = Param(nameof(Direction), TradingDirection.Both)
			.SetDisplay("Direction", "Allowed trade direction", "Execution");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), false)
			.SetDisplay("Only One Position", "Allow only a single open position", "Execution");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert buy/sell conditions", "Execution");

		_closeOpposite = Param(nameof(CloseOppositePositions), false)
			.SetDisplay("Close Opposite", "Close the opposite exposure before opening", "Execution");
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

		_maHistory.Clear();
		_longTrailingStop = 0m;
		_shortTrailingStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePipSize();

		var movingAverage = CreateMovingAverage(MaMethod, MaPeriod, MaPrice);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(movingAverage, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, movingAverage);
			DrawOwnTrades(area);
		}

		var stopLoss = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null;
		var takeProfit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null;

		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(stopLoss: stopLoss, takeProfit: takeProfit, useMarketOrders: true);
		}
		else
		{
			StartProtection();
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFinal)
			return;

		var ma = maValue.ToDecimal();
		_maHistory.Add(ma);

		var maxHistory = MaShift + 10;
		if (_maHistory.Count > maxHistory)
			_maHistory.RemoveRange(0, _maHistory.Count - maxHistory);

		UpdateTrailing(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var requiredSamples = MaShift + 2;
		if (_maHistory.Count < requiredSamples)
			return;

		var referenceIndex = _maHistory.Count - requiredSamples;
		if (referenceIndex < 0)
			return;

		var reference = _maHistory[referenceIndex];
		var closePrice = candle.ClosePrice;

		var allowLong = Direction is TradingDirection.Both or TradingDirection.BuyOnly;
		var allowShort = Direction is TradingDirection.Both or TradingDirection.SellOnly;

		var buySignal = !ReverseSignals ? closePrice > reference : closePrice < reference;
		var sellSignal = !ReverseSignals ? closePrice < reference : closePrice > reference;

		if (buySignal && allowLong)
			TryEnterLong(closePrice);
		else if (sellSignal && allowShort)
			TryEnterShort(closePrice);
	}

	private void TryEnterLong(decimal entryPrice)
	{
		if (OnlyOnePosition && Position != 0m)
			return;

		if (!CloseOppositePositions && Position < 0m)
			return;

		var volume = CalculateOrderVolume(entryPrice, true);
		if (volume <= 0m)
		{
			AddWarningLog("Skip long entry because calculated volume is non-positive. Volume={0:0.####}", volume);
			return;
		}

		var orderVolume = volume;

		if (CloseOppositePositions && Position < 0m)
		{
			orderVolume += Math.Abs(Position);
			_shortTrailingStop = 0m;
		}

		var order = BuyMarket(orderVolume);
		if (order != null)
		{
			AddInfoLog("Enter long at {0:0.#####} with volume {1:0.####}", entryPrice, orderVolume);
		}
	}

	private void TryEnterShort(decimal entryPrice)
	{
		if (OnlyOnePosition && Position != 0m)
			return;

		if (!CloseOppositePositions && Position > 0m)
			return;

		var volume = CalculateOrderVolume(entryPrice, false);
		if (volume <= 0m)
		{
			AddWarningLog("Skip short entry because calculated volume is non-positive. Volume={0:0.####}", volume);
			return;
		}

		var orderVolume = volume;

		if (CloseOppositePositions && Position > 0m)
		{
			orderVolume += Math.Abs(Position);
			_longTrailingStop = 0m;
		}

		var order = SellMarket(orderVolume);
		if (order != null)
		{
			AddInfoLog("Enter short at {0:0.#####} with volume {1:0.####}", entryPrice, orderVolume);
		}
	}

	private decimal CalculateOrderVolume(decimal entryPrice, bool isLong)
	{
		var mode = LotMode;
		if (mode == LotManagementMode.FixedVolume)
			return NormalizeVolume(LotOrRiskValue);

		var stopDistance = StopLossPips * _pipSize;
		if (stopDistance <= 0m)
			return NormalizeVolume(LotOrRiskValue);

		var riskAmount = GetPortfolioValue() * LotOrRiskValue / 100m;
		if (riskAmount <= 0m)
			return NormalizeVolume(LotOrRiskValue);

		var stopPrice = isLong ? entryPrice - stopDistance : entryPrice + stopDistance;
		return CalculateFixedMarginVolume(entryPrice, stopPrice, riskAmount);
	}

	private decimal GetPortfolioValue()
	{
		var current = Portfolio?.CurrentValue ?? 0m;
		if (current > 0m)
			return current;

		var begin = Portfolio?.BeginValue ?? 0m;
		return begin > 0m ? begin : current;
	}

	private decimal CalculateFixedMarginVolume(decimal entryPrice, decimal stopPrice, decimal riskAmount)
	{
		if (riskAmount <= 0m || entryPrice <= 0m || stopPrice <= 0m)
			return NormalizeVolume(LotOrRiskValue);

		var stopDistance = Math.Abs(entryPrice - stopPrice);
		if (stopDistance <= 0m)
			return NormalizeVolume(LotOrRiskValue);

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var stepPrice = Security?.StepPrice ?? priceStep;
		if (stepPrice <= 0m)
			stepPrice = priceStep;

		var stepsCount = stopDistance / priceStep;
		if (stepsCount <= 0m)
			return NormalizeVolume(LotOrRiskValue);

		var riskPerVolume = stepsCount * stepPrice;
		if (riskPerVolume <= 0m)
			return NormalizeVolume(LotOrRiskValue);

		var rawVolume = riskAmount / riskPerVolume;
		return NormalizeVolume(rawVolume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			volume = Math.Ceiling(volume / step) * step;
		}

		return volume;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			_longTrailingStop = 0m;
			_shortTrailingStop = 0m;
			return;
		}

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (trailingDistance <= 0m)
			return;

		var positionPrice = PositionPrice ?? candle.ClosePrice;

		if (Position > 0m)
		{
			var profit = candle.ClosePrice - positionPrice;
			if (profit >= trailingDistance + trailingStep)
			{
				var candidate = candle.ClosePrice - trailingDistance;
				if (_longTrailingStop <= 0m || candidate - _longTrailingStop >= trailingStep)
					_longTrailingStop = candidate;
			}

			if (_longTrailingStop > 0m && candle.LowPrice <= _longTrailingStop)
			{
				SellMarket(Math.Abs(Position));
				_longTrailingStop = 0m;
			}
		}
		else if (Position < 0m)
		{
			var profit = positionPrice - candle.ClosePrice;
			if (profit >= trailingDistance + trailingStep)
			{
				var candidate = candle.ClosePrice + trailingDistance;
				if (_shortTrailingStop <= 0m || _shortTrailingStop - candidate >= trailingStep)
					_shortTrailingStop = candidate;
			}

			if (_shortTrailingStop > 0m && candle.HighPrice >= _shortTrailingStop)
			{
				BuyMarket(Math.Abs(Position));
				_shortTrailingStop = 0m;
			}
		}
	}

	private void UpdatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var decimals = Security?.Decimals ?? GetDecimalDigits(step);
		var adjust = decimals is 3 or 5 ? 10m : 1m;

		_pipSize = step * adjust;
		if (_pipSize <= 0m)
			_pipSize = step;
	}

	private static int GetDecimalDigits(decimal value)
	{
		value = Math.Abs(value);
		var digits = 0;

		while (value != Math.Truncate(value) && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		return digits;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length, CandlePrice price)
	{
		LengthIndicator<decimal> indicator = method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage(),
			MovingAverageMethod.Exponential => new ExponentialMovingAverage(),
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage(),
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage(),
			_ => new SimpleMovingAverage(),
		};

		indicator.Length = length;

		switch (indicator)
		{
			case SimpleMovingAverage sma:
				sma.CandlePrice = price;
				break;
			case ExponentialMovingAverage ema:
				ema.CandlePrice = price;
				break;
			case SmoothedMovingAverage smoothed:
				smoothed.CandlePrice = price;
				break;
			case WeightedMovingAverage wma:
				wma.CandlePrice = price;
				break;
		}

		return indicator;
	}

	/// <summary>
	/// Defines the lot sizing modes supported by the strategy.
	/// </summary>
	public enum LotManagementMode
	{
		/// <summary>
		/// Use the parameter value as a direct volume.
		/// </summary>
		FixedVolume,

		/// <summary>
		/// Interpret the parameter as risk percent of portfolio equity.
		/// </summary>
		RiskPercent
	}

	/// <summary>
	/// Trade direction filters.
	/// </summary>
	public enum TradingDirection
	{
		/// <summary>
		/// Long trades only.
		/// </summary>
		BuyOnly,

		/// <summary>
		/// Short trades only.
		/// </summary>
		SellOnly,

		/// <summary>
		/// Allow both directions.
		/// </summary>
		Both
	}

	/// <summary>
	/// Supported moving average smoothing methods.
	/// </summary>
	public enum MovingAverageMethod
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		LinearWeighted
	}
}

