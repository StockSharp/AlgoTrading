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
/// Sweet Spot Extreme strategy ported from MetaTrader 4.
/// Uses dual EMA slope confirmation on 15-minute candles combined with a 30-minute CCI filter.
/// Applies risk-based position sizing with optional streak reduction and closes trades on trend reversals or fixed targets.
/// </summary>
public class SweetSpotExtremeStrategy : Strategy
{
	private readonly StrategyParam<int> _maxTradesPerSymbol;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<decimal> _stopPoints;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _closeMaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _buyCciLevel;
	private readonly StrategyParam<decimal> _sellCciLevel;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _cciCandleType;

	private ExponentialMovingAverage _trendMa;
	private ExponentialMovingAverage _closeMa;
	private CommodityChannelIndex _cci;

	private decimal? _previousTrendValue;
	private decimal? _previousCloseMaValue;
	private decimal? _latestCciValue;

	private decimal _signedPosition;
	private decimal? _lastEntryPrice;
	private Sides? _lastEntrySide;
	private int _consecutiveLosses;

	private decimal _priceStep;

	/// <summary>
	/// Maximum number of aggregated trades per symbol.
	/// </summary>
	public int MaxTradesPerSymbol
	{
		get => _maxTradesPerSymbol.Value;
		set => _maxTradesPerSymbol.Value = value;
	}

	/// <summary>
	/// Fallback fixed lot size when risk-based sizing is unavailable.
	/// </summary>
	public decimal Lots
	{
		get => _lots.Value;
		set => _lots.Value = value;
	}

	/// <summary>
	/// Fraction of portfolio equity committed to each trade.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Volume reduction factor applied after consecutive losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public decimal StopPoints
	{
		get => _stopPoints.Value;
		set => _stopPoints.Value = value;
	}

	/// <summary>
	/// EMA period used for the trend slope filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// EMA period used for the close smoothing filter.
	/// </summary>
	public int CloseMaPeriod
	{
		get => _closeMaPeriod.Value;
		set => _closeMaPeriod.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index lookback period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Oversold CCI threshold for long entries.
	/// </summary>
	public decimal BuyCciLevel
	{
		get => _buyCciLevel.Value;
		set => _buyCciLevel.Value = value;
	}

	/// <summary>
	/// Overbought CCI threshold for short entries.
	/// </summary>
	public decimal SellCciLevel
	{
		get => _sellCciLevel.Value;
		set => _sellCciLevel.Value = value;
	}

	/// <summary>
	/// Minimum volume applied after normalization.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for EMA calculations (default 15-minute).
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for the CCI filter (default 30-minute).
	/// </summary>
	public DataType CciCandleType
	{
		get => _cciCandleType.Value;
		set => _cciCandleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SweetSpotExtremeStrategy()
	{
		_maxTradesPerSymbol = Param(nameof(MaxTradesPerSymbol), 3)
		.SetGreaterOrEqual(1)
		.SetDisplay("Max Trades", "Maximum simultaneous trades per direction", "Risk");

		_lots = Param(nameof(Lots), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Fixed Lots", "Fallback volume when equity data is unavailable", "Risk");

		_maximumRisk = Param(nameof(MaximumRisk), 0.05m)
		.SetNotNegative()
		.SetDisplay("Maximum Risk", "Fraction of equity risked per trade", "Risk");

		_decreaseFactor = Param(nameof(DecreaseFactor), 6m)
		.SetNotNegative()
		.SetDisplay("Decrease Factor", "Volume reduction after loss streaks", "Risk");

		_stopPoints = Param(nameof(StopPoints), 10m)
		.SetNotNegative()
		.SetDisplay("Profit Target", "Take-profit distance in points", "Exits");

		_maPeriod = Param(nameof(MaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("Trend EMA Period", "EMA period for trend slope", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(40, 120, 5);

		_closeMaPeriod = Param(nameof(CloseMaPeriod), 70)
		.SetGreaterThanZero()
		.SetDisplay("Close EMA Period", "EMA period for close smoothing", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(30, 100, 5);

		_cciPeriod = Param(nameof(CciPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Lookback for the CCI filter", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(8, 20, 1);

		_buyCciLevel = Param(nameof(BuyCciLevel), -200m)
		.SetDisplay("Buy CCI", "Oversold threshold for long entries", "Indicators");

		_sellCciLevel = Param(nameof(SellCciLevel), 200m)
		.SetDisplay("Sell CCI", "Overbought threshold for short entries", "Indicators");

		_minVolume = Param(nameof(MinVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Minimum Volume", "Lower bound for normalized order size", "Risk");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Trend Candle", "Candle type for EMA calculations", "Data");

		_cciCandleType = Param(nameof(CciCandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("CCI Candle", "Candle type for the CCI filter", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security is null)
		yield break;

		yield return (security, TrendCandleType);

		if (TrendCandleType != CciCandleType)
		yield return (security, CciCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trendMa = null;
		_closeMa = null;
		_cci = null;
		_previousTrendValue = null;
		_previousCloseMaValue = null;
		_latestCciValue = null;
		_signedPosition = 0m;
		_lastEntryPrice = null;
		_lastEntrySide = null;
		_consecutiveLosses = 0;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		_priceStep = 1m;

		_trendMa = new ExponentialMovingAverage { Length = MaPeriod };
		_closeMa = new ExponentialMovingAverage { Length = CloseMaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };

		SubscribeCandles(TrendCandleType)
		.Bind(ProcessTrendCandle)
		.Start();

		SubscribeCandles(CciCandleType)
		.Bind(ProcessCciCandle)
		.Start();
	}

	private void ProcessTrendCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_trendMa is null || _closeMa is null)
		return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var previousTrend = _previousTrendValue;
		var currentTrend = _trendMa.Process(median).ToDecimal();

		var previousClose = _previousCloseMaValue;
		var currentClose = _closeMa.Process(median).ToDecimal();

		_previousTrendValue = currentTrend;
		_previousCloseMaValue = currentClose;

		if (!_trendMa.IsFormed || !_closeMa.IsFormed)
		return;

		if (previousTrend is null || previousClose is null)
		return;

		var cci = _latestCciValue;
		if (cci is null)
		return;

		var price = candle.ClosePrice;
		var volume = CalculateTradeVolume(price);
		if (volume <= 0m)
		return;

		var volumeStep = Security?.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
		volumeStep = 1m;

		var maxExposure = MaxTradesPerSymbol > 0 ? MaxTradesPerSymbol * volume : volume;

		var buySignal = currentTrend > previousTrend && currentClose > previousClose && cci <= BuyCciLevel;
		var sellSignal = currentTrend < previousTrend && currentClose < previousClose && cci >= SellCciLevel;

		if (buySignal)
		{
		if (Position < 0m)
		{
		var closeVolume = -Position;
		if (closeVolume >= volumeStep)
		BuyMarket(closeVolume);
		}

		var currentLong = Position > 0m ? Position : 0m;
		var remainingLong = maxExposure - currentLong;
		if (remainingLong >= volumeStep)
		{
		var orderVolume = Math.Min(volume, remainingLong);
		BuyMarket(orderVolume);
		}
		}
		else if (sellSignal)
		{
		if (Position > 0m)
		{
		var closeVolume = Position;
		if (closeVolume >= volumeStep)
		SellMarket(closeVolume);
		}

		var currentShort = Position < 0m ? -Position : 0m;
		var remainingShort = maxExposure - currentShort;
		if (remainingShort >= volumeStep)
		{
		var orderVolume = Math.Min(volume, remainingShort);
		SellMarket(orderVolume);
		}
		}

		ApplyExitRules(candle, previousTrend, currentTrend);
	}

	private void ProcessCciCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_cci is null)
		return;

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var value = _cci.Process(typicalPrice);
		if (!value.IsFinal)
		return;

		_latestCciValue = value.GetValue<decimal>();
	}

	private void ApplyExitRules(ICandleMessage candle, decimal? previousTrend, decimal currentTrend)
	{
		var step = _priceStep;
		var targetDistance = StopPoints > 0m ? StopPoints * step : 0m;

		if (Position > 0m)
		{
		var slopeReversal = previousTrend is not null && currentTrend <= previousTrend.Value;
		var reachedTarget = targetDistance > 0m && _lastEntrySide == Sides.Buy && _lastEntryPrice is not null
		&& candle.ClosePrice - _lastEntryPrice.Value >= targetDistance;

		if (slopeReversal || reachedTarget)
		SellMarket(Position);
		}
		else if (Position < 0m)
		{
		var slopeReversal = previousTrend is not null && currentTrend >= previousTrend.Value;
		var reachedTarget = targetDistance > 0m && _lastEntrySide == Sides.Sell && _lastEntryPrice is not null
		&& _lastEntryPrice.Value - candle.ClosePrice >= targetDistance;

		if (slopeReversal || reachedTarget)
		BuyMarket(-Position);
		}
	}

	private decimal CalculateTradeVolume(decimal price)
	{
		var baseVolume = Lots > 0m ? Lots : (Volume > 0m ? Volume : 1m);
		var volume = baseVolume;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (price > 0m && equity > 0m && MaximumRisk > 0m)
		{
		volume = equity * MaximumRisk / price;
		}

		if (DecreaseFactor > 0m && _consecutiveLosses > 1)
		{
		var reduction = volume * _consecutiveLosses / DecreaseFactor;
		volume -= reduction;
		}

		if (volume <= 0m)
		volume = baseVolume;

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		var step = security?.VolumeStep ?? 1m;
		if (step <= 0m)
		step = 1m;

		var minVolume = MinVolume;
		var securityMin = security?.MinVolume ?? 0m;
		if (securityMin > 0m && securityMin > minVolume)
		minVolume = securityMin;

		if (volume < minVolume)
		volume = minVolume;

		var steps = Math.Floor(volume / step);
		if (steps < 1m)
		steps = 1m;

		volume = steps * step;

		var maxVolume = security?.MaxVolume;
		if (maxVolume is not null && maxVolume.Value > 0m && volume > maxVolume.Value)
		volume = maxVolume.Value;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
		return;

		var delta = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previous = _signedPosition;
		_signedPosition += delta;

		if (previous == 0m && _signedPosition != 0m)
		{
		_lastEntrySide = trade.Order.Side;
		_lastEntryPrice = trade.Trade.Price;
		}
		else if (previous != 0m && _signedPosition == 0m)
		{
		if (_lastEntrySide is not null && _lastEntryPrice is not null)
		{
		var exitPrice = trade.Trade.Price;
		var profit = _lastEntrySide == Sides.Buy
		? exitPrice - _lastEntryPrice.Value
		: _lastEntryPrice.Value - exitPrice;

		if (profit > 0m)
		{
		_consecutiveLosses = 0;
		}
		else if (profit < 0m)
		{
		_consecutiveLosses++;
		}
		}

		_lastEntrySide = null;
		_lastEntryPrice = null;
		}
	}
}

