using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Get trend strategy converted from MetaTrader 4.
/// </summary>
public class GetTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _m15CandleType;
	private readonly StrategyParam<DataType> _h1CandleType;
	private readonly StrategyParam<int> _thresholdPoints;
	private readonly StrategyParam<int> _m15MaPeriod;
	private readonly StrategyParam<int> _h1MaPeriod;
	private readonly StrategyParam<int> _slowStochasticPeriod;
	private readonly StrategyParam<int> _fastStochasticPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _tradeVolume;

	private decimal? _maM15Value;
	private decimal? _maH1Value;
	private decimal? _h1Close;
	private decimal? _prevFastStochastic;
	private decimal? _entryPrice;

	private Order? _stopLossOrder;
	private Order? _takeProfitOrder;

	/// <summary>
	/// Initializes a new instance of <see cref="GetTrendStrategy"/>.
	/// </summary>
	public GetTrendStrategy()
	{
		_m15CandleType = Param(nameof(M15CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("M15 Candle Type", "Primary timeframe used for signals", "Data");

		_h1CandleType = Param(nameof(H1CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("H1 Candle Type", "Higher timeframe used for confirmation", "Data");

		_thresholdPoints = Param(nameof(ThresholdPoints), 50)
			.SetNotNegative()
			.SetDisplay("Threshold (points)", "Maximum distance between price and the slow MA", "Signals");

		_m15MaPeriod = Param(nameof(M15MaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("M15 SMMA Period", "Length of the smoothed moving average on M15", "Indicators");

		_h1MaPeriod = Param(nameof(H1MaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("H1 SMMA Period", "Length of the smoothed moving average on H1", "Indicators");

		_slowStochasticPeriod = Param(nameof(SlowStochasticPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow Stochastic Period", "%K length used for the signal %D line", "Indicators");

		_fastStochasticPeriod = Param(nameof(FastStochasticPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast Stochastic Period", "%K length used for the main stochastic", "Indicators");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 570m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Take-profit distance expressed in price steps", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Stop-loss distance expressed in price steps", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 200m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (points)", "Trailing distance applied after the trade is in profit", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume submitted with each market order", "Trading");
	}

	/// <summary>
	/// Main timeframe processed by the strategy.
	/// </summary>
	public DataType M15CandleType
	{
		get => _m15CandleType.Value;
		set => _m15CandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for trend confirmation.
	/// </summary>
	public DataType H1CandleType
	{
		get => _h1CandleType.Value;
		set => _h1CandleType.Value = value;
	}

	/// <summary>
	/// Maximum distance between price and the slow M15 moving average expressed in points.
	/// </summary>
	public int ThresholdPoints
	{
		get => _thresholdPoints.Value;
		set => _thresholdPoints.Value = value;
	}

	/// <summary>
	/// Smoothed moving average period calculated on the M15 candles.
	/// </summary>
	public int M15MaPeriod
	{
		get => _m15MaPeriod.Value;
		set => _m15MaPeriod.Value = value;
	}

	/// <summary>
	/// Smoothed moving average period calculated on the H1 candles.
	/// </summary>
	public int H1MaPeriod
	{
		get => _h1MaPeriod.Value;
		set => _h1MaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the stochastic oscillator used as the slow %D signal.
	/// </summary>
	public int SlowStochasticPeriod
	{
		get => _slowStochasticPeriod.Value;
		set => _slowStochasticPeriod.Value = value;
	}

	/// <summary>
	/// Period of the stochastic oscillator used as the fast %K line.
	/// </summary>
	public int FastStochasticPeriod
	{
		get => _fastStochasticPeriod.Value;
		set => _fastStochasticPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing distance applied once the trade becomes profitable.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Volume submitted with each market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, M15CandleType), (Security, H1CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_maM15Value = null;
		_maH1Value = null;
		_h1Close = null;
		_prevFastStochastic = null;
		_entryPrice = null;

		CancelProtectionOrders();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		// Configure smoothed moving averages that replicate the MT4 setup.
		var maM15 = new SmoothedMovingAverage
		{
			Length = M15MaPeriod,
			CandlePrice = CandlePrice.Median,
		};

		var maH1 = new SmoothedMovingAverage
		{
			Length = H1MaPeriod,
			CandlePrice = CandlePrice.Median,
		};

		// The MT4 version uses two stochastic oscillators with different base lengths.
		var stochFast = new StochasticOscillator
		{
			Length = FastStochasticPeriod,
			K = { Length = 3 },
			D = { Length = 3 },
		};

		var stochSlow = new StochasticOscillator
		{
			Length = SlowStochasticPeriod,
			K = { Length = 3 },
			D = { Length = 3 },
		};

		var m15Subscription = SubscribeCandles(M15CandleType);
		m15Subscription
			.BindEx(maM15, stochFast, stochSlow, ProcessM15Candle)
			.Start();

		var h1Subscription = SubscribeCandles(H1CandleType);
		h1Subscription
			.BindEx(maH1, ProcessH1Candle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, m15Subscription);
			DrawIndicator(area, maM15);
			DrawIndicator(area, stochFast);
		}
	}

	private void ProcessH1Candle(ICandleMessage candle, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFinal)
			return;

		_maH1Value = maValue.GetValue<decimal>();
		_h1Close = candle.ClosePrice;
	}

	private void ProcessM15Candle(
		ICandleMessage candle,
		IIndicatorValue maValue,
		IIndicatorValue fastValue,
		IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFinal || !fastValue.IsFinal || !slowValue.IsFinal)
			return;

		var ma = maValue.GetValue<decimal>();
		var fast = ((StochasticOscillatorValue)fastValue).K;
		var slow = ((StochasticOscillatorValue)slowValue).D;

		_maM15Value = ma;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFastStochastic = fast;
			return;
		}

		if (_maH1Value.HasValue && _h1Close.HasValue)
			EvaluateEntrySignals(candle, ma, fast, slow);

		ManageTrailing(candle);

		_prevFastStochastic = fast;
	}

	private void EvaluateEntrySignals(ICandleMessage candle, decimal maM15, decimal fast, decimal slow)
	{
		var maH1 = _maH1Value!.Value;
		var priceH1 = _h1Close!.Value;
		var priceM15 = candle.ClosePrice;

		var step = GetPriceStep();
		var threshold = ThresholdPoints * step;

		var fastCrossUp = _prevFastStochastic.HasValue && _prevFastStochastic.Value < slow && fast > slow;
		var fastCrossDown = _prevFastStochastic.HasValue && _prevFastStochastic.Value > slow && fast < slow;

		var wantBuy = priceM15 < maM15 && priceH1 < maH1 && maM15 - priceM15 <= threshold;
		var wantSell = priceM15 > maM15 && priceH1 > maH1 && priceM15 - maM15 <= threshold;

		if (wantBuy && slow < 20m && fast < 20m && fastCrossUp)
			TryEnterLong(candle);
		else if (wantSell && slow > 80m && fast > 80m && fastCrossDown)
			TryEnterShort(candle);
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		var baseVolume = TradeVolume;
		var oppositeVolume = Position < 0m ? Math.Abs(Position) : 0m;
		var totalVolume = baseVolume + oppositeVolume;

		if (totalVolume <= 0m)
			return;

		var resultingPosition = Position + totalVolume;

		// Close the opposite position first by buying the required size.
		BuyMarket(totalVolume);

		_entryPrice = resultingPosition > 0m ? candle.ClosePrice : null;

		ResetProtectionOrders();

		var protectiveVolume = Math.Abs(resultingPosition);
		if (protectiveVolume > 0m)
			PlaceProtectionOrders(true, candle.ClosePrice, protectiveVolume);
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		var baseVolume = TradeVolume;
		var oppositeVolume = Position > 0m ? Position : 0m;
		var totalVolume = baseVolume + oppositeVolume;

		if (totalVolume <= 0m)
			return;

		var resultingPosition = Position - totalVolume;

		// Close the opposite position first by selling the required size.
		SellMarket(totalVolume);

		_entryPrice = resultingPosition < 0m ? candle.ClosePrice : null;

		ResetProtectionOrders();

		var protectiveVolume = Math.Abs(resultingPosition);
		if (protectiveVolume > 0m)
			PlaceProtectionOrders(false, candle.ClosePrice, protectiveVolume);
	}

	private void ManageTrailing(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			CancelProtectionOrders();
			_entryPrice = null;
			return;
		}

		if (_entryPrice is null)
			return;

		if (TrailingStopPoints <= 0m)
			return;

		var step = GetPriceStep();
		var trailingDistance = TrailingStopPoints * step;

		if (Position > 0m)
		{
			var profit = candle.ClosePrice - _entryPrice.Value;
			if (profit < trailingDistance)
				return;

			var newStop = candle.ClosePrice - trailingDistance;
			UpdateStopOrder(true, newStop);
		}
		else if (Position < 0m)
		{
			var profit = _entryPrice.Value - candle.ClosePrice;
			if (profit < trailingDistance)
				return;

			var newStop = candle.ClosePrice + trailingDistance;
			UpdateStopOrder(false, newStop);
		}
	}

	private void UpdateStopOrder(bool isLong, decimal newStop)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			volume = TradeVolume;

		if (_stopLossOrder == null || _stopLossOrder.State != OrderStates.Active)
		{
			_stopLossOrder = isLong ? SellStop(volume, newStop) : BuyStop(volume, newStop);
			return;
		}

		if ((isLong && _stopLossOrder.Price >= newStop) || (!isLong && _stopLossOrder.Price <= newStop))
			return;

		CancelOrder(_stopLossOrder);
		_stopLossOrder = isLong ? SellStop(volume, newStop) : BuyStop(volume, newStop);
	}

	private void PlaceProtectionOrders(bool isLong, decimal price, decimal volume)
	{
		var step = GetPriceStep();
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		if (stopDistance > 0m)
		{
			var stopPrice = isLong ? price - stopDistance : price + stopDistance;
			_stopLossOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
		}

		if (takeDistance > 0m)
		{
			var takePrice = isLong ? price + takeDistance : price - takeDistance;
			_takeProfitOrder = isLong ? SellLimit(volume, takePrice) : BuyLimit(volume, takePrice);
		}
	}

	private void ResetProtectionOrders()
	{
		CancelProtectionOrders();
		_stopLossOrder = null;
		_takeProfitOrder = null;
	}

	private void CancelProtectionOrders()
	{
		CancelIfActive(ref _stopLossOrder);
		CancelIfActive(ref _takeProfitOrder);
	}

	private void CancelIfActive(ref Order? order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private decimal GetPriceStep()
	{
		return Security.PriceStep ?? 1m;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(Position position)
	{
		base.OnPositionChanged(position);

		if (position.Security != Security)
			return;

		if (position.CurrentValue == 0m)
		{
			CancelProtectionOrders();
			_entryPrice = null;
		}
		else if (position.AveragePrice != null)
		{
			_entryPrice = position.AveragePrice;
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelProtectionOrders();
		base.OnStopped();
	}
}
