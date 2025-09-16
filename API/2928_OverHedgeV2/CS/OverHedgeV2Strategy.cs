using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid hedging strategy converted from the OverHedge V2 MQL expert.
/// It alternates long and short entries inside a price tunnel and closes the cycle after
/// the combined unrealized profit reaches the configured target.
/// </summary>
public class OverHedgeV2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _baseMultiplier;
	private readonly StrategyParam<bool> _shutdownGrid;
	private readonly StrategyParam<decimal> _tunnelWidthPips;
	private readonly StrategyParam<decimal> _profitTargetPips;
	private readonly StrategyParam<decimal> _minProfitTargetPips;
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _longEmaPeriod;
	private readonly StrategyParam<decimal> _minDistancePips;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _shortEma;
	private EMA _longEma;

	private decimal _priceStep;
	private decimal _tunnelWidthValue;
	private decimal _profitTargetValue;
	private decimal _minProfitTargetValue;
	private decimal _minDistanceValue;

	private decimal _startBuyPrice;
	private decimal _startSellPrice;
	private bool _okToBuy;
	private bool _okToSell;
	private bool _firstDirectionSell;
	private bool _closeAllPositions;
	private bool _tradingStopped;

	private decimal _bestBid;
	private decimal _bestAsk;

	private readonly List<TradeDirection> _openTrades = new();

	/// <summary>
	/// Initial order size.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied after each filled order.
	/// </summary>
	public decimal BaseMultiplier
	{
		get => _baseMultiplier.Value;
		set => _baseMultiplier.Value = value;
	}

	/// <summary>
	/// When true the strategy liquidates and stops trading.
	/// </summary>
	public bool ShutdownGrid
	{
		get => _shutdownGrid.Value;
		set => _shutdownGrid.Value = value;
	}

	/// <summary>
	/// Additional tunnel width measured in pips.
	/// </summary>
	public decimal TunnelWidthPips
	{
		get => _tunnelWidthPips.Value;
		set => _tunnelWidthPips.Value = value;
	}

	/// <summary>
	/// Target profit per cycle in pips.
	/// </summary>
	public decimal ProfitTargetPips
	{
		get => _profitTargetPips.Value;
		set => _profitTargetPips.Value = value;
	}

	/// <summary>
	/// Minimal per-position profit before the basket can close.
	/// </summary>
	public decimal MinProfitTargetPips
	{
		get => _minProfitTargetPips.Value;
		set => _minProfitTargetPips.Value = value;
	}

	/// <summary>
	/// Fast EMA length used for trend confirmation.
	/// </summary>
	public int ShortEmaPeriod
	{
		get => _shortEmaPeriod.Value;
		set => _shortEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length used for trend confirmation.
	/// </summary>
	public int LongEmaPeriod
	{
		get => _longEmaPeriod.Value;
		set => _longEmaPeriod.Value = value;
	}

	/// <summary>
	/// Minimum EMA distance in pips required to select a direction.
	/// </summary>
	public decimal MinDistancePips
	{
		get => _minDistancePips.Value;
		set => _minDistancePips.Value = value;
	}

	/// <summary>
	/// Primary candle type for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private enum TradeDirection
	{
		Buy,
		Sell
	}

	/// <summary>
	/// Initializes <see cref="OverHedgeV2Strategy"/> parameters.
	/// </summary>
	public OverHedgeV2Strategy()
	{
		_startVolume = Param(nameof(StartVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Start Volume", "Initial order size", "Position Sizing")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 1m, 0.01m);

		_baseMultiplier = Param(nameof(BaseMultiplier), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("Base Multiplier", "Multiplier applied after each fill", "Position Sizing")
			.SetCanOptimize(true)
			.SetOptimize(1m, 2m, 0.1m);

		_shutdownGrid = Param(nameof(ShutdownGrid), false)
			.SetDisplay("Shutdown Grid", "Force liquidate and stop trading", "Risk Management");

		_tunnelWidthPips = Param(nameof(TunnelWidthPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Tunnel Width (pips)", "Extra distance added on top of the current spread", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_profitTargetPips = Param(nameof(ProfitTargetPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target (pips)", "Total basket profit before exit", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_minProfitTargetPips = Param(nameof(MinProfitTargetPips), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Min Profit (pips)", "Minimum gain per side before closing", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Fast EMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(4, 20, 1);

		_longEmaPeriod = Param(nameof(LongEmaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Slow EMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 2);

		_minDistancePips = Param(nameof(MinDistancePips), 5m)
			.SetGreaterThanZero()
			.SetDisplay("EMA Distance (pips)", "Minimum EMA separation to define trend", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");
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

		_shortEma = null;
		_longEma = null;

		_priceStep = 0m;
		_tunnelWidthValue = 0m;
		_profitTargetValue = 0m;
		_minProfitTargetValue = 0m;
		_minDistanceValue = 0m;

		_startBuyPrice = 0m;
		_startSellPrice = 0m;
		_okToBuy = false;
		_okToSell = false;
		_firstDirectionSell = false;
		_closeAllPositions = false;
		_tradingStopped = false;

		_bestBid = 0m;
		_bestAsk = 0m;

		_openTrades.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_shortEma = new EMA { Length = ShortEmaPeriod };
		_longEma = new EMA { Length = LongEmaPeriod };

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var candleSubscription = SubscribeCandles(CandleType)
			.Bind(_shortEma, _longEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawIndicator(area, _shortEma);
			DrawIndicator(area, _longEma);
			DrawOwnTrades(area);
		}

		UpdatePipValues();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask;
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortEma, decimal longEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_priceStep <= 0m)
			UpdatePipValues();

		if (ShutdownGrid)
		{
			_tradingStopped = true;

			if (CloseAllPositions())
			{
				_openTrades.Clear();
				_closeAllPositions = false;
			}

			return;
		}

		var spread = Math.Max(_bestAsk - _bestBid, 0m);
		var tunnelSize = spread * 2m + _tunnelWidthValue;

		if (_closeAllPositions)
		{
			if (CloseAllPositions())
			{
				_openTrades.Clear();
				_closeAllPositions = false;
			}

			return;
		}

		if (!_tradingStopped && CheckProfit(candle.ClosePrice))
		{
			_closeAllPositions = true;
			return;
		}

		if (_tradingStopped)
			return;

		var buyCount = _openTrades.Count(d => d == TradeDirection.Buy);
		var sellCount = _openTrades.Count(d => d == TradeDirection.Sell);

		if (buyCount + sellCount == 0)
		{
			var direction = CheckDirection(shortEma, longEma);
			if (direction == 0)
				return;

			_okToBuy = direction > 0;
			_okToSell = direction < 0;
			_firstDirectionSell = direction < 0;

			var reference = _bestBid > 0m ? _bestBid : candle.ClosePrice;

			if (_okToBuy)
			{
				_startBuyPrice = reference;
				_startSellPrice = reference - tunnelSize;
			}

			if (_okToSell)
			{
				_startSellPrice = reference;
				_startBuyPrice = reference + tunnelSize;
			}
		}
		else
		{
			_okToBuy = true;
			_okToSell = true;
		}

		var totalTrades = buyCount + sellCount;
		var desiredVolume = CalculateNextVolume(totalTrades);
		if (desiredVolume <= 0m)
			return;

		var wantLong = true;
		if (totalTrades > 2)
			wantLong = false;
		if (_firstDirectionSell)
			wantLong = !wantLong;

		var askPrice = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;

		if (wantLong && _okToBuy && askPrice >= _startBuyPrice)
		{
			BuyMarket(desiredVolume);
			_openTrades.Add(TradeDirection.Buy);
		}
		else if (!wantLong && _okToSell && askPrice <= _startSellPrice)
		{
			SellMarket(desiredVolume);
			_openTrades.Add(TradeDirection.Sell);
		}
	}

	private int CheckDirection(decimal shortEma, decimal longEma)
	{
		if (shortEma - longEma > _minDistanceValue)
			return 1;

		if (longEma - shortEma > _minDistanceValue)
			return -1;

		return 0;
	}

	private decimal CalculateNextVolume(int totalTrades)
	{
		var multiplier = (decimal)Math.Pow((double)BaseMultiplier, totalTrades);
		var rawVolume = StartVolume * multiplier;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = decimal.Floor(rawVolume / step);
			rawVolume = steps * step;
		}

		var minVolume = Security?.VolumeMin ?? 0m;
		if (minVolume > 0m && rawVolume < minVolume)
			return 0m;

		var maxVolume = Security?.VolumeMax ?? 0m;
		if (maxVolume > 0m && rawVolume > maxVolume)
			rawVolume = maxVolume;

		return rawVolume;
	}

	private bool CheckProfit(decimal currentPrice)
	{
		if (Position == 0)
			return false;

		var entryPrice = Position.AveragePrice;
		var priceDiff = Math.Abs(currentPrice - entryPrice);

		if (priceDiff < _minProfitTargetValue)
			return false;

		var volume = Math.Abs(Position);
		var profit = priceDiff * volume;

		return profit >= _profitTargetValue;
	}

	private bool CloseAllPositions()
	{
		if (Position > 0)
		{
			SellMarket(Position);
			return false;
		}

		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			return false;
		}

		return true;
	}

	private void UpdatePipValues()
	{
		_priceStep = Security?.PriceStep ?? 0.0001m;
		if (_priceStep <= 0m)
			_priceStep = 0.0001m;

		_tunnelWidthValue = TunnelWidthPips * _priceStep;
		_profitTargetValue = ProfitTargetPips * _priceStep;
		_minProfitTargetValue = MinProfitTargetPips * _priceStep;
		_minDistanceValue = MinDistancePips * _priceStep;
	}
}
