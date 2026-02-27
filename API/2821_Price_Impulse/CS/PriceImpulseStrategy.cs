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
/// Price impulse strategy that trades on rapid price moves using candle close prices.
/// </summary>
public class PriceImpulseStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _impulsePoints;
	private readonly StrategyParam<int> _historyGap;
	private readonly StrategyParam<int> _extraHistory;
	private readonly StrategyParam<int> _cooldownSeconds;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _priceHistory = [];

	private decimal _tickSize;
	private DateTimeOffset? _lastTradeTime;
	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Volume used for each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum impulse measured in price points to trigger a trade.
	/// </summary>
	public int ImpulsePoints
	{
		get => _impulsePoints.Value;
		set => _impulsePoints.Value = value;
	}

	/// <summary>
	/// Number of candles between price comparisons.
	/// </summary>
	public int HistoryGap
	{
		get => _historyGap.Value;
		set => _historyGap.Value = value;
	}

	/// <summary>
	/// Additional samples kept in the rolling buffer.
	/// </summary>
	public int ExtraHistory
	{
		get => _extraHistory.Value;
		set => _extraHistory.Value = value;
	}

	/// <summary>
	/// Minimum number of seconds between two trades.
	/// </summary>
	public int CooldownSeconds
	{
		get => _cooldownSeconds.Value;
		set => _cooldownSeconds.Value = value;
	}

	/// <summary>
	/// Candle type used for price monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private int HistoryCapacity => Math.Max(HistoryGap + ExtraHistory + 1, HistoryGap + 1);

	/// <summary>
	/// Initializes strategy parameters with sensible defaults.
	/// </summary>
	public PriceImpulseStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Volume used for each market order", "Trading")
			.SetGreaterThanZero()
			.SetOptimize(0.1m, 2m, 0.1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 150)
			.SetDisplay("Stop Loss Points", "Stop loss distance expressed in price points", "Risk")
			.SetNotNegative()
			.SetOptimize(50, 300, 50);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetDisplay("Take Profit Points", "Take profit distance expressed in price points", "Risk")
			.SetNotNegative()
			.SetOptimize(10, 200, 10);

		_impulsePoints = Param(nameof(ImpulsePoints), 15)
			.SetDisplay("Impulse Points", "Minimum price impulse required to trade", "Signals")
			.SetGreaterThanZero()
			.SetOptimize(5, 40, 5);

		_historyGap = Param(nameof(HistoryGap), 15)
			.SetDisplay("Gap Candles", "Number of candles between comparison points", "Signals")
			.SetNotNegative()
			.SetOptimize(5, 40, 5);

		_extraHistory = Param(nameof(ExtraHistory), 15)
			.SetDisplay("Extra History", "Additional samples kept to absorb bursts", "Signals")
			.SetNotNegative()
			.SetOptimize(0, 30, 5);

		_cooldownSeconds = Param(nameof(CooldownSeconds), 100)
			.SetDisplay("Cooldown Seconds", "Minimum number of seconds between trades", "Risk")
			.SetNotNegative()
			.SetOptimize(0, 300, 20);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for price tracking", "General");
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

		_priceHistory.Clear();
		_lastTradeTime = null;
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_tickSize = Security?.PriceStep ?? 1m;
		if (_tickSize <= 0)
			_tickSize = 1m;

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
		if (candle.State != CandleStates.Finished)
			return;

		var currentPrice = candle.ClosePrice;
		_priceHistory.Add(currentPrice);

		var capacity = HistoryCapacity;
		while (_priceHistory.Count > capacity)
			_priceHistory.RemoveAt(0);

		var candleTime = candle.CloseTime;

		// Check SL/TP for existing positions.
		if (Position > 0)
		{
			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
			{ SellMarket(Position); _entryPrice = null; _stopLossPrice = null; _takeProfitPrice = null; return; }
			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{ SellMarket(Position); _entryPrice = null; _stopLossPrice = null; _takeProfitPrice = null; return; }
		}
		else if (Position < 0)
		{
			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
			{ BuyMarket(Math.Abs(Position)); _entryPrice = null; _stopLossPrice = null; _takeProfitPrice = null; return; }
			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{ BuyMarket(Math.Abs(Position)); _entryPrice = null; _stopLossPrice = null; _takeProfitPrice = null; return; }
		}

		if (_priceHistory.Count <= HistoryGap)
			return;

		var impulseThreshold = ImpulsePoints * _tickSize;
		var lastIndex = _priceHistory.Count - 1;
		var compareIndex = lastIndex - HistoryGap;
		if (compareIndex < 0) return;

		var comparisonPrice = _priceHistory[compareIndex];
		var upImpulse = currentPrice - comparisonPrice;
		var downImpulse = comparisonPrice - currentPrice;

		if (upImpulse > impulseThreshold && Position <= 0 && IsCooldownPassed(candleTime))
		{
			BuyMarket(OrderVolume);
			_entryPrice = currentPrice;
			_stopLossPrice = StopLossPoints > 0 ? currentPrice - StopLossPoints * _tickSize : null;
			_takeProfitPrice = TakeProfitPoints > 0 ? currentPrice + TakeProfitPoints * _tickSize : null;
			_lastTradeTime = candleTime;
			return;
		}

		if (downImpulse > impulseThreshold && Position >= 0 && IsCooldownPassed(candleTime))
		{
			SellMarket(OrderVolume);
			_entryPrice = currentPrice;
			_stopLossPrice = StopLossPoints > 0 ? currentPrice + StopLossPoints * _tickSize : null;
			_takeProfitPrice = TakeProfitPoints > 0 ? currentPrice - TakeProfitPoints * _tickSize : null;
			_lastTradeTime = candleTime;
		}
	}

	private bool IsCooldownPassed(DateTimeOffset time)
	{
		if (_lastTradeTime is null)
			return true;

		var cooldownSeconds = CooldownSeconds;
		if (cooldownSeconds <= 0)
			return true;

		return time - _lastTradeTime.Value >= TimeSpan.FromSeconds(cooldownSeconds);
	}
}
