using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price level clustering strategy using dual SMAs and nearest liquidity bands.
/// </summary>
public class TechnicalTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _resistanceThreshold;
	private readonly StrategyParam<int> _historyDepth;
	private readonly StrategyParam<decimal> _levelTolerance;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private readonly Queue<decimal> _closeWindow = new();
	private readonly Dictionary<decimal, int> _levelCounts = new();
	private decimal _bestAsk;
	private decimal _bestBid;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int ResistanceThreshold
	{
		get => _resistanceThreshold.Value;
		set => _resistanceThreshold.Value = value;
	}

	public int HistoryDepth
	{
		get => _historyDepth.Value;
		set => _historyDepth.Value = value;
	}

	public decimal LevelTolerance
	{
		get => _levelTolerance.Value;
		set => _levelTolerance.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TechnicalTraderStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 25)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Fast moving average period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Slow moving average period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_stopLossPoints = Param(nameof(StopLossPoints), 30)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 80, 10);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(50, 200, 10);

		_resistanceThreshold = Param(nameof(ResistanceThreshold), 15)
		.SetGreaterThanZero()
		.SetDisplay("Cluster Threshold", "Minimum occurrences for price level", "Levels")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);

		_historyDepth = Param(nameof(HistoryDepth), 500)
		.SetGreaterThanZero()
		.SetDisplay("History Depth", "Number of candles for clustering", "Levels")
		.SetCanOptimize(true)
		.SetOptimize(100, 600, 50);

		_levelTolerance = Param(nameof(LevelTolerance), 0.0005m)
		.SetNotNegative()
		.SetDisplay("Level Tolerance", "Maximum distance from cluster", "Levels")
		.SetCanOptimize(true)
		.SetOptimize(0.0001m, 0.001m, 0.0001m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to process", "General");
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

		_closeWindow.Clear();
		_levelCounts.Clear();
		_bestAsk = 0m;
		_bestBid = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(_fastMa, _slowMa, ProcessCandle)
		.Start();

		SubscribeOrderBook()
		.Bind(depth =>
		{
			_bestBid = depth.GetBestBid()?.Price ?? _bestBid;
			_bestAsk = depth.GetBestAsk()?.Price ?? _bestAsk;
		})
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_closeWindow.Enqueue(candle.ClosePrice);
		while (_closeWindow.Count > HistoryDepth)
		{
			_closeWindow.Dequeue();
		}

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		return;

		if (_closeWindow.Count < ResistanceThreshold)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_bestAsk == 0m)
		_bestAsk = candle.ClosePrice;

		if (_bestBid == 0m)
		_bestBid = candle.ClosePrice;

		RebuildLevelCounts();

		var tolerance = LevelTolerance;
		var supportLevel = FindSupportLevel(tolerance);
		var resistanceLevel = FindResistanceLevel(tolerance);

		if (ManagePosition(candle.ClosePrice))
		return;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
		priceStep = 1m;

		if (Position <= 0 && fastValue > slowValue && supportLevel.HasValue)
		{
			var volume = Volume + Math.Abs(Position);
			_entryPrice = _bestAsk;
			_stopPrice = _entryPrice - StopLossPoints * priceStep;
			_takePrice = _entryPrice + TakeProfitPoints * priceStep;
			BuyMarket(volume);
			return;
		}

		if (Position >= 0 && fastValue < slowValue && resistanceLevel.HasValue)
		{
			var volume = Volume + Math.Abs(Position);
			_entryPrice = _bestBid;
			_stopPrice = _entryPrice + StopLossPoints * priceStep;
			_takePrice = _entryPrice - TakeProfitPoints * priceStep;
			SellMarket(volume);
		}
	}

	private void RebuildLevelCounts()
	{
		_levelCounts.Clear();

		foreach (var close in _closeWindow)
		{
			var rounded = Math.Round(close, 3, MidpointRounding.AwayFromZero);
			if (_levelCounts.TryGetValue(rounded, out var count))
			{
				_levelCounts[rounded] = count + 1;
			}
			else
			{
				_levelCounts.Add(rounded, 1);
			}
		}
	}

	private decimal? FindSupportLevel(decimal tolerance)
	{
		if (_bestAsk == 0m)
		return null;

		decimal? result = null;

		foreach (var pair in _levelCounts)
		{
			if (pair.Value <= ResistanceThreshold)
			continue;

			var level = pair.Key;
			if (_bestAsk <= level)
			continue;

			if (Math.Abs(_bestAsk - level) > tolerance)
			continue;

			if (result == null || level > result.Value)
			{
				result = level;
			}
		}

		return result;
	}

	private decimal? FindResistanceLevel(decimal tolerance)
	{
		if (_bestBid == 0m)
		return null;

		decimal? result = null;

		foreach (var pair in _levelCounts)
		{
			if (pair.Value <= ResistanceThreshold)
			continue;

			var level = pair.Key;
			if (_bestBid >= level)
			continue;

			if (Math.Abs(level - _bestBid) > tolerance)
			continue;

			if (result == null || level < result.Value)
			{
				result = level;
			}
		}

		return result;
	}

	private bool ManagePosition(decimal closePrice)
	{
		if (Position > 0)
		{
			var exitPrice = _bestBid != 0m ? _bestBid : closePrice;

			if (_stopPrice > 0m && exitPrice <= _stopPrice)
			{
				SellMarket(Position);
				ResetTradeTargets();
				return true;
			}

			if (_takePrice > 0m && exitPrice >= _takePrice)
			{
				SellMarket(Position);
				ResetTradeTargets();
				return true;
			}
		}
		else if (Position < 0)
		{
			var exitPrice = _bestAsk != 0m ? _bestAsk : closePrice;

			if (_stopPrice > 0m && exitPrice >= _stopPrice)
			{
				BuyMarket(-Position);
				ResetTradeTargets();
				return true;
			}

			if (_takePrice > 0m && exitPrice <= _takePrice)
			{
				BuyMarket(-Position);
				ResetTradeTargets();
				return true;
			}
		}

		return false;
	}

	private void ResetTradeTargets()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}
}
