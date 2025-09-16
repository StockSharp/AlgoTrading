namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that emulates the Poker_SHOW MetaTrader 5 expert advisor.
/// Combines a moving average trend filter with random trade triggering and fixed risk targets.
/// </summary>
public class PokerShowStrategy : Strategy
{
	private readonly StrategyParam<PokerCombination> _combination;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<int> _distancePoints;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<bool> _reverseSignal;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator? _ma;
	private readonly List<decimal> _maHistory = [];
	private readonly Random _random = new();

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _priceStep;

	/// <summary>
	/// Minimum poker hand value that must be greater than a random draw to enable a trade.
	/// </summary>
	public PokerCombination Combination
	{
		get => _combination.Value;
		set => _combination.Value = value;
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
	/// Stop loss distance measured in price steps (points).
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price steps (points).
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Minimum required distance between price and moving average in points.
	/// </summary>
	public int DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal moving average shift in bars.
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
	/// Price source for moving average calculations.
	/// </summary>
	public AppliedPrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Reverse the signal direction.
	/// </summary>
	public bool ReverseSignal
	{
		get => _reverseSignal.Value;
		set => _reverseSignal.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="PokerShowStrategy"/>.
	/// </summary>
	public PokerShowStrategy()
	{
		_combination = Param(nameof(Combination), PokerCombination.Couple)
		.SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume in lots", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 50)
		.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150)
		.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk");

		_enableBuy = Param(nameof(EnableBuy), true)
		.SetDisplay("Enable Buy", "Allow opening long positions", "Signals");

		_enableSell = Param(nameof(EnableSell), true)
		.SetDisplay("Enable Sell", "Allow opening short positions", "Signals");

		_distancePoints = Param(nameof(DistancePoints), 50)
		.SetDisplay("MA Distance", "Minimum distance between price and MA", "Signals");

		_maPeriod = Param(nameof(MaPeriod), 24)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of the moving average", "Moving Average");

		_maShift = Param(nameof(MaShift), 0)
		.SetDisplay("MA Shift", "Horizontal shift applied to the moving average", "Moving Average");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Ema)
		.SetDisplay("MA Method", "Moving average smoothing type", "Moving Average");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrice.Close)
		.SetDisplay("Applied Price", "Price input for the moving average", "Moving Average");

		_reverseSignal = Param(nameof(ReverseSignal), false)
		.SetDisplay("Reverse Signals", "Invert MA and price relationship", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for market data", "General");
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

		_ma = null;
		_maHistory.Clear();
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		_ma = CreateMovingAverage(MaMethod, MaPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var price = GetPrice(candle);
		var maValue = _ma!.Process(price, candle.OpenTime, true).ToDecimal();

		_maHistory.Add(maValue);

		var shift = Math.Max(0, MaShift);
		var historySize = shift + 2;
		if (_maHistory.Count > historySize)
		_maHistory.RemoveRange(0, _maHistory.Count - historySize);

		var targetBack = shift + 1;
		if (_maHistory.Count <= targetBack)
		return;

		var maIndex = _maHistory.Count - targetBack - 1;
		var shiftedMa = _maHistory[maIndex];

		var distance = Math.Max(0, DistancePoints) * _priceStep;

		if (Position > 0)
		{
			// Manage long position risk before looking for new entries.
			if (TryCloseLong(candle))
			ResetRiskLevels();

			return;
		}

		if (Position < 0)
		{
			// Manage short position risk before looking for new entries.
			if (TryCloseShort(candle))
			ResetRiskLevels();

			return;
		}

		// Guard against disabled sides.
		if (!EnableBuy && !EnableSell)
		return;

		var threshold = (int)Combination;
		var orderVolume = TradeVolume;

		// Determine trading direction based on moving average placement.
		var allowBuy = EnableBuy && ((!ReverseSignal && shiftedMa > price + distance) || (ReverseSignal && shiftedMa < price - distance));
		var allowSell = EnableSell && ((!ReverseSignal && shiftedMa < price - distance) || (ReverseSignal && shiftedMa > price + distance));

		if (!allowBuy && !allowSell)
		return;

		var stopPoints = Math.Max(0, StopLossPoints);
		var takePoints = Math.Max(0, TakeProfitPoints);

		var executed = false;

		if (allowBuy)
		{
			var randomValue = _random.Next(0, 32768);
			if (randomValue < threshold)
			{
				// Close opposite short if needed and open a new long position.
				var volume = orderVolume + Math.Abs(Position);
				BuyMarket(volume);

				var entryPrice = candle.ClosePrice;
				_stopLossPrice = stopPoints > 0 ? entryPrice - stopPoints * _priceStep : null;
				_takeProfitPrice = takePoints > 0 ? entryPrice + takePoints * _priceStep : null;

				executed = true;
			}
		}

		if (!executed && allowSell)
		{
			var randomValue = _random.Next(0, 32768);
			if (randomValue < threshold)
			{
				// Close opposite long if needed and open a new short position.
				var volume = orderVolume + Math.Abs(Position);
				SellMarket(volume);

				var entryPrice = candle.ClosePrice;
				_stopLossPrice = stopPoints > 0 ? entryPrice + stopPoints * _priceStep : null;
				_takeProfitPrice = takePoints > 0 ? entryPrice - takePoints * _priceStep : null;
			}
		}
	}

	private bool TryCloseLong(ICandleMessage candle)
	{
		var closed = false;

		if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
		{
			ClosePosition();
			closed = true;
		}
		else if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
		{
			ClosePosition();
			closed = true;
		}

		return closed;
	}

	private bool TryCloseShort(ICandleMessage candle)
	{
		var closed = false;

		if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
		{
			ClosePosition();
			closed = true;
		}
		else if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
		{
			ClosePosition();
			closed = true;
		}

		return closed;
	}

	private void ResetRiskLevels()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private decimal GetPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethod method, int period)
	{
		return method switch
		{
			MovingAverageMethod.Sma => new SimpleMovingAverage { Length = period },
			MovingAverageMethod.Ema => new ExponentialMovingAverage { Length = period },
			MovingAverageMethod.Smma => new SmoothedMovingAverage { Length = period },
			MovingAverageMethod.Lwma => new WeightedMovingAverage { Length = period },
			_ => new SimpleMovingAverage { Length = period }
		};
	}
}

/// <summary>
/// Poker combination thresholds used to gate random trade execution.
/// </summary>
public enum PokerCombination
{
	/// <summary>
	/// Straight flush probability threshold.
	/// </summary>
	Royal0 = 127,

	/// <summary>
	/// Four of a kind probability threshold.
	/// </summary>
	Royal1 = 255,

	/// <summary>
	/// Full house probability threshold.
	/// </summary>
	Royal2 = 511,

	/// <summary>
	/// Flush probability threshold.
	/// </summary>
	Royal3 = 1023,

	/// <summary>
	/// Straight probability threshold.
	/// </summary>
	Royal4 = 2047,

	/// <summary>
	/// Three of a kind probability threshold.
	/// </summary>
	Royal5 = 4095,

	/// <summary>
	/// Two pairs probability threshold.
	/// </summary>
	Royal6 = 8191,

	/// <summary>
	/// One pair probability threshold.
	/// </summary>
	Couple = 16383
}

/// <summary>
/// Moving average smoothing methods supported by the strategy.
/// </summary>
public enum MovingAverageMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Sma = 0,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Ema = 1,

	/// <summary>
	/// Smoothed moving average.
	/// </summary>
	Smma = 2,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma = 3
}

/// <summary>
/// Price sources emulating MetaTrader applied price options.
/// </summary>
public enum AppliedPrice
{
	/// <summary>
	/// Use close price.
	/// </summary>
	Close = 0,

	/// <summary>
	/// Use open price.
	/// </summary>
	Open = 1,

	/// <summary>
	/// Use high price.
	/// </summary>
	High = 2,

	/// <summary>
	/// Use low price.
	/// </summary>
	Low = 3,

	/// <summary>
	/// Use median price (high + low) / 2.
	/// </summary>
	Median = 4,

	/// <summary>
	/// Use typical price (high + low + close) / 3.
	/// </summary>
	Typical = 5,

	/// <summary>
	/// Use weighted price (high + low + 2 * close) / 4.
	/// </summary>
	Weighted = 6
}
