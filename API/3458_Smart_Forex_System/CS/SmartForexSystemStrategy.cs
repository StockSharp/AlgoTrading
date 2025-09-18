using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Smart Forex System" MetaTrader expert advisor that combines a momentum filter
/// with a martingale-style averaging grid and adaptive take-profit management.
/// </summary>
public class SmartForexSystemStrategy : Strategy
{
	private readonly StrategyParam<StartMode> _mode;
	private readonly StrategyParam<decimal> _percentThreshold;
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<decimal> _gridStepPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _firstTakeProfitPips;
	private readonly StrategyParam<decimal> _gridTakeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<GridEntry> _buyEntries = new();
	private readonly List<GridEntry> _sellEntries = new();

	private ICandleMessage? _previousCandle;
	private decimal? _referenceClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="SmartForexSystemStrategy"/> class.
	/// </summary>
	public SmartForexSystemStrategy()
	{
		_mode = Param(nameof(Mode), StartMode.LongAndShort)
			.SetDisplay("Trading Mode", "Directional filter for opening baskets", "Trading")
			.SetCanOptimize(true);

		_percentThreshold = Param(nameof(PercentThreshold), 1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Momentum Threshold", "Minimum relative price change (in pips) required for the signal", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_startVolume = Param(nameof(StartVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Start Volume", "Initial order volume for a new basket", "Money management")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 1m, 0.01m);

		_maxVolume = Param(nameof(MaxVolume), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Max Volume", "Absolute cap for a single order volume", "Money management")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_lotExponent = Param(nameof(LotExponent), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Multiplier applied to each additional grid order", "Money management")
			.SetCanOptimize(true)
			.SetOptimize(1.1m, 3m, 0.1m);

		_gridStepPips = Param(nameof(GridStepPips), 26m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step", "Minimum distance in pips before adding to the basket", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 5m);

		_maxTrades = Param(nameof(MaxTrades), 12)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of positions allowed per direction", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_firstTakeProfitPips = Param(nameof(FirstTakeProfitPips), 30m)
			.SetGreaterThanZero()
			.SetDisplay("First Take Profit", "Take-profit distance in pips for the very first order", "Exits")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 5m);

		_gridTakeProfitPips = Param(nameof(GridTakeProfitPips), 7m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Take Profit", "Take-profit distance in pips when the basket already contains several orders", "Exits")
			.SetCanOptimize(true)
			.SetOptimize(3m, 30m, 1m);

		_stopLossPips = Param(nameof(StopLossPips), 400m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Hard stop distance in pips from the latest order price", "Exits")
			.SetCanOptimize(true)
			.SetOptimize(50m, 600m, 25m);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(1)))
			.SetDisplay("Candle Type", "Primary timeframe used for signal evaluation", "Signals");
	}

	/// <summary>
	/// Determines which market directions the strategy is allowed to trade.
	/// </summary>
	public StartMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Minimum relative price change (in pseudo-pips) that activates the momentum filter.
	/// </summary>
	public decimal PercentThreshold
	{
		get => _percentThreshold.Value;
		set => _percentThreshold.Value = value;
	}

	/// <summary>
	/// Initial order volume for a brand-new averaging basket.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Absolute ceiling for any individual order volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to every subsequent order volume in the grid.
	/// </summary>
	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	/// <summary>
	/// Minimum distance in pips before another averaging order can be added.
	/// </summary>
	public decimal GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of grid entries per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips for a single isolated order.
	/// </summary>
	public decimal FirstTakeProfitPips
	{
		get => _firstTakeProfitPips.Value;
		set => _firstTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips that is applied once the basket contains multiple orders.
	/// </summary>
	public decimal GridTakeProfitPips
	{
		get => _gridTakeProfitPips.Value;
		set => _gridTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips measured from the latest entry price.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Candle type that defines the working timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		var point = GetPointValue();
		if (point <= 0m)
		{
			_previousCandle = candle;
			_referenceClose ??= candle.ClosePrice;
			return;
		}

		ManageOpenBaskets(candle.ClosePrice, point);

		if (Mode != StartMode.Off)
		{
			var signal = DetermineSignal(candle);

			if (signal == TradeDirection.Buy && AllowsLongs() && _buyEntries.Count == 0)
			{
				OpenBuy(candle.ClosePrice, StartVolume);
			}
			else if (signal == TradeDirection.Sell && AllowsShorts() && _sellEntries.Count == 0)
			{
				OpenSell(candle.ClosePrice, StartVolume);
			}

			TryOpenNextBuy(candle.ClosePrice, point);
			TryOpenNextSell(candle.ClosePrice, point);
		}

		_previousCandle = candle;
		_referenceClose = candle.ClosePrice;
	}

	private TradeDirection DetermineSignal(ICandleMessage candle)
	{
		if (_previousCandle == null || _referenceClose == null || _referenceClose.Value == 0m)
			return TradeDirection.None;

		var previousOpen = _previousCandle.OpenPrice;
		var previousClose = _previousCandle.ClosePrice;
		var force = (candle.ClosePrice - _referenceClose.Value) / _referenceClose.Value * 10000m;

		if (previousClose < previousOpen && force <= -PercentThreshold)
			return TradeDirection.Buy;

		if (previousClose > previousOpen && force >= PercentThreshold)
			return TradeDirection.Sell;

		return TradeDirection.None;
	}

	private void ManageOpenBaskets(decimal price, decimal point)
	{
		if (_buyEntries.Count > 0)
		{
			var latestBuy = _buyEntries[^1].Price;
			var stop = latestBuy - StopLossPips * point;
			if (StopLossPips > 0m && price <= stop)
			{
				CloseBuys();
			}

			if (_buyEntries.Count > 0)
			{
				var average = GetAveragePrice(_buyEntries);
				var targetDistance = _buyEntries.Count == 1 ? FirstTakeProfitPips : GridTakeProfitPips;
				if (targetDistance > 0m)
				{
					var target = average + targetDistance * point;
					if (price >= target)
						CloseBuys();
				}
			}
		}

		if (_sellEntries.Count > 0)
		{
			var latestSell = _sellEntries[^1].Price;
			var stop = latestSell + StopLossPips * point;
			if (StopLossPips > 0m && price >= stop)
			{
				CloseSells();
			}

			if (_sellEntries.Count > 0)
			{
				var average = GetAveragePrice(_sellEntries);
				var targetDistance = _sellEntries.Count == 1 ? FirstTakeProfitPips : GridTakeProfitPips;
				if (targetDistance > 0m)
				{
					var target = average - targetDistance * point;
					if (price <= target)
						CloseSells();
				}
			}
		}
	}

	private void TryOpenNextBuy(decimal price, decimal point)
	{
		if (!AllowsLongs() || _buyEntries.Count == 0)
			return;

		if (_buyEntries.Count >= MaxTrades)
			return;

		var lastPrice = _buyEntries[^1].Price;
		var distance = lastPrice - price;
		var required = GridStepPips * point;

		if (distance >= required)
		{
			var volume = CalculateNextVolume(_buyEntries);
			OpenBuy(price, volume);
		}
	}

	private void TryOpenNextSell(decimal price, decimal point)
	{
		if (!AllowsShorts() || _sellEntries.Count == 0)
			return;

		if (_sellEntries.Count >= MaxTrades)
			return;

		var lastPrice = _sellEntries[^1].Price;
		var distance = price - lastPrice;
		var required = GridStepPips * point;

		if (distance >= required)
		{
			var volume = CalculateNextVolume(_sellEntries);
			OpenSell(price, volume);
		}
	}

	private void OpenBuy(decimal price, decimal volume)
	{
		var adjusted = AdjustVolume(volume);
		if (adjusted <= 0m)
			return;

		BuyMarket(adjusted);
		_buyEntries.Add(new GridEntry(price, adjusted));
	}

	private void OpenSell(decimal price, decimal volume)
	{
		var adjusted = AdjustVolume(volume);
		if (adjusted <= 0m)
			return;

		SellMarket(adjusted);
		_sellEntries.Add(new GridEntry(price, adjusted));
	}

	private void CloseBuys()
	{
		var total = GetTotalVolume(_buyEntries);
		if (total <= 0m)
			return;

		SellMarket(total);
		_buyEntries.Clear();
	}

	private void CloseSells()
	{
		var total = GetTotalVolume(_sellEntries);
		if (total <= 0m)
			return;

		BuyMarket(total);
		_sellEntries.Clear();
	}

	private decimal CalculateNextVolume(List<GridEntry> entries)
	{
		if (entries.Count == 0)
			return StartVolume;

		var lastVolume = entries[^1].Volume;
		var proposed = lastVolume * LotExponent;
		if (proposed <= 0m)
			proposed = StartVolume;

		return proposed;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var result = volume;

		if (MaxVolume > 0m && result > MaxVolume)
			result = MaxVolume;

		var minVolume = Security.MinVolume ?? 0m;
		var maxVolume = Security.MaxVolume;
		var step = Security.VolumeStep ?? 0m;

		if (result < minVolume)
			result = minVolume;

		if (maxVolume.HasValue && result > maxVolume.Value)
			result = maxVolume.Value;

		if (step > 0m)
		{
			var steps = Math.Round(result / step, MidpointRounding.AwayFromZero);
			result = steps * step;
		}

		return result;
	}

	private static decimal GetTotalVolume(List<GridEntry> entries)
	{
		decimal total = 0m;
		for (var i = 0; i < entries.Count; i++)
			total += entries[i].Volume;
		return total;
	}

	private static decimal GetAveragePrice(List<GridEntry> entries)
	{
		decimal volume = 0m;
		decimal weighted = 0m;
		for (var i = 0; i < entries.Count; i++)
		{
			var entry = entries[i];
			volume += entry.Volume;
			weighted += entry.Price * entry.Volume;
		}

		return volume > 0m ? weighted / volume : 0m;
	}

	private decimal GetPointValue()
	{
		var point = Security.PriceStep;
		if (point == null || point == 0m)
			return 0.0001m;
		return point.Value;
	}

	private bool AllowsLongs()
	{
		return Mode == StartMode.LongOnly || Mode == StartMode.LongAndShort;
	}

	private bool AllowsShorts()
	{
		return Mode == StartMode.ShortOnly || Mode == StartMode.LongAndShort;
	}

	/// <summary>
	/// Directional trading modes supported by the strategy.
	/// </summary>
	public enum StartMode
	{
		ShortOnly = 1,
		LongOnly = 2,
		LongAndShort = 3,
		Off = 4,
	}

	private enum TradeDirection
	{
		None,
		Buy,
		Sell,
	}

	private readonly struct GridEntry
	{
		public GridEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }

		public decimal Volume { get; }
	}
}
