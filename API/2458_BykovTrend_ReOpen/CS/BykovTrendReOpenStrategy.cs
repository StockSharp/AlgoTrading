using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BykovTrend strategy with position re-opening.
/// Uses Williams %R and ATR to detect trend changes.
/// Re-opens positions every fixed price step while trend persists.
/// </summary>
public class BykovTrendReOpenStrategy : Strategy
{
	private readonly StrategyParam<int> _risk;
	private readonly StrategyParam<int> _ssp;
	private readonly StrategyParam<decimal> _priceStep;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _enableLongOpen;
	private readonly StrategyParam<bool> _enableShortOpen;
	private readonly StrategyParam<bool> _enableLongClose;
	private readonly StrategyParam<bool> _enableShortClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastBuyPrice;
	private decimal _lastSellPrice;
	private int _buyCount;
	private int _sellCount;
	private bool _trendUp;

	/// <summary>
	/// Risk parameter for trend detection.
	/// </summary>
	public int Risk
	{
		get => _risk.Value;
		set => _risk.Value = value;
	}

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int Ssp
	{
		get => _ssp.Value;
		set => _ssp.Value = value;
	}

	/// <summary>
	/// Price distance to re-open position.
	/// </summary>
	public decimal PriceStep
	{
		get => _priceStep.Value;
		set => _priceStep.Value = value;
	}

	/// <summary>
	/// Maximum number of positions in one direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLongOpen
	{
		get => _enableLongOpen.Value;
		set => _enableLongOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShortOpen
	{
		get => _enableShortOpen.Value;
		set => _enableShortOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on opposite signals.
	/// </summary>
	public bool EnableLongClose
	{
		get => _enableLongClose.Value;
		set => _enableLongClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on opposite signals.
	/// </summary>
	public bool EnableShortClose
	{
		get => _enableShortClose.Value;
		set => _enableShortClose.Value = value;
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
	/// Constructor.
	/// </summary>
	public BykovTrendReOpenStrategy()
	{
		_risk = Param(nameof(Risk), 3)
			.SetGreaterThanZero()
			.SetDisplay("Risk", "Risk parameter for BykovTrend", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_ssp = Param(nameof(Ssp), 9)
			.SetGreaterThanZero()
			.SetDisplay("SSP", "Williams %R period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_priceStep = Param(nameof(PriceStep), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Price Step", "Distance for re-open", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 100m);

		_maxPositions = Param(nameof(MaxPositions), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum positions per side", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop-loss distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 2000m, 100m);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take-profit distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 4000m, 100m);

		_enableLongOpen = Param(nameof(EnableLongOpen), true)
			.SetDisplay("Enable Long Open", "Allow long entries", "Trading");

		_enableShortOpen = Param(nameof(EnableShortOpen), true)
			.SetDisplay("Enable Short Open", "Allow short entries", "Trading");

		_enableLongClose = Param(nameof(EnableLongClose), true)
			.SetDisplay("Enable Long Close", "Allow long exits", "Trading");

		_enableShortClose = Param(nameof(EnableShortClose), true)
			.SetDisplay("Enable Short Close", "Allow short exits", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");
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

		_lastBuyPrice = 0m;
		_lastSellPrice = 0m;
		_buyCount = 0;
		_sellCount = 0;
		_trendUp = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var wpr = new WilliamsR { Length = Ssp };
		var atr = new AverageTrueRange { Length = 15 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(wpr, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wpr, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var k = 33 - Risk;
		var newTrend = _trendUp;
		if (wpr < -100 + k)
			newTrend = false;
		if (wpr > -k)
			newTrend = true;

		var buySignal = !_trendUp && newTrend;
		var sellSignal = _trendUp && !newTrend;

		if (buySignal)
		{
			if (EnableShortClose)
				CloseShortPositions();
			TryOpenLong(candle.ClosePrice);
		}
		else if (sellSignal)
		{
			if (EnableLongClose)
				CloseLongPositions();
			TryOpenShort(candle.ClosePrice);
		}

		if (Position > 0)
			CheckRebuy(candle.ClosePrice);
		else if (Position < 0)
			CheckResell(candle.ClosePrice);

		_trendUp = newTrend;
	}

	private void TryOpenLong(decimal price)
	{
		if (!EnableLongOpen || Position > 0)
			return;

		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		_lastBuyPrice = price;
		_buyCount = 1;
		LogInfo($"Open long at {price}");
	}

	private void TryOpenShort(decimal price)
	{
		if (!EnableShortOpen || Position < 0)
			return;

		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		_lastSellPrice = price;
		_sellCount = 1;
		LogInfo($"Open short at {price}");
	}

	private void CloseLongPositions()
	{
		if (Position <= 0)
			return;

		SellMarket(Math.Abs(Position));
		ResetState();
		LogInfo("Close long positions");
	}

	private void CloseShortPositions()
	{
		if (Position >= 0)
			return;

		BuyMarket(Math.Abs(Position));
		ResetState();
		LogInfo("Close short positions");
	}

	private void CheckRebuy(decimal price)
	{
		if (_buyCount >= MaxPositions)
		{
			CheckStops(price, true);
			return;
		}

		if (price - _lastBuyPrice >= PriceStep)
		{
			BuyMarket(Volume);
			_lastBuyPrice = price;
			_buyCount++;
			LogInfo($"Re-open long at {price}");
		}

		CheckStops(price, true);
	}

	private void CheckResell(decimal price)
	{
		if (_sellCount >= MaxPositions)
		{
			CheckStops(price, false);
			return;
		}

		if (_lastSellPrice - price >= PriceStep)
		{
			SellMarket(Volume);
			_lastSellPrice = price;
			_sellCount++;
			LogInfo($"Re-open short at {price}");
		}

		CheckStops(price, false);
	}

	private void CheckStops(decimal price, bool isLong)
	{
		var entry = isLong ? _lastBuyPrice : _lastSellPrice;

		if (StopLoss > 0)
		{
			var stopPrice = isLong ? entry - StopLoss : entry + StopLoss;
			if (isLong ? price <= stopPrice : price >= stopPrice)
			{
				if (isLong)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				ResetState();
				LogInfo($"Stop loss triggered at {price}");
				return;
			}
		}

		if (TakeProfit > 0)
		{
			var target = isLong ? entry + TakeProfit : entry - TakeProfit;
			if (isLong ? price >= target : price <= target)
			{
				if (isLong)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				ResetState();
				LogInfo($"Take profit triggered at {price}");
			}
		}
	}

	private void ResetState()
	{
		_lastBuyPrice = 0m;
		_lastSellPrice = 0m;
		_buyCount = 0;
		_sellCount = 0;
	}
}
