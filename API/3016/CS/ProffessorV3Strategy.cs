using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid-based strategy converted from the Proffessor v3 MQL expert.
/// </summary>
public class ProffessorV3Strategy : Strategy
{
	private const int AdxPeriod = 14;

	private readonly StrategyParam<decimal> _volumeParam;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _lotAddition;
	private readonly StrategyParam<int> _maxLevels;
	private readonly StrategyParam<decimal> _gridDeltaIncrement;
	private readonly StrategyParam<decimal> _gridInitialOffset;
	private readonly StrategyParam<decimal> _gridStep;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _lossLimit;
	private readonly StrategyParam<decimal> _adxFlatLevel;
	private readonly StrategyParam<int> _barOffset;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex? _adx;
	private decimal?[]? _adxMainHistory;
	private decimal?[]? _plusHistory;
	private decimal?[]? _minusHistory;
	private decimal _adjustedPoint;
	private decimal _deltaIncrement;
	private decimal _delta1;
	private decimal _delta2;
	private bool _shouldCloseAll;

	/// <summary>
	/// Multiplier applied to the volume of grid orders.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Additional volume added to each grid order.
	/// </summary>
	public decimal LotAddition
	{
		get => _lotAddition.Value;
		set => _lotAddition.Value = value;
	}

	/// <summary>
	/// Maximum number of grid levels per side.
	/// </summary>
	public int MaxLevels
	{
		get => _maxLevels.Value;
		set => _maxLevels.Value = value;
	}

	/// <summary>
	/// Increment that widens the grid spacing for deeper levels.
	/// </summary>
	public decimal GridDeltaIncrement
	{
		get => _gridDeltaIncrement.Value;
		set => _gridDeltaIncrement.Value = value;
	}

	/// <summary>
	/// Distance in points to the first protective order.
	/// </summary>
	public decimal GridInitialOffset
	{
		get => _gridInitialOffset.Value;
		set => _gridInitialOffset.Value = value;
	}

	/// <summary>
	/// Base spacing between subsequent grid levels.
	/// </summary>
	public decimal GridStep
	{
		get => _gridStep.Value;
		set => _gridStep.Value = value;
	}

	/// <summary>
	/// Unrealized profit threshold that triggers full liquidation.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Unrealized loss threshold that triggers full liquidation.
	/// </summary>
	public decimal LossLimit
	{
		get => _lossLimit.Value;
		set => _lossLimit.Value = value;
	}

	/// <summary>
	/// ADX level separating flat and trending regimes.
	/// </summary>
	public decimal AdxFlatLevel
	{
		get => _adxFlatLevel.Value;
		set => _adxFlatLevel.Value = value;
	}

	/// <summary>
	/// Number of completed candles to look back when reading ADX values.
	/// </summary>
	public int BarOffset
	{
		get => _barOffset.Value;
		set => _barOffset.Value = value;
	}

	/// <summary>
	/// Hour (0-23) when trading is allowed to start.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour (0-23) when trading stops accepting new entries.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Candle data type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ProffessorV3Strategy()
	{
		_volumeParam = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Base market order volume", "Trading");

		_lotMultiplier = Param(nameof(LotMultiplier), 1m)
		.SetDisplay("Lot Multiplier", "Multiplier applied to pending order volume", "Trading")
		.SetCanOptimize(true);

		_lotAddition = Param(nameof(LotAddition), 0.01m)
		.SetDisplay("Lot Addition", "Additional volume added to each grid order", "Trading")
		.SetCanOptimize(true);

		_maxLevels = Param(nameof(MaxLevels), 5)
		.SetRange(0, 10)
		.SetDisplay("Max Levels", "Maximum number of pending orders per side", "Grid")
		.SetCanOptimize(true);

		_gridDeltaIncrement = Param(nameof(GridDeltaIncrement), -5m)
		.SetDisplay("Delta Increment", "Increment added to the spacing for deeper levels (points)", "Grid");

		_gridInitialOffset = Param(nameof(GridInitialOffset), 70m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Offset", "Distance to the first protective order (points)", "Grid")
		.SetCanOptimize(true);

		_gridStep = Param(nameof(GridStep), 60m)
		.SetGreaterThanZero()
		.SetDisplay("Grid Step", "Distance between subsequent grid levels (points)", "Grid")
		.SetCanOptimize(true);

		_profitTarget = Param(nameof(ProfitTarget), 15m)
		.SetDisplay("Profit Close", "Unrealized profit target for closing everything", "Risk")
		.SetCanOptimize(true);

		_lossLimit = Param(nameof(LossLimit), -150m)
		.SetDisplay("Loss Close", "Unrealized loss threshold that forces liquidation", "Risk");

		_adxFlatLevel = Param(nameof(AdxFlatLevel), 40m)
		.SetGreaterThanZero()
		.SetDisplay("ADX Flat Level", "ADX threshold distinguishing flat and trend regimes", "Indicators")
		.SetCanOptimize(true);

		_barOffset = Param(nameof(BarOffset), 2)
		.SetRange(0, 10)
		.SetDisplay("Bar Offset", "Number of closed candles to delay ADX signals", "Indicators");

		_startHour = Param(nameof(StartHour), 0)
		.SetRange(0, 23)
		.SetDisplay("Start Hour", "Hour when trading window opens", "Schedule");

		_endHour = Param(nameof(EndHour), 24)
		.SetRange(0, 24)
		.SetDisplay("End Hour", "Hour when trading window closes", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Candle series used for calculations", "General");
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
		_adxMainHistory = null;
		_plusHistory = null;
		_minusHistory = null;
		_shouldCloseAll = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		UpdatePointScaling();
		EnsureHistorySize();

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_adx, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		// Only act on finished candles to mimic the original tick logic safely.
		if (candle.State != CandleStates.Finished)
		return;

		if (_shouldCloseAll)
		{
			if (HandleCloseAll())
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsWithinTradingHours(candle.OpenTime))
		return;

		if (!adxValue.IsFinal)
		return;

		var adxData = (AverageDirectionalIndexValue)adxValue;

		if (adxData.MovingAverage is not decimal adxMain ||
		adxData.Dx.Plus is not decimal plusDi ||
		adxData.Dx.Minus is not decimal minusDi)
		{
			return;
		}

		EnsureHistorySize();
		PushAdxValues(adxMain, plusDi, minusDi);

		if (!TryGetDelayedAdx(out var delayedAdx, out var delayedPlus, out var delayedMinus))
		return;

		if (Position == 0)
		{
			EvaluateEntry(candle, delayedAdx, delayedPlus, delayedMinus);
		}
		else
		{
			EvaluateExit(candle.ClosePrice);
		}
	}

	private void EvaluateEntry(ICandleMessage candle, decimal adxMain, decimal plusDi, decimal minusDi)
	{
		// Normalize base volume according to symbol limits.
		var baseVolume = AdjustVolume(Volume);
		if (baseVolume <= 0m)
		return;

		// Approximate bid/ask using the latest close and price step.
		var (bid, ask) = GetBidAsk(candle.ClosePrice);

		// Pre-calculate offsets used across the grid.
		var protectiveOffset = NormalizePrice(bid - _delta1);
		var protectiveBuyStop = NormalizePrice(ask + _delta1);

		if (adxMain < AdxFlatLevel && plusDi > minusDi)
		{
			// Flat market with bullish bias: open long and stage hedging orders.
			BuyMarket(baseVolume);
			SellStop(baseVolume, protectiveOffset);
			PlaceFlatBullishGrid(baseVolume, bid, ask);
		}
		else if (adxMain < AdxFlatLevel && plusDi < minusDi)
		{
			// Flat market with bearish bias: open short and stage recovery grid.
			SellMarket(baseVolume);
			BuyStop(baseVolume, protectiveBuyStop);
			PlaceFlatBearishGrid(baseVolume, bid, ask);
		}
		else if (adxMain >= AdxFlatLevel && plusDi > minusDi)
		{
			// Strong bullish trend: use stop orders to pyramid along the move.
			BuyMarket(baseVolume);
			SellStop(baseVolume, protectiveOffset);
			PlaceTrendingBullishGrid(baseVolume, bid, ask);
		}
		else if (adxMain >= AdxFlatLevel && plusDi < minusDi)
		{
			// Strong bearish trend: use stop orders to follow momentum.
			SellMarket(baseVolume);
			BuyStop(baseVolume, protectiveBuyStop);
			PlaceTrendingBearishGrid(baseVolume, bid, ask);
		}
	}

	private void EvaluateExit(decimal price)
	{
		var profit = CalculateUnrealizedProfit(price);

		if (profit > ProfitTarget || (LossLimit != 0m && profit < LossLimit))
		{
			_shouldCloseAll = true;
			HandleCloseAll();
		}
	}

	private decimal CalculateUnrealizedProfit(decimal price)
	{
		if (Position == 0)
		return 0m;

		var averagePrice = Position.AveragePrice;
		if (averagePrice is null)
		return 0m;

		var volume = Math.Abs(Position);

		return Position > 0
		? (price - averagePrice.Value) * volume
		: (averagePrice.Value - price) * volume;
	}

	private bool HandleCloseAll()
	{
		var hadPosition = false;

		if (Position > 0)
		{
			SellMarket(Position);
			hadPosition = true;
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			hadPosition = true;
		}

		CancelActiveOrders();

		if (!hadPosition)
		{
			_shouldCloseAll = false;
			return false;
		}

		return true;
	}

	private void PlaceFlatBullishGrid(decimal baseVolume, decimal bid, decimal ask)
	{
		for (var level = 1; level <= MaxLevels; level++)
		{
			var volume = AdjustVolume(baseVolume * LotMultiplier + LotAddition);
			if (volume <= 0m)
			continue;

			var offset = CalculateLevelOffset(level);
			var buyPrice = NormalizePrice(ask - (_delta1 + offset));
			var sellPrice = NormalizePrice(bid + offset);

			// Buy limits average into pullbacks, sell limits harvest rebounds.
			BuyLimit(volume, buyPrice);
			SellLimit(volume, sellPrice);
		}
	}

	private void PlaceFlatBearishGrid(decimal baseVolume, decimal bid, decimal ask)
	{
		for (var level = 1; level <= MaxLevels; level++)
		{
			var volume = AdjustVolume(baseVolume * LotMultiplier + LotAddition);
			if (volume <= 0m)
			continue;

			var offset = CalculateLevelOffset(level);
			var buyPrice = NormalizePrice(ask - offset);
			var sellPrice = NormalizePrice(bid + (_delta1 + offset));

			// Buy limits cover shorts on deep pullbacks, sell limits resell higher.
			BuyLimit(volume, buyPrice);
			SellLimit(volume, sellPrice);
		}
	}

	private void PlaceTrendingBullishGrid(decimal baseVolume, decimal bid, decimal ask)
	{
		for (var level = 1; level <= MaxLevels; level++)
		{
			var volume = AdjustVolume(baseVolume * LotMultiplier + LotAddition);
			if (volume <= 0m)
			continue;

			var offset = CalculateLevelOffset(level);
			var sellStopPrice = NormalizePrice(bid - (_delta1 + offset));
			var buyStopPrice = NormalizePrice(ask + offset);

			// Sell stops hedge the long book, buy stops pyramid into breakout.
			SellStop(volume, sellStopPrice);
			BuyStop(volume, buyStopPrice);
		}
	}

	private void PlaceTrendingBearishGrid(decimal baseVolume, decimal bid, decimal ask)
	{
		for (var level = 1; level <= MaxLevels; level++)
		{
			var volume = AdjustVolume(baseVolume * LotMultiplier + LotAddition);
			if (volume <= 0m)
			continue;

			var offset = CalculateLevelOffset(level);
			var sellStopPrice = NormalizePrice(bid - offset);
			var buyStopPrice = NormalizePrice(ask + (_delta1 + offset));

			// Sell stops follow momentum, buy stops cap losses on reversals.
			SellStop(volume, sellStopPrice);
			BuyStop(volume, buyStopPrice);
		}
	}

	private decimal CalculateLevelOffset(int level)
	{
		var increment = _delta2 + _deltaIncrement * level / 2m;
		return level * increment;
	}

	private void UpdatePointScaling()
	{
		var step = Security?.PriceStep ?? 1m;
		_adjustedPoint = step;

		var decimals = Security?.Decimals;
		if (decimals is 3 or 5)
		_adjustedPoint *= 10m;

		if (_adjustedPoint == 0m)
		_adjustedPoint = 1m;

		_deltaIncrement = GridDeltaIncrement * _adjustedPoint;
		_delta1 = GridInitialOffset * _adjustedPoint;
		_delta2 = GridStep * _adjustedPoint;
	}

	private void EnsureHistorySize()
	{
		var size = Math.Max(BarOffset + 1, 1);

		if (_adxMainHistory?.Length == size)
		return;

		_adxMainHistory = new decimal?[size];
		_plusHistory = new decimal?[size];
		_minusHistory = new decimal?[size];
	}

	private void PushAdxValues(decimal adxMain, decimal plusDi, decimal minusDi)
	{
		if (_adxMainHistory is null || _plusHistory is null || _minusHistory is null)
		return;

		for (var i = _adxMainHistory.Length - 1; i > 0; i--)
		{
			_adxMainHistory[i] = _adxMainHistory[i - 1];
			_plusHistory[i] = _plusHistory[i - 1];
			_minusHistory[i] = _minusHistory[i - 1];
		}

		_adxMainHistory[0] = adxMain;
		_plusHistory[0] = plusDi;
		_minusHistory[0] = minusDi;
	}

	private bool TryGetDelayedAdx(out decimal adxMain, out decimal plusDi, out decimal minusDi)
	{
		adxMain = 0m;
		plusDi = 0m;
		minusDi = 0m;

		if (_adxMainHistory is null || _plusHistory is null || _minusHistory is null)
		return false;

		var index = Math.Min(BarOffset, _adxMainHistory.Length - 1);

		var storedAdx = _adxMainHistory[index];
		var storedPlus = _plusHistory[index];
		var storedMinus = _minusHistory[index];

		if (storedAdx is null || storedPlus is null || storedMinus is null)
		return false;

		adxMain = storedAdx.Value;
		plusDi = storedPlus.Value;
		minusDi = storedMinus.Value;
		return true;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var normalized = volume;

		var step = Security?.VolumeStep;
		if (step is not null && step.Value > 0m)
		{
			var steps = Math.Floor(normalized / step.Value);
			normalized = steps * step.Value;
		}

		var minVolume = Security?.MinVolume;
		if (minVolume is not null && normalized < minVolume.Value)
		return 0m;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume is not null && normalized > maxVolume.Value)
		normalized = maxVolume.Value;

		return normalized;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step is null || step.Value <= 0m)
		return price;

		var steps = Math.Round(price / step.Value, MidpointRounding.AwayFromZero);
		return steps * step.Value;
	}

	private (decimal bid, decimal ask) GetBidAsk(decimal closePrice)
	{
		var step = Security?.PriceStep ?? 0m;
		var halfStep = step / 2m;

		var bid = closePrice - halfStep;
		var ask = closePrice + halfStep;

		if (bid <= 0m)
		bid = closePrice;

		if (ask <= 0m)
		ask = closePrice;

		return (bid, ask);
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.UtcDateTime.Hour;

		if (StartHour < EndHour)
		return hour >= StartHour && hour < EndHour;

		if (StartHour > EndHour)
		return !(hour >= EndHour && hour < StartHour);

		return false;
	}
}
