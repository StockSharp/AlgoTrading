using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams AO + AC strategy converted from MQL4.
/// Filters Bollinger Band width, waits for a bullish or bearish RSI reading, and then requires the Awesome Oscillator to cross the zero line together with an accelerating Accelerator Oscillator sequence.
/// </summary>
public class WilliamsAoAcStrategy : Strategy
{
	private const int AcceleratorSmoothingPeriod = 5;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _bollingerSpreadLower;
	private readonly StrategyParam<decimal> _bollingerSpreadUpper;
	private readonly StrategyParam<int> _aoFastPeriod;
	private readonly StrategyParam<int> _aoSlowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyThreshold;
	private readonly StrategyParam<decimal> _rsiSellThreshold;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _tradingWindowHours;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _trailingStopPoints;

	private BollingerBands? _bollinger;
	private RelativeStrengthIndex? _rsi;
	private AwesomeOscillator? _awesome;
	private SimpleMovingAverage? _aoAverage;

	private readonly List<decimal> _acceleratorHistory = new();

	private decimal _pipSize;
	private decimal _previousAo;
	private bool _hasPreviousAo;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Initializes a new instance of the <see cref="WilliamsAoAcStrategy"/>.
	/// </summary>
	public WilliamsAoAcStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series used for calculations", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("BB Period", "Bollinger Bands lookback length", "Bollinger Bands")
		.SetCanOptimize(true);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
		.SetGreaterThanZero()
		.SetDisplay("BB Deviation", "Standard deviation multiplier for the bands", "Bollinger Bands")
		.SetCanOptimize(true);

		_bollingerSpreadLower = Param(nameof(BollingerSpreadLower), 40m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("BB Spread Min", "Minimum band width in points required to trade", "Bollinger Bands")
		.SetCanOptimize(true);

		_bollingerSpreadUpper = Param(nameof(BollingerSpreadUpper), 210m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("BB Spread Max", "Maximum band width in points allowed to trade", "Bollinger Bands")
		.SetCanOptimize(true);

		_aoFastPeriod = Param(nameof(AoFastPeriod), 11)
		.SetGreaterThanZero()
		.SetDisplay("AO Fast", "Short moving average period for Awesome Oscillator", "Awesome Oscillator")
		.SetCanOptimize(true);

		_aoSlowPeriod = Param(nameof(AoSlowPeriod), 40)
		.SetGreaterThanZero()
		.SetDisplay("AO Slow", "Long moving average period for Awesome Oscillator", "Awesome Oscillator")
		.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Relative Strength Index period", "RSI")
		.SetCanOptimize(true);

		_rsiBuyThreshold = Param(nameof(RsiBuyThreshold), 46m)
		.SetDisplay("RSI Buy", "Minimum RSI value that confirms bullish momentum", "RSI")
		.SetCanOptimize(true);

		_rsiSellThreshold = Param(nameof(RsiSellThreshold), 40m)
		.SetDisplay("RSI Sell", "Maximum RSI value that confirms bearish momentum", "RSI")
		.SetCanOptimize(true);

		_entryHour = Param(nameof(EntryHour), 0)
		.SetDisplay("Entry Hour", "Hour of day (0-23) when trading window opens", "Session")
		.SetRange(0, 23);

		_tradingWindowHours = Param(nameof(TradingWindowHours), 20)
		.SetGreaterOrEqual(0)
		.SetDisplay("Trading Hours", "Number of consecutive hours allowed for trading", "Session")
		.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Lot size used for each new entry", "Risk")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 60)
		.SetGreaterOrEqual(0)
		.SetDisplay("Stop Loss", "Protective stop distance expressed in points", "Risk")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 90)
		.SetGreaterOrEqual(0)
		.SetDisplay("Take Profit", "Profit target distance expressed in points", "Risk")
		.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 30)
		.SetGreaterOrEqual(0)
		.SetDisplay("Trailing Stop", "Trailing stop distance in points applied after profits", "Risk")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Type of candles used for the trading logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Minimum allowed Bollinger Band width in points.
	/// </summary>
	public decimal BollingerSpreadLower
	{
		get => _bollingerSpreadLower.Value;
		set => _bollingerSpreadLower.Value = value;
	}

	/// <summary>
	/// Maximum allowed Bollinger Band width in points.
	/// </summary>
	public decimal BollingerSpreadUpper
	{
		get => _bollingerSpreadUpper.Value;
		set => _bollingerSpreadUpper.Value = value;
	}

	/// <summary>
	/// Fast period for the Awesome Oscillator.
	/// </summary>
	public int AoFastPeriod
	{
		get => _aoFastPeriod.Value;
		set => _aoFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow period for the Awesome Oscillator.
	/// </summary>
	public int AoSlowPeriod
	{
		get => _aoSlowPeriod.Value;
		set => _aoSlowPeriod.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI value required to confirm a long setup.
	/// </summary>
	public decimal RsiBuyThreshold
	{
		get => _rsiBuyThreshold.Value;
		set => _rsiBuyThreshold.Value = value;
	}

	/// <summary>
	/// RSI value required to confirm a short setup.
	/// </summary>
	public decimal RsiSellThreshold
	{
		get => _rsiSellThreshold.Value;
		set => _rsiSellThreshold.Value = value;
	}

	/// <summary>
	/// Hour at which the trading session opens.
	/// </summary>
	public int EntryHour
	{
		get => _entryHour.Value;
		set => _entryHour.Value = value;
	}

	/// <summary>
	/// Number of consecutive hours that remain tradable starting from <see cref="EntryHour"/>.
	/// </summary>
	public int TradingWindowHours
	{
		get => _tradingWindowHours.Value;
		set => _tradingWindowHours.Value = value;
	}

	/// <summary>
	/// Lot size used for new market entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points applied once the trade moves in favor.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
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

		_acceleratorHistory.Clear();
		_previousAo = 0m;
		_hasPreviousAo = false;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
		_pipSize = 1m;

		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_awesome = new AwesomeOscillator
		{
			ShortPeriod = AoFastPeriod,
			LongPeriod = AoSlowPeriod
		};

		_aoAverage = new SimpleMovingAverage
		{
			Length = AcceleratorSmoothingPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_bollinger, _rsi, _awesome, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _awesome);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal rsiValue, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateTrailingStop(candle);

		var averageValue = _aoAverage!.Process(aoValue);
		if (!averageValue.IsFinal || !averageValue.TryGetValue(out var aoAverage))
		{
			_previousAo = aoValue;
			_hasPreviousAo = true;
			return;
		}

		var previousAo = _previousAo;
		var hasPrevious = _hasPreviousAo;

		_previousAo = aoValue;
		_hasPreviousAo = true;

		var acValue = aoValue - aoAverage;
		_acceleratorHistory.Add(acValue);
		if (_acceleratorHistory.Count > 5)
		_acceleratorHistory.RemoveAt(0);

		if (!hasPrevious)
		return;

		var aoCrossUp = previousAo < 0m && aoValue > 0m;
		var aoCrossDown = previousAo > 0m && aoValue < 0m;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var candleTime = candle.OpenTime != default ? candle.OpenTime : candle.Time;
		if (!IsWithinTradingWindow(candleTime))
		return;

		var spreadPoints = CalculateBandSpread(upperBand, lowerBand);
		if (spreadPoints < BollingerSpreadLower || spreadPoints > BollingerSpreadUpper)
		return;

		var bullishAccelerator = IsAcceleratorBullish();
		var bearishAccelerator = IsAcceleratorBearish();

		var buySignal = aoCrossUp && bullishAccelerator && rsiValue > RsiBuyThreshold;
		var sellSignal = aoCrossDown && bearishAccelerator && rsiValue < RsiSellThreshold;

		if (buySignal && Position <= 0m)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (sellSignal && Position >= 0m)
		{
			EnterShort(candle.ClosePrice);
		}
	}

	private decimal CalculateBandSpread(decimal upperBand, decimal lowerBand)
	{
		var width = upperBand - lowerBand;
		return _pipSize > 0m ? width / _pipSize : width;
	}

	private bool IsAcceleratorBullish()
	{
		if (_acceleratorHistory.Count < 3)
		return false;

		var last = _acceleratorHistory[^1];
		var prev = _acceleratorHistory[^2];
		var prev2 = _acceleratorHistory[^3];

		return last > prev && last > 0m && prev > 0m && prev2 > 0m;
	}

	private bool IsAcceleratorBearish()
	{
		if (_acceleratorHistory.Count < 3)
		return false;

		var last = _acceleratorHistory[^1];
		var prev = _acceleratorHistory[^2];
		var prev2 = _acceleratorHistory[^3];

		return last < prev && last < 0m && prev < 0m && prev2 < 0m;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var entry = ((EntryHour % 24) + 24) % 24;
		var duration = TradingWindowHours;

		if (duration < 0)
		return true;

		var hour = time.Hour;
		var closeHour = (entry + duration) % 24;
		var wraps = entry + duration > 23;

		if (duration == 0)
		return hour == entry;

		return wraps ? hour >= entry || hour <= closeHour : hour >= entry && hour <= closeHour;
	}

	private void EnterLong(decimal price)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
		{
			AddWarningLog("Skip long entry because trade volume is non-positive. Volume={0:0.####}", volume);
			return;
		}

		var closingVolume = Position < 0m ? Math.Abs(Position) : 0m;
		var totalVolume = closingVolume + volume;
		if (totalVolume <= 0m)
		return;

		var resultingPosition = Position + totalVolume;
		BuyMarket(totalVolume);

		if (TakeProfitPoints > 0)
		SetTakeProfit(TakeProfitPoints, price, resultingPosition);

		if (StopLossPoints > 0)
		SetStopLoss(StopLossPoints, price, resultingPosition);

		_longEntryPrice = price;
		_shortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private void EnterShort(decimal price)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
		{
			AddWarningLog("Skip short entry because trade volume is non-positive. Volume={0:0.####}", volume);
			return;
		}

		var closingVolume = Position > 0m ? Position : 0m;
		var totalVolume = closingVolume + volume;
		if (totalVolume <= 0m)
		return;

		var resultingPosition = Position - totalVolume;
		SellMarket(totalVolume);

		if (TakeProfitPoints > 0)
		SetTakeProfit(TakeProfitPoints, price, resultingPosition);

		if (StopLossPoints > 0)
		SetStopLoss(StopLossPoints, price, resultingPosition);

		_shortEntryPrice = price;
		_longEntryPrice = null;
		_shortTrailingStop = null;
		_longTrailingStop = null;
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0)
		return;

		if (_pipSize <= 0m)
		return;

		var trailingDistance = TrailingStopPoints * _pipSize;
		if (trailingDistance <= 0m)
		return;

		var closePrice = candle.ClosePrice;

		if (Position > 0m && _longEntryPrice.HasValue)
		{
			var profit = closePrice - _longEntryPrice.Value;
			if (profit <= trailingDistance)
			return;

			var desiredStop = closePrice - trailingDistance;
			if (_longTrailingStop is null || desiredStop > _longTrailingStop.Value)
			{
				_longTrailingStop = desiredStop;
				SetStopLoss(TrailingStopPoints, closePrice, Position);
			}
		}
		else if (Position < 0m && _shortEntryPrice.HasValue)
		{
			var profit = _shortEntryPrice.Value - closePrice;
			if (profit <= trailingDistance)
			return;

			var desiredStop = closePrice + trailingDistance;
			if (_shortTrailingStop is null || desiredStop < _shortTrailingStop.Value)
			{
				_shortTrailingStop = desiredStop;
				SetStopLoss(TrailingStopPoints, closePrice, Position);
			}
		}
	}
}
