namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class Up3x1Premium2VmStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _rangeThreshold;
	private readonly StrategyParam<decimal> _bodyThreshold;
	private readonly StrategyParam<decimal> _dailyRangeThreshold;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _convergenceTolerance;

	private decimal _pointSize;
	// Cached point size to convert point-based parameters into prices.

	private ICandleMessage _previousCandle;
	// Rolling history of the last two finished primary candles.
	private ICandleMessage _secondPreviousCandle;

	private decimal? _fastMaPrevious;
	// Cached indicator values to emulate the iMA shift access from MQL.
	private decimal? _fastMaEarlier;
	private decimal? _slowMaPrevious;
	private decimal? _slowMaEarlier;

	private ICandleMessage _previousDailyCandle;
	// Latest finished daily candle required by the midnight breakout filter.
	private decimal? _dailySma;

	private decimal? _longTrailingStop;
	// Trailing state mirrors the dynamic stop modification in the expert advisor.
	private decimal? _shortTrailingStop;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	public Up3x1Premium2VmStrategy()
	// Initialize parameters that map the original expert inputs one-to-one.
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 12)
			.SetDisplay("Fast MA Period", "Length of the fast smoothed moving average", "Indicators")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 26)
			.SetDisplay("Slow MA Period", "Length of the slow smoothed moving average", "Indicators")
			.SetCanOptimize(true);

		_rangeThreshold = Param(nameof(RangeThreshold), 0.0060m)
			.SetDisplay("Range Threshold", "Minimum candle range required for the momentum filter", "Trading");

		_bodyThreshold = Param(nameof(BodyThreshold), 0.0050m)
			.SetDisplay("Body Threshold", "Minimum bullish or bearish candle body", "Trading");

		_dailyRangeThreshold = Param(nameof(DailyRangeThreshold), 0.0060m)
			.SetDisplay("Daily Range Threshold", "Minimum daily range for the midnight filter", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150m)
			.SetDisplay("Take Profit Points", "Take profit distance expressed in price points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetDisplay("Stop Loss Points", "Stop loss distance expressed in price points", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 10m)
			.SetDisplay("Trailing Stop Points", "Trailing stop distance in price points", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.05m)
			.SetDisplay("Trade Volume", "Lot size used for market orders", "Trading");

		_convergenceTolerance = Param(nameof(ConvergenceTolerance), 0.00001m)
			.SetDisplay("Convergence Tolerance", "Maximum difference between averages to trigger exits", "Trading");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public decimal RangeThreshold
	{
		get => _rangeThreshold.Value;
		set => _rangeThreshold.Value = value;
	}

	public decimal BodyThreshold
	{
		get => _bodyThreshold.Value;
		set => _bodyThreshold.Value = value;
	}

	public decimal DailyRangeThreshold
	{
		get => _dailyRangeThreshold.Value;
		set => _dailyRangeThreshold.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public decimal ConvergenceTolerance
	{
		get => _convergenceTolerance.Value;
		set => _convergenceTolerance.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security == null)
			yield break;

		yield return (security, CandleType);
		yield return (security, TimeSpan.FromDays(1).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	// Prepare subscriptions and risk modules once the strategy is launched.
	{
		base.OnStarted(time);

		_pointSize = Security?.PriceStep ?? 0m;
		// Default to one point when the instrument does not expose a price step.
		if (_pointSize <= 0m)
			_pointSize = 1m;

		var fastMa = new SmoothedMovingAverage
		{
			Length = FastMaPeriod,
			CandlePrice = CandlePrice.Typical,
		};

		var slowMa = new SmoothedMovingAverage
		{
			Length = SlowMaPeriod,
			CandlePrice = CandlePrice.Typical,
		};

		var mainSubscription = SubscribeCandles(CandleType);
		// Subscribe to the primary timeframe and bind both smoothed moving averages.
		mainSubscription
			.Bind(fastMa, slowMa, ProcessPrimaryCandle)
			.Start();

		var dailyMa = new SimpleMovingAverage { Length = 10 };
		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		// Daily candles replicate the PERIOD_D1 series used by the original EA.
		dailySubscription
			.Bind(dailyMa, ProcessDailyCandle)
			.Start();

		Unit takeProfit = null;
		Unit stopLoss = null;

		if (TakeProfitPoints > 0m && _pointSize > 0m)
			takeProfit = new Unit(TakeProfitPoints * _pointSize, UnitTypes.Price);

		if (StopLossPoints > 0m && _pointSize > 0m)
			stopLoss = new Unit(StopLossPoints * _pointSize, UnitTypes.Price);

		StartProtection(takeProfit, stopLoss, useMarketOrders: true);
		// Use the built-in protection engine to manage stop-loss and take-profit orders.

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged()
	// Reset or update trailing anchors whenever the position flips.
	{
		base.OnPositionChanged();

		if (Position > 0m)
		{
			_longEntryPrice = PositionPrice;
			_shortEntryPrice = null;
			_shortTrailingStop = null;
		}
		else if (Position < 0m)
		{
			_shortEntryPrice = PositionPrice;
			_longEntryPrice = null;
			_longTrailingStop = null;
		}
		else
		{
			ResetTrailingState();
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_previousDailyCandle = candle;
		_dailySma = smaValue;
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (UpdateTrailingStops(candle))
		// Closing a position via trailing logic short-circuits the rest of the processing.
		{
			UpdateHistory(candle, fastMaValue, slowMaValue);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		// Defer trading until the environment is ready and historical buffers are filled.
		{
			UpdateHistory(candle, fastMaValue, slowMaValue);
			return;
		}

		if (_previousCandle is not { } previous ||
			_secondPreviousCandle is not { } secondPrevious ||
			_fastMaPrevious is not decimal fastPrev ||
			_fastMaEarlier is not decimal fastEarlier ||
			_slowMaPrevious is not decimal slowPrev ||
			_slowMaEarlier is not decimal slowEarlier)
		{
			UpdateHistory(candle, fastMaValue, slowMaValue);
			return;
		}

		var askPrice = GetCurrentPrice(ExecutionTypes.Buy) ?? candle.ClosePrice;
		var bidPrice = GetCurrentPrice(ExecutionTypes.Sell) ?? candle.ClosePrice;

		var bullishTrend = fastEarlier < slowEarlier && slowPrev < fastPrev && secondPrevious.OpenPrice < previous.OpenPrice;
		// First branch reproduces the dual smoothed moving average crossover with open price filter.
		var bullishMomentum = (previous.HighPrice - previous.LowPrice) > RangeThreshold && previous.OpenPrice < previous.ClosePrice && (previous.ClosePrice - previous.OpenPrice) > BodyThreshold;
		var dailyBull = _previousDailyCandle is { } daily && daily.OpenPrice > daily.ClosePrice && (daily.OpenPrice - daily.ClosePrice) > DailyRangeThreshold;
		var dailySmaBias = _dailySma is decimal dailySma && (dailySma > askPrice || dailySma < askPrice || dailySma == askPrice);

		var bearishTrend = fastEarlier > slowEarlier && slowPrev > fastPrev && secondPrevious.OpenPrice > previous.OpenPrice;
		// Symmetric logic for short entries keeps parity with the MQL implementation.
		var bearishMomentum = (previous.HighPrice - previous.LowPrice) > RangeThreshold && previous.OpenPrice > previous.ClosePrice && (previous.OpenPrice - previous.ClosePrice) > BodyThreshold;
		var dailyBear = _previousDailyCandle is { } dailyCandle && dailyCandle.OpenPrice < dailyCandle.ClosePrice && (dailyCandle.ClosePrice - dailyCandle.OpenPrice) > DailyRangeThreshold;

		if (Position == 0m)
		{
			var volume = NormalizeVolume(TradeVolume);
		// Adjust the requested lot size according to exchange volume constraints.
			if (volume > 0m)
			{
				if (bullishTrend || bullishMomentum || dailyBull || dailySmaBias)
				{
					BuyMarket(volume);
					_longEntryPrice = askPrice;
					_longTrailingStop = null;
				}
				else if (bearishTrend || bearishMomentum || dailyBear)
				{
					SellMarket(volume);
					_shortEntryPrice = bidPrice;
					_shortTrailingStop = null;
				}
			}
		}
		else if (Position > 0m)
		{
			var difference = Math.Abs(fastPrev - slowPrev);
			if (difference <= ConvergenceTolerance)
		// MQL closes the trade when both smoothed averages become equal; tolerance simulates equality.
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
				{
					SellMarket(volume);
				}
			}
		}
		else if (Position < 0m)
		{
			var difference = Math.Abs(fastPrev - slowPrev);
			if (difference <= ConvergenceTolerance)
		// MQL closes the trade when both smoothed averages become equal; tolerance simulates equality.
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
				{
					BuyMarket(volume);
				}
			}
		}

		UpdateHistory(candle, fastMaValue, slowMaValue);
	}

	private bool UpdateTrailingStops(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m || _pointSize <= 0m)
		// Trailing remains dormant when the user disables it or price step data is unavailable.
			return false;

		var trailingDistance = TrailingStopPoints * _pointSize;
		var bidPrice = GetCurrentPrice(ExecutionTypes.Sell) ?? candle.ClosePrice;
		var askPrice = GetCurrentPrice(ExecutionTypes.Buy) ?? candle.ClosePrice;

		if (Position > 0m && (PositionPrice ?? _longEntryPrice) is decimal entryLong)
		{
			_longEntryPrice = entryLong;

			if (bidPrice - entryLong > trailingDistance)
		// The expert raises the stop after price advances beyond the trailing distance.
			{
				var candidate = bidPrice - trailingDistance;
				if (!_longTrailingStop.HasValue || candidate > _longTrailingStop.Value)
				_longTrailingStop = candidate;
			}

			if (_longTrailingStop is decimal stopPrice && candle.LowPrice <= stopPrice)
		// Emulate the OrderModify stop execution once price violates the trailing level.
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
				{
					SellMarket(volume);
				}
				return true;
			}
		}
		else
		{
			_longTrailingStop = null;
		}

		if (Position < 0m && (PositionPrice ?? _shortEntryPrice) is decimal entryShort)
		{
			_shortEntryPrice = entryShort;

			if (entryShort - askPrice > trailingDistance)
		// Mirror the short trailing stop logic using the best available ask price.
			{
				var candidate = askPrice + trailingDistance;
				if (!_shortTrailingStop.HasValue || candidate < _shortTrailingStop.Value)
				_shortTrailingStop = candidate;
			}

			if (_shortTrailingStop is decimal stopPrice && candle.HighPrice >= stopPrice)
		// Close short trades when the market touches the adjusted protective stop.
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
				{
					BuyMarket(volume);
				}
				return true;
			}
		}
		else
		{
			_shortTrailingStop = null;
		}

		return false;
	}

	private void UpdateHistory(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue)
	{
		_fastMaEarlier = _fastMaPrevious;
		_fastMaPrevious = fastMaValue;
		_slowMaEarlier = _slowMaPrevious;
		_slowMaPrevious = slowMaValue;

		_secondPreviousCandle = _previousCandle;
		_previousCandle = candle;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var adjusted = Math.Abs(volume);
		// Negative values indicate percentage sizing; absolute value keeps the magnitude only.
		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
			adjusted = Math.Floor(adjusted / step) * step;

		var minVolume = security.VolumeMin ?? (step > 0m ? step : 0m);
		if (minVolume > 0m && adjusted < minVolume)
			adjusted = minVolume;

		var maxVolume = security.VolumeMax;
		if (maxVolume.HasValue && adjusted > maxVolume.Value)
			adjusted = maxVolume.Value;

		return adjusted;
	}

	private void ResetTrailingState()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}
}
