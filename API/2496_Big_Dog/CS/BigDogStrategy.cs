using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Big Dog range breakout strategy that trades breakouts from a tight range built between configurable hours.
/// </summary>
public class BigDogStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<decimal> _maxRangePoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _distancePoints;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private DateTime? _rangeDate;
	private bool _longReady;
	private bool _shortReady;
	private decimal? _longStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _adjustedPointSize;

	/// <summary>
	/// Hour (0-23) when the consolidation range calculation starts.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour (0-23) when the consolidation range calculation stops.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Maximum acceptable range height measured in adjusted points.
	/// </summary>
	public decimal MaxRangePoints
	{
		get => _maxRangePoints.Value;
		set => _maxRangePoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in adjusted points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum distance required between the current price and breakout level, measured in adjusted points.
	/// </summary>
	public decimal DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Order volume that will be used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for range calculation and breakout detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BigDogStrategy"/> class.
	/// </summary>
	public BigDogStrategy()
	{
		_startHour = Param(nameof(StartHour), 14)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Hour to begin measuring the range", "Session");

		_stopHour = Param(nameof(StopHour), 16)
			.SetRange(0, 23)
			.SetDisplay("Stop Hour", "Hour to stop measuring the range", "Session");

		_maxRangePoints = Param(nameof(MaxRangePoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Max Range", "Maximum allowed height of the consolidation range (points)", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take-profit distance in adjusted points", "Trading");

		_distancePoints = Param(nameof(DistancePoints), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Min Distance", "Minimum distance from current price to breakout level (points)", "Trading");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume used for each breakout order", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe used for range detection", "Data");
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
		_rangeHigh = null;
		_rangeLow = null;
		_rangeDate = null;
		_longReady = false;
		_shortReady = false;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_bestBid = null;
		_bestAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		_adjustedPointSize = CalculateAdjustedPointSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		SubscribeOrderBook()
			.Bind(depth =>
			{
				// Store the latest best bid/ask values to approximate current market price.
				_bestBid = depth.GetBestBid()?.Price ?? _bestBid;
				_bestAsk = depth.GetBestAsk()?.Price ?? _bestAsk;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentDate = candle.OpenTime.Date;

		if (_rangeDate != currentDate)
		{
			ResetDailyState(currentDate);
		}

		UpdateRange(candle);

		if (candle.OpenTime.Hour >= StopHour)
		{
			PrepareBreakoutLevels(candle);
		}

		ProcessEntries(candle);
		ProcessRiskManagement(candle);
	}

	private void ResetDailyState(DateTime date)
	{
		CancelActiveOrders();
		_rangeDate = date;
		_rangeHigh = null;
		_rangeLow = null;
		_longReady = false;
		_shortReady = false;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}

	private void UpdateRange(ICandleMessage candle)
	{
		var hour = candle.OpenTime.Hour;

		if (hour < StartHour || hour >= StopHour)
			return;

		_rangeHigh = _rangeHigh.HasValue
			? Math.Max(_rangeHigh.Value, candle.HighPrice)
			: candle.HighPrice;

		_rangeLow = _rangeLow.HasValue
			? Math.Min(_rangeLow.Value, candle.LowPrice)
			: candle.LowPrice;
	}

	private void PrepareBreakoutLevels(ICandleMessage candle)
	{
		if (!_rangeHigh.HasValue || !_rangeLow.HasValue)
			return;

		var rangeHeight = _rangeHigh.Value - _rangeLow.Value;
		var maxRange = ConvertToPrice(MaxRangePoints);

		if (rangeHeight >= maxRange)
		{
			// Reset pending plans when the range becomes too wide.
			_longReady = false;
			_shortReady = false;
			return;
		}

		var minDistance = ConvertToPrice(DistancePoints);
		var ask = _bestAsk ?? candle.ClosePrice;
		var bid = _bestBid ?? candle.ClosePrice;

		if (!_longReady && Position >= 0 && (_rangeHigh.Value - ask) > minDistance)
		{
			_longReady = true;
			_longEntryPrice = _rangeHigh.Value;
			_longStopPrice = _rangeLow.Value;
			_longTakeProfitPrice = _rangeHigh.Value + ConvertToPrice(TakeProfitPoints);
		}

		if (!_shortReady && Position <= 0 && (bid - _rangeLow.Value) > minDistance)
		{
			_shortReady = true;
			_shortEntryPrice = _rangeLow.Value;
			_shortStopPrice = _rangeHigh.Value;
			_shortTakeProfitPrice = _rangeLow.Value - ConvertToPrice(TakeProfitPoints);
		}
	}

	private void ProcessEntries(ICandleMessage candle)
	{
		var volume = OrderVolume;

		if (_longReady && candle.HighPrice >= _longEntryPrice && Position <= 0)
		{
			// Enter long on breakout of the session high.
			var qty = volume + Math.Max(0m, -Position);
			if (qty > 0m)
			{
				BuyMarket(qty);
			}

			_longReady = false;
			_shortReady = false;
		}

		if (_shortReady && candle.LowPrice <= _shortEntryPrice && Position >= 0)
		{
			// Enter short on breakout of the session low.
			var qty = volume + Math.Max(0m, Position);
			if (qty > 0m)
			{
				SellMarket(qty);
			}

			_shortReady = false;
			_longReady = false;
		}
	}

	private void ProcessRiskManagement(ICandleMessage candle)
	{
		if (Position > 0 && _longStopPrice.HasValue && _longTakeProfitPrice.HasValue)
		{
			// Close the long position if stop-loss or take-profit levels are touched.
			if (candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Position);
				_longStopPrice = null;
				_longTakeProfitPrice = null;
			}
			else if (candle.HighPrice >= _longTakeProfitPrice.Value)
			{
				SellMarket(Position);
				_longStopPrice = null;
				_longTakeProfitPrice = null;
			}
		}
		else if (Position < 0 && _shortStopPrice.HasValue && _shortTakeProfitPrice.HasValue)
		{
			// Close the short position if stop-loss or take-profit levels are touched.
			if (candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(-Position);
				_shortStopPrice = null;
				_shortTakeProfitPrice = null;
			}
			else if (candle.LowPrice <= _shortTakeProfitPrice.Value)
			{
				BuyMarket(-Position);
				_shortStopPrice = null;
				_shortTakeProfitPrice = null;
			}
		}
		else
		{
			_longStopPrice = null;
			_longTakeProfitPrice = null;
			_shortStopPrice = null;
			_shortTakeProfitPrice = null;
		}
	}

	private decimal ConvertToPrice(decimal points)
	{
		return points * _adjustedPointSize;
	}

	private decimal CalculateAdjustedPointSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var decimals = Security?.Decimals ?? 0;
		var multiplier = decimals == 3 || decimals == 5 ? 10m : 1m;

		return step * multiplier;
	}
}
