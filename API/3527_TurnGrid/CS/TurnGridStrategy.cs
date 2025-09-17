using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy that mirrors the TurnGrid Expert Advisor logic from MQL5.
/// </summary>
public class TurnGridStrategy : Strategy
{
	private enum TradeDirection
	{
		Buy,
		Sell,
	}

	private struct GridLevel
	{
		public decimal Price;
		public bool HasBuy;
		public bool HasSell;
		public decimal BuyVolumeTicket;
		public decimal SellVolumeTicket;
	}

	private readonly StrategyParam<decimal> _gridDistance;
	private readonly StrategyParam<int> _gridShares;
	private readonly StrategyParam<decimal> _equityTakeProfit;
	private readonly StrategyParam<decimal> _feeRate;
	private readonly StrategyParam<DataType> _candleType;

	private GridLevel[]? _grid;
	private int _currentIndex;
	private decimal _openBudget;
	private decimal _openMoneyIncrement;
	private int _buyCount;
	private int _sellCount;
	private decimal _lastPrice;
	private decimal _totalFee;
	private decimal _initialBalance;
	private bool _resetRequested;
	private decimal _resetPrice;

	public decimal GridDistance
	{
		get => _gridDistance.Value;
		set => _gridDistance.Value = value;
	}

	public int GridShares
	{
		get => _gridShares.Value;
		set => _gridShares.Value = value;
	}

	public decimal EquityTakeProfit
	{
		get => _equityTakeProfit.Value;
		set => _equityTakeProfit.Value = value;
	}

	public decimal FeeRate
	{
		get => _feeRate.Value;
		set => _feeRate.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TurnGridStrategy()
	{
		_gridDistance = Param(nameof(GridDistance), 0.01m)
			.SetDisplay("Grid Distance", "Relative distance between grid levels", "Grid");
		_gridShares = Param(nameof(GridShares), 50)
			.SetDisplay("Max Grid Positions", "Maximum number of open grid entries", "Grid");
		_equityTakeProfit = Param(nameof(EquityTakeProfit), 0.02m)
			.SetDisplay("Equity Take Profit", "Equity growth ratio that triggers a reset", "Risk");
		_feeRate = Param(nameof(FeeRate), 0.0008m)
			.SetDisplay("Fee Rate", "Estimated transaction fee per trade", "Costs");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used to drive the grid", "Data");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_grid = null;
		_currentIndex = 0;
		_openBudget = 0m;
		_openMoneyIncrement = 0m;
		_buyCount = 0;
		_sellCount = 0;
		_lastPrice = 0m;
		_totalFee = 0m;
		_initialBalance = 0m;
		_resetRequested = false;
		_resetPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastPrice = candle.ClosePrice;

		if (_resetRequested)
		{
			if (!TryCloseNetPosition())
				return;

			InitializeGrid(_resetPrice);
			_resetRequested = false;
		}

		if (_grid == null)
		{
			InitializeGrid(candle.ClosePrice);
			return;
		}

		if (!UpdateCurrentIndex(candle.ClosePrice))
			return;

		if (CheckEquityTarget())
		{
			RequestReset(candle.ClosePrice);
			return;
		}

		CloseReachedPositions();
		ManageOpenings();
	}

	private void InitializeGrid(decimal price)
	{
		if (price <= 0m)
			return;

		var shares = Math.Max(1, GridShares);
		var size = shares * 4;

		_grid = new GridLevel[size];
		_currentIndex = shares * 2;

		_grid[_currentIndex].Price = price;

		for (var i = _currentIndex + 1; i < size; i++)
		{
			_grid[i].Price = _grid[i - 1].Price * (1m + GridDistance);
		}

		for (var i = _currentIndex - 1; i >= 0; i--)
		{
			_grid[i].Price = _grid[i + 1].Price * (1m - GridDistance);
		}

		_buyCount = 0;
		_sellCount = 0;
		_totalFee = 0m;

		var portfolio = Portfolio;
		_initialBalance = portfolio?.CurrentValue ?? portfolio?.CurrentBalance ?? _initialBalance;
		if (_initialBalance <= 0m)
			_initialBalance = shares * price;

		_openBudget = _initialBalance / shares;
		if (_openBudget <= 0m)
			_openBudget = price;

		_openMoneyIncrement = CalculateOpenMoneyIncrement();
		_lastPrice = price;

		TryOpenBuy();
	}

	private bool UpdateCurrentIndex(decimal price)
	{
		if (_grid == null)
			return false;

		var newIndex = _currentIndex;

		while (newIndex + 1 < _grid.Length && price >= _grid[newIndex + 1].Price)
			newIndex++;

		while (newIndex - 1 >= 0 && price <= _grid[newIndex - 1].Price)
			newIndex--;

		if (newIndex == _currentIndex)
			return false;

		_currentIndex = newIndex;
		return true;
	}

	private bool CheckEquityTarget()
	{
		if (_initialBalance <= 0m)
			return false;

		var portfolio = Portfolio;
		var equity = portfolio?.CurrentValue ?? portfolio?.CurrentBalance ?? 0m;
		if (equity <= 0m)
			return false;

		return equity - _totalFee > _initialBalance * (1m + EquityTakeProfit);
	}

	private void RequestReset(decimal price)
	{
		_resetRequested = true;
		_resetPrice = price;
		_grid = null;
		_buyCount = 0;
		_sellCount = 0;
		_totalFee = 0m;

		TryCloseNetPosition();
	}

	private bool TryCloseNetPosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
			return false;
		}

		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			return false;
		}

		return true;
	}

	private void CloseReachedPositions()
	{
		if (_grid == null)
			return;

		ref var currentLevel = ref _grid[_currentIndex];

		if (currentLevel.BuyVolumeTicket > 0m)
		{
			SellMarket(currentLevel.BuyVolumeTicket);
			_buyCount = Math.Max(0, _buyCount - 1);

			currentLevel.BuyVolumeTicket = 0m;

			var anchorIndex = _currentIndex - 2;
			if (anchorIndex >= 0)
				_grid[anchorIndex].HasBuy = false;
		}

		if (currentLevel.SellVolumeTicket > 0m)
		{
			BuyMarket(currentLevel.SellVolumeTicket);
			_sellCount = Math.Max(0, _sellCount - 1);

			currentLevel.SellVolumeTicket = 0m;

			var anchorIndex = _currentIndex + 2;
			if (_grid != null && anchorIndex < _grid.Length)
				_grid[anchorIndex].HasSell = false;
		}
	}

	private void ManageOpenings()
	{
		if (_grid == null)
			return;

		ref var level = ref _grid[_currentIndex];

		if (level.HasBuy && !level.HasSell)
		{
			TryOpenSell();
			return;
		}

		if (!level.HasBuy && level.HasSell)
		{
			TryOpenBuy();
			return;
		}

		if (!level.HasBuy && !level.HasSell)
		{
			if (_buyCount > _sellCount)
				TryOpenSell();
			else
				TryOpenBuy();
		}
	}

	private void TryOpenBuy()
	{
		if (_grid == null)
			return;

		if (_buyCount + _sellCount >= GridShares)
			return;

		var volume = CalculateVolume(TradeDirection.Buy);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		ref var level = ref _grid[_currentIndex];
		level.HasBuy = true;

		var targetIndex = _currentIndex + 2;
		if (targetIndex < _grid.Length)
			_grid[targetIndex].BuyVolumeTicket += volume;

		_buyCount++;
	}

	private void TryOpenSell()
	{
		if (_grid == null)
			return;

		if (_buyCount + _sellCount >= GridShares)
			return;

		var volume = CalculateVolume(TradeDirection.Sell);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		ref var level = ref _grid[_currentIndex];
		level.HasSell = true;

		var targetIndex = _currentIndex - 2;
		if (targetIndex >= 0)
			_grid[targetIndex].SellVolumeTicket += volume;

		_sellCount++;
	}

	private decimal CalculateVolume(TradeDirection direction)
	{
		if (_lastPrice <= 0m)
			return 0m;

		var firstMoney = _openBudget / 10m;
		if (firstMoney <= 0m)
			firstMoney = _lastPrice;

		decimal money;
		switch (direction)
		{
			case TradeDirection.Buy:
				money = firstMoney + _buyCount * _openMoneyIncrement;
				break;
			case TradeDirection.Sell:
				money = firstMoney + _sellCount * _openMoneyIncrement;
				break;
			default:
				money = firstMoney;
				break;
		}

		if (money <= 0m)
			return 0m;

		var volume = money / _lastPrice;
		volume = NormalizeVolume(volume);

		if (volume <= 0m)
			return 0m;

		_totalFee += _lastPrice * volume * FeeRate;
		AddInfoLog($"Total Fee = {_totalFee:F2}; Grid = {_buyCount + _sellCount} / {GridShares} ({_buyCount}, {_sellCount})");

		return volume;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
			volume = step * Math.Round(volume / step, MidpointRounding.AwayFromZero);

		var min = security.VolumeMin ?? 0m;
		if (min > 0m && volume < min)
			return 0m;

		var max = security.VolumeMax ?? decimal.MaxValue;
		if (volume > max)
			volume = max;

		return volume;
	}

	private decimal CalculateOpenMoneyIncrement()
	{
		var halfShares = GridShares / 2m;
		if (halfShares <= 1m)
			return 0m;

		var numerator = _initialBalance / 2m - halfShares / 10m;
		if (numerator <= 0m)
			numerator = _initialBalance / 4m;

		var denominator = halfShares * (halfShares - 1m) / 2m;
		if (denominator <= 0m)
			return 0m;

		return numerator / denominator;
	}
}
