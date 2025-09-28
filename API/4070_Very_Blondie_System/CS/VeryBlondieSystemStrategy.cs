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
/// Grid-based mean reversion strategy converted from the MT4 expert advisor "VBS - Very Blondie System".
/// Places an initial market order and four layered limit orders when price deviates from the recent range.
/// Closes the whole basket once the floating profit reaches the cash target or the break-even guard is triggered.
/// </summary>
public class VeryBlondieSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _gridOrderCount;

	private readonly StrategyParam<int> _periodLength;
	private readonly StrategyParam<decimal> _limitPoints;
	private readonly StrategyParam<decimal> _gridPoints;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _lockDownPoints;
	private readonly StrategyParam<decimal> _pointValue;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private readonly List<Order> _gridOrders = new();

	private decimal _pointSize;
	private decimal _limitDistance;
	private decimal _gridDistance;
	private decimal _lockDistance;
	private decimal _pnlConversion;

	private decimal? _longBreakEven;
	private decimal? _shortBreakEven;

	private decimal? _bestBid;
	private decimal? _bestAsk;

	/// <summary>
	/// Initializes a new instance of the <see cref="VeryBlondieSystemStrategy"/> class.
	/// </summary>
	public VeryBlondieSystemStrategy()
	{
		_periodLength = Param(nameof(PeriodLength), 60)
		.SetDisplay("Lookback Bars", "Number of candles for range calculation", "General")
		.SetGreaterThanZero();

		_limitPoints = Param(nameof(LimitPoints), 1000m)
		.SetDisplay("Deviation Threshold", "Distance in points between price and range extreme", "General")
		.SetRange(0m, 100_000m)
		.SetCanOptimize(true);

		_gridPoints = Param(nameof(GridPoints), 1500m)
		.SetDisplay("Grid Step", "Distance in points between consecutive limit orders", "Orders")
		.SetRange(0m, 100_000m)
		.SetCanOptimize(true);

		_gridOrderCount = Param(nameof(GridOrderCount), 4)
		.SetGreaterThanZero()
		.SetDisplay("Grid Orders", "Number of layered limit orders per side", "Orders");

		_profitTarget = Param(nameof(ProfitTarget), 40m)
		.SetDisplay("Profit Target", "Floating profit target in account currency", "Risk")
		.SetRange(0m, 100_000m)
		.SetCanOptimize(true);

		_lockDownPoints = Param(nameof(LockDownPoints), 0m)
		.SetDisplay("Lockdown Points", "Distance in points before activating break-even protection", "Risk")
		.SetRange(0m, 100_000m)
		.SetCanOptimize(true);

		_pointValue = Param(nameof(PointValue), 0m)
		.SetDisplay("Point Value", "Price change produced by one MT4 point (0 = auto from security)", "General")
		.SetRange(0m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series used by the strategy", "Data");
	}

	/// <summary>
	/// Number of candles used to measure the highest high and lowest low.
	/// </summary>
	public int PeriodLength
	{
		get => _periodLength.Value;
		set => _periodLength.Value = value;
	}

	/// <summary>
	/// Required distance between the current bid and the range extremes.
	/// </summary>
	public decimal LimitPoints
	{
		get => _limitPoints.Value;
		set => _limitPoints.Value = value;
	}

	/// <summary>
	/// Spacing in points between grid limit orders.
	/// </summary>
	public decimal GridPoints
	{
		get => _gridPoints.Value;
		set => _gridPoints.Value = value;
	}

	/// <summary>
	/// Floating profit target that closes every open position.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Distance in points that arms the break-even exit.
	/// </summary>
	public decimal LockDownPoints
	{
		get => _lockDownPoints.Value;
		set => _lockDownPoints.Value = value;
	}

	/// <summary>
	/// Price increment produced by a single MT4 point.
	/// </summary>
	public decimal PointValue
	{
		get => _pointValue.Value;
		set => _pointValue.Value = value;
	}

	/// <summary>
	/// Number of grid orders placed on each side of the market.
	/// </summary>
	public int GridOrderCount
	{
		get => _gridOrderCount.Value;
		set => _gridOrderCount.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations and trading decisions.
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

		_gridOrders.Clear();
		_longBreakEven = null;
		_shortBreakEven = null;
		_bestBid = null;
		_bestAsk = null;
		_pointSize = 0m;
		_limitDistance = 0m;
		_gridDistance = 0m;
		_lockDistance = 0m;
		_pnlConversion = 1m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializePointMetrics();

		_highest = new Highest { Length = PeriodLength, CandlePrice = CandlePrice.High };
		_lowest = new Lowest { Length = PeriodLength, CandlePrice = CandlePrice.Low };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_highest, _lowest, ProcessCandle)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
		_bestBid = bidPrice;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
		_bestAsk = askPrice;
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		// Work only with finished candles to match the MT4 bar-based logic.
		if (candle.State != CandleStates.Finished)
		return;

		// Indicators must be fully warmed up before trading.
		if (!_highest.IsFormed || !_lowest.IsFormed)
		return;

		// Ensure trading is allowed and all required connections are alive.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (HasActiveExposure())
		{
			if (TryHandleProfitTarget(candle))
			return;

			ApplyLockDown(candle);
			return;
		}

		TryOpenCycle(candle, highest, lowest);
	}

	private void TryOpenCycle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		var bestBid = GetBestBid(candle.ClosePrice);
		var bestAsk = GetBestAsk(candle.ClosePrice);

		// Buy setup: price is sufficiently below the recent high.
		if (highest - bestBid > _limitDistance && _limitDistance > 0m)
		{
			OpenBuyCycle(bestAsk);
			return;
		}

		// Sell setup: price is sufficiently above the recent low.
		if (bestBid - lowest > _limitDistance && _limitDistance > 0m)
		{
			OpenSellCycle(bestBid);
		}
	}

	private void OpenBuyCycle(decimal bestAsk)
	{
		var volume = ResolveBaseVolume();
		if (volume <= 0m)
		return;

		// Enter the first market position immediately.
		BuyMarket(volume);

		if (_gridDistance <= 0m)
		return;

		for (var level = 1; level <= GridOrderCount; level++)
		{
			var limitPrice = NormalizePrice(bestAsk - level * _gridDistance);
			if (limitPrice <= 0m)
			continue;

			var limitVolume = NormalizeVolume(volume * Pow2(level));
			if (limitVolume <= 0m)
			continue;

			var order = BuyLimit(limitPrice, limitVolume);
			if (order != null)
			_gridOrders.Add(order);
		}
	}

	private void OpenSellCycle(decimal bestBid)
	{
		var volume = ResolveBaseVolume();
		if (volume <= 0m)
		return;

		// Enter the first market position immediately.
		SellMarket(volume);

		if (_gridDistance <= 0m)
		return;

		for (var level = 1; level <= GridOrderCount; level++)
		{
			var limitPrice = NormalizePrice(bestBid + level * _gridDistance);
			if (limitPrice <= 0m)
			continue;

			var limitVolume = NormalizeVolume(volume * Pow2(level));
			if (limitVolume <= 0m)
			continue;

			var order = SellLimit(limitPrice, limitVolume);
			if (order != null)
			_gridOrders.Add(order);
		}
	}

	private bool TryHandleProfitTarget(ICandleMessage candle)
	{
		if (ProfitTarget <= 0m)
		return false;

		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
		return false;

		var pnl = (candle.ClosePrice - entryPrice) * Position * _pnlConversion;
		if (pnl >= ProfitTarget)
		{
			CloseAllPositionsAndOrders();
			return true;
		}

		return false;
	}

	private void ApplyLockDown(ICandleMessage candle)
	{
		if (_lockDistance <= 0m || Position == 0m)
		return;

		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
		return;

		if (Position > 0m)
		{
			// Arm the break-even guard once price moves by LockDown points.
			if (!_longBreakEven.HasValue && candle.ClosePrice - entryPrice >= _lockDistance)
			_longBreakEven = entryPrice + _pointSize;

			if (_longBreakEven.HasValue && candle.LowPrice <= _longBreakEven.Value)
			{
				CloseAllPositionsAndOrders();
				_longBreakEven = null;
			}
		}
		else if (Position < 0m)
		{
			if (!_shortBreakEven.HasValue && entryPrice - candle.ClosePrice >= _lockDistance)
			_shortBreakEven = entryPrice - _pointSize;

			if (_shortBreakEven.HasValue && candle.HighPrice >= _shortBreakEven.Value)
			{
				CloseAllPositionsAndOrders();
				_shortBreakEven = null;
			}
		}
	}

	private void CloseAllPositionsAndOrders()
	{
		var exposure = Position;
		if (exposure > 0m)
		SellMarket(exposure);
		else if (exposure < 0m)
		BuyMarket(Math.Abs(exposure));

		CancelPendingGridOrders();
	}

	private void CancelPendingGridOrders()
	{
		foreach (var order in _gridOrders)
		{
			if (order.State == OrderStates.Active)
			CancelOrder(order);
		}

		_gridOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order == null || order.Security != Security)
		return;

		if (order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_gridOrders.Remove(order);
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_longBreakEven = null;
			_shortBreakEven = null;
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelPendingGridOrders();
		base.OnStopped();
	}

	private bool HasActiveExposure()
	{
		if (Position != 0m)
		return true;

		foreach (var order in _gridOrders)
		{
			if (order.State == OrderStates.Active)
			return true;
		}

		return false;
	}

	private decimal ResolveBaseVolume()
	{
		var portfolio = Portfolio;
		var balance = portfolio?.CurrentValue ?? portfolio?.BeginValue;
		if (balance is decimal b && b > 0m)
		{
			var lots = Math.Round(b / 100m, MidpointRounding.AwayFromZero) / 1000m;
			var normalized = NormalizeVolume(lots);
			if (normalized > 0m)
			return normalized;
		}

		var fallback = NormalizeVolume(Volume);
		if (fallback > 0m)
		return fallback;

		return NormalizeVolume(1m);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security?.VolumeStep is decimal step && step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (security?.MinVolume is decimal minVolume && minVolume > 0m && volume < minVolume)
		return 0m;

		if (security?.MaxVolume is decimal maxVolume && maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m)
		{
			var steps = Math.Round(price / step, 0, MidpointRounding.AwayFromZero);
			price = steps * step;
			price = security.ShrinkPrice(price);
		}

		return price;
	}

	private decimal GetBestBid(decimal fallback)
	{
		if (_bestBid is decimal bid && bid > 0m)
		return bid;

		var security = Security;
		if (security?.BestBid?.Price is decimal bestBid && bestBid > 0m)
		return bestBid;

		return fallback;
	}

	private decimal GetBestAsk(decimal fallback)
	{
		if (_bestAsk is decimal ask && ask > 0m)
		return ask;

		var security = Security;
		if (security?.BestAsk?.Price is decimal bestAsk && bestAsk > 0m)
		return bestAsk;

		return fallback;
	}

	private void InitializePointMetrics()
	{
		_pointSize = ResolvePointValue();
		if (_pointSize <= 0m)
		_pointSize = 1m;

		_limitDistance = LimitPoints * _pointSize;
		_gridDistance = GridPoints * _pointSize;
		_lockDistance = LockDownPoints * _pointSize;
		_pnlConversion = ResolvePnlConversion();
	}

	private decimal ResolvePointValue()
	{
		if (PointValue > 0m)
		return PointValue;

		var security = Security;
		if (security?.PriceStep is decimal priceStep && priceStep > 0m)
		return priceStep;

		if (security?.Step is decimal step && step > 0m)
		return step;

		return 1m;
	}

	private decimal ResolvePnlConversion()
	{
		var security = Security;
		if (security?.StepPrice is decimal stepPrice && stepPrice > 0m)
		{
			var step = security.PriceStep ?? security.Step ?? 0m;
			if (step > 0m)
			return stepPrice / step;
		}

		var multiplier = security?.Multiplier ?? 1m;
		return multiplier <= 0m ? 1m : multiplier;
	}

	private static decimal Pow2(int exponent)
	{
		var result = 1m;
		for (var i = 0; i < exponent; i++)
		result *= 2m;
		return result;
	}
}
