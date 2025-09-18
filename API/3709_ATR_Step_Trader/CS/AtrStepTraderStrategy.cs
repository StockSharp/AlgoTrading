using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-step ATR trend strategy converted from the "atrTrader" MQL5 expert advisor.
/// Filters trends with a dual moving-average stack, opens breakouts, and pyramids positions using ATR distances.
/// </summary>
public class AtrStepTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _pyramidLimit;
	private readonly StrategyParam<decimal> _stepMultiplier;
	private readonly StrategyParam<decimal> _stepsMultiplier;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private int _bullishStreak;
	private int _bearishStreak;
	private decimal? _previousSlow;
	private decimal? _longEntryHigh;
	private decimal? _longEntryLow;
	private decimal? _shortEntryHigh;
	private decimal? _shortEntryLow;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Number of consecutive bars that must confirm the trend direction.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Maximum number of stacked entries per direction.
	/// </summary>
	public int PyramidLimit
	{
		get => _pyramidLimit.Value;
		set => _pyramidLimit.Value = value;
	}

	/// <summary>
	/// ATR multiple used for breakout gating.
	/// </summary>
	public decimal StepMultiplier
	{
		get => _stepMultiplier.Value;
		set => _stepMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiple used for pyramiding distance checks.
	/// </summary>
	public decimal StepsMultiplier
	{
		get => _stepsMultiplier.Value;
		set => _stepsMultiplier.Value = value;
	}

	/// <summary>
	/// Additional multiplier that widens the protective stop distance.
	/// </summary>
	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	/// <summary>
	/// Base order volume for market entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="AtrStepTraderStrategy"/>.
	/// </summary>
	public AtrStepTraderStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 70)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
			.SetCanOptimize(true)
			.SetOptimize(50, 100, 10);

		_slowPeriod = Param(nameof(SlowPeriod), 180)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Length of the slow moving average", "Trend Filter")
			.SetCanOptimize(true)
			.SetOptimize(120, 240, 20);

		_atrPeriod = Param(nameof(AtrPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR window used for distance calculations", "Volatility")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 10);

		_momentumPeriod = Param(nameof(MomentumPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Bars", "Number of consecutive bars required for trend confirmation", "Trend Filter")
			.SetCanOptimize(true)
			.SetOptimize(30, 80, 5);

		_pyramidLimit = Param(nameof(PyramidLimit), 3)
			.SetGreaterThanZero()
			.SetDisplay("Pyramid Limit", "Maximum number of entries per direction", "Position Sizing")
			.SetCanOptimize(true)
			.SetOptimize(2, 4, 1);

		_stepMultiplier = Param(nameof(StepMultiplier), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Step Multiplier", "ATR multiple for breakout validation", "Entry Logic")
			.SetCanOptimize(true)
			.SetOptimize(2m, 6m, 1m);

		_stepsMultiplier = Param(nameof(StepsMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Steps Multiplier", "ATR multiple for add-on spacing", "Entry Logic")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_stopMultiplier = Param(nameof(StopMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Multiplier", "Extra multiplier applied on top of the step distance", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(2m, 4m, 0.5m);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base order size for market entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for processing", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bullishStreak = 0;
		_bearishStreak = 0;
		_previousSlow = null;
		_longEntryHigh = null;
		_longEntryLow = null;
		_shortEntryHigh = null;
		_shortEntryLow = null;
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		var fastMa = new SMA { Length = FastPeriod };
		var slowMa = new SMA { Length = SlowPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var highest = new Highest { Length = MomentumPeriod, CandlePrice = CandlePrice.High };
		var lowest = new Lowest { Length = MomentumPeriod, CandlePrice = CandlePrice.Low };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, atr, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, atr);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal atrValue, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (atrValue <= 0m)
			return;

		UpdateMomentumCounters(fastValue, slowValue);

		var price = candle.ClosePrice;
		var previousSlow = _previousSlow;
		_previousSlow = slowValue;

		var volume = Volume;
		if (volume <= 0m)
			volume = 1m;

		var netPosition = Position;
		var longCount = netPosition > 0m ? (int)Math.Round(netPosition / volume, MidpointRounding.AwayFromZero) : 0;
		var shortCount = netPosition < 0m ? (int)Math.Round(-netPosition / volume, MidpointRounding.AwayFromZero) : 0;

		if (longCount == 0 && shortCount == 0)
		{
			if (previousSlow.HasValue && slowValue > 0m)
			{
				var bullishReady = _bullishStreak >= MomentumPeriod && price > previousSlow.Value && price <= highest - StepMultiplier * atrValue;
				if (bullishReady)
				{
					BuyMarket(Volume);
					longCount = 1;
					_longEntryHigh = price;
					_longEntryLow = price;
					_longStopPrice = price - StepMultiplier * StopMultiplier * atrValue;
				}
			}

			if (longCount == 0 && previousSlow.HasValue && slowValue > 0m)
			{
				var bearishReady = _bearishStreak >= MomentumPeriod && price < previousSlow.Value && price >= lowest + StepMultiplier * atrValue;
				if (bearishReady)
				{
					SellMarket(Volume);
					shortCount = 1;
					_shortEntryHigh = price;
					_shortEntryLow = price;
					_shortStopPrice = price + StepMultiplier * StopMultiplier * atrValue;
				}
			}
		}
		else if (longCount > 0 && shortCount == 0)
		{
			ManageLongPosition(ref longCount, price, atrValue);
		}
		else if (shortCount > 0 && longCount == 0)
		{
			ManageShortPosition(ref shortCount, price, atrValue);
		}
	}

	private void UpdateMomentumCounters(decimal fastValue, decimal slowValue)
	{
		if (fastValue > slowValue)
		{
			_bullishStreak++;
			_bearishStreak = 0;
		}
		else if (fastValue < slowValue)
		{
			_bearishStreak++;
			_bullishStreak = 0;
		}
		else
		{
			_bullishStreak++;
			_bearishStreak++;
		}
	}

	private void ManageLongPosition(ref int longCount, decimal price, decimal atrValue)
	{
		if (_longEntryHigh is not decimal high || _longEntryLow is not decimal low)
			return;

		var stepsDistance = StepsMultiplier * atrValue;
		var stepDistance = StepMultiplier * atrValue;

		if (_longStopPrice.HasValue && price <= _longStopPrice.Value)
		{
			SellMarket(Position);
			longCount = 0;
			ResetLongState();
			return;
		}

		if (longCount < PyramidLimit)
		{
			if (price >= high + stepsDistance || price <= low - stepsDistance)
			{
				BuyMarket(Volume);
				longCount++;
				_longEntryHigh = Math.Max(high, price);
				_longEntryLow = Math.Min(low, price);
				UpdateLongStopAfterEntry(price, atrValue);
				return;
			}
		}

		if (price <= low - stepsDistance)
		{
			SellMarket(Position);
			longCount = 0;
			ResetLongState();
			return;
		}

		if (longCount >= PyramidLimit)
		{
			var tightened = price - stepDistance;
			if (!_longStopPrice.HasValue || tightened > _longStopPrice.Value)
				_longStopPrice = tightened;
		}
	}

	private void ManageShortPosition(ref int shortCount, decimal price, decimal atrValue)
	{
		if (_shortEntryHigh is not decimal high || _shortEntryLow is not decimal low)
			return;

		var stepsDistance = StepsMultiplier * atrValue;
		var stepDistance = StepMultiplier * atrValue;

		if (_shortStopPrice.HasValue && price >= _shortStopPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			shortCount = 0;
			ResetShortState();
			return;
		}

		if (shortCount < PyramidLimit)
		{
			if (price <= low - stepsDistance || price >= high + stepsDistance)
			{
				SellMarket(Volume);
				shortCount++;
				_shortEntryHigh = Math.Max(high, price);
				_shortEntryLow = Math.Min(low, price);
				UpdateShortStopAfterEntry(price, atrValue);
				return;
			}
		}

		if (price >= high + stepsDistance)
		{
			BuyMarket(Math.Abs(Position));
			shortCount = 0;
			ResetShortState();
			return;
		}

		if (shortCount >= PyramidLimit)
		{
			var tightened = price + stepDistance;
			if (!_shortStopPrice.HasValue || tightened < _shortStopPrice.Value)
				_shortStopPrice = tightened;
		}
	}

	private void UpdateLongStopAfterEntry(decimal entryPrice, decimal atrValue)
	{
		var stop = entryPrice - StepMultiplier * StopMultiplier * atrValue;
		if (!_longStopPrice.HasValue || stop > _longStopPrice.Value)
			_longStopPrice = stop;
	}

	private void UpdateShortStopAfterEntry(decimal entryPrice, decimal atrValue)
	{
		var stop = entryPrice + StepMultiplier * StopMultiplier * atrValue;
		if (!_shortStopPrice.HasValue || stop < _shortStopPrice.Value)
			_shortStopPrice = stop;
	}

	private void ResetLongState()
	{
		_longEntryHigh = null;
		_longEntryLow = null;
		_longStopPrice = null;
		_bullishStreak = 0;
	}

	private void ResetShortState()
	{
		_shortEntryHigh = null;
		_shortEntryLow = null;
		_shortStopPrice = null;
		_bearishStreak = 0;
	}
}
