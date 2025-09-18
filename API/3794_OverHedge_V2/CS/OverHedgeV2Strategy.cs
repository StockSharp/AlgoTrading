using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Converted from the MetaTrader "OverHedge V2" expert advisor.
/// Implements a hedged grid that alternates buy and sell positions
/// while scaling volume using a martingale style multiplier.
/// </summary>
public class OverHedgeV2Strategy : Strategy
{
	private const decimal VolumeTolerance = 1e-6m;

	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _hedgeMultiplier;
	private readonly StrategyParam<int> _tunnelWidth;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<int> _emaShortPeriod;
	private readonly StrategyParam<int> _emaLongPeriod;
	private readonly StrategyParam<bool> _shutdownGrid;
	private readonly StrategyParam<DataType> _candleType;

	private readonly HashSet<Order> _activeEntryOrders = new();
	private readonly HashSet<Order> _activeExitOrders = new();

	private decimal _priceStep;
	private decimal _stepPrice;
	private decimal _lastBid;
	private decimal _lastAsk;
	private bool _hasBid;
	private bool _hasAsk;
	private bool _allowBuy;
	private bool _allowSell;
	private bool _firstDirectionSell;
	private decimal _startBuyPrice;
	private decimal _startSellPrice;
	private int _directionSignal;
	private int _openTradeCount;
	private decimal _longExposure;
	private decimal _shortExposure;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;
	private bool _isClosingPhase;

	/// <summary>
	/// Initializes a new instance of the <see cref="OverHedgeV2Strategy"/> class.
	/// </summary>
	public OverHedgeV2Strategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Initial contract volume for the first hedge order.", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1m, 0.1m);

		_hedgeMultiplier = Param(nameof(HedgeMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Hedge Multiplier", "Multiplier applied to the volume of each subsequent hedge leg.", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1.5m, 3m, 0.5m);

		_tunnelWidth = Param(nameof(TunnelWidth), 20)
		.SetGreaterThanZero()
		.SetDisplay("Tunnel Width (points)", "Base distance in price points between alternating hedge orders.", "Levels")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);

		_profitTarget = Param(nameof(ProfitTarget), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Profit Target", "Open profit value that closes all hedge positions.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(50m, 300m, 50m);

		_emaShortPeriod = Param(nameof(EmaShortPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Short EMA", "Length of the fast EMA used for direction detection.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);

		_emaLongPeriod = Param(nameof(EmaLongPeriod), 21)
		.SetGreaterThanZero()
		.SetDisplay("Long EMA", "Length of the slow EMA used for direction detection.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(15, 40, 1);

		_shutdownGrid = Param(nameof(ShutdownGrid), false)
		.SetDisplay("Shutdown Grid", "If enabled the strategy closes all positions and stops trading.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used to calculate the EMA filters.", "Indicators");
	}

	/// <summary>
	/// Base volume used for the first hedge trade.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Volume multiplier for each subsequent hedge order.
	/// </summary>
	public decimal HedgeMultiplier
	{
		get => _hedgeMultiplier.Value;
		set => _hedgeMultiplier.Value = value;
	}

	/// <summary>
	/// Base tunnel distance between alternating entries expressed in price points.
	/// </summary>
	public int TunnelWidth
	{
		get => _tunnelWidth.Value;
		set => _tunnelWidth.Value = value;
	}

	/// <summary>
	/// Profit value that triggers closing of all hedge legs.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Length of the fast EMA.
	/// </summary>
	public int EmaShortPeriod
	{
		get => _emaShortPeriod.Value;
		set => _emaShortPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow EMA.
	/// </summary>
	public int EmaLongPeriod
	{
		get => _emaLongPeriod.Value;
		set => _emaLongPeriod.Value = value;
	}

	/// <summary>
	/// Whether the strategy should close positions and halt trading.
	/// </summary>
	public bool ShutdownGrid
	{
		get => _shutdownGrid.Value;
		set => _shutdownGrid.Value = value;
	}

	/// <summary>
	/// Candle data type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

		if (CandleType != null)
		yield return (Security, CandleType);

		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_activeEntryOrders.Clear();
		_activeExitOrders.Clear();
		_priceStep = 0m;
		_stepPrice = 0m;
		_lastBid = 0m;
		_lastAsk = 0m;
		_hasBid = false;
		_hasAsk = false;
		_directionSignal = 0;
		ResetCycle();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeInstrumentMetrics();

		var fastEma = new EMA { Length = EmaShortPeriod };
		var slowEma = new EMA { Length = EmaLongPeriod };

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(fastEma, slowEma, ProcessCandle)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Trade is null || trade.Order is null)
		return;

		var order = trade.Order;
		var direction = order.Direction;
		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;
		var isEntry = _activeEntryOrders.Contains(order);
		var isExit = _activeExitOrders.Contains(order);

		if (direction == Sides.Buy)
		{
			if (isExit)
			{
				var reduced = Math.Min(volume, _shortExposure);
				if (reduced > 0m)
				{
					_shortExposure = Math.Max(0m, _shortExposure - reduced);
					if (_shortExposure <= VolumeTolerance)
					{
						_shortExposure = 0m;
						_shortAveragePrice = 0m;
					}
				}
			}
			else
			{
				var total = _longExposure + volume;
				if (total > 0m)
				{
					_longAveragePrice = (_longExposure * _longAveragePrice + volume * price) / total;
					_longExposure = total;
					_openTradeCount++;
				}
			}
		}
		else if (direction == Sides.Sell)
		{
			if (isExit)
			{
				var reduced = Math.Min(volume, _longExposure);
				if (reduced > 0m)
				{
					_longExposure = Math.Max(0m, _longExposure - reduced);
					if (_longExposure <= VolumeTolerance)
					{
						_longExposure = 0m;
						_longAveragePrice = 0m;
					}
				}
			}
			else
			{
				var total = _shortExposure + volume;
				if (total > 0m)
				{
					_shortAveragePrice = (_shortExposure * _shortAveragePrice + volume * price) / total;
					_shortExposure = total;
					_openTradeCount++;
				}
			}
		}

		if (order.Balance <= 0m || IsOrderCompleted(order))
		{
			_activeEntryOrders.Remove(order);
			_activeExitOrders.Remove(order);
		}

		if (_isClosingPhase && !HasExposure() && !HasPendingExitOrders())
		{
			ResetCycle();
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnline)
		return;

		_directionSignal = Math.Sign(fastValue - slowValue);
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;
			if (bid > 0m)
			{
				_lastBid = bid;
				_hasBid = true;
			}
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		{
			var ask = (decimal)askValue;
			if (ask > 0m)
			{
				_lastAsk = ask;
				_hasAsk = true;
			}
		}

		if (!_hasBid || !_hasAsk)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (ShutdownGrid)
		{
			RequestCloseAllPositions();
			return;
		}

		if (ProfitTarget > 0m && HasExposure())
		{
			var openProfit = CalculateOpenProfit();
			if (openProfit >= ProfitTarget)
			{
				LogInfo($"Profit target reached with unrealized profit {openProfit:F2}. Closing hedge cycle.");
				RequestCloseAllPositions();
				return;
			}
		}

		if (_isClosingPhase)
		return;

		if (!HasExposure())
		{
			if (_directionSignal == 0)
			return;

			InitializeCycle();
		}

		var nextVolume = CalculateNextVolume();
		if (nextVolume <= 0m)
		return;

		var wantLong = ShouldEnterLong();

		if (wantLong)
		{
			if (!_allowBuy || HasPendingEntryOrder(Sides.Buy))
			return;

			if (_startBuyPrice > 0m && _lastAsk >= _startBuyPrice)
			{
				OpenLong(nextVolume);
			}
		}
		else
		{
			if (!_allowSell || HasPendingEntryOrder(Sides.Sell))
			return;

			if (_startSellPrice > 0m && _lastAsk <= _startSellPrice)
			{
				OpenShort(nextVolume);
			}
		}
	}

	private void InitializeInstrumentMetrics()
	{
		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		_priceStep = 0.0001m;

		_stepPrice = Security?.StepPrice ?? 0m;
		if (_stepPrice <= 0m)
		_stepPrice = _priceStep;
	}

	private void InitializeCycle()
	{
		_allowBuy = _directionSignal > 0;
		_allowSell = _directionSignal < 0;
		_firstDirectionSell = _directionSignal < 0;

		var tunnelOffset = CalculateTunnelOffset();

		if (_allowBuy)
		{
			_startBuyPrice = _lastBid;
			_startSellPrice = _lastBid - tunnelOffset;
		}

		if (_allowSell)
		{
			_startSellPrice = _lastBid;
			_startBuyPrice = _lastBid + tunnelOffset;
		}

		_openTradeCount = 0;
		_isClosingPhase = false;

		var directionText = _allowBuy && !_allowSell ? "Buy" : _allowSell && !_allowBuy ? "Sell" : "Both";
		LogInfo($"New hedge cycle initialized. Direction={directionText}. StartBuy={_startBuyPrice}, StartSell={_startSellPrice}");
	}

	private void OpenLong(decimal volume)
	{
		var normalized = NormalizeVolume(volume);
		if (normalized <= 0m)
		return;

		var order = BuyMarket(normalized);
		if (order != null)
		{
			_activeEntryOrders.Add(order);
			LogInfo($"Opening long hedge leg. Volume={normalized}, Ask={_lastAsk}");
		}
	}

	private void OpenShort(decimal volume)
	{
		var normalized = NormalizeVolume(volume);
		if (normalized <= 0m)
		return;

		var order = SellMarket(normalized);
		if (order != null)
		{
			_activeEntryOrders.Add(order);
			LogInfo($"Opening short hedge leg. Volume={normalized}, Bid={_lastBid}");
		}
	}

	private void RequestCloseAllPositions()
	{
		if (!HasExposure())
		{
			ResetCycle();
			return;
		}

		_isClosingPhase = true;

		if (_longExposure > VolumeTolerance && !HasPendingExitOrder(Sides.Sell))
		{
			var order = SellMarket(NormalizeVolume(_longExposure));
			if (order != null)
			{
				_activeExitOrders.Add(order);
				LogInfo($"Closing long exposure volume={_longExposure} at bid price {_lastBid}.");
			}
		}

		if (_shortExposure > VolumeTolerance && !HasPendingExitOrder(Sides.Buy))
		{
			var order = BuyMarket(NormalizeVolume(_shortExposure));
			if (order != null)
			{
				_activeExitOrders.Add(order);
				LogInfo($"Closing short exposure volume={_shortExposure} at ask price {_lastAsk}.");
			}
		}
	}

	private decimal CalculateTunnelOffset()
	{
		var spread = Math.Max(0m, _lastAsk - _lastBid);
		var points = _priceStep > 0m ? spread / _priceStep : 0m;
		var tunnelPoints = 2m * points + TunnelWidth;
		var offset = tunnelPoints * _priceStep;
		return offset > 0m ? offset : TunnelWidth * (_priceStep > 0m ? _priceStep : 0.0001m);
	}

	private decimal CalculateNextVolume()
	{
		var baseVolume = BaseVolume;
		if (baseVolume <= 0m)
		return 0m;

		var multiplier = HedgeMultiplier;
		if (multiplier <= 0m)
		return 0m;

		var factor = (decimal)Math.Pow((double)multiplier, _openTradeCount);
		return baseVolume * factor;
	}

	private bool ShouldEnterLong()
	{
		var wantLong = (_openTradeCount % 2) == 0;
		if (_firstDirectionSell)
		wantLong = !wantLong;
		return wantLong;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step <= 0m)
		return volume;

		var normalized = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;
		if (normalized <= 0m)
		normalized = step;
		return normalized;
	}

	private bool HasExposure()
	{
		return _longExposure > VolumeTolerance || _shortExposure > VolumeTolerance;
	}

	private decimal CalculateOpenProfit()
	{
		var factor = _priceStep > 0m ? (_stepPrice > 0m ? _stepPrice / _priceStep : 1m) : 1m;
		var profit = 0m;

		if (_longExposure > VolumeTolerance)
		{
			profit += (_lastBid - _longAveragePrice) * _longExposure * factor;
		}

		if (_shortExposure > VolumeTolerance)
		{
			profit += (_shortAveragePrice - _lastAsk) * _shortExposure * factor;
		}

		return profit;
	}

	private bool HasPendingEntryOrder(Sides side)
	{
		foreach (var order in _activeEntryOrders)
		{
			if (order.Direction == side && !IsOrderCompleted(order))
			return true;
		}

		return false;
	}

	private bool HasPendingExitOrder(Sides side)
	{
		foreach (var order in _activeExitOrders)
		{
			if (order.Direction == side && !IsOrderCompleted(order))
			return true;
		}

		return false;
	}

	private bool HasPendingExitOrders()
	{
		foreach (var order in _activeExitOrders)
		{
			if (!IsOrderCompleted(order))
			return true;
		}

		return false;
	}

	private void ResetCycle()
	{
		_allowBuy = false;
		_allowSell = false;
		_firstDirectionSell = false;
		_startBuyPrice = 0m;
		_startSellPrice = 0m;
		_openTradeCount = 0;
		_longExposure = 0m;
		_shortExposure = 0m;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;
		_isClosingPhase = false;
	}
}
