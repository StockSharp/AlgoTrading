using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger band pending order grid with trailing stop management.
/// Converts the Bands 2 MetaTrader strategy to StockSharp high level API.
/// Places symmetric buy/sell stop orders around the Bollinger envelope when price trades inside the channel.
/// </summary>
public class BandsPendingBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _hourStart;
	private readonly StrategyParam<int> _hourEnd;
	private readonly StrategyParam<StopLossMode> _stopLossMode;
	private readonly StrategyParam<decimal> _firstTakeProfitPips;
	private readonly StrategyParam<decimal> _secondTakeProfitPips;
	private readonly StrategyParam<decimal> _thirdTakeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _stepPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<AppliedPrice> _maPriceType;
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<int> _bandsShift;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<AppliedPrice> _bandsPriceType;

	private IIndicator _maIndicator = new SimpleMovingAverage();
	private BollingerBands _bollinger = new();

	private readonly Queue<decimal> _maBuffer = new();
	private readonly Queue<decimal> _middleBuffer = new();
	private readonly Queue<decimal> _upperBuffer = new();
	private readonly Queue<decimal> _lowerBuffer = new();

	private readonly List<EntryOrderInfo> _entryOrders = new();

	private Order? _stopOrder;
	private Order? _takeOrder;
	private decimal? _currentStopPrice;
	private decimal? _currentTakePrice;
	private bool _isLongPosition;
	private decimal? _entryPrice;

	private decimal _pipSize;
	private decimal _stepOffset;
	private decimal _firstTakeOffset;
	private decimal _secondTakeOffset;
	private decimal _thirdTakeOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;

	/// <summary>
	/// Volume of every pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
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
	/// Hour to start placing pending orders.
	/// </summary>
	public int HourStart
	{
		get => _hourStart.Value;
		set => _hourStart.Value = value;
	}

	/// <summary>
	/// Hour to stop placing pending orders.
	/// </summary>
	public int HourEnd
	{
		get => _hourEnd.Value;
		set => _hourEnd.Value = value;
	}

	/// <summary>
	/// Stop loss placement mode.
	/// </summary>
	public StopLossMode StopLossMode
	{
		get => _stopLossMode.Value;
		set => _stopLossMode.Value = value;
	}

	/// <summary>
	/// Take profit distance for the first order in pips.
	/// </summary>
	public decimal FirstTakeProfitPips
	{
		get => _firstTakeProfitPips.Value;
		set => _firstTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Take profit distance for the second order in pips.
	/// </summary>
	public decimal SecondTakeProfitPips
	{
		get => _secondTakeProfitPips.Value;
		set => _secondTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Take profit distance for the third order in pips.
	/// </summary>
	public decimal ThirdTakeProfitPips
	{
		get => _thirdTakeProfitPips.Value;
		set => _thirdTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop size in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips before adjusting the stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Distance between stacked pending orders in pips.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
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
	/// Forward shift for the moving average.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Applied price for the moving average.
	/// </summary>
	public AppliedPrice MaPriceType
	{
		get => _maPriceType.Value;
		set => _maPriceType.Value = value;
	}

	/// <summary>
	/// Bollinger bands averaging period.
	/// </summary>
	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift for the Bollinger bands.
	/// </summary>
	public int BandsShift
	{
		get => _bandsShift.Value;
		set => _bandsShift.Value = value;
	}

	/// <summary>
	/// Bollinger band deviation multiplier.
	/// </summary>
	public decimal BandsDeviation
	{
		get => _bandsDeviation.Value;
		set => _bandsDeviation.Value = value;
	}

	/// <summary>
	/// Applied price for Bollinger bands.
	/// </summary>
	public AppliedPrice BandsPriceType
	{
		get => _bandsPriceType.Value;
		set => _bandsPriceType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BandsPendingBreakoutStrategy"/>.
	/// </summary>
	public BandsPendingBreakoutStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Volume per pending order", "General")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for analysis", "General");

		_hourStart = Param(nameof(HourStart), 4)
			.SetDisplay("Start Hour", "Hour to begin placing orders", "Session")
			.SetRange(0, 23);

		_hourEnd = Param(nameof(HourEnd), 18)
			.SetDisplay("End Hour", "Hour to stop placing orders", "Session")
			.SetRange(1, 24);

		_stopLossMode = Param(nameof(StopLossMode), StopLossMode.BollingerBands)
			.SetDisplay("Stop Loss Mode", "How stops are calculated", "Risk");

		_firstTakeProfitPips = Param(nameof(FirstTakeProfitPips), 21m)
			.SetDisplay("First TP", "Take profit for the first order in pips", "Risk")
			.SetGreaterOrEqualZero();

		_secondTakeProfitPips = Param(nameof(SecondTakeProfitPips), 34m)
			.SetDisplay("Second TP", "Take profit for the second order in pips", "Risk")
			.SetGreaterOrEqualZero();

		_thirdTakeProfitPips = Param(nameof(ThirdTakeProfitPips), 55m)
			.SetDisplay("Third TP", "Take profit for the third order in pips", "Risk")
			.SetGreaterOrEqualZero();

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
			.SetGreaterOrEqualZero();

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step", "Additional distance required before trailing", "Risk")
			.SetGreaterOrEqualZero();

		_stepPips = Param(nameof(StepPips), 15m)
			.SetDisplay("Grid Step", "Distance between pending orders in pips", "Orders")
			.SetGreaterThanZero();

		_maPeriod = Param(nameof(MaPeriod), 15)
			.SetDisplay("MA Period", "Moving average length", "Moving Average")
			.SetGreaterThanZero();

		_maShift = Param(nameof(MaShift), 3)
			.SetDisplay("MA Shift", "Forward shift for the moving average", "Moving Average")
			.SetGreaterOrEqualZero();

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Exponential)
			.SetDisplay("MA Method", "Moving average calculation method", "Moving Average");

		_maPriceType = Param(nameof(MaPriceType), AppliedPrice.Close)
			.SetDisplay("MA Price", "Applied price for the moving average", "Moving Average");

		_bandsPeriod = Param(nameof(BandsPeriod), 15)
			.SetDisplay("Bands Period", "Bollinger average length", "Bollinger Bands")
			.SetGreaterThanZero();

		_bandsShift = Param(nameof(BandsShift), 0)
			.SetDisplay("Bands Shift", "Forward shift for the Bollinger bands", "Bollinger Bands")
			.SetGreaterOrEqualZero();

		_bandsDeviation = Param(nameof(BandsDeviation), 2m)
			.SetDisplay("Bands Deviation", "Standard deviation multiplier", "Bollinger Bands")
			.SetGreaterThanZero();

		_bandsPriceType = Param(nameof(BandsPriceType), AppliedPrice.Close)
			.SetDisplay("Bands Price", "Applied price for Bollinger bands", "Bollinger Bands");
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

		_maBuffer.Clear();
		_middleBuffer.Clear();
		_upperBuffer.Clear();
		_lowerBuffer.Clear();
		_entryOrders.Clear();

		_stopOrder = null;
		_takeOrder = null;
		_currentStopPrice = null;
		_currentTakePrice = null;
		_isLongPosition = false;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (HourEnd <= HourStart)
			throw new InvalidOperationException("HourEnd must be greater than HourStart.");

		_maIndicator = CreateMovingAverage(MaMethod, MaPeriod);
		_bollinger = new BollingerBands { Length = BandsPeriod, Width = BandsDeviation };

		_maBuffer.Clear();
		_middleBuffer.Clear();
		_upperBuffer.Clear();
		_lowerBuffer.Clear();

		_pipSize = CalculatePipSize();
		_stepOffset = StepPips * _pipSize;
		_firstTakeOffset = FirstTakeProfitPips * _pipSize;
		_secondTakeOffset = SecondTakeProfitPips * _pipSize;
		_thirdTakeOffset = ThirdTakeProfitPips * _pipSize;
		_trailingStopOffset = TrailingStopPips * _pipSize;
		_trailingStepOffset = TrailingStepPips * _pipSize;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			if (_maIndicator is Indicator indicator)
				DrawIndicator(area, indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailing(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		CancelPendingOrders();

		var openHour = candle.OpenTime.Hour;
		if (openHour < HourStart || openHour >= HourEnd)
			return;

		var maInput = GetAppliedPrice(candle, MaPriceType);
		var maValue = _maIndicator.Process(new DecimalIndicatorValue(_maIndicator, maInput, candle.OpenTime)).ToNullableDecimal();

		decimal? shiftedMa = null;
		if (maValue is decimal maDecimal)
			shiftedMa = UpdateBuffer(_maBuffer, maDecimal, MaShift);

		var bandInput = GetAppliedPrice(candle, BandsPriceType);
		var bandsValue = (BollingerBandsValue)_bollinger.Process(new DecimalIndicatorValue(_bollinger, bandInput, candle.OpenTime));

		if (bandsValue.UpBand is not decimal upBand ||
			bandsValue.LowBand is not decimal lowBand ||
			bandsValue.MovingAverage is not decimal middleBand)
		{
			return;
		}

		var shiftedUpper = UpdateBuffer(_upperBuffer, upBand, BandsShift);
		var shiftedLower = UpdateBuffer(_lowerBuffer, lowBand, BandsShift);
		var shiftedMiddle = UpdateBuffer(_middleBuffer, middleBand, BandsShift);

		if (shiftedUpper is null || shiftedLower is null || shiftedMiddle is null)
			return;

		if (StopLossMode == StopLossMode.MovingAverage && shiftedMa is null)
			return;

		var closePrice = candle.ClosePrice;
		if (closePrice >= shiftedUpper.Value || closePrice <= shiftedLower.Value)
			return;

		PlaceEntryOrders(candle, shiftedUpper.Value, shiftedLower.Value, shiftedMiddle.Value, shiftedMa);
	}

	private void PlaceEntryOrders(ICandleMessage candle, decimal upperBand, decimal lowerBand, decimal middleBand, decimal? movingAverage)
	{
		if (OrderVolume <= 0m)
			return;

		for (var index = 0; index < 3; index++)
		{
			var offset = _stepOffset * index;

			var buyPrice = NormalizePrice(upperBand + offset);
			var sellPrice = NormalizePrice(lowerBand - offset);

			if (buyPrice <= 0m || sellPrice <= 0m)
				continue;

			decimal? buyStopLoss = null;
			decimal? sellStopLoss = null;

			switch (StopLossMode)
			{
				case StopLossMode.BollingerBands:
					buyStopLoss = NormalizePrice(lowerBand + offset);
					sellStopLoss = NormalizePrice(upperBand - offset);
					break;
				case StopLossMode.MovingAverage:
					if (movingAverage is null)
						return;

					buyStopLoss = NormalizePrice(movingAverage.Value + offset);
					sellStopLoss = NormalizePrice(movingAverage.Value - offset);
					break;
			}

			decimal? buyTakeProfit = index switch
			{
				0 when _firstTakeOffset > 0m => NormalizePrice(buyPrice + _firstTakeOffset),
				1 when _secondTakeOffset > 0m => NormalizePrice(buyPrice + _secondTakeOffset),
				2 when _thirdTakeOffset > 0m => NormalizePrice(buyPrice + _thirdTakeOffset),
				_ => null
			};

			decimal? sellTakeProfit = index switch
			{
				0 when _firstTakeOffset > 0m => NormalizePrice(sellPrice - _firstTakeOffset),
				1 when _secondTakeOffset > 0m => NormalizePrice(sellPrice - _firstTakeOffset),
				2 when _thirdTakeOffset > 0m => NormalizePrice(sellPrice - _firstTakeOffset),
				_ => null
			};

			var buyOrder = BuyStop(OrderVolume, buyPrice);
			if (buyOrder != null)
			{
				_entryOrders.Add(new EntryOrderInfo
				{
					Order = buyOrder,
					IsLong = true,
					StopPrice = buyStopLoss,
					TakeProfitPrice = buyTakeProfit
				});
			}

			var sellOrder = SellStop(OrderVolume, sellPrice);
			if (sellOrder != null)
			{
				_entryOrders.Add(new EntryOrderInfo
				{
					Order = sellOrder,
					IsLong = false,
					StopPrice = sellStopLoss,
					TakeProfitPrice = sellTakeProfit
				});
			}
		}
	}

	private void CancelPendingOrders()
	{
		for (var i = _entryOrders.Count - 1; i >= 0; i--)
		{
			var entry = _entryOrders[i];
			if (entry.Order != null && entry.Order.State == OrderStates.Active)
				CancelOrder(entry.Order);
		}

		_entryOrders.Clear();
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (_trailingStopOffset <= 0m || _trailingStepOffset <= 0m)
			return;

		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
			return;

		var entryPrice = PositionPrice ?? _entryPrice;
		if (entryPrice is null or <= 0m)
			return;

		var currentPrice = candle.ClosePrice;

		if (_isLongPosition && Position > 0)
		{
			var profitDistance = currentPrice - entryPrice.Value;
			if (profitDistance <= _trailingStopOffset + _trailingStepOffset)
				return;

			var triggerLevel = currentPrice - (_trailingStopOffset + _trailingStepOffset);
			if (_currentStopPrice.HasValue && _currentStopPrice.Value >= triggerLevel)
				return;

			var newStop = NormalizePrice(currentPrice - _trailingStopOffset);
			UpdateStopOrder(newStop, positionVolume, true);
		}
		else if (!_isLongPosition && Position < 0)
		{
			var profitDistance = entryPrice.Value - currentPrice;
			if (profitDistance <= _trailingStopOffset + _trailingStepOffset)
				return;

			var triggerLevel = currentPrice + (_trailingStopOffset + _trailingStepOffset);
			if (_currentStopPrice.HasValue && _currentStopPrice.Value <= triggerLevel && _currentStopPrice.Value != 0m)
				return;

			var newStop = NormalizePrice(currentPrice + _trailingStopOffset);
			UpdateStopOrder(newStop, positionVolume, false);
		}
	}

	private void UpdateStopOrder(decimal newStopPrice, decimal volume, bool isLong)
	{
		if (newStopPrice <= 0m)
			return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (_currentStopPrice.HasValue && priceStep > 0m)
		{
			var difference = Math.Abs(_currentStopPrice.Value - newStopPrice);
			if (difference < priceStep / 2m)
				return;
		}

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = isLong
			? SellStop(volume, newStopPrice)
			: BuyStop(volume, newStopPrice);

		_currentStopPrice = newStopPrice;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		var index = _entryOrders.FindIndex(e => e.Order == trade.Order);
		if (index >= 0)
		{
			var info = _entryOrders[index];
			CancelOtherEntryOrders(info.Order);
			_entryOrders.RemoveAt(index);

			_isLongPosition = info.IsLong;
			_entryPrice = PositionPrice ?? trade.Trade.Price;

			SetupProtection(info.IsLong, info.StopPrice, info.TakeProfitPrice);

			return;
		}

		if (_stopOrder != null && trade.Order == _stopOrder)
		{
			_stopOrder = null;
			_currentStopPrice = null;
		}
		else if (_takeOrder != null && trade.Order == _takeOrder)
		{
			_takeOrder = null;
			_currentTakePrice = null;
		}
	}

	private void CancelOtherEntryOrders(Order executedOrder)
	{
		for (var i = _entryOrders.Count - 1; i >= 0; i--)
		{
			var entry = _entryOrders[i];
			if (entry.Order == executedOrder)
				continue;

			if (entry.Order != null && entry.Order.State == OrderStates.Active)
				CancelOrder(entry.Order);

			_entryOrders.RemoveAt(i);
		}
	}

	private void SetupProtection(bool isLong, decimal? stopPrice, decimal? takeProfitPrice)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);

		_stopOrder = null;
		_takeOrder = null;
		_currentStopPrice = null;
		_currentTakePrice = null;

		if (stopPrice is decimal sl && sl > 0m)
		{
			sl = NormalizePrice(sl);
			_stopOrder = isLong ? SellStop(volume, sl) : BuyStop(volume, sl);
			_currentStopPrice = sl;
		}

		if (takeProfitPrice is decimal tp && tp > 0m)
		{
			tp = NormalizePrice(tp);
			_takeOrder = isLong ? SellLimit(volume, tp) : BuyLimit(volume, tp);
			_currentTakePrice = tp;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
			return;

		_entryPrice = null;
		_isLongPosition = false;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);
		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);

		_stopOrder = null;
		_takeOrder = null;
		_currentStopPrice = null;
		_currentTakePrice = null;
	}

	private static decimal? UpdateBuffer(Queue<decimal> buffer, decimal value, int shift)
	{
		buffer.Enqueue(value);
		var required = shift + 1;
		while (buffer.Count > required)
			buffer.Dequeue();

		if (buffer.Count < required)
			return null;

		return buffer.Peek();
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step is null or 0m)
			return price;

		return Math.Round(price / step.Value, MidpointRounding.AwayFromZero) * step.Value;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			return 1m;

		var step = priceStep;
		var decimals = 0;
		while (step < 1m && decimals < 10)
		{
			step *= 10m;
			decimals++;
		}

		return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice priceType)
	{
		return priceType switch
		{
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.LinearWeighted => new LinearWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private sealed class EntryOrderInfo
	{
		public required Order Order { get; init; }
		public required bool IsLong { get; init; }
		public decimal? StopPrice { get; init; }
		public decimal? TakeProfitPrice { get; init; }
	}
}

public enum StopLossMode
{
	/// <summary>
	/// Place stops at the opposite Bollinger band plus the configured step.
	/// </summary>
	BollingerBands,

	/// <summary>
	/// Place stops around the moving average line.
	/// </summary>
	MovingAverage,

	/// <summary>
	/// Do not set an initial stop loss.
	/// </summary>
	None
}

public enum MovingAverageMethod
{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted
}

public enum AppliedPrice
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}
