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
/// Separate trade strategy ported from MetaTrader logic.
/// Combines moving averages, ATR and standard deviation filters with pip-based risk management.
/// </summary>
public class SeparateTradeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<AppliedPrice> _priceType;
	private readonly StrategyParam<decimal> _stopLossBuyPips;
	private readonly StrategyParam<decimal> _stopLossSellPips;
	private readonly StrategyParam<decimal> _takeProfitBuyPips;
	private readonly StrategyParam<decimal> _takeProfitSellPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _deltaBuyPips;
	private readonly StrategyParam<decimal> _deltaSellPips;
	private readonly StrategyParam<int> _atrPeriodBuy;
	private readonly StrategyParam<int> _atrPeriodSell;
	private readonly StrategyParam<decimal> _atrLevelBuy;
	private readonly StrategyParam<decimal> _atrLevelSell;
	private readonly StrategyParam<int> _stdPeriodBuy;
	private readonly StrategyParam<int> _stdPeriodSell;
	private readonly StrategyParam<decimal> _stdLevelBuy;
	private readonly StrategyParam<decimal> _stdLevelSell;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _slowMa = null!;
	private LengthIndicator<decimal> _fastMa = null!;
	private AverageTrueRange _atrBuy = null!;
	private AverageTrueRange _atrSell = null!;
	private StandardDeviation _stdBuy = null!;
	private StandardDeviation _stdSell = null!;

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	private DateTimeOffset _lastLongEntryTime = DateTimeOffset.MinValue;
	private DateTimeOffset _lastShortEntryTime = DateTimeOffset.MinValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="SeparateTradeStrategy"/> class.
	/// </summary>
	public SeparateTradeStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order size expressed in lots", "Orders");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 65)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Period", "Period for the slower moving average", "Indicators");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Period", "Period for the faster moving average", "Indicators");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Exponential)
		.SetDisplay("MA Method", "Smoothing algorithm applied to both averages", "Indicators");

		_priceType = Param(nameof(PriceType), AppliedPrice.Close)
		.SetDisplay("Applied Price", "Price input for moving averages and deviation", "Indicators");

		_stopLossBuyPips = Param(nameof(StopLossBuyPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss Buy (pips)", "Stop-loss distance for long trades", "Risk");

		_stopLossSellPips = Param(nameof(StopLossSellPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss Sell (pips)", "Stop-loss distance for short trades", "Risk");

		_takeProfitBuyPips = Param(nameof(TakeProfitBuyPips), 50m)
		.SetNotNegative()
		.SetDisplay("Take Profit Buy (pips)", "Take-profit distance for long trades", "Risk");

		_takeProfitSellPips = Param(nameof(TakeProfitSellPips), 50m)
		.SetNotNegative()
		.SetDisplay("Take Profit Sell (pips)", "Take-profit distance for short trades", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Distance of the trailing stop", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Minimum advance required before moving the trailing stop", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum simultaneous net positions", "Risk");

		_deltaBuyPips = Param(nameof(DeltaBuyPips), 2m)
		.SetNotNegative()
		.SetDisplay("MA Delta Buy (pips)", "Maximum fast-slow MA distance for long entries", "Filters");

		_deltaSellPips = Param(nameof(DeltaSellPips), 2m)
		.SetNotNegative()
		.SetDisplay("MA Delta Sell (pips)", "Maximum fast-slow MA distance for short entries", "Filters");

		_atrPeriodBuy = Param(nameof(AtrPeriodBuy), 26)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period Buy", "ATR lookback for long filter", "Indicators");

		_atrPeriodSell = Param(nameof(AtrPeriodSell), 26)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period Sell", "ATR lookback for short filter", "Indicators");

		_atrLevelBuy = Param(nameof(AtrLevelBuy), 0.0016m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Level Buy", "Upper ATR threshold required for long entries", "Filters");

		_atrLevelSell = Param(nameof(AtrLevelSell), 0.0016m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Level Sell", "Upper ATR threshold required for short entries", "Filters");

		_stdPeriodBuy = Param(nameof(StdDevPeriodBuy), 54)
		.SetGreaterThanZero()
		.SetDisplay("StdDev Period Buy", "Standard deviation lookback for long filter", "Indicators");

		_stdPeriodSell = Param(nameof(StdDevPeriodSell), 54)
		.SetGreaterThanZero()
		.SetDisplay("StdDev Period Sell", "Standard deviation lookback for short filter", "Indicators");

		_stdLevelBuy = Param(nameof(StdDevLevelBuy), 0.0051m)
		.SetGreaterThanZero()
		.SetDisplay("StdDev Level Buy", "Standard deviation ceiling for long entries", "Filters");

		_stdLevelSell = Param(nameof(StdDevLevelSell), 0.0051m)
		.SetGreaterThanZero()
		.SetDisplay("StdDev Level Sell", "Standard deviation ceiling for short entries", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Data series used for calculations", "General");
	}

	/// <summary>
	/// Trade volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Period of the slower moving average.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the faster moving average.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to both moving averages.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price input used for the moving averages and standard deviation.
	/// </summary>
	public AppliedPrice PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades expressed in pips.
	/// </summary>
	public decimal StopLossBuyPips
	{
		get => _stopLossBuyPips.Value;
		set => _stopLossBuyPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades expressed in pips.
	/// </summary>
	public decimal StopLossSellPips
	{
		get => _stopLossSellPips.Value;
		set => _stopLossSellPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long trades expressed in pips.
	/// </summary>
	public decimal TakeProfitBuyPips
	{
		get => _takeProfitBuyPips.Value;
		set => _takeProfitBuyPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades expressed in pips.
	/// </summary>
	public decimal TakeProfitSellPips
	{
		get => _takeProfitSellPips.Value;
		set => _takeProfitSellPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Required price advance before the trailing stop moves.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum allowed simultaneous net positions.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Maximum moving average distance for long entries expressed in pips.
	/// </summary>
	public decimal DeltaBuyPips
	{
		get => _deltaBuyPips.Value;
		set => _deltaBuyPips.Value = value;
	}

	/// <summary>
	/// Maximum moving average distance for short entries expressed in pips.
	/// </summary>
	public decimal DeltaSellPips
	{
		get => _deltaSellPips.Value;
		set => _deltaSellPips.Value = value;
	}

	/// <summary>
	/// ATR lookback period used for long entries.
	/// </summary>
	public int AtrPeriodBuy
	{
		get => _atrPeriodBuy.Value;
		set => _atrPeriodBuy.Value = value;
	}

	/// <summary>
	/// ATR lookback period used for short entries.
	/// </summary>
	public int AtrPeriodSell
	{
		get => _atrPeriodSell.Value;
		set => _atrPeriodSell.Value = value;
	}

	/// <summary>
	/// ATR threshold that must not be exceeded for long entries.
	/// </summary>
	public decimal AtrLevelBuy
	{
		get => _atrLevelBuy.Value;
		set => _atrLevelBuy.Value = value;
	}

	/// <summary>
	/// ATR threshold that must not be exceeded for short entries.
	/// </summary>
	public decimal AtrLevelSell
	{
		get => _atrLevelSell.Value;
		set => _atrLevelSell.Value = value;
	}

	/// <summary>
	/// Standard deviation lookback for long entries.
	/// </summary>
	public int StdDevPeriodBuy
	{
		get => _stdPeriodBuy.Value;
		set => _stdPeriodBuy.Value = value;
	}

	/// <summary>
	/// Standard deviation lookback for short entries.
	/// </summary>
	public int StdDevPeriodSell
	{
		get => _stdPeriodSell.Value;
		set => _stdPeriodSell.Value = value;
	}

	/// <summary>
	/// Standard deviation ceiling for long entries.
	/// </summary>
	public decimal StdDevLevelBuy
	{
		get => _stdLevelBuy.Value;
		set => _stdLevelBuy.Value = value;
	}

	/// <summary>
	/// Standard deviation ceiling for short entries.
	/// </summary>
	public decimal StdDevLevelSell
	{
		get => _stdLevelSell.Value;
		set => _stdLevelSell.Value = value;
	}

	/// <summary>
	/// Candle data type used by the strategy.
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

		_entryPrice = 0m;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
		_pipSize = 0m;
		_lastLongEntryTime = DateTimeOffset.MinValue;
		_lastShortEntryTime = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		{
			LogError("Trailing stop requires a positive trailing step.");
			Stop();
			return;
		}

		Volume = TradeVolume;
		_pipSize = CalculatePipSize();

		_slowMa = CreateMovingAverage(MaMethod, SlowMaPeriod);
		_fastMa = CreateMovingAverage(MaMethod, FastMaPeriod);
		_atrBuy = new AverageTrueRange { Length = AtrPeriodBuy };
		_atrSell = new AverageTrueRange { Length = AtrPeriodSell };
		_stdBuy = new StandardDeviation { Length = StdDevPeriodBuy };
		_stdSell = new StandardDeviation { Length = StdDevPeriodSell };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = GetAppliedPrice(candle);

		var slowValue = _slowMa.Process(price);
		var fastValue = _fastMa.Process(price);
		var atrBuyValue = _atrBuy.Process(candle);
		var atrSellValue = _atrSell.Process(candle);
		var stdBuyValue = _stdBuy.Process(price);
		var stdSellValue = _stdSell.Process(price);

		if (!_slowMa.IsFormed || !_fastMa.IsFormed || !_atrBuy.IsFormed || !_atrSell.IsFormed || !_stdBuy.IsFormed || !_stdSell.IsFormed)
		return;

		var slow = slowValue.ToDecimal();
		var fast = fastValue.ToDecimal();
		var atrBuy = atrBuyValue.ToDecimal();
		var atrSell = atrSellValue.ToDecimal();
		var stdBuy = stdBuyValue.ToDecimal();
		var stdSell = stdSellValue.ToDecimal();

		UpdateTrailing(candle);

		if (Position > 0m)
		{
			if (CheckLongExit(candle))
			return;
		}
		else if (Position < 0m)
		{
			if (CheckShortExit(candle))
			return;
		}

		if (Position != 0m)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (MaxPositions <= 0)
		return;

		var deltaBuy = ToPrice(DeltaBuyPips);
		var deltaSell = ToPrice(DeltaSellPips);
		var deltaBuyOk = deltaBuy <= 0m || fast - slow < deltaBuy;
		var deltaSellOk = deltaSell <= 0m || slow - fast < deltaSell;

		if (candle.OpenTime > _lastLongEntryTime && slow < fast && atrBuy < AtrLevelBuy && deltaBuyOk && stdBuy < StdDevLevelBuy)
		{
			EnterLong(candle);
			return;
		}

		if (candle.OpenTime > _lastShortEntryTime && slow > fast && atrSell < AtrLevelSell && deltaSellOk && stdSell < StdDevLevelSell)
		{
			EnterShort(candle);
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = Volume + Math.Abs(Position);
		if (volume <= 0m)
		return;

		_entryPrice = candle.ClosePrice;
		_longStop = StopLossBuyPips > 0m ? _entryPrice - ToPrice(StopLossBuyPips) : 0m;
		_longTake = TakeProfitBuyPips > 0m ? _entryPrice + ToPrice(TakeProfitBuyPips) : 0m;
		_shortStop = 0m;
		_shortTake = 0m;
		_lastLongEntryTime = candle.OpenTime;

		BuyMarket(volume);
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = Volume + Math.Abs(Position);
		if (volume <= 0m)
		return;

		_entryPrice = candle.ClosePrice;
		_shortStop = StopLossSellPips > 0m ? _entryPrice + ToPrice(StopLossSellPips) : 0m;
		_shortTake = TakeProfitSellPips > 0m ? _entryPrice - ToPrice(TakeProfitSellPips) : 0m;
		_longStop = 0m;
		_longTake = 0m;
		_lastShortEntryTime = candle.OpenTime;

		SellMarket(volume);
	}

	private bool CheckLongExit(ICandleMessage candle)
	{
		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		return false;

		if (_longStop > 0m && candle.LowPrice <= _longStop)
		{
			SellMarket(positionVolume);
			ResetPositionState();
			return true;
		}

		if (_longTake > 0m && candle.HighPrice >= _longTake)
		{
			SellMarket(positionVolume);
			ResetPositionState();
			return true;
		}

		return false;
	}

	private bool CheckShortExit(ICandleMessage candle)
	{
		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		return false;

		if (_shortStop > 0m && candle.HighPrice >= _shortStop)
		{
			BuyMarket(positionVolume);
			ResetPositionState();
			return true;
		}

		if (_shortTake > 0m && candle.LowPrice <= _shortTake)
		{
			BuyMarket(positionVolume);
			ResetPositionState();
			return true;
		}

		return false;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m)
		return;

		var trailingDistance = ToPrice(TrailingStopPips);
		var trailingStep = ToPrice(TrailingStepPips);
		if (trailingDistance <= 0m || trailingStep < 0m)
		return;

		if (Position > 0m)
		{
			var move = candle.ClosePrice - _entryPrice;
			if (move > trailingDistance + trailingStep)
			{
				var required = candle.ClosePrice - (trailingDistance + trailingStep);
				var newStop = candle.ClosePrice - trailingDistance;
				if (_longStop == 0m || _longStop < required)
				_longStop = newStop;
			}
		}
		else if (Position < 0m)
		{
			var move = _entryPrice - candle.ClosePrice;
			if (move > trailingDistance + trailingStep)
			{
				var required = candle.ClosePrice + trailingDistance + trailingStep;
				var newStop = candle.ClosePrice + trailingDistance;
				if (_shortStop == 0m || _shortStop > required)
				_shortStop = newStop;
			}
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
	}

	private decimal ToPrice(decimal pips)
	{
		if (pips <= 0m || _pipSize <= 0m)
		return 0m;

		return pips * _pipSize;
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return PriceType switch
		{
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		var decimals = GetDecimalPlaces(step);
		if (decimals == 3 || decimals == 5)
		return step * 10m;

		return step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);
		var decimals = 0;
		var scaled = value;

		while (scaled != Math.Floor(scaled) && decimals < 10)
		{
			scaled *= 10m;
			decimals++;
		}

		return decimals;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Moving average types supported by the strategy.
	/// </summary>
	public enum MovingAverageMethod
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average.</summary>
		Smoothed,
		/// <summary>Linear weighted moving average.</summary>
		LinearWeighted,
	}

	/// <summary>
	/// Price sources available for indicator calculations.
	/// </summary>
	public enum AppliedPrice
	{
		/// <summary>Close price.</summary>
		Close,
		/// <summary>Open price.</summary>
		Open,
		/// <summary>High price.</summary>
		High,
		/// <summary>Low price.</summary>
		Low,
		/// <summary>Median price (high + low) / 2.</summary>
		Median,
		/// <summary>Typical price (high + low + close) / 3.</summary>
		Typical,
		/// <summary>Weighted price (high + low + close * 2) / 4.</summary>
		Weighted,
	}
}