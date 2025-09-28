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
/// Multi-timeframe slope direction plus RSI strategy converted from the MetaTrader advisor.
/// Aligns four Hull moving averages across the configured timeframes and validates RSI filters before trading.
/// Recreates the ATR-based stop-loss and take-profit placement used by the original EA.
/// </summary>
public class SlopeRsiMtfStrategy : Strategy
{
	private readonly StrategyParam<int> _slopeTriggerLength;
	private readonly StrategyParam<int> _slopeTrendLength;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLowerBound;
	private readonly StrategyParam<decimal> _rsiMiddleLevel;
	private readonly StrategyParam<decimal> _rsiUpperBound;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<bool> _useCompounding;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _balanceDivider;
	private readonly StrategyParam<DataType> _baseTimeframe;
	private readonly StrategyParam<DataType> _hourTimeframe;
	private readonly StrategyParam<DataType> _fourHourTimeframe;
	private readonly StrategyParam<DataType> _dayTimeframe;

	private TimeframeState _baseState = null!;
	private TimeframeState _hourState = null!;
	private TimeframeState _fourHourState = null!;
	private TimeframeState _dayState = null!;
	private AverageTrueRange _hourAtr = null!;
	private AverageTrueRange _dayAtr = null!;

	private decimal? _hourAtrValue;
	private decimal? _dayAtrValue;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private bool? _protectionIsLong;
	private int _longEntries;
	private int _shortEntries;

	/// <summary>
	/// Length of the Hull moving average on the base timeframe.
	/// </summary>
	public int SlopeTriggerLength
	{
		get => _slopeTriggerLength.Value;
		set => _slopeTriggerLength.Value = value;
	}

	/// <summary>
	/// Length of the Hull moving averages on higher timeframes.
	/// </summary>
	public int SlopeTrendLength
	{
		get => _slopeTrendLength.Value;
		set => _slopeTrendLength.Value = value;
	}

	/// <summary>
	/// RSI calculation period for every timeframe.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold used for sell signals.
	/// </summary>
	public decimal RsiLowerBound
	{
		get => _rsiLowerBound.Value;
		set => _rsiLowerBound.Value = value;
	}

	/// <summary>
	/// Middle RSI level separating bullish and bearish regimes.
	/// </summary>
	public decimal RsiMiddleLevel
	{
		get => _rsiMiddleLevel.Value;
		set => _rsiMiddleLevel.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold used for buy signals.
	/// </summary>
	public decimal RsiUpperBound
	{
		get => _rsiUpperBound.Value;
		set => _rsiUpperBound.Value = value;
	}

	/// <summary>
	/// ATR period used for stop-loss and take-profit distances.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Maximum number of scale-in entries per direction.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Enables account-balance-based position sizing.
	/// </summary>
	public bool UseCompounding
	{
		get => _useCompounding.Value;
		set => _useCompounding.Value = value;
	}

	/// <summary>
	/// Fixed lot size used when compounding is disabled.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Divider applied to portfolio equity when calculating the compounded volume.
	/// </summary>
	public decimal BalanceDivider
	{
		get => _balanceDivider.Value;
		set => _balanceDivider.Value = value;
	}

	/// <summary>
	/// Primary timeframe that drives order execution.
	/// </summary>
	public DataType BaseTimeframe
	{
		get => _baseTimeframe.Value;
		set => _baseTimeframe.Value = value;
	}

	/// <summary>
	/// First confirmation timeframe (typically one hour).
	/// </summary>
	public DataType HourTimeframe
	{
		get => _hourTimeframe.Value;
		set => _hourTimeframe.Value = value;
	}

	/// <summary>
	/// Second confirmation timeframe (typically four hours).
	/// </summary>
	public DataType FourHourTimeframe
	{
		get => _fourHourTimeframe.Value;
		set => _fourHourTimeframe.Value = value;
	}

	/// <summary>
	/// Highest confirmation timeframe (typically one day).
	/// </summary>
	public DataType DayTimeframe
	{
		get => _dayTimeframe.Value;
		set => _dayTimeframe.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SlopeRsiMtfStrategy"/> class.
	/// </summary>
	public SlopeRsiMtfStrategy()
	{
		_slopeTriggerLength = Param(nameof(SlopeTriggerLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("Base Hull Length", "Hull MA period on the trading timeframe", "Indicators");

		_slopeTrendLength = Param(nameof(SlopeTrendLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Higher Hull Length", "Hull MA period on confirmation timeframes", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period used on all timeframes", "Indicators");

		_rsiLowerBound = Param(nameof(RsiLowerBound), 10m)
			.SetDisplay("RSI Lower", "Lower RSI bound for sell filters", "Filters");

		_rsiMiddleLevel = Param(nameof(RsiMiddleLevel), 50m)
			.SetDisplay("RSI Middle", "Neutral RSI threshold", "Filters");

		_rsiUpperBound = Param(nameof(RsiUpperBound), 90m)
			.SetDisplay("RSI Upper", "Upper RSI bound for buy filters", "Filters");

		_atrPeriod = Param(nameof(AtrPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR window for protective levels", "Risk");

		_maxOrders = Param(nameof(MaxOrders), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Orders", "Maximum number of scale-in trades per direction", "Risk");

		_useCompounding = Param(nameof(UseCompounding), true)
			.SetDisplay("Use Compounding", "Recalculate volume from portfolio value", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Fixed lot size when compounding is off", "Risk");

		_balanceDivider = Param(nameof(BalanceDivider), 100000m)
			.SetGreaterThanZero()
			.SetDisplay("Balance Divider", "Equity divider for compounded volume", "Risk");

		_baseTimeframe = Param(nameof(BaseTimeframe), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Base Timeframe", "Trading timeframe", "Timeframes");

		_hourTimeframe = Param(nameof(HourTimeframe), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Hour Timeframe", "First confirmation timeframe", "Timeframes");

		_fourHourTimeframe = Param(nameof(FourHourTimeframe), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("4H Timeframe", "Second confirmation timeframe", "Timeframes");

		_dayTimeframe = Param(nameof(DayTimeframe), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Day Timeframe", "Highest confirmation timeframe", "Timeframes");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		var used = new HashSet<DataType>();
		var frames = new[] { BaseTimeframe, HourTimeframe, FourHourTimeframe, DayTimeframe };

		foreach (var frame in frames)
		{
			if (used.Add(frame))
				yield return (Security, frame);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_hourAtrValue = null;
		_dayAtrValue = null;
		_stopPrice = null;
		_takePrice = null;
		_protectionIsLong = null;
		_longEntries = 0;
		_shortEntries = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_baseState = new TimeframeState(SlopeTriggerLength, RsiPeriod);
		_hourState = new TimeframeState(SlopeTrendLength, RsiPeriod);
		_fourHourState = new TimeframeState(SlopeTrendLength, RsiPeriod);
		_dayState = new TimeframeState(SlopeTrendLength, RsiPeriod);

		_hourAtr = new AverageTrueRange { Length = AtrPeriod };
		_dayAtr = new AverageTrueRange { Length = AtrPeriod };

		var baseSubscription = SubscribeCandles(BaseTimeframe);
		baseSubscription
			.Bind(_baseState.Hull, _baseState.Rsi, ProcessBaseCandle)
			.Start();

		var hourSubscription = SubscribeCandles(HourTimeframe);
		hourSubscription
			.Bind(_hourState.Hull, _hourState.Rsi, _hourAtr, ProcessHourCandle)
			.Start();

		var fourHourSubscription = SubscribeCandles(FourHourTimeframe);
		fourHourSubscription
			.Bind(_fourHourState.Hull, _fourHourState.Rsi, ProcessFourHourCandle)
			.Start();

		var daySubscription = SubscribeCandles(DayTimeframe);
		daySubscription
			.Bind(_dayState.Hull, _dayState.Rsi, _dayAtr, ProcessDayCandle)
			.Start();

		var adjustedVolume = AdjustVolume(BaseVolume);
		if (adjustedVolume > 0m)
			Volume = adjustedVolume;

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawIndicator(area, _baseState.Hull);
			DrawOwnTrades(area);
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle, decimal hullValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_baseState.UpdateHull(hullValue);
		_baseState.UpdateRsi(rsiValue);

		if (HandleProtection(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!AreStatesReady())
			return;

		var bullish = CheckBullishConditions();
		var bearish = CheckBearishConditions();

		if (bullish)
		{
			HandleLongSignal(candle);
		}
		else if (bearish)
		{
			HandleShortSignal(candle);
		}
	}

	private void ProcessHourCandle(ICandleMessage candle, decimal hullValue, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_hourState.UpdateHull(hullValue);
		_hourState.UpdateRsi(rsiValue);

		if (_hourAtr.IsFormed)
			_hourAtrValue = atrValue;
	}

	private void ProcessFourHourCandle(ICandleMessage candle, decimal hullValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_fourHourState.UpdateHull(hullValue);
		_fourHourState.UpdateRsi(rsiValue);
	}

	private void ProcessDayCandle(ICandleMessage candle, decimal hullValue, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_dayState.UpdateHull(hullValue);
		_dayState.UpdateRsi(rsiValue);

		if (_dayAtr.IsFormed)
			_dayAtrValue = atrValue;
	}

	private bool AreStatesReady()
	{
		return _baseState.IsReady() &&
			_hourState.IsReady() &&
			_fourHourState.IsReady() &&
			_dayState.IsReady();
	}

	private bool CheckBullishConditions()
	{
		return _baseState.IsBullish(RsiMiddleLevel, RsiUpperBound) &&
			_hourState.IsBullish(RsiMiddleLevel, RsiUpperBound) &&
			_fourHourState.IsBullish(RsiMiddleLevel, RsiUpperBound) &&
			_dayState.IsBullish(RsiMiddleLevel, RsiUpperBound);
	}

	private bool CheckBearishConditions()
	{
		return _baseState.IsBearish(RsiLowerBound, RsiMiddleLevel) &&
			_hourState.IsBearish(RsiLowerBound, RsiMiddleLevel) &&
			_fourHourState.IsBearish(RsiLowerBound, RsiMiddleLevel) &&
			_dayState.IsBearish(RsiLowerBound, RsiMiddleLevel);
	}

	private void HandleLongSignal(ICandleMessage candle)
	{
		if (_hourAtrValue is not decimal atrStop || _dayAtrValue is not decimal atrTake)
			return;

		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_shortEntries = 0;
		}

		if (_longEntries >= MaxOrders)
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_longEntries++;

		SetProtection(candle.ClosePrice, atrStop, atrTake, true);
		LogInfo($"Buy signal at {candle.OpenTime:O}. Close={candle.ClosePrice}, ATR stop={atrStop}, ATR take={atrTake}.");
	}

	private void HandleShortSignal(ICandleMessage candle)
	{
		if (_hourAtrValue is not decimal atrStop || _dayAtrValue is not decimal atrTake)
			return;

		if (Position > 0)
		{
			SellMarket(Position);
			_longEntries = 0;
		}

		if (_shortEntries >= MaxOrders)
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_shortEntries++;

		SetProtection(candle.ClosePrice, atrStop, atrTake, false);
		LogInfo($"Sell signal at {candle.OpenTime:O}. Close={candle.ClosePrice}, ATR stop={atrStop}, ATR take={atrTake}.");
	}

	private bool HandleProtection(ICandleMessage candle)
	{
		if (Security is null)
			return false;

		if (_protectionIsLong == true && Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetProtection();
				LogInfo($"Long stop-loss triggered at {stop}.");
				return true;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetProtection();
				LogInfo($"Long take-profit triggered at {take}.");
				return true;
			}
		}
		else if (_protectionIsLong == false && Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				LogInfo($"Short stop-loss triggered at {stop}.");
				return true;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				LogInfo($"Short take-profit triggered at {take}.");
				return true;
			}
		}

		return false;
	}

	private void SetProtection(decimal price, decimal atrStop, decimal atrTake, bool isLong)
	{
		if (Security is null)
			return;

		var stop = isLong ? price - atrStop : price + atrStop;
		var take = isLong ? price + atrTake : price - atrTake;

		_stopPrice = Security.ShrinkPrice(stop);
		_takePrice = Security.ShrinkPrice(take);
		_protectionIsLong = isLong;
	}

	private void ResetProtection()
	{
		_stopPrice = null;
		_takePrice = null;
		_protectionIsLong = null;
	}

	private decimal CalculateOrderVolume()
	{
		var volume = BaseVolume;

		if (UseCompounding)
		{
			var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			if (portfolioValue > 0m && BalanceDivider > 0m)
				volume = portfolioValue / BalanceDivider;
		}

		if (volume <= 0m)
			volume = BaseVolume;

		var adjusted = AdjustVolume(volume);
		return adjusted > 0m ? adjusted : volume;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var rounded = step * Math.Floor(volume / step);
			volume = rounded > 0m ? rounded : step;
		}

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_longEntries = 0;
			_shortEntries = 0;
			ResetProtection();
		}
		else if (Position > 0m)
		{
			_shortEntries = 0;
			_protectionIsLong = true;
		}
		else
		{
			_longEntries = 0;
			_protectionIsLong = false;
		}
	}

	private sealed class TimeframeState
	{
		public TimeframeState(int hullLength, int rsiPeriod)
		{
			Hull = new HullMovingAverage { Length = hullLength };
			Rsi = new RelativeStrengthIndex { Length = rsiPeriod };
		}

		public HullMovingAverage Hull { get; }
		public RelativeStrengthIndex Rsi { get; }
		public decimal? PreviousHull { get; private set; }
		public decimal? CurrentHull { get; private set; }
		public decimal? CurrentRsi { get; private set; }

		public void UpdateHull(decimal value)
		{
			PreviousHull = CurrentHull;
			CurrentHull = value;
		}

		public void UpdateRsi(decimal value)
		{
			CurrentRsi = value;
		}

		public bool IsReady()
		{
			return Hull.IsFormed && Rsi.IsFormed && PreviousHull.HasValue && CurrentHull.HasValue && CurrentRsi.HasValue;
		}

		public bool IsBullish(decimal middle, decimal upper)
		{
			if (!IsReady())
				return false;

			if (CurrentHull <= PreviousHull)
				return false;

			var rsi = CurrentRsi.Value;
			return rsi > middle && rsi < upper;
		}

		public bool IsBearish(decimal lower, decimal middle)
		{
			if (!IsReady())
				return false;

			if (CurrentHull >= PreviousHull)
				return false;

			var rsi = CurrentRsi.Value;
			return rsi < middle && rsi > lower;
		}
	}
}
