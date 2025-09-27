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
/// Bill Williams Alligator breakout strategy converted from the "BARS Alligator" MQL expert.
/// Opens a position when the Alligator lips cross the jaw and manages exits via lips versus teeth crosses.
/// Supports pip-based stop-loss, take-profit and trailing stop distances with optional risk-based position sizing.
/// </summary>
public class BarsAlligatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<MoneyManagementModes> _moneyMode;
	private readonly StrategyParam<decimal> _moneyValue;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<MovingAverageTypes> _maType;
	private readonly StrategyParam<AppliedPriceTypes> _appliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _jaw = null!;
	private LengthIndicator<decimal> _teeth = null!;
	private LengthIndicator<decimal> _lips = null!;

	private decimal?[] _jawHistory = Array.Empty<decimal?>();
	private decimal?[] _teethHistory = Array.Empty<decimal?>();
	private decimal?[] _lipsHistory = Array.Empty<decimal?>();

	private decimal? _entryPrice;
	private decimal? _stopLoss;
	private decimal? _takeProfit;

	private decimal _pipSize;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;

	/// <summary>
	/// Trade volume expressed in lots or contracts when using fixed sizing.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Zero disables the protective stop.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Zero disables the profit target.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Zero disables trailing behaviour.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional price distance in pips the market must move before the trailing stop advances.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Money management selection between fixed volume and risk-based sizing.
	/// </summary>
	public MoneyManagementModes MoneyMode
	{
		get => _moneyMode.Value;
		set => _moneyMode.Value = value;
	}

	/// <summary>
	/// Risk percentage applied when <see cref="MoneyMode"/> is <see cref="MoneyManagementModes.RiskPercent"/>.
	/// Ignored when fixed volume sizing is active.
	/// </summary>
	public decimal MoneyValue
	{
		get => _moneyValue.Value;
		set => _moneyValue.Value = value;
	}

	/// <summary>
	/// Maximum number of additive entries per direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Length of the Alligator jaw moving average.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw series.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Length of the Alligator teeth moving average.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the teeth series.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Length of the Alligator lips moving average.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the lips series.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Moving average type shared by all Alligator lines.
	/// </summary>
	public MovingAverageTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Candle price used as input for the moving averages.
	/// </summary>
	public AppliedPriceTypes AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Candle type consumed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize default parameters.
	/// </summary>
	public BarsAlligatorStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Fixed trade volume when MoneyMode is FixedVolume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 150)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop-loss distance expressed in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take-profit distance expressed in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Step", "Extra distance before the trailing stop moves", "Risk");

		_moneyMode = Param(nameof(MoneyMode), MoneyManagementModes.FixedVolume)
		.SetDisplay("Money Mode", "Choose between fixed volume or risk percentage sizing", "Risk");

		_moneyValue = Param(nameof(MoneyValue), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Money Value", "Risk percentage applied when MoneyMode=RiskPercent", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum additive entries per direction", "Trading");

		_jawPeriod = Param(nameof(JawPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Jaw Period", "Alligator jaw moving average length", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
		.SetNotNegative()
		.SetDisplay("Jaw Shift", "Forward shift of the jaw line", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Teeth Period", "Alligator teeth moving average length", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
		.SetNotNegative()
		.SetDisplay("Teeth Shift", "Forward shift of the teeth line", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Lips Period", "Alligator lips moving average length", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
		.SetNotNegative()
		.SetDisplay("Lips Shift", "Forward shift of the lips line", "Alligator");

		_maType = Param(nameof(MaType), MovingAverageTypes.Smoothed)
		.SetDisplay("MA Type", "Moving average calculation for the Alligator lines", "Alligator");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceTypes.Median)
		.SetDisplay("Applied Price", "Price component supplied to the averages", "Alligator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for analysis", "General");
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

		_jawHistory = Array.Empty<decimal?>();
		_teethHistory = Array.Empty<decimal?>();
		_lipsHistory = Array.Empty<decimal?>();

		_entryPrice = null;
		_stopLoss = null;
		_takeProfit = null;

		_pipSize = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_trailingStopDistance = 0m;
		_trailingStepDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
		{
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");
		}

		_jaw = CreateMovingAverage(MaType, JawPeriod);
		_teeth = CreateMovingAverage(MaType, TeethPeriod);
		_lips = CreateMovingAverage(MaType, LipsPeriod);

		_jawHistory = CreateHistoryBuffer(JawShift);
		_teethHistory = CreateHistoryBuffer(TeethShift);
		_lipsHistory = CreateHistoryBuffer(LipsShift);

		UpdatePipParameters();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (ApplyStops(candle))
		return;

		ApplyTrailing(candle);

		var price = GetPrice(candle, AppliedPrice);
		var jawValue = _jaw.Process(new DecimalIndicatorValue(_jaw, price, candle.OpenTime));
		var teethValue = _teeth.Process(new DecimalIndicatorValue(_teeth, price, candle.OpenTime));
		var lipsValue = _lips.Process(new DecimalIndicatorValue(_lips, price, candle.OpenTime));

		if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal)
		return;

		var jaw = jawValue.ToDecimal();
		var teeth = teethValue.ToDecimal();
		var lips = lipsValue.ToDecimal();

		UpdateHistory(_jawHistory, jaw);
		UpdateHistory(_teethHistory, teeth);
		UpdateHistory(_lipsHistory, lips);

		var hasJawHistory = TryGetShiftedValue(_jawHistory, JawShift + 1, out var jawPrev)
		&& TryGetShiftedValue(_jawHistory, JawShift + 2, out var jawPrevPrev);
		var hasTeethHistory = TryGetShiftedValue(_teethHistory, TeethShift + 1, out var teethPrev)
		&& TryGetShiftedValue(_teethHistory, TeethShift + 2, out var teethPrevPrev);
		var hasLipsHistory = TryGetShiftedValue(_lipsHistory, LipsShift + 1, out var lipsPrev)
		&& TryGetShiftedValue(_lipsHistory, LipsShift + 2, out var lipsPrevPrev);

		if (!hasJawHistory || !hasTeethHistory || !hasLipsHistory)
		return;

		var buySignal = lipsPrev >= jawPrev && lipsPrevPrev < jawPrevPrev;
		var sellSignal = lipsPrev <= jawPrev && lipsPrevPrev > jawPrevPrev;

		var closeLongSignal = lipsPrev <= teethPrev && lipsPrevPrev > teethPrevPrev;
		var closeShortSignal = lipsPrev >= teethPrev && lipsPrevPrev < teethPrevPrev;

		if (Position > 0 && closeLongSignal && _entryPrice.HasValue && candle.ClosePrice >= _entryPrice.Value)
		{
			SellMarket(Position);
			ResetTradeLevels();
			return;
		}

		if (Position < 0 && closeShortSignal && _entryPrice.HasValue && candle.ClosePrice <= _entryPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetTradeLevels();
			return;
		}

		if (buySignal)
		TryEnterLong(candle);
		else if (sellSignal)
		TryEnterShort(candle);
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (MaxPositions <= 0)
		return;

		if (Position < 0)
		return;

		var baseVolume = CalculateEntryVolume();
		if (baseVolume <= 0m)
		return;

		var existingVolume = Position > 0 ? Position : 0m;
		var maxVolume = baseVolume * MaxPositions;
		var remainingVolume = maxVolume - existingVolume;
		if (remainingVolume <= 0m)
		return;

		var tradeVolume = Math.Min(baseVolume, remainingVolume);
		if (tradeVolume <= 0m)
		return;

		var stopPrice = StopLossPips > 0 ? candle.ClosePrice - _stopLossDistance : (decimal?)null;
		if (stopPrice.HasValue && stopPrice.Value >= candle.ClosePrice)
		return;

		var takePrice = TakeProfitPips > 0 ? candle.ClosePrice + _takeProfitDistance : (decimal?)null;

		BuyMarket(tradeVolume);

		var newVolume = existingVolume + tradeVolume;
		_entryPrice = existingVolume > 0m && _entryPrice.HasValue
		? ((_entryPrice.Value * existingVolume) + (candle.ClosePrice * tradeVolume)) / newVolume
		: candle.ClosePrice;
		_stopLoss = stopPrice;
		_takeProfit = takePrice;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (MaxPositions <= 0)
		return;

		if (Position > 0)
		return;

		var baseVolume = CalculateEntryVolume();
		if (baseVolume <= 0m)
		return;

		var existingVolume = Position < 0 ? Math.Abs(Position) : 0m;
		var maxVolume = baseVolume * MaxPositions;
		var remainingVolume = maxVolume - existingVolume;
		if (remainingVolume <= 0m)
		return;

		var tradeVolume = Math.Min(baseVolume, remainingVolume);
		if (tradeVolume <= 0m)
		return;

		var stopPrice = StopLossPips > 0 ? candle.ClosePrice + _stopLossDistance : (decimal?)null;
		if (stopPrice.HasValue && stopPrice.Value <= candle.ClosePrice)
		return;

		var takePrice = TakeProfitPips > 0 ? candle.ClosePrice - _takeProfitDistance : (decimal?)null;

		SellMarket(tradeVolume);

		var newVolume = existingVolume + tradeVolume;
		_entryPrice = existingVolume > 0m && _entryPrice.HasValue
		? ((_entryPrice.Value * existingVolume) + (candle.ClosePrice * tradeVolume)) / newVolume
		: candle.ClosePrice;
		_stopLoss = stopPrice;
		_takeProfit = takePrice;
	}

	private bool ApplyStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopLoss.HasValue && candle.LowPrice <= _stopLoss.Value)
			{
				SellMarket(Position);
				ResetTradeLevels();
				return true;
			}

			if (_takeProfit.HasValue && candle.HighPrice >= _takeProfit.Value)
			{
				SellMarket(Position);
				ResetTradeLevels();
				return true;
			}
		}
		else if (Position < 0)
		{
			var absPos = Math.Abs(Position);
			if (_stopLoss.HasValue && candle.HighPrice >= _stopLoss.Value)
			{
				BuyMarket(absPos);
				ResetTradeLevels();
				return true;
			}

			if (_takeProfit.HasValue && candle.LowPrice <= _takeProfit.Value)
			{
				BuyMarket(absPos);
				ResetTradeLevels();
				return true;
			}
		}

		return false;
	}

	private void ApplyTrailing(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m || _trailingStepDistance <= 0m || !_entryPrice.HasValue)
		return;

		if (Position > 0)
		{
			var gain = candle.ClosePrice - _entryPrice.Value;
			if (gain > _trailingStopDistance + _trailingStepDistance)
			{
				var newStop = candle.ClosePrice - _trailingStopDistance;
				var minAllowed = candle.ClosePrice - (_trailingStopDistance + _trailingStepDistance);
				if (!_stopLoss.HasValue || _stopLoss.Value < minAllowed)
				_stopLoss = newStop;
			}
		}
		else if (Position < 0)
		{
			var gain = _entryPrice.Value - candle.ClosePrice;
			if (gain > _trailingStopDistance + _trailingStepDistance)
			{
				var newStop = candle.ClosePrice + _trailingStopDistance;
				var maxAllowed = candle.ClosePrice + (_trailingStopDistance + _trailingStepDistance);
				if (!_stopLoss.HasValue || _stopLoss.Value > maxAllowed)
				_stopLoss = newStop;
			}
		}
	}

	private void UpdatePipParameters()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		var decimals = Security?.Decimals ?? 0;

		_pipSize = step;
		if (decimals == 3 || decimals == 5)
		_pipSize = step * 10m;

		_stopLossDistance = StopLossPips * _pipSize;
		_takeProfitDistance = TakeProfitPips * _pipSize;
		_trailingStopDistance = TrailingStopPips * _pipSize;
		_trailingStepDistance = TrailingStepPips * _pipSize;
	}

	private decimal CalculateEntryVolume()
	{
		if (MoneyMode == MoneyManagementModes.RiskPercent)
		{
			if (_stopLossDistance <= 0m)
			return OrderVolume;

			var portfolioValue = Portfolio?.CurrentValue ?? 0m;
			if (portfolioValue <= 0m)
			return OrderVolume;

			var riskCapital = portfolioValue * MoneyValue / 100m;
			if (riskCapital <= 0m)
			return OrderVolume;

			var rawVolume = riskCapital / _stopLossDistance;
			var volumeStep = Security?.VolumeStep ?? 1m;
			if (volumeStep <= 0m)
			volumeStep = 1m;

			var roundedVolume = Math.Floor(rawVolume / volumeStep) * volumeStep;
			if (roundedVolume <= 0m)
			roundedVolume = volumeStep;

			return roundedVolume;
		}

		return OrderVolume;
	}

	private void ResetTradeLevels()
	{
		_entryPrice = null;
		_stopLoss = null;
		_takeProfit = null;
	}

	private static decimal?[] CreateHistoryBuffer(int shift)
	{
		var size = Math.Max(shift + 4, 4);
		return new decimal?[size];
	}

	private static void UpdateHistory(decimal?[] buffer, decimal value)
	{
		if (buffer.Length == 0)
		return;

		Array.Copy(buffer, 1, buffer, 0, buffer.Length - 1);
		buffer[^1] = value;
	}

	private static bool TryGetShiftedValue(decimal?[] buffer, int offsetFromEnd, out decimal value)
	{
		value = 0m;

		if (buffer.Length < offsetFromEnd)
		return false;

		var index = buffer.Length - offsetFromEnd;
		if (index < 0)
		return false;

		if (buffer[index] is not decimal stored)
		return false;

		value = stored;
		return true;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageTypes type, int length)
	{
		return type switch
		{
			MovingAverageTypes.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypes.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypes.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypes.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SmoothedMovingAverage { Length = length }
		};
	}

	private static decimal GetPrice(ICandleMessage candle, AppliedPriceTypes priceType)
	{
		return priceType switch
		{
			AppliedPriceTypes.Open => candle.OpenPrice,
			AppliedPriceTypes.High => candle.HighPrice,
			AppliedPriceTypes.Low => candle.LowPrice,
			AppliedPriceTypes.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceTypes.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceTypes.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	public enum MoneyManagementModes
	{
		/// <summary>
		/// Use a fixed volume defined by <see cref="BarsAlligatorStrategy.OrderVolume"/>.
		/// </summary>
		FixedVolume,

		/// <summary>
		/// Allocate volume so that the stop-loss represents the configured risk percentage of equity.
		/// </summary>
		RiskPercent
	}

	/// <summary>
	/// Moving average types supported by the Alligator implementation.
	/// </summary>
	public enum MovingAverageTypes
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
		Weighted
	}

	/// <summary>
	/// Price sources that can feed the Alligator averages.
	/// </summary>
	public enum AppliedPriceTypes
	{
		/// <summary>
		/// Candle close price.
		/// </summary>
		Close,

		/// <summary>
		/// Candle open price.
		/// </summary>
		Open,

		/// <summary>
		/// Candle high price.
		/// </summary>
		High,

		/// <summary>
		/// Candle low price.
		/// </summary>
		Low,

		/// <summary>
		/// Median price calculated as (high + low) / 2.
		/// </summary>
		Median,

		/// <summary>
		/// Typical price calculated as (high + low + close) / 3.
		/// </summary>
		Typical,

		/// <summary>
		/// Weighted price calculated as (high + low + 2 * close) / 4.
		/// </summary>
		Weighted
	}
}
