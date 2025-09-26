using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic driven short strategy with grid style scaling.
/// It mirrors the behavior of the "stochSell" MetaTrader expert by waiting for multi timeframe stochastic confirmation, selling at market and stacking pending sell stops.
/// </summary>
public class StochSellStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrThreshold;
	private readonly StrategyParam<int> _fastKPeriod;
	private readonly StrategyParam<int> _fastDPeriod;
	private readonly StrategyParam<int> _fastSlowing;
	private readonly StrategyParam<int> _mediumKPeriod;
	private readonly StrategyParam<int> _mediumDPeriod;
	private readonly StrategyParam<int> _mediumSlowing;
	private readonly StrategyParam<int> _slowKPeriod;
	private readonly StrategyParam<int> _slowDPeriod;
	private readonly StrategyParam<int> _slowSlowing;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _longTermOversoldLevel;
	private readonly StrategyParam<decimal> _profitTargetPips;
	private readonly StrategyParam<int> _gridOrdersCount;
	private readonly StrategyParam<decimal> _gridStartOffsetPips;
	private readonly StrategyParam<decimal> _gridStepPips;
	private readonly StrategyParam<decimal> _gridVolume;
	private readonly StrategyParam<int> _gridExpirationMinutes;
	private readonly StrategyParam<decimal> _marketVolume;

	private AverageTrueRange _atr = null!;
	private StochasticOscillator _fastStochastic = null!;
	private StochasticOscillator _mediumStochastic = null!;
	private StochasticOscillator _slowStochastic = null!;

	private decimal? _previousFast;
	private decimal? _previousMedium;
	private decimal _pipSize;
	private decimal _shortVolume;
	private decimal _averageEntryPrice;
	private readonly List<GridOrderInfo> _gridOrders = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="StochSellStrategy"/> class.
	/// </summary>
	public StochSellStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for all indicators", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 35)
			.SetDisplay("ATR Period", "Number of candles used for ATR smoothing", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 5);

		_atrThreshold = Param(nameof(AtrThreshold), 0.00043m)
			.SetDisplay("ATR Threshold", "Upper volatility filter measured in price units", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.0002m, 0.001m, 0.0001m);

		_fastKPeriod = Param(nameof(FastKPeriod), 30)
			.SetDisplay("Fast %K", "Lookback for the fast stochastic", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 5);

		_fastDPeriod = Param(nameof(FastDPeriod), 3)
			.SetDisplay("Fast %D", "Signal smoothing for the fast stochastic", "Indicators");

		_fastSlowing = Param(nameof(FastSlowing), 3)
			.SetDisplay("Fast Slowing", "Smoothing factor for the fast stochastic", "Indicators");

		_mediumKPeriod = Param(nameof(MediumKPeriod), 100)
			.SetDisplay("Medium %K", "Lookback for the medium stochastic", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60, 150, 10);

		_mediumDPeriod = Param(nameof(MediumDPeriod), 1)
			.SetDisplay("Medium %D", "Signal smoothing for the medium stochastic", "Indicators");

		_mediumSlowing = Param(nameof(MediumSlowing), 3)
			.SetDisplay("Medium Slowing", "Smoothing factor for the medium stochastic", "Indicators");

		_slowKPeriod = Param(nameof(SlowKPeriod), 900)
			.SetDisplay("Slow %K", "Lookback for the slow stochastic", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(400, 1200, 100);

		_slowDPeriod = Param(nameof(SlowDPeriod), 1)
			.SetDisplay("Slow %D", "Signal smoothing for the slow stochastic", "Indicators");

		_slowSlowing = Param(nameof(SlowSlowing), 3)
			.SetDisplay("Slow Slowing", "Smoothing factor for the slow stochastic", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetDisplay("Trigger Level", "%K value that must be crossed downward", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(10m, 30m, 5m);

		_longTermOversoldLevel = Param(nameof(LongTermOversoldLevel), 40m)
			.SetDisplay("Slow Confirmation", "Maximum value allowed for the slow stochastic", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(30m, 60m, 5m);

		_profitTargetPips = Param(nameof(ProfitTargetPips), 23m)
			.SetDisplay("Profit Target", "Desired profit in pips before flattening", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(15m, 40m, 5m);

		_gridOrdersCount = Param(nameof(GridOrdersCount), 8)
			.SetDisplay("Grid Orders", "Number of supplemental sell stops", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_gridStartOffsetPips = Param(nameof(GridStartOffsetPips), -13m)
			.SetDisplay("Grid Offset", "Initial distance from the trigger price in pips", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(-30m, -5m, 5m);

		_gridStepPips = Param(nameof(GridStepPips), -23m)
			.SetDisplay("Grid Step", "Distance in pips between consecutive pending orders", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(-40m, -10m, 5m);

		_gridVolume = Param(nameof(GridVolume), 0.7m)
			.SetDisplay("Grid Volume", "Volume multiplier applied to each pending order", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(0.3m, 1.0m, 0.1m);

		_gridExpirationMinutes = Param(nameof(GridExpirationMinutes), 45)
			.SetDisplay("Grid Expiration", "Lifetime of pending sell stops in minutes", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(15, 120, 15);

		_marketVolume = Param(nameof(MarketVolume), 0.3m)
			.SetDisplay("Market Volume", "Volume used for the initial market sell", "Execution")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1.0m, 0.1m);
	}

	/// <summary>
	/// Primary candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ATR smoothing length.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Maximum allowed ATR before trades are disallowed.
	/// </summary>
	public decimal AtrThreshold
	{
		get => _atrThreshold.Value;
		set => _atrThreshold.Value = value;
	}

	/// <summary>
	/// Fast stochastic %K length.
	/// </summary>
	public int FastKPeriod
	{
		get => _fastKPeriod.Value;
		set => _fastKPeriod.Value = value;
	}

	/// <summary>
	/// Fast stochastic %D smoothing.
	/// </summary>
	public int FastDPeriod
	{
		get => _fastDPeriod.Value;
		set => _fastDPeriod.Value = value;
	}

	/// <summary>
	/// Fast stochastic slowing.
	/// </summary>
	public int FastSlowing
	{
		get => _fastSlowing.Value;
		set => _fastSlowing.Value = value;
	}

	/// <summary>
	/// Medium stochastic %K length.
	/// </summary>
	public int MediumKPeriod
	{
		get => _mediumKPeriod.Value;
		set => _mediumKPeriod.Value = value;
	}

	/// <summary>
	/// Medium stochastic %D smoothing.
	/// </summary>
	public int MediumDPeriod
	{
		get => _mediumDPeriod.Value;
		set => _mediumDPeriod.Value = value;
	}

	/// <summary>
	/// Medium stochastic slowing.
	/// </summary>
	public int MediumSlowing
	{
		get => _mediumSlowing.Value;
		set => _mediumSlowing.Value = value;
	}

	/// <summary>
	/// Slow stochastic %K length.
	/// </summary>
	public int SlowKPeriod
	{
		get => _slowKPeriod.Value;
		set => _slowKPeriod.Value = value;
	}

	/// <summary>
	/// Slow stochastic %D smoothing.
	/// </summary>
	public int SlowDPeriod
	{
		get => _slowDPeriod.Value;
		set => _slowDPeriod.Value = value;
	}

	/// <summary>
	/// Slow stochastic slowing.
	/// </summary>
	public int SlowSlowing
	{
		get => _slowSlowing.Value;
		set => _slowSlowing.Value = value;
	}

	/// <summary>
	/// Oversold trigger shared by the fast and medium stochastic oscillators.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Maximum accepted value for the slow stochastic before entering.
	/// </summary>
	public decimal LongTermOversoldLevel
	{
		get => _longTermOversoldLevel.Value;
		set => _longTermOversoldLevel.Value = value;
	}

	/// <summary>
	/// Profit target expressed in pips.
	/// </summary>
	public decimal ProfitTargetPips
	{
		get => _profitTargetPips.Value;
		set => _profitTargetPips.Value = value;
	}

	/// <summary>
	/// Number of pending sell stops placed after the market entry.
	/// </summary>
	public int GridOrdersCount
	{
		get => _gridOrdersCount.Value;
		set => _gridOrdersCount.Value = value;
	}

	/// <summary>
	/// Distance from the current price to the first pending order in pips.
	/// </summary>
	public decimal GridStartOffsetPips
	{
		get => _gridStartOffsetPips.Value;
		set => _gridStartOffsetPips.Value = value;
	}

	/// <summary>
	/// Distance between subsequent pending orders in pips.
	/// </summary>
	public decimal GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied to pending orders relative to one lot.
	/// </summary>
	public decimal GridVolume
	{
		get => _gridVolume.Value;
		set => _gridVolume.Value = value;
	}

	/// <summary>
	/// Expiration for pending orders measured in minutes.
	/// </summary>
	public int GridExpirationMinutes
	{
		get => _gridExpirationMinutes.Value;
		set => _gridExpirationMinutes.Value = value;
	}

	/// <summary>
	/// Volume of the initial market sell order.
	/// </summary>
	public decimal MarketVolume
	{
		get => _marketVolume.Value;
		set => _marketVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousFast = null;
		_previousMedium = null;
		_pipSize = 0m;
		_shortVolume = 0m;
		_averageEntryPrice = 0m;
		_gridOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };

		_fastStochastic = CreateStochastic(FastKPeriod, FastDPeriod, FastSlowing);
		_mediumStochastic = CreateStochastic(MediumKPeriod, MediumDPeriod, MediumSlowing);
		_slowStochastic = CreateStochastic(SlowKPeriod, SlowDPeriod, SlowSlowing);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_atr, _fastStochastic, _mediumStochastic, _slowStochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastStochastic);
			DrawIndicator(area, _mediumStochastic);
			DrawIndicator(area, _slowStochastic);
			DrawOwnTrades(area);
		}
	}

	private StochasticOscillator CreateStochastic(int kPeriod, int dPeriod, int slowing)
	{
		return new StochasticOscillator
		{
			Length = kPeriod,
			K = { Length = slowing },
			D = { Length = dPeriod }
		};
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue fastValue, IIndicatorValue mediumValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		CleanupGridOrders(candle.CloseTime);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!atrValue.IsFinal || !fastValue.IsFinal || !mediumValue.IsFinal || !slowValue.IsFinal)
			return;

		var atr = atrValue.ToDecimal();
		var fast = (StochasticOscillatorValue)fastValue;
		var medium = (StochasticOscillatorValue)mediumValue;
		var slow = (StochasticOscillatorValue)slowValue;

		if (fast.K is not decimal fastK || medium.K is not decimal mediumK || slow.K is not decimal slowK)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		if (_previousFast is null || _previousMedium is null)
		{
			_previousFast = fastK;
			_previousMedium = mediumK;
			return;
		}

		var atrOk = atr < AtrThreshold;
		var slowOk = slowK < LongTermOversoldLevel;
		var fastCross = _previousFast > OversoldLevel && fastK <= OversoldLevel;
		var mediumCross = _previousMedium > OversoldLevel && mediumK <= OversoldLevel;

		if (atrOk && slowOk && fastCross && mediumCross && Position == 0m && ActiveOrders.Count == 0)
		{
			EnterShortBasket(candle);
		}

		_previousFast = fastK;
		_previousMedium = mediumK;

		if (Position < 0m && _pipSize > 0m && ProfitTargetPips > 0m && _shortVolume > 0m)
		{
			var profitPips = (_averageEntryPrice - candle.ClosePrice) / _pipSize;
			if (profitPips >= ProfitTargetPips)
			{
				BuyMarket(Math.Abs(Position));
				CancelGridOrders();
			}
		}
	}

	private void EnterShortBasket(ICandleMessage candle)
	{
		var volume = MarketVolume;
		if (volume <= 0m)
			return;

		SellMarket(volume);

		if (GridOrdersCount > 0)
			PlaceGridOrders(candle);
	}

	private void PlaceGridOrders(ICandleMessage candle)
	{
		if (_pipSize <= 0m)
			return;

		CancelGridOrders();

		var referencePrice = candle.ClosePrice + GridStartOffsetPips * _pipSize;
		var step = GridStepPips * _pipSize;
		var volume = GridVolume;

		if (step == 0m || volume <= 0m)
			return;

		var expiration = GridExpirationMinutes > 0
			? candle.CloseTime + TimeSpan.FromMinutes(GridExpirationMinutes)
			: (DateTimeOffset?)null;

		for (var i = 0; i < GridOrdersCount; i++)
		{
			var price = referencePrice + i * step;
			if (price <= 0m)
				continue;

			var roundedPrice = RoundPrice(price);
			var order = SellStop(volume, roundedPrice);

			if (order != null)
				_gridOrders.Add(new GridOrderInfo(order, expiration));
		}
	}

	private void CleanupGridOrders(DateTimeOffset currentTime)
	{
		for (var i = _gridOrders.Count - 1; i >= 0; i--)
		{
			var info = _gridOrders[i];
			var order = info.Order;

			if (order.State == OrderStates.Done || order.State == OrderStates.Failed || order.State == OrderStates.Canceled)
			{
				_gridOrders.RemoveAt(i);
				continue;
			}

			if (info.Expiration is DateTimeOffset expiration && currentTime >= expiration)
			{
				CancelOrder(order);
				_gridOrders.RemoveAt(i);
			}
		}
	}

	private void CancelGridOrders()
	{
		foreach (var info in _gridOrders)
		{
			if (info.Order.State == OrderStates.Active)
				CancelOrder(info.Order);
		}

		_gridOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
			return;

		var tradePrice = trade.Trade?.Price ?? trade.Order.Price ?? 0m;
		var tradeVolume = trade.Trade?.Volume ?? trade.Order.Volume;

		if (tradeVolume <= 0m)
			return;

		if (trade.Order.Side == Sides.Sell)
		{
			var total = _shortVolume + tradeVolume;
			if (total > 0m)
				_averageEntryPrice = (_averageEntryPrice * _shortVolume + tradePrice * tradeVolume) / total;

			_shortVolume = total;
		}
		else if (trade.Order.Side == Sides.Buy)
		{
			_shortVolume -= tradeVolume;
			if (_shortVolume <= 0m)
			{
				_shortVolume = 0m;
				_averageEntryPrice = 0m;
				CancelGridOrders();
			}
		}

		for (var i = _gridOrders.Count - 1; i >= 0; i--)
		{
			if (_gridOrders[i].Order == trade.Order)
				_gridOrders.RemoveAt(i);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		var digits = 0;
		var value = step;

		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
			return step * 10m;

		return step;
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
			return price;

		return Math.Round(price / step.Value, MidpointRounding.AwayFromZero) * step.Value;
	}

	private sealed class GridOrderInfo
	{
		public GridOrderInfo(Order order, DateTimeOffset? expiration)
		{
			Order = order;
			Expiration = expiration;
		}

		public Order Order { get; }
		public DateTimeOffset? Expiration { get; }
	}
}
