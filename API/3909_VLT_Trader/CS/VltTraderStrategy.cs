using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility breakout straddle strategy converted from the MetaTrader 4 "VLT_TRADER" expert advisor.
/// </summary>
public class VltTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _entryOffsetPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _lookbackCandles;
	private readonly StrategyParam<DataType> _candleType;

	private Lowest _rangeLowest = null!;
	private decimal? _previousRange;
	private decimal? _previousHigh;
	private decimal? _previousLow;
	private Order _buyStopOrder;
	private Order _sellStopOrder;

	/// <summary>
	/// Initializes a new instance of <see cref="VltTraderStrategy"/>.
	/// </summary>
	public VltTraderStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Lot size used when placing pending orders", "Trading")
			.SetCanOptimize(true);

		_entryOffsetPoints = Param(nameof(EntryOffsetPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Entry Offset (points)", "Distance added to the previous high/low when placing the stop orders", "Trading")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Take profit distance applied to both long and short trades", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (points)", "Protective stop distance attached to each pending order", "Risk")
			.SetCanOptimize(true);

		_lookbackCandles = Param(nameof(LookbackCandles), 8)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Candles", "Number of earlier candles used to measure historical volatility", "Volatility")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for volatility calculations", "Data");
	}

	/// <summary>
	/// Lot size used for pending orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Offset in points added to the previous high or low when placing the stops.
	/// </summary>
	public int EntryOffsetPoints
	{
		get => _entryOffsetPoints.Value;
		set => _entryOffsetPoints.Value = value;
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
	/// Stop loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Number of candles used when searching for the smallest historical range.
	/// </summary>
	public int LookbackCandles
	{
		get => _lookbackCandles.Value;
		set => _lookbackCandles.Value = value;
	}

	/// <summary>
	/// Candle data type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security security, DataType dataType)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rangeLowest = null!;
		_previousRange = null;
		_previousHigh = null;
		_previousLow = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rangeLowest = new Lowest
		{
			Length = Math.Max(1, LookbackCandles)
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_rangeLowest.Length != Math.Max(1, LookbackCandles))
			_rangeLowest.Length = Math.Max(1, LookbackCandles);

		if (_previousRange is decimal previousRange &&
			_previousHigh is decimal previousHigh &&
			_previousLow is decimal previousLow &&
			_rangeLowest.IsFormed)
		{
			var minHistoricalRange = _rangeLowest.GetCurrentValue<decimal>();

			if (previousRange < minHistoricalRange)
				TryPlacePendingOrders(previousHigh, previousLow);
		}

		if (_previousRange is decimal range)
		{
			_rangeLowest.Process(new DecimalIndicatorValue(_rangeLowest, range, candle.OpenTime));
		}

		_previousRange = candle.HighPrice - candle.LowPrice;
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}

	private void TryPlacePendingOrders(decimal previousHigh, decimal previousLow)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return;

		var volume = NormalizeVolume(OrderVolume);
		if (volume <= 0m)
			return;

		var offset = EntryOffsetPoints * step;
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		if (offset <= 0m)
			return;

		if (!HasActiveBuySide())
		{
			var entryPrice = previousHigh + offset;
			if (entryPrice > 0m)
			{
				var stopLoss = StopLossPoints > 0 ? entryPrice - stopDistance : null;
				var takeProfit = TakeProfitPoints > 0 ? entryPrice + takeDistance : null;

				var order = BuyStop(volume, entryPrice, stopLoss: stopLoss, takeProfit: takeProfit);
				if (order != null)
					_buyStopOrder = order;
			}
		}

		if (!HasActiveSellSide())
		{
			var entryPrice = previousLow - offset;
			if (entryPrice > 0m)
			{
				var stopLoss = StopLossPoints > 0 ? entryPrice + stopDistance : null;
				var takeProfit = TakeProfitPoints > 0 ? entryPrice - takeDistance : null;

				var order = SellStop(volume, entryPrice, stopLoss: stopLoss, takeProfit: takeProfit);
				if (order != null)
					_sellStopOrder = order;
			}
		}
	}

	private bool HasActiveBuySide()
	{
		return Position > 0m || IsOrderActive(_buyStopOrder);
	}

	private bool HasActiveSellSide()
	{
		return Position < 0m || IsOrderActive(_sellStopOrder);
	}

	private static bool IsOrderActive(Order order)
	{
		return order != null && order.State.IsActive();
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security == null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			volume = Math.Round(volume / step) * step;
		}

		var minVolume = Security.MinVolume;
		if (minVolume.HasValue && volume < minVolume.Value)
			volume = minVolume.Value;

		var maxVolume = Security.MaxVolume;
		if (maxVolume.HasValue && volume > maxVolume.Value)
			volume = maxVolume.Value;

		if (volume <= 0m && minVolume.HasValue)
			volume = minVolume.Value;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == _buyStopOrder)
			_buyStopOrder = null;
		else if (trade.Order == _sellStopOrder)
			_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == _buyStopOrder && !order.State.IsActive())
			_buyStopOrder = null;
		else if (order == _sellStopOrder && !order.State.IsActive())
			_sellStopOrder = null;
	}
}
