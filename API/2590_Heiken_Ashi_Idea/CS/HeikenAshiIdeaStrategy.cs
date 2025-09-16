using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe Heikin Ashi strategy that uses pending limit orders and an ATR filter.
/// </summary>
public class HeikenAshiIdeaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _distancePoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useCloseAll;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _closeAllCandleType;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _useAtrFilter;
	private readonly StrategyParam<int> _atrPeriod;

	private AverageTrueRange _atr;

	private bool _hasAtrValue;
	private bool _hasPrevAtrValue;
	private decimal _lastAtrValue;
	private decimal _prevAtrValue;

	private bool _baseHasCurrent;
	private bool _baseHasPrevious;
	private HeikinAshiCandle _baseCurrentHa;
	private HeikinAshiCandle _basePreviousHa;

	private bool _higherHasCurrent;
	private bool _higherHasPrevious;
	private HeikinAshiCandle _higherCurrentHa;
	private HeikinAshiCandle _higherPreviousHa;

	private Order _buyOrder;
	private Order _sellOrder;

	private DateTimeOffset? _lastCloseAllTime;

	private decimal _priceStep;
	private decimal _comparisonTolerance;

	/// <summary>
	/// Distance in price steps used to offset pending orders from the market price.
	/// </summary>
	public decimal DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps (0 disables the protective stop).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps (0 disables the target).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Whether to flatten positions when a new candle of the close-all timeframe opens.
	/// </summary>
	public bool UseCloseAllOnNewBar
	{
		get => _useCloseAll.Value;
		set => _useCloseAll.Value = value;
	}

	/// <summary>
	/// Primary candle type used for trade signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type that confirms the trend.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used to trigger the close-all routine.
	/// </summary>
	public DataType CloseAllCandleType
	{
		get => _closeAllCandleType.Value;
		set => _closeAllCandleType.Value = value;
	}

	/// <summary>
	/// First hour of the trading window (inclusive).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Last hour of the trading window (inclusive).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Whether the strategy requires rising ATR values before placing new orders.
	/// </summary>
	public bool UseAtrFilter
	{
		get => _useAtrFilter.Value;
		set => _useAtrFilter.Value = value;
	}

	/// <summary>
	/// ATR period used in the volatility filter.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HeikenAshiIdeaStrategy"/> class.
	/// </summary>
	public HeikenAshiIdeaStrategy()
	{
		_distancePoints = Param(nameof(DistancePoints), 8m)
				.SetGreaterThanZero()
				.SetDisplay("Pending Distance (pts)", "Distance for pending limit orders measured in price steps.", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
				.SetDisplay("Stop Loss (pts)", "Stop-loss distance in price steps. Set to 0 to disable the protective stop.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
				.SetDisplay("Take Profit (pts)", "Take-profit distance in price steps. Set to 0 to disable the target.", "Risk");

		_useCloseAll = Param(nameof(UseCloseAllOnNewBar), true)
				.SetDisplay("Close On Higher Bar", "Flatten positions when a new candle of the close-all timeframe opens.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
				.SetDisplay("Primary Candle Type", "Primary timeframe used for trading signals.", "Data");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Higher Candle Type", "Confirmation timeframe used for Heikin Ashi trend filter.", "Data");

		_closeAllCandleType = Param(nameof(CloseAllCandleType), TimeSpan.FromDays(7).TimeFrame())
				.SetDisplay("Close-All Candle Type", "Timeframe that triggers a complete exit on a new bar.", "Data");

		_startHour = Param(nameof(StartHour), 9)
				.SetRange(0, 23)
				.SetDisplay("Start Hour", "First hour of the trading window (inclusive).", "Session");

		_endHour = Param(nameof(EndHour), 19)
				.SetRange(0, 23)
				.SetDisplay("End Hour", "Last hour of the trading window (inclusive).", "Session");

		_useAtrFilter = Param(nameof(UseAtrFilter), true)
				.SetDisplay("Use ATR Filter", "Require rising ATR to allow new orders.", "Filters");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period used for the ATR volatility filter.", "Filters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, HigherCandleType);

		if (UseCloseAllOnNewBar)
				yield return (Security, CloseAllCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_atr?.Reset();
		_hasAtrValue = false;
		_hasPrevAtrValue = false;
		_lastAtrValue = 0m;
		_prevAtrValue = 0m;

		_baseHasCurrent = false;
		_baseHasPrevious = false;
		_baseCurrentHa = default;
		_basePreviousHa = default;

		_higherHasCurrent = false;
		_higherHasPrevious = false;
		_higherCurrentHa = default;
		_higherPreviousHa = default;

		_buyOrder = null;
		_sellOrder = null;
		_lastCloseAllTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;

		if (_priceStep <= 0m)
				_priceStep = 1m;

		_comparisonTolerance = _priceStep / 2m;

		_atr = new AverageTrueRange { Length = AtrPeriod };

		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription
				.Bind(_atr, ProcessPrimaryCandle)
				.Start();

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription
				.Bind(ProcessHigherCandle)
				.Start();

		if (UseCloseAllOnNewBar)
		{
				var closeAllSubscription = SubscribeCandles(CloseAllCandleType);
				closeAllSubscription
					.Bind(ProcessCloseAllCandle)
					.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
				DrawCandles(area, primarySubscription);
				DrawOwnTrades(area);
		}

		var takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * _priceStep, UnitTypes.Price) : null;
		var stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints * _priceStep, UnitTypes.Price) : null;

		if (takeProfitUnit != null || stopLossUnit != null)
		{
				StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
				return;

		UpdateAtrState(atrValue);
		UpdateHeikinAshiState(candle, ref _baseHasCurrent, ref _baseHasPrevious, ref _baseCurrentHa, ref _basePreviousHa);
		TryPlaceOrders(candle);
	}

	private void ProcessHigherCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
				return;

		UpdateHeikinAshiState(candle, ref _higherHasCurrent, ref _higherHasPrevious, ref _higherCurrentHa, ref _higherPreviousHa);
	}

	private void ProcessCloseAllCandle(ICandleMessage candle)
	{
		if (!UseCloseAllOnNewBar || candle.State != CandleStates.Finished)
				return;

		if (_lastCloseAllTime == candle.OpenTime)
				return;

		_lastCloseAllTime = candle.OpenTime;

		CancelTrackedOrders();

		if (Position != 0m)
				ClosePosition();
	}

	private void UpdateAtrState(decimal atrValue)
	{
		if (!_atr.IsFormed)
				return;

		if (_hasAtrValue)
		{
				_prevAtrValue = _lastAtrValue;
				_hasPrevAtrValue = true;
		}

		_lastAtrValue = atrValue;
		_hasAtrValue = true;
	}

	private void UpdateHeikinAshiState(ICandleMessage candle, ref bool hasCurrent, ref bool hasPrevious, ref HeikinAshiCandle current, ref HeikinAshiCandle previous)
	{
		var hadCurrent = hasCurrent;
		var last = current;

		var haOpen = hadCurrent ? (last.Open + last.Close) / 2m : (candle.OpenPrice + candle.ClosePrice) / 2m;
		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haHigh = Math.Max(Math.Max(candle.HighPrice, haOpen), haClose);
		var haLow = Math.Min(Math.Min(candle.LowPrice, haOpen), haClose);

		var haCandle = new HeikinAshiCandle(haOpen, haHigh, haLow, haClose);

		if (hadCurrent)
		{
				previous = last;
				hasPrevious = true;
		}

		current = haCandle;
		hasCurrent = true;
	}

	private void TryPlaceOrders(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
				return;

		if (DistancePoints <= 0m || StopLossPoints < 0m || TakeProfitPoints < 0m)
				return;

		var timeOfDay = candle.OpenTime.TimeOfDay;
		if (!IsWithinTradeHours(timeOfDay))
				return;

		if (!_baseHasPrevious || !_higherHasPrevious)
				return;

		if (UseAtrFilter)
		{
				if (!_hasPrevAtrValue || _lastAtrValue <= _prevAtrValue)
						return;
		}

		UpdateOrderReferences();

		var longSignal = IsHeikinBullishBreakout(_baseCurrentHa, _basePreviousHa) && IsHeikinBullishBreakout(_higherCurrentHa, _higherPreviousHa);
		var shortSignal = IsHeikinBearishBreakout(_baseCurrentHa, _basePreviousHa) && IsHeikinBearishBreakout(_higherCurrentHa, _higherPreviousHa);

		var offset = DistancePoints * _priceStep;

		if (longSignal && Position <= 0m)
		{
				if (_sellOrder != null && _sellOrder.State == OrderStates.Active)
				{
						CancelOrder(_sellOrder);
						_sellOrder = null;
				}

				if (_buyOrder == null || _buyOrder.State != OrderStates.Active)
				{
						var price = candle.ClosePrice - offset;

						if (price <= 0m)
								price = _priceStep;

						var volume = Volume + (Position < 0m ? Math.Abs(Position) : 0m);

						if (volume > 0m)
								_buyOrder = BuyLimit(price, volume);
				}
		}
		else if (shortSignal && Position >= 0m)
		{
				if (_buyOrder != null && _buyOrder.State == OrderStates.Active)
				{
						CancelOrder(_buyOrder);
						_buyOrder = null;
				}

				if (_sellOrder == null || _sellOrder.State != OrderStates.Active)
				{
						var price = candle.ClosePrice + offset;
						var volume = Volume + (Position > 0m ? Math.Abs(Position) : 0m);

						if (volume > 0m)
								_sellOrder = SellLimit(price, volume);
				}
		}
	}

	private void UpdateOrderReferences()
	{
		if (_buyOrder != null && _buyOrder.State != OrderStates.Active)
				_buyOrder = null;

		if (_sellOrder != null && _sellOrder.State != OrderStates.Active)
				_sellOrder = null;
	}

	private void CancelTrackedOrders()
	{
		if (_buyOrder != null)
		{
				if (_buyOrder.State == OrderStates.Active)
						CancelOrder(_buyOrder);

				_buyOrder = null;
		}

		if (_sellOrder != null)
		{
				if (_sellOrder.State == OrderStates.Active)
						CancelOrder(_sellOrder);

				_sellOrder = null;
		}
	}

	private bool IsWithinTradeHours(TimeSpan time)
	{
		var start = TimeSpan.FromHours(StartHour);
		var end = TimeSpan.FromHours(EndHour);

		if (end < start)
				return time >= start || time <= end;

		return time >= start && time <= end;
	}

	private bool IsHeikinBullishBreakout(HeikinAshiCandle current, HeikinAshiCandle previous)
	{
		return IsBullish(current) && HasNoLowerShadow(current) && IsBullish(previous) && HasLowerShadow(previous);
	}

	private bool IsHeikinBearishBreakout(HeikinAshiCandle current, HeikinAshiCandle previous)
	{
		return IsBearish(current) && HasNoUpperShadow(current) && IsBearish(previous) && HasUpperShadow(previous);
	}

	private static bool IsBullish(HeikinAshiCandle candle)
	{
		return candle.Close > candle.Open;
	}

	private static bool IsBearish(HeikinAshiCandle candle)
	{
		return candle.Close < candle.Open;
	}

	private bool HasNoLowerShadow(HeikinAshiCandle candle)
	{
		return Math.Abs(candle.Open - candle.Low) <= _comparisonTolerance;
	}

	private bool HasLowerShadow(HeikinAshiCandle candle)
	{
		return Math.Abs(candle.Open - candle.Low) > _comparisonTolerance;
	}

	private bool HasNoUpperShadow(HeikinAshiCandle candle)
	{
		return Math.Abs(candle.Open - candle.High) <= _comparisonTolerance;
	}

	private bool HasUpperShadow(HeikinAshiCandle candle)
	{
		return Math.Abs(candle.Open - candle.High) > _comparisonTolerance;
	}

	private readonly struct HeikinAshiCandle
	{
		public HeikinAshiCandle(decimal open, decimal high, decimal low, decimal close)
		{
				Open = open;
				High = high;
				Low = low;
				Close = close;
		}

		public decimal Open { get; }

		public decimal High { get; }

		public decimal Low { get; }

		public decimal Close { get; }
	}
}
