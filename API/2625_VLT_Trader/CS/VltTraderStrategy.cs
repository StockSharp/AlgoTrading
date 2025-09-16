using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility contraction breakout strategy converted from the VLT_TRADER MQL version.
/// Places stop orders when the latest candle range is the smallest within the recent history.
/// </summary>
public class VltTraderStrategy : Strategy
{
	private const decimal BreakoutBufferPips = 10m;

	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _maxCandleSizePips;
	private readonly StrategyParam<int> _candleCount;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _takeProfitOffset;
	private decimal _stopLossOffset;
	private decimal _maxRange;

	private readonly Queue<decimal> _rangeHistory = new();

	private decimal? _lastFinishedRange;
	private decimal? _lastFinishedHigh;
	private decimal? _lastFinishedLow;
	private DateTimeOffset? _lastProcessedOpenTime;

	private Order? _buyBreakoutOrder;
	private Order? _sellBreakoutOrder;
	private Order? _stopOrder;
	private Order? _takeProfitOrder;

	/// <summary>
	/// Initializes a new instance of the <see cref="VltTraderStrategy"/> class.
	/// </summary>
	public VltTraderStrategy()
	{
		Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance for the take profit order", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 5m);

		_stopLossPips = Param(nameof(StopLossPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Distance for the protective stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 5m);

		_maxCandleSizePips = Param(nameof(MaxCandleSizePips), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Max Candle Size (pips)", "Largest candle range considered for comparison", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 20m);

		_candleCount = Param(nameof(CandleCount), 6)
			.SetGreaterThanZero()
			.SetDisplay("Candle Count", "Number of historical candles used for the volatility filter", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to build signal candles", "General");
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Maximum candle size that is considered when searching for the minimal range.
	/// </summary>
	public decimal MaxCandleSizePips
	{
		get => _maxCandleSizePips.Value;
		set => _maxCandleSizePips.Value = value;
	}

	/// <summary>
	/// Number of historical candles used for the volatility filter.
	/// </summary>
	public int CandleCount
	{
		get => _candleCount.Value;
		set => _candleCount.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
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

		_rangeHistory.Clear();
		_lastFinishedRange = null;
		_lastFinishedHigh = null;
		_lastFinishedLow = null;
		_lastProcessedOpenTime = null;
		_buyBreakoutOrder = null;
		_sellBreakoutOrder = null;
		_stopOrder = null;
		_takeProfitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		UpdateOffsets();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		UpdateOffsets();

		if (candle.State == CandleStates.Finished)
		{
			HandleFinishedCandle(candle);
		}
		else if (candle.State == CandleStates.Active)
		{
			HandleActiveCandle(candle);
		}
	}

	private void HandleFinishedCandle(ICandleMessage candle)
	{
		if (_lastFinishedRange is decimal prevRange && CandleCount > 0)
		{
			_rangeHistory.Enqueue(prevRange);

			while (_rangeHistory.Count > CandleCount)
				_rangeHistory.Dequeue();
		}

		_lastFinishedRange = candle.HighPrice - candle.LowPrice;
		_lastFinishedHigh = candle.HighPrice;
		_lastFinishedLow = candle.LowPrice;
	}

	private void HandleActiveCandle(ICandleMessage candle)
	{
		if (_lastProcessedOpenTime == candle.OpenTime)
			return;

		_lastProcessedOpenTime = candle.OpenTime;

		if (_lastFinishedRange is not decimal range ||
			_lastFinishedHigh is not decimal prevHigh ||
			_lastFinishedLow is not decimal prevLow)
		{
			return;
		}

		if (range <= 0m)
			return;

		if (candle.OpenPrice > prevHigh)
			return;

		var minRange = decimal.MaxValue;

		if (_maxRange > 0m)
		{
			foreach (var histRange in _rangeHistory)
			{
				if (histRange <= 0m || histRange >= _maxRange)
					continue;

				if (histRange < minRange)
					minRange = histRange;
			}
		}

		if (range >= minRange)
			return;

		PlaceEntryOrders(prevHigh, prevLow);
	}

	private void PlaceEntryOrders(decimal prevHigh, decimal prevLow)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Volume;
		if (volume <= 0m)
			return;

		var breakoutOffset = BreakoutBufferPips * _pipSize;

		if (Position <= 0)
		{
			CancelOrderIfActive(ref _buyBreakoutOrder);
			var buyPrice = NormalizePrice(prevHigh + breakoutOffset);
			_buyBreakoutOrder = BuyStop(volume, buyPrice);
		}

		if (Position >= 0)
		{
			CancelOrderIfActive(ref _sellBreakoutOrder);
			var sellPrice = NormalizePrice(prevLow - breakoutOffset);
			_sellBreakoutOrder = SellStop(volume, sellPrice);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			RegisterProtection(true);
		}
		else if (Position < 0)
		{
			RegisterProtection(false);
		}
		else
		{
			CancelProtectionOrders();
		}
	}

	private void RegisterProtection(bool isLong)
	{
		CancelProtectionOrders();

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var entryPrice = PositionPrice;

		if (_stopLossOffset > 0m)
		{
			var stopPrice = isLong ? entryPrice - _stopLossOffset : entryPrice + _stopLossOffset;
			stopPrice = NormalizePrice(stopPrice);
			_stopOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
		}

		if (_takeProfitOffset > 0m)
		{
			var takePrice = isLong ? entryPrice + _takeProfitOffset : entryPrice - _takeProfitOffset;
			takePrice = NormalizePrice(takePrice);
			_takeProfitOrder = isLong ? SellLimit(volume, takePrice) : BuyLimit(volume, takePrice);
		}
	}

	private void CancelProtectionOrders()
	{
		CancelOrderIfActive(ref _stopOrder);
		CancelOrderIfActive(ref _takeProfitOrder);
	}

	private void CancelOrderIfActive(ref Order? order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

	order = null;
	}

	private void UpdateOffsets()
	{
		_takeProfitOffset = TakeProfitPips * _pipSize;
		_stopLossOffset = StopLossPips * _pipSize;
		_maxRange = MaxCandleSizePips * _pipSize;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security.PriceStep ?? 1m;
		if (step <= 0m)
			return price;

		var ratio = price / step;
		var rounded = decimal.Round(ratio, 0, MidpointRounding.AwayFromZero);
		return rounded * step;
	}

	private decimal CalculatePipSize()
	{
		var step = Security.PriceStep ?? 1m;
		if (step <= 0m)
			return 1m;

		return step <= 0.001m ? step * 10m : step;
	}
}

