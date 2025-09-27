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
/// Port of the "Trickerless RHMP" MetaTrader expert advisor using high level StockSharp API.
/// The strategy combines ADX trend filtering, smoothed moving averages and ATR based risk management.
/// </summary>
public class TrickerlessRhmpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _minSlopePips;
	private readonly StrategyParam<decimal> _maxSlopePips;
	private readonly StrategyParam<decimal> _trendSpacePips;
	private readonly StrategyParam<decimal> _candleSpikeMultiplier;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _trailingAtrMultiplier;
	private readonly StrategyParam<int> _maxNetPositions;
	private readonly StrategyParam<TimeSpan> _sleepInterval;
	private readonly StrategyParam<decimal> _dailyProfitTarget;
	private readonly StrategyParam<bool> _allowNewEntries;
	private readonly StrategyParam<decimal> _spikeVolumeMultiplier;
	private readonly StrategyParam<bool> _emergencyExit;

	private AverageTrueRange _atr = null!;
	private AverageDirectionalIndex _adx = null!;
	private SmoothedMovingAverage _fastMa = null!;
	private SmoothedMovingAverage _slowMa = null!;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _prevAdx;
	private decimal? _prevPlusDi;
	private decimal? _prevMinusDi;
	private decimal? _lastAtr;
	private decimal? _prevRange;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private DateTimeOffset _lastEntryTime;
	private decimal _sessionStartEquity;
	private bool _tradingSuspended;

	/// <summary>
	/// Initializes a new instance of <see cref="TrickerlessRhmpStrategy"/>.
	/// </summary>
	public TrickerlessRhmpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for all calculations.", "General");

		_orderVolume = Param(nameof(OrderVolume), 0.03m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base volume used for entries.", "Trading");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Average True Range period used for volatility sizing.", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Average Directional Index period.", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 10m)
			.SetDisplay("ADX Threshold", "Minimum ADX value required to consider a trend.", "Filters");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMMA", "Length of the fast smoothed moving average.", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMMA", "Length of the slow smoothed moving average.", "Indicators");

		_minSlopePips = Param(nameof(MinSlopePips), 2m)
			.SetDisplay("Min Slope (pips)", "Lower bound for MA slope measured in pips.", "Filters");

		_maxSlopePips = Param(nameof(MaxSlopePips), 9m)
			.SetDisplay("Max Slope (pips)", "Upper bound for MA slope measured in pips.", "Filters");

		_trendSpacePips = Param(nameof(TrendSpacePips), 5m)
			.SetDisplay("Trend Space (pips)", "Required distance between price and fast MA.", "Filters");

		_candleSpikeMultiplier = Param(nameof(CandleSpikeMultiplier), 7m)
			.SetDisplay("Candle Spike Mult", "How much larger the current range must be to trigger spike entries.", "Filters");

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 1.0m)
			.SetDisplay("ATR Take Profit", "Take profit distance expressed in ATR multiples.", "Risk");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 1.5m)
			.SetDisplay("ATR Stop Loss", "Stop loss distance expressed in ATR multiples.", "Risk");

		_trailingAtrMultiplier = Param(nameof(TrailingAtrMultiplier), 0m)
			.SetDisplay("ATR Trailing", "Trailing stop distance expressed in ATR multiples.", "Risk");

		_maxNetPositions = Param(nameof(MaxNetPositions), 1)
			.SetDisplay("Max Net Positions", "Maximum number of net position units (0 = unlimited).", "Trading");

		_sleepInterval = Param(nameof(SleepInterval), TimeSpan.FromMinutes(24))
			.SetDisplay("Sleep Interval", "Minimum wait between new entries.", "Trading");

		_dailyProfitTarget = Param(nameof(DailyProfitTarget), 0.045m)
			.SetDisplay("Daily Profit Target", "Stop trading after realized PnL reaches this fraction of start equity.", "Risk");

		_allowNewEntries = Param(nameof(AllowNewEntries), true)
			.SetDisplay("Allow Entries", "Enable or disable opening new positions.", "Trading");

		_spikeVolumeMultiplier = Param(nameof(SpikeVolumeMultiplier), 1.0m)
			.SetDisplay("Spike Volume Mult", "Multiplier applied to base volume for spike entries.", "Trading");

		_emergencyExit = Param(nameof(EmergencyExit), false)
			.SetDisplay("Emergency Exit", "Close all positions immediately when enabled.", "Risk");
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base order volume used for each new trade.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
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
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold that confirms trend strength.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Fast smoothed moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothed moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Minimum slope measured in pips to confirm a directional bias.
	/// </summary>
	public decimal MinSlopePips
	{
		get => _minSlopePips.Value;
		set => _minSlopePips.Value = value;
	}

	/// <summary>
	/// Maximum slope measured in pips to avoid overextended moves.
	/// </summary>
	public decimal MaxSlopePips
	{
		get => _maxSlopePips.Value;
		set => _maxSlopePips.Value = value;
	}

	/// <summary>
	/// Required distance between price and the fast moving average.
	/// </summary>
	public decimal TrendSpacePips
	{
		get => _trendSpacePips.Value;
		set => _trendSpacePips.Value = value;
	}

	/// <summary>
	/// Multiplier that defines a spike candle.
	/// </summary>
	public decimal CandleSpikeMultiplier
	{
		get => _candleSpikeMultiplier.Value;
		set => _candleSpikeMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in ATR multiples.
	/// </summary>
	public decimal TakeProfitAtrMultiplier
	{
		get => _takeProfitAtrMultiplier.Value;
		set => _takeProfitAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in ATR multiples.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in ATR multiples.
	/// </summary>
	public decimal TrailingAtrMultiplier
	{
		get => _trailingAtrMultiplier.Value;
		set => _trailingAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum number of net position units.
	/// </summary>
	public int MaxNetPositions
	{
		get => _maxNetPositions.Value;
		set => _maxNetPositions.Value = value;
	}

	/// <summary>
	/// Minimum delay between new entries.
	/// </summary>
	public TimeSpan SleepInterval
	{
		get => _sleepInterval.Value;
		set => _sleepInterval.Value = value;
	}

	/// <summary>
	/// Fraction of start equity after which trading stops for the session.
	/// </summary>
	public decimal DailyProfitTarget
	{
		get => _dailyProfitTarget.Value;
		set => _dailyProfitTarget.Value = value;
	}

	/// <summary>
	/// Determines whether new entries are allowed.
	/// </summary>
	public bool AllowNewEntries
	{
		get => _allowNewEntries.Value;
		set => _allowNewEntries.Value = value;
	}

	/// <summary>
	/// Volume multiplier used for spike entries.
	/// </summary>
	public decimal SpikeVolumeMultiplier
	{
		get => _spikeVolumeMultiplier.Value;
		set => _spikeVolumeMultiplier.Value = value;
	}

	/// <summary>
	/// When enabled the strategy liquidates and stops trading.
	/// </summary>
	public bool EmergencyExit
	{
		get => _emergencyExit.Value;
		set => _emergencyExit.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = null;
		_prevSlow = null;
		_prevAdx = null;
		_prevPlusDi = null;
		_prevMinusDi = null;
		_lastAtr = null;
		_prevRange = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_lastEntryTime = DateTimeOffset.MinValue;
		_sessionStartEquity = 0m;
		_tradingSuspended = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_fastMa = new SmoothedMovingAverage { Length = FastMaPeriod };
		_slowMa = new SmoothedMovingAverage { Length = SlowMaPeriod };

		_sessionStartEquity = Portfolio?.CurrentValue ?? 0m;
		_lastEntryTime = DateTimeOffset.MinValue;
		_tradingSuspended = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_atr, _adx, _fastMa, _slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue adxValue, IIndicatorValue fastValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (EmergencyExit)
		{
			if (Position != 0)
				ClosePosition();

			return;
		}

		if (!atrValue.IsFinal || !adxValue.IsFinal || !fastValue.IsFinal || !slowValue.IsFinal)
			return;

		var atr = atrValue.ToDecimal();
		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		var adxData = (AverageDirectionalIndexValue)adxValue;
		if (adxData.MovingAverage is not decimal adx)
			return;

		var dx = adxData.Dx;
		if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
			return;

		_lastAtr = atr;

		var priceStep = Security?.PriceStep ?? 0.0001m;
		var slope = _prevSlow is decimal prevSlow ? slow - prevSlow : 0m;
		var slopeAbs = Math.Abs(slope);
		var enoughSlope = slopeAbs >= MinSlopePips * priceStep;
		var notTooSteep = slopeAbs <= MaxSlopePips * priceStep;
		var distanceFromFast = Math.Abs(candle.ClosePrice - fast);
		var enoughDistance = distanceFromFast >= TrendSpacePips * priceStep;
		var fastRising = _prevFast is decimal prevFast && fast > prevFast;
		var fastFalling = _prevFast is decimal prevFastDown && fast < prevFastDown;
		var adxIncreasing = _prevAdx is decimal prevAdx ? adx >= prevAdx : false;

		var strongTrend = adx >= AdxThreshold && adxIncreasing && enoughSlope && notTooSteep && enoughDistance;
		var bullish = strongTrend && fast > slow && plusDi >= minusDi && fastRising;
		var bearish = strongTrend && fast < slow && minusDi >= plusDi && fastFalling;

		ManageOpenPositions(candle);

		UpdateDailyLimit();

		var now = candle.CloseTime;
		var canEnter = AllowNewEntries && !_tradingSuspended && now - _lastEntryTime >= SleepInterval;

		if (canEnter)
		{
			if (bullish)
			{
				EnterLong(candle, atr);
			}
			else if (bearish)
			{
				EnterShort(candle, atr);
			}
			else
			{
				ProcessSpikeEntry(candle, atr, plusDi, minusDi);
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevAdx = adx;
		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;
		_prevRange = candle.HighPrice - candle.LowPrice;
	}

	private void EnterLong(ICandleMessage candle, decimal atr)
	{
		if (Volume <= 0)
			return;

		var maxExposure = MaxNetPositions > 0 ? MaxNetPositions * Volume : decimal.MaxValue;
		if (Math.Abs(Position) >= maxExposure && Position >= 0)
			return;

		if (Position < 0)
			BuyMarket(Math.Abs(Position));

		if (Math.Abs(Position) >= maxExposure)
			return;

		BuyMarket(Volume);
		_longEntryPrice = candle.ClosePrice;
		_longStopPrice = candle.ClosePrice - atr * StopLossAtrMultiplier;
		_lastEntryTime = candle.CloseTime;
	}

	private void EnterShort(ICandleMessage candle, decimal atr)
	{
		if (Volume <= 0)
			return;

		var maxExposure = MaxNetPositions > 0 ? MaxNetPositions * Volume : decimal.MaxValue;
		if (Math.Abs(Position) >= maxExposure && Position <= 0)
			return;

		if (Position > 0)
			SellMarket(Position);

		if (Math.Abs(Position) >= maxExposure)
			return;

		SellMarket(Volume);
		_shortEntryPrice = candle.ClosePrice;
		_shortStopPrice = candle.ClosePrice + atr * StopLossAtrMultiplier;
		_lastEntryTime = candle.CloseTime;
	}

	private void ProcessSpikeEntry(ICandleMessage candle, decimal atr, decimal plusDi, decimal minusDi)
	{
		if (_prevRange is null || Volume <= 0)
			return;

		var currentRange = candle.HighPrice - candle.LowPrice;
		if (_prevRange <= 0m || currentRange <= CandleSpikeMultiplier * _prevRange)
			return;

		var directionUp = candle.ClosePrice >= candle.OpenPrice && plusDi >= minusDi;
		var directionDown = candle.ClosePrice < candle.OpenPrice && minusDi >= plusDi;

		if (!directionUp && !directionDown)
			return;

		var spikeVolume = Volume * SpikeVolumeMultiplier;
		if (spikeVolume <= 0)
			return;

		var maxExposure = MaxNetPositions > 0 ? MaxNetPositions * Volume : decimal.MaxValue;

		if (directionUp)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Math.Abs(Position) < maxExposure)
			{
				BuyMarket(spikeVolume);
				_longEntryPrice ??= candle.ClosePrice;
				_longStopPrice ??= candle.ClosePrice - atr * StopLossAtrMultiplier;
				_lastEntryTime = candle.CloseTime;
			}
		}
		else if (directionDown)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Math.Abs(Position) < maxExposure)
			{
				SellMarket(spikeVolume);
				_shortEntryPrice ??= candle.ClosePrice;
				_shortStopPrice ??= candle.ClosePrice + atr * StopLossAtrMultiplier;
				_lastEntryTime = candle.CloseTime;
			}
		}
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (_lastAtr is not decimal atr || atr <= 0)
		{
			if (Position == 0)
			{
				ResetPositionState();
			}
			return;
		}

		if (Position > 0)
		{
			var entry = _longEntryPrice ?? candle.ClosePrice;
			var take = entry + atr * TakeProfitAtrMultiplier;
			var stop = _longStopPrice ?? entry - atr * StopLossAtrMultiplier;

			if (TrailingAtrMultiplier > 0)
			{
				var trailingStop = candle.ClosePrice - atr * TrailingAtrMultiplier;
				if (_longStopPrice is null || trailingStop > _longStopPrice)
					_longStopPrice = trailingStop;
				stop = _longStopPrice.Value;
			}

			if (candle.LowPrice <= stop || candle.ClosePrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
			}
			else if (candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			var entry = _shortEntryPrice ?? candle.ClosePrice;
			var take = entry - atr * TakeProfitAtrMultiplier;
			var stop = _shortStopPrice ?? entry + atr * StopLossAtrMultiplier;

			if (TrailingAtrMultiplier > 0)
			{
				var trailingStop = candle.ClosePrice + atr * TrailingAtrMultiplier;
				if (_shortStopPrice is null || trailingStop < _shortStopPrice)
					_shortStopPrice = trailingStop;
				stop = _shortStopPrice.Value;
			}

			if (candle.HighPrice >= stop || candle.ClosePrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
			else if (candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	private void UpdateDailyLimit()
	{
		if (_sessionStartEquity <= 0m)
			return;

		var realized = PnLManager?.RealizedPnL ?? 0m;
		if (realized / _sessionStartEquity >= DailyProfitTarget)
			_tradingSuspended = true;
	}

	private void ResetPositionState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_shortEntryPrice = null;
		_shortStopPrice = null;
	}
}

