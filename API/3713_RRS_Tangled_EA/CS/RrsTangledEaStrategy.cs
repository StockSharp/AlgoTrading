namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo.Candles;

/// <summary>
/// Randomized hedging strategy converted from the MetaTrader "RRS Tangled EA" advisor.
/// </summary>
public class RrsTangledEaStrategy : Strategy
{
	/// <summary>
	/// Risk handling modes that mirror the original MetaTrader inputs.
	/// </summary>
	public enum RiskModes
	{
		/// <summary>
		/// Risk a fixed monetary amount.
		/// </summary>
		FixedMoney,
		/// <summary>
		/// Risk a percentage of the account balance.
		/// </summary>
		BalancePercentage,
	}

	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStartPips;
	private readonly StrategyParam<decimal> _trailingGapPips;
	private readonly StrategyParam<decimal> _maxSpreadPips;
	private readonly StrategyParam<int> _maxOpenTrades;
	private readonly StrategyParam<RiskModes> _riskMode;
	private readonly StrategyParam<decimal> _riskAmount;
	private readonly StrategyParam<string> _tradeComment;
	private readonly StrategyParam<string> _notes;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<TradeEntry> _buyEntries = new();
	private readonly List<TradeEntry> _sellEntries = new();

	private Random _random = new(Environment.TickCount);
	private decimal _point;
	private decimal? _buyTrailingStop;
	private decimal? _sellTrailingStop;
	private decimal? _lastSpread;
	private decimal _initialBalance;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public RrsTangledEaStrategy()
	{
		_minVolume = Param(nameof(MinVolume), 0.01m)
			.SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
			.SetGreaterThanZero();

		_maxVolume = Param(nameof(MaxVolume), 0.50m)
			.SetDisplay("Maximum Volume", "Upper bound for random position sizing", "Money Management")
			.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
			.SetDisplay("Take Profit (pips)", "Distance in pips for profit targets", "Risk")
			.SetRange(0m, 10_000m);

		_stopLossPips = Param(nameof(StopLossPips), 200m)
			.SetDisplay("Stop Loss (pips)", "Distance in pips for protective stops", "Risk")
			.SetRange(0m, 10_000m);

		_trailingStartPips = Param(nameof(TrailingStartPips), 50m)
			.SetDisplay("Trailing Start (pips)", "Activation distance for the trailing logic", "Risk")
			.SetRange(0m, 10_000m);

		_trailingGapPips = Param(nameof(TrailingGapPips), 50m)
			.SetDisplay("Trailing Gap (pips)", "Gap maintained by the trailing stop", "Risk")
			.SetRange(0m, 10_000m);

		_maxSpreadPips = Param(nameof(MaxSpreadPips), 100m)
			.SetDisplay("Max Spread (pips)", "Maximum allowed spread before opening new trades", "Filters")
			.SetRange(0m, 10_000m);

		_maxOpenTrades = Param(nameof(MaxOpenTrades), 10)
			.SetDisplay("Max Open Trades", "Maximum simultaneous random entries", "General")
			.SetRange(1, 1000);

		_riskMode = Param(nameof(RiskManagementMode), RiskModes.BalancePercentage)
			.SetDisplay("Risk Mode", "Select fixed risk or balance percentage", "Risk");

		_riskAmount = Param(nameof(RiskAmount), 5m)
			.SetDisplay("Risk Amount", "Money risk (fixed or percentage)", "Risk")
			.SetGreaterThanZero();

		_tradeComment = Param(nameof(TradeComment), "RRS")
			.SetDisplay("Trade Comment", "Comment stored with each order", "General");

		_notes = Param(nameof(Notes), "Note For Your Reference")
			.SetDisplay("Notes", "Informational note shown in the status string", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for processing", "Data");
	}

	/// <summary>
	/// Minimum random volume.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Maximum random volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing start distance expressed in pips.
	/// </summary>
	public decimal TrailingStartPips
	{
		get => _trailingStartPips.Value;
		set => _trailingStartPips.Value = value;
	}

	/// <summary>
	/// Trailing gap distance expressed in pips.
	/// </summary>
	public decimal TrailingGapPips
	{
		get => _trailingGapPips.Value;
		set => _trailingGapPips.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in pips.
	/// </summary>
	public decimal MaxSpreadPips
	{
		get => _maxSpreadPips.Value;
		set => _maxSpreadPips.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous open trades.
	/// </summary>
	public int MaxOpenTrades
	{
		get => _maxOpenTrades.Value;
		set => _maxOpenTrades.Value = value;
	}

	/// <summary>
	/// Risk handling mode.
	/// </summary>
	public RiskModes RiskManagementMode
	{
		get => _riskMode.Value;
		set => _riskMode.Value = value;
	}

	/// <summary>
	/// Risk amount (fixed money or percentage).
	/// </summary>
	public decimal RiskAmount
	{
		get => _riskAmount.Value;
		set => _riskAmount.Value = value;
	}

	/// <summary>
	/// Optional trade comment stored for reference.
	/// </summary>
	public string TradeComment
	{
		get => _tradeComment.Value;
		set => _tradeComment.Value = value;
	}

	/// <summary>
	/// Informational note displayed in the status string.
	/// </summary>
	public string Notes
	{
		get => _notes.Value;
		set => _notes.Value = value;
	}

	/// <summary>
	/// Candle data type used for processing.
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

		_buyEntries.Clear();
		_sellEntries.Clear();
		_buyTrailingStop = null;
		_sellTrailingStop = null;
		_lastSpread = null;
		_initialBalance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_point = GetPointValue();
		_initialBalance = GetCurrentBalance();
		_random = new Random(Environment.TickCount ^ GetHashCode());

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateSpread();
		UpdateTrailing(candle);
		CheckStopsAndTargets(candle);

		var price = candle.ClosePrice;
		var floating = CalculateUnrealizedPnL(price);
		var riskLimit = CalculateRiskLimit();

		if (floating <= riskLimit && (_buyEntries.Count > 0 || _sellEntries.Count > 0))
		{
			CloseAllTrades();
			StatusInfo = $"Risk management triggered. Floating={floating:F2} Threshold={riskLimit:F2}";
			return;
		}

		UpdateStatus(price, floating);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsSpreadAcceptable())
			return;

		var currentTrades = _buyEntries.Count + _sellEntries.Count;
		if (currentTrades >= MaxOpenTrades)
			return;

		var roll = _random.Next(4);
		if (roll == 1)
		{
			OpenBuy(price);
		}
		else if (roll == 2)
		{
			OpenSell(price);
		}
	}

	private void UpdateSpread()
	{
		var bid = Security?.BestBid?.Price;
		var ask = Security?.BestAsk?.Price;
		if (bid.HasValue && ask.HasValue)
			_lastSpread = ask.Value - bid.Value;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStartPips <= 0m || TrailingGapPips <= 0m)
		{
			_buyTrailingStop = null;
			_sellTrailingStop = null;
			return;
		}

		var bid = Security?.BestBid?.Price ?? candle.ClosePrice;
		var ask = Security?.BestAsk?.Price ?? candle.ClosePrice;

		var startDistance = TrailingStartPips * _point;
		var gapDistance = TrailingGapPips * _point;

		if (_buyEntries.Count > 0)
		{
			var avgBuy = GetAveragePrice(_buyEntries);
			if (bid - avgBuy >= startDistance)
			{
				var desiredStop = bid - gapDistance;
				if (_buyTrailingStop == null || desiredStop > _buyTrailingStop.Value)
					_buyTrailingStop = desiredStop;

				if (_buyTrailingStop != null && bid <= _buyTrailingStop.Value)
					CloseBuys();
			}
		}
		else
		{
			_buyTrailingStop = null;
		}

		if (_sellEntries.Count > 0)
		{
			var avgSell = GetAveragePrice(_sellEntries);
			if (avgSell - ask >= startDistance)
			{
				var desiredStop = ask + gapDistance;
				if (_sellTrailingStop == null || desiredStop < _sellTrailingStop.Value)
					_sellTrailingStop = desiredStop;

				if (_sellTrailingStop != null && ask >= _sellTrailingStop.Value)
					CloseSells();
			}
		}
		else
		{
			_sellTrailingStop = null;
		}
	}

	private void CheckStopsAndTargets(ICandleMessage candle)
	{
		var stopDistance = StopLossPips * _point;
		var takeDistance = TakeProfitPips * _point;

		if (_buyEntries.Count > 0)
		{
			var avgBuy = GetAveragePrice(_buyEntries);
			if (StopLossPips > 0m && avgBuy - candle.LowPrice >= stopDistance)
			{
				CloseBuys();
			}
			else if (TakeProfitPips > 0m && candle.HighPrice - avgBuy >= takeDistance)
			{
				CloseBuys();
			}
		}
		else
		{
			_buyTrailingStop = null;
		}

		if (_sellEntries.Count > 0)
		{
			var avgSell = GetAveragePrice(_sellEntries);
			if (StopLossPips > 0m && candle.HighPrice - avgSell >= stopDistance)
			{
				CloseSells();
			}
			else if (TakeProfitPips > 0m && avgSell - candle.LowPrice >= takeDistance)
			{
				CloseSells();
			}
		}
		else
		{
			_sellTrailingStop = null;
		}
	}

	private void OpenBuy(decimal price)
	{
		var volume = GenerateRandomVolume();
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_buyEntries.Add(new TradeEntry(price, volume));
	}

	private void OpenSell(decimal price)
	{
		var volume = GenerateRandomVolume();
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_sellEntries.Add(new TradeEntry(price, volume));
	}

	private decimal GenerateRandomVolume()
	{
		var min = MinVolume;
		var max = MaxVolume;

		if (max < min)
			(min, max) = (max, min);

		var randomValue = (decimal)_random.NextDouble();
		var volume = min + (max - min) * randomValue;

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		var result = volume;

		if (result <= 0m)
			return 0m;

		var minVolume = Security?.MinVolume ?? 0m;
		var maxVolume = Security?.MaxVolume;
		var step = Security?.VolumeStep ?? 0m;

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

	private bool IsSpreadAcceptable()
	{
		if (MaxSpreadPips <= 0m)
			return true;

		if (!_lastSpread.HasValue)
			return true;

		return _lastSpread.Value <= MaxSpreadPips * _point;
	}

	private void CloseAllTrades()
	{
		CloseBuys();
		CloseSells();
	}

	private void CloseBuys()
	{
		var total = GetTotalVolume(_buyEntries);
		if (total <= 0m)
			return;

		SellMarket(total);
		_buyEntries.Clear();
		_buyTrailingStop = null;
	}

	private void CloseSells()
	{
		var total = GetTotalVolume(_sellEntries);
		if (total <= 0m)
			return;

		BuyMarket(total);
		_sellEntries.Clear();
		_sellTrailingStop = null;
	}

	private decimal CalculateUnrealizedPnL(decimal price)
	{
		if (_point <= 0m)
			return 0m;

		var stepPrice = Security?.StepPrice ?? 1m;
		decimal total = 0m;

		for (var i = 0; i < _buyEntries.Count; i++)
		{
			var entry = _buyEntries[i];
			var difference = price - entry.Price;
			var steps = difference / _point;
			total += steps * stepPrice * entry.Volume;
		}

		for (var i = 0; i < _sellEntries.Count; i++)
		{
			var entry = _sellEntries[i];
			var difference = entry.Price - price;
			var steps = difference / _point;
			total += steps * stepPrice * entry.Volume;
		}

		return total;
	}

	private decimal CalculateRiskLimit()
	{
		var mode = RiskManagementMode;
		var risk = Math.Abs(RiskAmount);

		return mode switch
		{
			RiskModes.BalancePercentage => -GetCurrentBalance() * risk / 100m,
			_ => -risk,
		};
	}

	private decimal GetCurrentBalance()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue > 0m)
			return portfolio.CurrentValue;

		if (portfolio?.BeginValue > 0m)
			return portfolio.BeginValue;

		return _initialBalance;
	}

	private void UpdateStatus(decimal price, decimal floating)
	{
		var balance = GetCurrentBalance();
		var modeDescription = RiskManagementMode == RiskModes.BalancePercentage
			? $"Balance % ({RiskAmount:F2})"
			: $"Fixed ({RiskAmount:F2})";

		var spreadText = _lastSpread.HasValue ? (_lastSpread.Value / _point).ToString("F2") : "n/a";

		StatusInfo = $"Balance={balance:F2} FloatingPnL={floating:F2} Trades(Buy={_buyEntries.Count}, Sell={_sellEntries.Count}) " +
			$"Risk={modeDescription} Spread(pips)={spreadText} Notes={Notes}";
	}

	private decimal GetPointValue()
	{
		var point = Security?.PriceStep;
		if (point == null || point == 0m)
			return 0.0001m;

		return point.Value;
	}

	private static decimal GetTotalVolume(List<TradeEntry> entries)
	{
		decimal total = 0m;
		for (var i = 0; i < entries.Count; i++)
			total += entries[i].Volume;
		return total;
	}

	private static decimal GetAveragePrice(List<TradeEntry> entries)
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

	private readonly struct TradeEntry
	{
		public TradeEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }

		public decimal Volume { get; }
	}
}

