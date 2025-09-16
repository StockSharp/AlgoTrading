using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Market Capture grid strategy translated from the MQL implementation.
/// The strategy maintains a moving price center and opens hedge-style trades around it.
/// </summary>
public class MarketCaptureStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _useEquityTargets;
	private readonly StrategyParam<bool> _trackEquityDrawdown;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _gridSteps;
	private readonly StrategyParam<decimal> _equityProfitPercent;
	private readonly StrategyParam<decimal> _equityLossPercent;
	private readonly StrategyParam<int> _lossCloseUp;
	private readonly StrategyParam<int> _lossCloseDown;
	private readonly StrategyParam<bool> _openInitialShort;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<TradeInfo> _trades = new();

	private decimal _centerLevel;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _equityBase;
	private decimal _equityTarget;
	private decimal _equityStop;
	private bool _initialShortPlaced;

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Use equity growth target for closing losing trades.
	/// </summary>
	public bool UseEquityTargets
	{
		get => _useEquityTargets.Value;
		set => _useEquityTargets.Value = value;
	}

	/// <summary>
	/// Use drawdown tracking to cut losing trades.
	/// </summary>
	public bool TrackEquityDrawdown
	{
		get => _trackEquityDrawdown.Value;
		set => _trackEquityDrawdown.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Grid spacing expressed in price steps.
	/// </summary>
	public decimal GridSteps
	{
		get => _gridSteps.Value;
		set => _gridSteps.Value = value;
	}

	/// <summary>
	/// Percent gain that triggers cutting losing positions.
	/// </summary>
	public decimal EquityProfitPercent
	{
		get => _equityProfitPercent.Value;
		set => _equityProfitPercent.Value = value;
	}

	/// <summary>
	/// Percent loss that triggers cutting losing positions.
	/// </summary>
	public decimal EquityLossPercent
	{
		get => _equityLossPercent.Value;
		set => _equityLossPercent.Value = value;
	}

	/// <summary>
	/// Maximum losing trades to close after profit target.
	/// </summary>
	public int NumberLossPositionsCloseUp
	{
		get => _lossCloseUp.Value;
		set => _lossCloseUp.Value = value;
	}

	/// <summary>
	/// Maximum losing trades to close during drawdown.
	/// </summary>
	public int NumberLossPositionsCloseDown
	{
		get => _lossCloseDown.Value;
		set => _lossCloseDown.Value = value;
	}

	/// <summary>
	/// Place an initial short trade on start similar to the MQL version.
	/// </summary>
	public bool OpenInitialShort
	{
		get => _openInitialShort.Value;
		set => _openInitialShort.Value = value;
	}

	/// <summary>
	/// Candle type for processing logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketCaptureStrategy"/> class.
	/// </summary>
	public MarketCaptureStrategy()
	{
		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow opening long trades", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow opening short trades", "Trading");

		_useEquityTargets = Param(nameof(UseEquityTargets), true)
			.SetDisplay("Use Equity Target", "Close losers after equity gain", "Risk");

		_trackEquityDrawdown = Param(nameof(TrackEquityDrawdown), true)
			.SetDisplay("Track Drawdown", "Close losers during drawdown", "Risk");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Steps", "Distance to take profit in price steps", "Trading");

		_gridSteps = Param(nameof(GridSteps), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Steps", "Grid spacing in price steps", "Trading");

		_equityProfitPercent = Param(nameof(EquityProfitPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Equity Gain %", "Percent gain before trimming losers", "Risk");

		_equityLossPercent = Param(nameof(EquityLossPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Equity Loss %", "Percent loss before trimming losers", "Risk");

		_lossCloseUp = Param(nameof(NumberLossPositionsCloseUp), 5)
			.SetGreaterThanZero()
			.SetDisplay("Loss Trades Up", "Losing trades to close after gain", "Risk");

		_lossCloseDown = Param(nameof(NumberLossPositionsCloseDown), 5)
			.SetGreaterThanZero()
			.SetDisplay("Loss Trades Down", "Losing trades to close after drawdown", "Risk");

		_openInitialShort = Param(nameof(OpenInitialShort), true)
			.SetDisplay("Open Initial Short", "Place the first sell trade on start", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");
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

		_trades.Clear();
		_centerLevel = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_equityBase = 0m;
		_equityTarget = 0m;
		_equityStop = 0m;
		_initialShortPlaced = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeEquity();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void InitializeEquity()
	{
		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity > 0m)
		{
			ResetEquityBase(equity);
		}
	}

	private void ResetEquityBase(decimal newBase)
	{
		_equityBase = newBase;
		UpdateEquityLevels();
	}

	private void UpdateEquityLevels()
	{
		if (_equityBase <= 0m)
		{
			_equityTarget = 0m;
			_equityStop = 0m;
			return;
		}

		_equityTarget = _equityBase * (1m + EquityProfitPercent / 100m);
		_equityStop = _equityBase * (1m - EquityLossPercent / 100m);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_equityBase <= 0m)
			InitializeEquity();
		else
			UpdateEquityLevels();

		var step = GetPriceStep();
		var gridDistance = Math.Max(GridSteps * step, step);
		var takeProfitDistance = Math.Max(TakeProfitSteps * step, step);
		var currentPrice = candle.ClosePrice;

		if (_centerLevel == 0m)
			_centerLevel = currentPrice;

		if (!_initialShortPlaced && OpenInitialShort && EnableShort && GetTradeVolume() > 0m)
		{
			var volume = GetTradeVolume();
			SellMarket(volume);
			RegisterTrade(false, currentPrice, volume, takeProfitDistance);
			_initialShortPlaced = true;
		}

		while (currentPrice >= _centerLevel + gridDistance)
			_centerLevel += gridDistance;

		while (currentPrice <= _centerLevel - gridDistance)
			_centerLevel -= gridDistance;

		var hasPrevLow = _prevLow > 0m;
		var hasPrevHigh = _prevHigh > 0m;

		if (EnableLong && hasPrevLow && currentPrice > _centerLevel && _prevLow <= _centerLevel)
			TryOpenLong(currentPrice, takeProfitDistance, gridDistance);

		if (EnableShort && hasPrevHigh && currentPrice < _centerLevel && _prevHigh >= _centerLevel)
			TryOpenShort(currentPrice, takeProfitDistance, gridDistance);

		ProcessTakeProfits(candle);
		ManageEquity(currentPrice);

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private decimal GetTradeVolume()
	{
		var volume = Volume;
		return volume > 0m ? volume : 1m;
	}

	private void TryOpenLong(decimal price, decimal takeProfitDistance, decimal gridDistance)
	{
		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		if (HasNearbyTrade(true, _centerLevel, gridDistance / 2m))
			return;

		BuyMarket(volume);
		RegisterTrade(true, price, volume, takeProfitDistance);
	}

	private void TryOpenShort(decimal price, decimal takeProfitDistance, decimal gridDistance)
	{
		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		if (HasNearbyTrade(false, _centerLevel, gridDistance / 2m))
			return;

		SellMarket(volume);
		RegisterTrade(false, price, volume, takeProfitDistance);
	}

	private bool HasNearbyTrade(bool isLong, decimal price, decimal threshold)
	{
		foreach (var trade in _trades)
		{
			if (trade.IsLong == isLong && Math.Abs(trade.EntryPrice - price) <= threshold)
				return true;
		}

		return false;
	}

	private void ProcessTakeProfits(ICandleMessage candle)
	{
		for (var i = _trades.Count - 1; i >= 0; i--)
		{
			var trade = _trades[i];
			var reached = trade.IsLong
				? candle.HighPrice >= trade.TargetPrice || candle.ClosePrice >= trade.TargetPrice
				: candle.LowPrice <= trade.TargetPrice || candle.ClosePrice <= trade.TargetPrice;

			if (!reached)
				continue;

			_trades.RemoveAt(i);
			if (trade.IsLong)
				SellMarket(trade.Volume);
			else
				BuyMarket(trade.Volume);
		}
	}

	private void ManageEquity(decimal price)
	{
		if (!UseEquityTargets && !TrackEquityDrawdown)
			return;

		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return;

		if (UseEquityTargets && _equityTarget > 0m && equity >= _equityTarget)
		{
			CloseLosingTrades(price, NumberLossPositionsCloseUp);
			ResetEquityBase(equity);
		}
		else if (TrackEquityDrawdown && _equityStop > 0m && equity <= _equityStop)
		{
			CloseLosingTrades(price, NumberLossPositionsCloseDown);
			ResetEquityBase(equity);
		}
	}

	private void CloseLosingTrades(decimal price, int maxToClose)
	{
		if (maxToClose <= 0)
			return;

		var losingTrades = GetLosingTrades(price);
		if (losingTrades.Count == 0)
			return;

		var toClose = losingTrades
			.OrderBy(pair => pair.profit)
			.Take(maxToClose)
			.Select(pair => pair.trade)
			.ToList();

		foreach (var trade in toClose)
			CloseTrade(trade);
	}

	private List<(TradeInfo trade, decimal profit)> GetLosingTrades(decimal price)
	{
		var losses = new List<(TradeInfo, decimal)>();

		foreach (var trade in _trades)
		{
			var profit = trade.IsLong
				? (price - trade.EntryPrice) * trade.Volume
				: (trade.EntryPrice - price) * trade.Volume;

			if (profit < 0m)
				losses.Add((trade, profit));
		}

		return losses;
	}

	private void CloseTrade(TradeInfo trade)
	{
		if (!_trades.Remove(trade))
			return;

		if (trade.IsLong)
			SellMarket(trade.Volume);
		else
			BuyMarket(trade.Volume);
	}

	private void RegisterTrade(bool isLong, decimal price, decimal volume, decimal takeProfitDistance)
	{
		var remaining = volume;
		if (remaining <= 0m)
			return;

		OffsetTrades(!isLong, ref remaining);

		if (remaining <= 0m)
			return;

		var target = isLong ? price + takeProfitDistance : price - takeProfitDistance;
		_trades.Add(new TradeInfo(isLong, price, remaining, target));
	}

	private void OffsetTrades(bool isLong, ref decimal volume)
	{
		for (var i = _trades.Count - 1; i >= 0 && volume > 0m; i--)
		{
			var trade = _trades[i];
			if (trade.IsLong != isLong)
				continue;

			var toOffset = Math.Min(trade.Volume, volume);
			trade.Volume -= toOffset;
			volume -= toOffset;

			if (trade.Volume <= 0m)
				_trades.RemoveAt(i);
		}
	}

	private sealed class TradeInfo
	{
		public TradeInfo(bool isLong, decimal entryPrice, decimal volume, decimal targetPrice)
		{
			IsLong = isLong;
			EntryPrice = entryPrice;
			Volume = volume;
			TargetPrice = targetPrice;
		}

		public bool IsLong { get; }
		public decimal EntryPrice { get; }
		public decimal Volume { get; set; }
		public decimal TargetPrice { get; set; }
	}
}
