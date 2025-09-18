using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "Binario_3" expert that trades breakouts of dual 144-period EMAs.
/// Places stop orders around the envelopes and manages stops, take profits and trailing exits.
/// </summary>
public class Binario3Strategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _pipDifferencePoints;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage? _emaHigh;
	private ExponentialMovingAverage? _emaLow;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;

	private decimal? _bestBid;
	private decimal? _bestAsk;

	private decimal? _pendingBuyStopLoss;
	private decimal? _pendingBuyTakeProfit;
	private decimal? _pendingSellStopLoss;
	private decimal? _pendingSellTakeProfit;

	private decimal? _longStopLevel;
	private decimal? _longTakeProfitLevel;
	private decimal? _longTrailingLevel;
	private decimal? _shortStopLevel;
	private decimal? _shortTakeProfitLevel;
	private decimal? _shortTrailingLevel;

	private decimal _previousPosition;

	public decimal TakeProfit
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal TrailingStop
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	public decimal PipDifference
	{
		get => _pipDifferencePoints.Value;
		set => _pipDifferencePoints.Value = value;
	}

	public decimal Lots
	{
		get => _lots.Value;
		set => _lots.Value = value;
	}

	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public Binario3Strategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfit), 850m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Target distance in points added to the breakout side.", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStop), 850m)
			.SetGreaterThanOrEqualToZero()
			.SetDisplay("Trailing Stop (points)", "Distance in points used for trailing exits.", "Risk");

		_pipDifferencePoints = Param(nameof(PipDifference), 25m)
			.SetGreaterThanOrEqualToZero()
			.SetDisplay("Offset (points)", "Extra distance added above/below the EMAs before placing pending orders.", "Entries");

		_lots = Param(nameof(Lots), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Size", "Fallback trade volume used when automatic sizing cannot be calculated.", "Volume");

		_maximumRisk = Param(nameof(MaximumRisk), 10m)
			.SetGreaterThanOrEqualToZero()
			.SetDisplay("Maximum Risk", "Risk factor copied from the original EA (used for auto lot sizing).", "Volume");

		_emaPeriod = Param(nameof(EmaPeriod), 144)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of the high/low exponential moving averages.", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series used to drive the strategy.", "General");
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

		_buyStopOrder = null;
		_sellStopOrder = null;
		_bestBid = null;
		_bestAsk = null;
		_pendingBuyStopLoss = null;
		_pendingBuyTakeProfit = null;
		_pendingSellStopLoss = null;
		_pendingSellTakeProfit = null;
		_longStopLevel = null;
		_longTakeProfitLevel = null;
		_longTrailingLevel = null;
		_shortStopLevel = null;
		_shortTakeProfitLevel = null;
		_shortTrailingLevel = null;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaHigh = new ExponentialMovingAverage { Length = EmaPeriod };
		_emaLow = new ExponentialMovingAverage { Length = EmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var bid = level1.TryGetDecimal(Level1Fields.BestBidPrice);
		if (bid != null)
			_bestBid = bid;

		var ask = level1.TryGetDecimal(Level1Fields.BestAskPrice);
		if (ask != null)
			_bestAsk = ask;

		ManageActivePosition();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_emaHigh == null || _emaLow == null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var highValue = _emaHigh.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
		var lowValue = _emaLow.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();

		if (!_emaHigh.IsFormed || !_emaLow.IsFormed)
			return;

		var step = GetPriceStep();
		if (step <= 0m)
			return;

		var ask = _bestAsk ?? Security?.BestAskPrice ?? candle.ClosePrice;
		var bid = _bestBid ?? Security?.BestBidPrice ?? candle.ClosePrice;

		if (ask <= 0m || bid <= 0m)
			return;

		var spread = Math.Max(0m, ask - bid);
		var offset = PipDifference * step;
		var totalTarget = (PipDifference + TakeProfit) * step;

		var buyPrice = NormalizePrice(highValue + spread + offset, step);
		var buyStopLoss = NormalizePrice(lowValue - step, step);
		var buyTakeProfit = NormalizePrice(highValue + totalTarget, step);

		var sellPrice = NormalizePrice(lowValue - offset, step);
		var sellStopLoss = NormalizePrice(highValue + spread + step, step);
		var sellTakeProfit = NormalizePrice(lowValue - spread - totalTarget, step);

		var closePrice = candle.ClosePrice;

		if (closePrice < highValue && closePrice > lowValue)
		{
			var volume = GetTradeVolume();
			if (volume <= 0m)
				return;

			UpdatePendingOrder(ref _buyStopOrder, buyPrice, volume, isBuy: true);
			UpdatePendingOrder(ref _sellStopOrder, sellPrice, volume, isBuy: false);

			_pendingBuyStopLoss = buyStopLoss;
			_pendingBuyTakeProfit = buyTakeProfit > 0m ? buyTakeProfit : null;
			_pendingSellStopLoss = sellStopLoss;
			_pendingSellTakeProfit = sellTakeProfit > 0m ? sellTakeProfit : null;
		}
		else
		{
			CancelPendingOrders();
		}
	}

	private void ManageActivePosition()
	{
		var currentPosition = Position;

		if (currentPosition > 0m)
		{
			if (_previousPosition <= 0m)
			{
				CancelOrderIfActive(ref _sellStopOrder);
				_longStopLevel = _pendingBuyStopLoss;
				_longTakeProfitLevel = _pendingBuyTakeProfit;
				_longTrailingLevel = null;
			}

			HandleLongPosition();
		}
		else if (currentPosition < 0m)
		{
			if (_previousPosition >= 0m)
			{
				CancelOrderIfActive(ref _buyStopOrder);
				_shortStopLevel = _pendingSellStopLoss;
				_shortTakeProfitLevel = _pendingSellTakeProfit;
				_shortTrailingLevel = null;
			}

			HandleShortPosition();
		}
		else
		{
			if (_previousPosition != 0m)
				ResetProtectionLevels();
		}

		_previousPosition = currentPosition;
	}

	private void HandleLongPosition()
	{
		var bid = _bestBid ?? Security?.BestBidPrice;
		if (bid == null || bid <= 0m)
			return;

		if (_longTakeProfitLevel.HasValue && bid >= _longTakeProfitLevel.Value)
		{
			ClosePosition();
			ResetProtectionLevels();
			return;
		}

		if (_longStopLevel.HasValue && bid <= _longStopLevel.Value)
		{
			ClosePosition();
			ResetProtectionLevels();
			return;
		}

		var step = GetPriceStep();
		if (TrailingStop <= 0m || step <= 0m)
		{
			_longTrailingLevel = null;
			return;
		}

		var trailingDistance = TrailingStop * step;
		var entryPrice = Position.AveragePrice;

		if (entryPrice > 0m && bid - entryPrice > trailingDistance)
		{
			var candidate = bid.Value - trailingDistance;
			if (!_longTrailingLevel.HasValue || candidate > _longTrailingLevel.Value)
				_longTrailingLevel = candidate;
		}

		if (_longTrailingLevel.HasValue && bid <= _longTrailingLevel.Value)
		{
			ClosePosition();
			ResetProtectionLevels();
		}
	}

	private void HandleShortPosition()
	{
		var ask = _bestAsk ?? Security?.BestAskPrice;
		if (ask == null || ask <= 0m)
			return;

		if (_shortTakeProfitLevel.HasValue && ask <= _shortTakeProfitLevel.Value)
		{
			ClosePosition();
			ResetProtectionLevels();
			return;
		}

		if (_shortStopLevel.HasValue && ask >= _shortStopLevel.Value)
		{
			ClosePosition();
			ResetProtectionLevels();
			return;
		}

		var step = GetPriceStep();
		if (TrailingStop <= 0m || step <= 0m)
		{
			_shortTrailingLevel = null;
			return;
		}

		var trailingDistance = TrailingStop * step;
		var entryPrice = Position.AveragePrice;

		if (entryPrice > 0m && entryPrice - ask > trailingDistance)
		{
			var candidate = ask.Value + trailingDistance;
			if (!_shortTrailingLevel.HasValue || candidate < _shortTrailingLevel.Value)
				_shortTrailingLevel = candidate;
		}

		if (_shortTrailingLevel.HasValue && ask >= _shortTrailingLevel.Value)
		{
			ClosePosition();
			ResetProtectionLevels();
		}
	}

	private void UpdatePendingOrder(ref Order? order, decimal price, decimal volume, bool isBuy)
	{
		if (price <= 0m || volume <= 0m)
		{
			CancelOrderIfActive(ref order);
			return;
		}

		var normalizedPrice = NormalizePrice(price, GetPriceStep());

		if (order != null)
		{
			if (order.Price == normalizedPrice && order.Volume == volume && order.State.IsActive())
				return;

			CancelOrderIfActive(ref order);
		}

		order = isBuy
			? BuyStop(volume, normalizedPrice)
			: SellStop(volume, normalizedPrice);
	}

	private void CancelPendingOrders()
	{
		CancelOrderIfActive(ref _buyStopOrder);
		CancelOrderIfActive(ref _sellStopOrder);
	}

	private void CancelOrderIfActive(ref Order? order)
	{
		if (order == null)
			return;

		if (order.State.IsActive())
			CancelOrder(order);

		order = null;
	}

	private void ResetProtectionLevels()
	{
		_longStopLevel = null;
		_longTakeProfitLevel = null;
		_longTrailingLevel = null;
		_shortStopLevel = null;
		_shortTakeProfitLevel = null;
		_shortTrailingLevel = null;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = Security?.MinPriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;
		return step;
	}

	private decimal NormalizePrice(decimal price, decimal step)
	{
		if (step <= 0m)
			return price;

		return Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
	}

	private decimal GetTradeVolume()
	{
		var volume = Lots;

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
		if (balance.HasValue && balance.Value > 0m && MaximumRisk > 0m)
		{
			var riskVolume = balance.Value * MaximumRisk / 50000m;
			if (riskVolume > 0m)
				volume = Math.Max(volume, riskVolume);
		}

		return Math.Max(0m, volume);
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null || order.Security != Security)
			return;

		if (_buyStopOrder != null && order == _buyStopOrder && !_buyStopOrder.State.IsActive())
			_buyStopOrder = null;

		if (_sellStopOrder != null && order == _sellStopOrder && !_sellStopOrder.State.IsActive())
			_sellStopOrder = null;
	}
}
