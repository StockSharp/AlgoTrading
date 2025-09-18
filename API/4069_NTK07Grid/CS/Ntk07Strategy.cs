using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the NTK_07 MetaTrader strategy using the StockSharp high level API.
/// </summary>
public class Ntk07Strategy : Strategy
{
	/// <summary>
	/// Money management modes supported by the strategy.
	/// </summary>
	public enum MoneyManagementMode
	{
		/// <summary>
		/// Use the <see cref="InitialLot"/> and <see cref="LotLimit"/> values without modification.
		/// </summary>
		Fixed,

		/// <summary>
		/// Recalculate the starting volume from the portfolio balance and distribute it across the grid.
		/// </summary>
		BalanceBased,

		/// <summary>
		/// Keep the starting volume fixed and project the theoretical maximum volume using <see cref="Multiplier"/>.
		/// </summary>
		Progressive
	}

	private readonly StrategyParam<int> _netStepPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<bool> _trailProfit;
	private readonly StrategyParam<MoneyManagementMode> _moneyManagementMode;
	private readonly StrategyParam<decimal> _initialLot;
	private readonly StrategyParam<decimal> _lotLimit;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _percentRisk;
	private readonly StrategyParam<decimal> _minCapital;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<int> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _useMovingAverage;
	private readonly StrategyParam<int> _movingAverageLength;
	private readonly StrategyParam<int> _movingAverageShift;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<bool> _useChannelCenter;
	private readonly StrategyParam<decimal> _lotRoundingFactor;
	private readonly StrategyParam<decimal> _priceRoundingFactor;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Highest _highest = new();
	private readonly Lowest _lowest = new();
	private readonly SimpleMovingAverage _sma = new();

	private readonly Queue<decimal> _maBuffer = new();
	private readonly List<PositionLot> _longLots = new();
	private readonly List<PositionLot> _shortLots = new();

	private Order? _primaryBuyStop;
	private Order? _primarySellStop;
	private Order? _scalingBuyStop;
	private Order? _scalingSellStop;
	private Order? _longStopOrder;
	private Order? _longTakeProfitOrder;
	private Order? _shortStopOrder;
	private Order? _shortTakeProfitOrder;

	private decimal _pipSize;
	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _channelHigh;
	private decimal _channelLow;
	private bool _channelReady;
	private decimal? _shiftedMovingAverage;
	private decimal _lastBuyVolume;
	private decimal _lastSellVolume;
	private decimal _lastBuyPrice;
	private decimal _lastSellPrice;
	private decimal _calculatedBaseVolume;
	private decimal _calculatedLotLimit;
	private bool _pendingUpdateRequired;

	/// <summary>
	/// Initializes a new instance of the <see cref="Ntk07Strategy"/> class.
	/// </summary>
	public Ntk07Strategy()
	{
		_netStepPips = Param(nameof(NetStepPips), 23)
		.SetGreaterThanZero()
		.SetDisplay("Net Step (pips)", "Distance between grid levels", "Entries")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 115)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 300)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Take profit distance", "Risk")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 75)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_multiplier = Param(nameof(Multiplier), 1.7m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Multiplier", "Multiplier applied to each additional grid level", "Money Management")
		.SetCanOptimize(true);

		_trailProfit = Param(nameof(TrailProfit), true)
		.SetDisplay("Trail Profit", "Extend take-profit while trailing", "Risk");

		_moneyManagementMode = Param(nameof(ManagementMode), MoneyManagementMode.Progressive)
		.SetDisplay("Money Management", "How lot sizes are recalculated", "Money Management");

		_initialLot = Param(nameof(InitialLot), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Lot", "Base order volume", "Money Management")
		.SetCanOptimize(true);

		_lotLimit = Param(nameof(LotLimit), 7m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Limit", "Maximum allowed volume for a single grid level", "Money Management");

		_maxTrades = Param(nameof(MaxTrades), 4)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades", "Maximum grid depth", "Money Management")
		.SetCanOptimize(true);

		_percentRisk = Param(nameof(PercentRisk), 10m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Percent Risk", "Percent of balance used to size the grid (mode BalanceBased)", "Money Management");

		_minCapital = Param(nameof(MinCapital), 5000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Minimum Capital", "Minimum free capital required before trading", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), false)
		.SetDisplay("Use BreakEven", "Move stop to break-even after a configurable profit", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5)
		.SetGreaterOrEqualZero()
		.SetDisplay("BreakEven Offset (pips)", "Distance required before moving to break-even", "Risk");

		_useMovingAverage = Param(nameof(UseMovingAverageFilter), false)
		.SetDisplay("Use Moving Average", "Reduce trailing distance using a moving average filter", "Filters");

		_movingAverageLength = Param(nameof(MovingAverageLength), 100)
		.SetGreaterThanZero()
		.SetDisplay("MA Length", "Moving average length for trailing adjustments", "Filters");

		_movingAverageShift = Param(nameof(MovingAverageShift), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("MA Shift", "Number of completed candles used as a shift", "Filters");

		_startHour = Param(nameof(StartHour), 0)
		.SetRange(0, 23)
		.SetDisplay("Start Hour", "Earliest trading hour (inclusive)", "Schedule");

		_endHour = Param(nameof(EndHour), 24)
		.SetRange(0, 24)
		.SetDisplay("End Hour", "Latest trading hour (inclusive)", "Schedule");

		_channelPeriod = Param(nameof(ChannelPeriod), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("Channel Period", "Number of candles used to confirm breakouts", "Entries");

		_useChannelCenter = Param(nameof(UseChannelCenter), false)
		.SetDisplay("Use Channel Center", "Wait for price to touch the channel midpoint", "Entries");

		_lotRoundingFactor = Param(nameof(LotRoundingFactor), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Rounding Factor", "Divider used to round order volumes", "Money Management");

		_priceRoundingFactor = Param(nameof(PriceRoundingFactor), 10000m)
		.SetGreaterThanZero()
		.SetDisplay("Price Rounding Factor", "Divider used when computing the breakeven stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Working timeframe", "General");
	}

	/// <summary>
	/// Distance between grid levels expressed in pips.
	/// </summary>
	public int NetStepPips
	{
		get => _netStepPips.Value;
		set => _netStepPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied to each additional grid level.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Enable extending take-profit while trailing the stop.
	/// </summary>
	public bool TrailProfit
	{
		get => _trailProfit.Value;
		set => _trailProfit.Value = value;
	}

	/// <summary>
	/// Selected money management mode.
	/// </summary>
	public MoneyManagementMode ManagementMode
	{
		get => _moneyManagementMode.Value;
		set => _moneyManagementMode.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal InitialLot
	{
		get => _initialLot.Value;
		set => _initialLot.Value = value;
	}

	/// <summary>
	/// Maximum allowed volume for a single grid level.
	/// </summary>
	public decimal LotLimit
	{
		get => _lotLimit.Value;
		set => _lotLimit.Value = value;
	}

	/// <summary>
	/// Maximum number of grid levels.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Percentage of balance used to size the grid when balance based mode is enabled.
	/// </summary>
	public decimal PercentRisk
	{
		get => _percentRisk.Value;
		set => _percentRisk.Value = value;
	}

	/// <summary>
	/// Minimum free capital required before trading starts.
	/// </summary>
	public decimal MinCapital
	{
		get => _minCapital.Value;
		set => _minCapital.Value = value;
	}

	/// <summary>
	/// Enable automatic move to break-even after a profit threshold is reached.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Required profit expressed in pips before the stop is moved to break-even.
	/// </summary>
	public int BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Enables the moving average filter for trailing stops.
	/// </summary>
	public bool UseMovingAverageFilter
	{
		get => _useMovingAverage.Value;
		set => _useMovingAverage.Value = value;
	}

	/// <summary>
	/// Moving average length used by the trailing filter.
	/// </summary>
	public int MovingAverageLength
	{
		get => _movingAverageLength.Value;
		set => _movingAverageLength.Value = value;
	}

	/// <summary>
	/// Number of completed candles used as a shift for the moving average value.
	/// </summary>
	public int MovingAverageShift
	{
		get => _movingAverageShift.Value;
		set => _movingAverageShift.Value = value;
	}

	/// <summary>
	/// Earliest trading hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Latest trading hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Number of candles used to confirm breakout entries.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Use the channel midpoint as an additional filter.
	/// </summary>
	public bool UseChannelCenter
	{
		get => _useChannelCenter.Value;
		set => _useChannelCenter.Value = value;
	}

	/// <summary>
	/// Divider used to round order volumes.
	/// </summary>
	public decimal LotRoundingFactor
	{
		get => _lotRoundingFactor.Value;
		set => _lotRoundingFactor.Value = value;
	}

	/// <summary>
	/// Divider used to round the break-even price.
	/// </summary>
	public decimal PriceRoundingFactor
	{
		get => _priceRoundingFactor.Value;
		set => _priceRoundingFactor.Value = value;
	}

	/// <summary>
	/// Working candle type.
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

		_longLots.Clear();
		_shortLots.Clear();
		_maBuffer.Clear();

		_primaryBuyStop = null;
		_primarySellStop = null;
		_scalingBuyStop = null;
		_scalingSellStop = null;
		_longStopOrder = null;
		_longTakeProfitOrder = null;
		_shortStopOrder = null;
		_shortTakeProfitOrder = null;

		_bestBid = 0m;
		_bestAsk = 0m;
		_channelHigh = 0m;
		_channelLow = 0m;
		_channelReady = false;
		_shiftedMovingAverage = null;
		_lastBuyVolume = 0m;
		_lastSellVolume = 0m;
		_lastBuyPrice = 0m;
		_lastSellPrice = 0m;
		_calculatedBaseVolume = 0m;
		_calculatedLotLimit = 0m;
		_pendingUpdateRequired = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = ResolvePipSize();
		_calculatedBaseVolume = Math.Max(InitialLot, 0m);
		_calculatedLotLimit = Math.Max(LotLimit, 0m);

		_highest.Length = Math.Max(ChannelPeriod, 1);
		_lowest.Length = Math.Max(ChannelPeriod, 1);
		_sma.Length = Math.Max(MovingAverageLength, 1);

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(ProcessCandle)
		.Start();

		SubscribeOrderBook()
		.Bind(ProcessOrderBook)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			if (UseMovingAverageFilter)
			{
				DrawIndicator(area, _sma);
			}
			DrawOwnTrades(area);
		}
	}

	private void ProcessOrderBook(QuoteChangeMessage depth)
	{
		var bestBid = depth.GetBestBid();
		if (bestBid != null)
		_bestBid = bestBid.Price;

		var bestAsk = depth.GetBestAsk();
		if (bestAsk != null)
		_bestAsk = bestAsk.Price;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateChannel(candle);
		UpdateMovingAverage(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		RecalculateMoneyManagement();
		CleanupInactiveOrders();

		var time = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		if (time == default)
		return;

		if (time.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
		return;

		if (!IsWithinTradingHours(time))
		return;

		if (!HasRequiredCapital())
		return;

		var referenceBid = _bestBid > 0m ? _bestBid : candle.ClosePrice;
		var referenceAsk = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;

		if (_pipSize <= 0m || referenceBid <= 0m || referenceAsk <= 0m)
		return;

		if (_pendingUpdateRequired || !HasActivePendingOrders())
		{
			TryPlaceInitialOrders(referenceBid, referenceAsk);
			_pendingUpdateRequired = false;
		}

		ManageScalingOrders(referenceBid, referenceAsk);
		ManageTrailing(candle, referenceBid, referenceAsk);
	}

	private void UpdateChannel(ICandleMessage candle)
	{
		if (ChannelPeriod <= 0)
		{
			_channelReady = false;
			return;
		}

		_highest.Length = Math.Max(ChannelPeriod, 1);
		_lowest.Length = Math.Max(ChannelPeriod, 1);

		var highValue = _highest.Process(candle.HighPrice, candle.OpenTime, true);
		var lowValue = _lowest.Process(candle.LowPrice, candle.OpenTime, true);

		if (!highValue.IsFinal || !lowValue.IsFinal)
		{
			_channelReady = false;
			return;
		}

		_channelHigh = highValue.ToDecimal();
		_channelLow = lowValue.ToDecimal();
		_channelReady = true;
	}

	private void UpdateMovingAverage(ICandleMessage candle)
	{
		if (!UseMovingAverageFilter)
		{
			_shiftedMovingAverage = null;
			_maBuffer.Clear();
			return;
		}

		_sma.Length = Math.Max(MovingAverageLength, 1);
		var value = _sma.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		if (!value.IsFinal)
		return;

		var maValue = value.ToDecimal();
		_maBuffer.Enqueue(maValue);

		var maxSize = Math.Max(MovingAverageShift + 1, 1);
		while (_maBuffer.Count > maxSize)
		_maBuffer.Dequeue();

		var index = 0;
		var targetIndex = _maBuffer.Count - 1 - MovingAverageShift;
		if (targetIndex < 0)
		targetIndex = 0;

		decimal? shifted = null;
		foreach (var val in _maBuffer)
		{
			if (index == targetIndex)
			{
				shifted = val;
				break;
			}
			index++;
		}

		_shiftedMovingAverage = shifted ?? maValue;
	}

	private void RecalculateMoneyManagement()
	{
		var mode = ManagementMode;
		var lot = Math.Max(InitialLot, 0m);
		var limit = Math.Max(LotLimit, 0m);

		switch (mode)
		{
		case MoneyManagementMode.Fixed:
			{
				_calculatedBaseVolume = lot;
				_calculatedLotLimit = limit;
				break;
			}
		case MoneyManagementMode.BalanceBased:
			{
				var balance = Portfolio?.CurrentValue ?? 0m;
				if (balance <= 0m)
				{
					_calculatedBaseVolume = lot;
					_calculatedLotLimit = limit;
					break;
				}

				var factor = balance / 1000m * (PercentRisk / 100m);
				var rounded = (decimal)Math.Ceiling((double)factor);
				var lotLimit = rounded;

				for (var i = 0; i < MaxTrades; i++)
				{
					if (lotLimit <= 0m)
					break;
					lotLimit /= Multiplier;
				}

				var baseVolume = RoundVolume(Math.Max(lotLimit, lot));
				_calculatedBaseVolume = baseVolume;
				_calculatedLotLimit = Math.Max(rounded, baseVolume);
				break;
			}
		case MoneyManagementMode.Progressive:
			{
				var projected = lot;
				for (var i = 0; i < MaxTrades; i++)
				{
					projected *= Multiplier;
				}
				_calculatedBaseVolume = lot;
				_calculatedLotLimit = RoundVolume(Math.Max(projected, limit));
				break;
			}
		}
	}

	private void TryPlaceInitialOrders(decimal referenceBid, decimal referenceAsk)
	{
		if (Position != 0m)
		return;

		if (HasActiveOrder(_primaryBuyStop) || HasActiveOrder(_primarySellStop))
		return;

		if (!ShouldPlaceInitialOrders(referenceBid, referenceAsk))
		return;

		var volume = RoundVolume(_calculatedBaseVolume);
		if (volume <= 0m)
		return;

		var stepDistance = NetStepPips * _pipSize;
		if (stepDistance <= 0m)
		return;

		var buyPrice = NormalizePrice(referenceAsk + stepDistance);
		var sellPrice = NormalizePrice(referenceBid - stepDistance);

		_primaryBuyStop = BuyStop(volume: volume, price: buyPrice);
		_primarySellStop = SellStop(volume: volume, price: sellPrice);
	}

	private bool ShouldPlaceInitialOrders(decimal referenceBid, decimal referenceAsk)
	{
		if (ChannelPeriod <= 0 || !_channelReady)
		return true;

		var high = _channelHigh;
		var low = _channelLow;
		if (high <= 0m || low <= 0m || high <= low)
		return false;

		if (!UseChannelCenter)
		{
			return referenceAsk >= high || referenceBid <= low;
		}

		var mid = (high + low) / 2m;
		var tolerance = _pipSize;
		return Math.Abs(referenceAsk - mid) <= tolerance || Math.Abs(referenceBid - mid) <= tolerance;
	}

	private void ManageScalingOrders(decimal referenceBid, decimal referenceAsk)
	{
		var stepDistance = NetStepPips * _pipSize;
		if (stepDistance <= 0m)
		return;

		var longVolume = GetTotalVolume(_longLots);
		var shortVolume = GetTotalVolume(_shortLots);

		if (longVolume > 0m)
		{
			var nextVolume = RoundVolume(_lastBuyVolume * Multiplier);
			if (_lastBuyVolume <= 0m)
			nextVolume = RoundVolume(_calculatedBaseVolume * Multiplier);

			if (nextVolume > 0m && nextVolume <= _calculatedLotLimit)
			{
				var desiredPrice = NormalizePrice(_lastBuyPrice + stepDistance);
				EnsureScalingOrder(ref _scalingBuyStop, isLong: true, volume: nextVolume, price: desiredPrice);
			}
			else
			{
				CancelIfActive(_scalingBuyStop);
			}
		}
		else
		{
			_lastBuyVolume = 0m;
			_lastBuyPrice = 0m;
			CancelIfActive(_scalingBuyStop);
		}

		if (shortVolume > 0m)
		{
			var nextVolume = RoundVolume(_lastSellVolume * Multiplier);
			if (_lastSellVolume <= 0m)
			nextVolume = RoundVolume(_calculatedBaseVolume * Multiplier);

			if (nextVolume > 0m && nextVolume <= _calculatedLotLimit)
			{
				var desiredPrice = NormalizePrice(_lastSellPrice - stepDistance);
				EnsureScalingOrder(ref _scalingSellStop, isLong: false, volume: nextVolume, price: desiredPrice);
			}
			else
			{
				CancelIfActive(_scalingSellStop);
			}
		}
		else
		{
			_lastSellVolume = 0m;
			_lastSellPrice = 0m;
			CancelIfActive(_scalingSellStop);
		}
	}

	private void ManageTrailing(ICandleMessage candle, decimal referenceBid, decimal referenceAsk)
	{
		var longVolume = GetTotalVolume(_longLots);
		var shortVolume = GetTotalVolume(_shortLots);

		var stopLossDistance = StopLossPips * _pipSize;
		var takeProfitDistance = TakeProfitPips * _pipSize;
		var trailingDistance = TrailingStopPips * _pipSize;
		var breakEvenDistance = BreakEvenOffsetPips * _pipSize;

		if (longVolume > 0m)
		{
			var entryPrice = GetWeightedAveragePrice(_longLots);
			var stopPrice = entryPrice - stopLossDistance;
			var takePrice = entryPrice + takeProfitDistance;

			if (trailingDistance > 0m && (_lastBuyVolume <= 0m || RoundVolume(_lastBuyVolume * Multiplier) > _calculatedLotLimit))
			{
				var high = candle.HighPrice;
				var trailingCandidate = high - trailingDistance;

				if (UseMovingAverageFilter && _shiftedMovingAverage.HasValue && referenceAsk < _shiftedMovingAverage.Value)
				{
					trailingDistance /= 2m;
					trailingCandidate = high - trailingDistance;
				}

				if (trailingCandidate > stopPrice)
				stopPrice = trailingCandidate;

				if (TrailProfit && takeProfitDistance > 0m)
				{
					takePrice = high + takeProfitDistance;
				}
			}

			if (UseBreakEven && breakEvenDistance > 0m && referenceAsk - _lastBuyPrice >= breakEvenDistance)
			{
				var average = RoundPrice(GetWeightedAveragePrice(_longLots));
				if (average > stopPrice)
				stopPrice = average;
			}

			UpdateProtectionOrders(isLong: true, volume: longVolume, stopPrice: stopPrice, takePrice: takePrice);
		}
		else
		{
			CancelIfActive(_longStopOrder);
			CancelIfActive(_longTakeProfitOrder);
		}

		if (shortVolume > 0m)
		{
			var entryPrice = GetWeightedAveragePrice(_shortLots);
			var stopPrice = entryPrice + stopLossDistance;
			var takePrice = entryPrice - takeProfitDistance;

			if (trailingDistance > 0m && (_lastSellVolume <= 0m || RoundVolume(_lastSellVolume * Multiplier) > _calculatedLotLimit))
			{
				var low = candle.LowPrice;
				var trailingCandidate = low + trailingDistance;

				if (UseMovingAverageFilter && _shiftedMovingAverage.HasValue && referenceBid > _shiftedMovingAverage.Value)
				{
					trailingDistance /= 2m;
					trailingCandidate = low + trailingDistance;
				}

				if (trailingCandidate < stopPrice)
				stopPrice = trailingCandidate;

				if (TrailProfit && takeProfitDistance > 0m)
				{
					takePrice = low - takeProfitDistance;
				}
			}

			if (UseBreakEven && breakEvenDistance > 0m && _lastSellPrice - referenceBid >= breakEvenDistance)
			{
				var average = RoundPrice(GetWeightedAveragePrice(_shortLots));
				if (average < stopPrice)
				stopPrice = average;
			}

			UpdateProtectionOrders(isLong: false, volume: shortVolume, stopPrice: stopPrice, takePrice: takePrice);
		}
		else
		{
			CancelIfActive(_shortStopOrder);
			CancelIfActive(_shortTakeProfitOrder);
		}
	}

	private void EnsureScalingOrder(ref Order? order, bool isLong, decimal volume, decimal price)
	{
		if (volume <= 0m)
		{
			CancelIfActive(order);
			return;
		}

		if (order != null && order.State == OrderStates.Active)
		{
			if (order.Price == price && order.Volume == volume)
			return;

			CancelOrder(order);
		}

		order = isLong
		? BuyStop(volume: volume, price: price)
		: SellStop(volume: volume, price: price);
	}

	private void UpdateProtectionOrders(bool isLong, decimal volume, decimal stopPrice, decimal takePrice)
	{
		if (volume <= 0m)
		{
			if (isLong)
			{
				CancelIfActive(_longStopOrder);
				CancelIfActive(_longTakeProfitOrder);
			}
			else
			{
				CancelIfActive(_shortStopOrder);
				CancelIfActive(_shortTakeProfitOrder);
			}
			return;
		}

		var normalizedStop = NormalizePrice(stopPrice);
		var normalizedTake = NormalizePrice(takePrice);

		if (isLong)
		{
			if (_longStopOrder != null && _longStopOrder.State == OrderStates.Active)
			{
				if (_longStopOrder.Price != normalizedStop || _longStopOrder.Volume != volume)
				CancelOrder(_longStopOrder);
			}

			if (_longTakeProfitOrder != null && _longTakeProfitOrder.State == OrderStates.Active)
			{
				if (_longTakeProfitOrder.Price != normalizedTake || _longTakeProfitOrder.Volume != volume)
				CancelOrder(_longTakeProfitOrder);
			}

			if (_longStopOrder == null || _longStopOrder.State != OrderStates.Active)
			_longStopOrder = SellStop(volume: volume, price: normalizedStop);

			if (TakeProfitPips > 0 && (_longTakeProfitOrder == null || _longTakeProfitOrder.State != OrderStates.Active))
			_longTakeProfitOrder = SellLimit(volume: volume, price: normalizedTake);
			else if (TakeProfitPips <= 0)
			CancelIfActive(_longTakeProfitOrder);
		}
		else
		{
			if (_shortStopOrder != null && _shortStopOrder.State == OrderStates.Active)
			{
				if (_shortStopOrder.Price != normalizedStop || _shortStopOrder.Volume != volume)
				CancelOrder(_shortStopOrder);
			}

			if (_shortTakeProfitOrder != null && _shortTakeProfitOrder.State == OrderStates.Active)
			{
				if (_shortTakeProfitOrder.Price != normalizedTake || _shortTakeProfitOrder.Volume != volume)
				CancelOrder(_shortTakeProfitOrder);
			}

			if (_shortStopOrder == null || _shortStopOrder.State != OrderStates.Active)
			_shortStopOrder = BuyStop(volume: volume, price: normalizedStop);

			if (TakeProfitPips > 0 && (_shortTakeProfitOrder == null || _shortTakeProfitOrder.State != OrderStates.Active))
			_shortTakeProfitOrder = BuyLimit(volume: volume, price: normalizedTake);
			else if (TakeProfitPips <= 0)
			CancelIfActive(_shortTakeProfitOrder);
		}
	}

	private void CleanupInactiveOrders()
	{
		CleanupInactiveOrder(ref _primaryBuyStop);
		CleanupInactiveOrder(ref _primarySellStop);
		CleanupInactiveOrder(ref _scalingBuyStop);
		CleanupInactiveOrder(ref _scalingSellStop);
		CleanupInactiveOrder(ref _longStopOrder);
		CleanupInactiveOrder(ref _longTakeProfitOrder);
		CleanupInactiveOrder(ref _shortStopOrder);
		CleanupInactiveOrder(ref _shortTakeProfitOrder);
	}

	private void CleanupInactiveOrder(ref Order? order)
	{
		if (order == null)
		return;

		if (!order.State.IsActive())
		order = null;
	}

	private bool HasActiveOrder(Order? order)
	{
		return order != null && order.State.IsActive();
	}

	private bool HasActivePendingOrders()
	{
		if (HasActiveOrder(_primaryBuyStop) || HasActiveOrder(_primarySellStop) || HasActiveOrder(_scalingBuyStop) || HasActiveOrder(_scalingSellStop))
		return true;

		foreach (var order in Orders)
		{
			if ((order.Type == OrderTypes.Limit || order.Type == OrderTypes.Stop) && order.State.IsActive())
			return true;
		}

		return false;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var start = Math.Clamp(StartHour, 0, 23);
		var end = Math.Clamp(EndHour, 0, 24);
		if (start > end)
		return false;

		var hour = time.Hour;
		return hour >= start && hour <= end;
	}

	private bool HasRequiredCapital()
	{
		var capital = Portfolio?.CurrentValue;
		if (capital == null)
		return true;

		return capital.Value >= MinCapital;
	}

	private decimal RoundVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var factor = LotRoundingFactor;
		if (factor <= 0m)
		return volume;

		return Math.Round(volume * factor) / factor;
	}

	private decimal RoundPrice(decimal price)
	{
		var factor = PriceRoundingFactor;
		if (factor <= 0m)
		return price;

		return Math.Round(price * factor) / factor;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
		return price;

		return security.ShrinkPrice(price);
	}

	private decimal ResolvePipSize()
	{
		var security = Security;
		if (security == null)
		return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		var decimals = security.Decimals;
		if (decimals >= 3)
		return step * 10m;

		return step > 0m ? step : 0.0001m;
	}

	private decimal GetTotalVolume(List<PositionLot> lots)
	{
		var total = 0m;
		foreach (var lot in lots)
		total += lot.Volume;
		return total;
	}

	private decimal GetWeightedAveragePrice(List<PositionLot> lots)
	{
		var totalVolume = 0m;
		var totalValue = 0m;
		foreach (var lot in lots)
		{
			totalVolume += lot.Volume;
			totalValue += lot.Volume * lot.Price;
		}

		if (totalVolume <= 0m)
		return 0m;

		return totalValue / totalVolume;
	}

	private void CancelIfActive(Order? order)
	{
		if (order != null && order.State.IsActive())
		CancelOrder(order);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade?.Order;
		if (order == null)
		return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;
		if (volume <= 0m)
		return;

		if (order.Side == Sides.Buy)
		{
			var remaining = ReducePositions(_shortLots, volume);
			if (remaining > 0m)
			_longLots.Add(new PositionLot(price, remaining));

			_lastBuyVolume = volume;
			_lastBuyPrice = price;

			if (order == _primaryBuyStop)
			_primaryBuyStop = null;
			if (order == _scalingBuyStop)
			_scalingBuyStop = null;

			if (HasActiveOrder(_primarySellStop))
			CancelOrder(_primarySellStop);
			if (HasActiveOrder(_scalingSellStop))
			CancelOrder(_scalingSellStop);
		}
		else if (order.Side == Sides.Sell)
		{
			var remaining = ReducePositions(_longLots, volume);
			if (remaining > 0m)
			_shortLots.Add(new PositionLot(price, remaining));

			_lastSellVolume = volume;
			_lastSellPrice = price;

			if (order == _primarySellStop)
			_primarySellStop = null;
			if (order == _scalingSellStop)
			_scalingSellStop = null;

			if (HasActiveOrder(_primaryBuyStop))
			CancelOrder(_primaryBuyStop);
			if (HasActiveOrder(_scalingBuyStop))
			CancelOrder(_scalingBuyStop);
		}

		_pendingUpdateRequired = true;
	}

	private decimal ReducePositions(List<PositionLot> positions, decimal volume)
	{
		var index = 0;
		while (index < positions.Count && volume > 0m)
		{
			var lot = positions[index];
			if (lot.Volume > volume)
			{
				lot.Volume -= volume;
				volume = 0m;
				break;
			}

			volume -= lot.Volume;
			positions.RemoveAt(index);
		}

		return volume;
	}

	private sealed class PositionLot
	{
		public PositionLot(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }
		public decimal Volume { get; set; }
	}
}