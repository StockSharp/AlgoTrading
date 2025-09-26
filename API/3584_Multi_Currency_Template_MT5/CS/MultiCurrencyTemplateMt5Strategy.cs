namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class MultiCurrencyTemplateMt5Strategy : Strategy
{
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<bool> _newBarTrade;
	private readonly StrategyParam<DataType> _tradeCandleType;
	private readonly StrategyParam<bool> _tradeMultipair;
	private readonly StrategyParam<string> _pairsToTrade;
	private readonly StrategyParam<string> _commentary;
	private readonly StrategyParam<bool> _enableMartingale;
	private readonly StrategyParam<decimal> _nextLotMultiplier;
	private readonly StrategyParam<decimal> _stepPoints;
	private readonly StrategyParam<bool> _enableTakeProfitAverage;
	private readonly StrategyParam<decimal> _takeProfitOffsetPoints;
	private readonly StrategyParam<DataType> _signalCandleType;

	private readonly Dictionary<Security, SymbolContext> _contexts = new();
	private readonly List<Security> _configuredSecurities = new();

	public MultiCurrencyTemplateMt5Strategy()
	{
		_lots = Param(nameof(Lots), 0.01m)
			.SetDisplay("Lots", "Base trading volume used for the first position.", "Trading")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop loss (pips)", "Initial protective stop distance expressed in MetaTrader pips.", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
			.SetDisplay("Take profit (pips)", "Initial take-profit target expressed in MetaTrader pips.", "Risk")
			.SetNotNegative();

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 15m)
			.SetDisplay("Trailing stop (points)", "Trailing distance applied when only one ticket is open.", "Risk")
			.SetNotNegative();

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m)
			.SetDisplay("Trailing step (points)", "Minimum progress required before the trailing stop is moved again.", "Risk")
			.SetNotNegative();

		_slippagePoints = Param(nameof(SlippagePoints), 100)
			.SetDisplay("Slippage (points)", "Reserved for analytics to mimic the MetaTrader slippage setting.", "Execution")
			.SetNotNegative();

		_newBarTrade = Param(nameof(NewBarTrade), true)
			.SetDisplay("Trade on new bar", "When enabled, entries are allowed only after a fresh bar is formed.", "Timing");

		_tradeCandleType = Param(nameof(TradeCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Trade timeframe", "Heartbeat candles used for new-bar detection and money management.", "Timing");

		_tradeMultipair = Param(nameof(TradeMultipair), false)
			.SetDisplay("Trade multiple pairs", "Enable multi-currency mode that manages several securities at once.", "Trading");

		_pairsToTrade = Param(nameof(PairsToTrade), "EURUSD,GBPUSD")
			.SetDisplay("Pairs to trade", "Comma separated list of additional securities traded in multicurrency mode.", "Trading");

		_commentary = Param(nameof(Commentary), "MultiCurrency EA")
			.SetDisplay("Comment", "Order comment preserved from the original expert advisor.", "Execution");

		_enableMartingale = Param(nameof(EnableMartingale), true)
			.SetDisplay("Enable martingale", "When true the strategy scales in using distance based averaging orders.", "Scaling");

		_nextLotMultiplier = Param(nameof(NextLotMultiplier), 1.2m)
			.SetDisplay("Lot multiplier", "Multiplier applied to the volume of each additional averaging order.", "Scaling")
			.SetGreaterThanZero();

		_stepPoints = Param(nameof(StepPoints), 300m)
			.SetDisplay("Step (points)", "Price distance in MetaTrader points that triggers the next averaging order.", "Scaling")
			.SetGreaterThanZero();

		_enableTakeProfitAverage = Param(nameof(EnableTakeProfitAverage), true)
			.SetDisplay("Enable TP average", "Activates the break-even take-profit logic for martingale baskets.", "Scaling");

		_takeProfitOffsetPoints = Param(nameof(TakeProfitOffsetPoints), 75m)
			.SetDisplay("TP offset (points)", "Additional distance added above the break-even price when TP averaging is active.", "Scaling")
			.SetNotNegative();

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Signal timeframe", "Slow timeframe used to evaluate the candlestick pattern entry filter.", "Timing");
	}

	public decimal Lots
	{
		get => _lots.Value;
		set => _lots.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	public int SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	public bool NewBarTrade
	{
		get => _newBarTrade.Value;
		set => _newBarTrade.Value = value;
	}

	public DataType TradeCandleType
	{
		get => _tradeCandleType.Value;
		set => _tradeCandleType.Value = value;
	}

	public bool TradeMultipair
	{
		get => _tradeMultipair.Value;
		set => _tradeMultipair.Value = value;
	}

	public string PairsToTrade
	{
		get => _pairsToTrade.Value;
		set => _pairsToTrade.Value = value;
	}

	public string Commentary
	{
		get => _commentary.Value;
		set => _commentary.Value = value;
	}

	public bool EnableMartingale
	{
		get => _enableMartingale.Value;
		set => _enableMartingale.Value = value;
	}

	public decimal NextLotMultiplier
	{
		get => _nextLotMultiplier.Value;
		set => _nextLotMultiplier.Value = value;
	}

	public decimal StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	public bool EnableTakeProfitAverage
	{
		get => _enableTakeProfitAverage.Value;
		set => _enableTakeProfitAverage.Value = value;
	}

	public decimal TakeProfitOffsetPoints
	{
		get => _takeProfitOffsetPoints.Value;
		set => _takeProfitOffsetPoints.Value = value;
	}

	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (_configuredSecurities.Count == 0)
		{
			if (Security != null)
			{
				yield return (Security, SignalCandleType);
				yield return (Security, TradeCandleType);
			}

			yield break;
		}

		foreach (var security in _configuredSecurities)
		{
			yield return (security, SignalCandleType);
			yield return (security, TradeCandleType);
		}
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_contexts.Clear();
		_configuredSecurities.Clear();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_contexts.Clear();
		_configuredSecurities.Clear();

		var securities = new HashSet<Security>();

		if (TradeMultipair)
		{
			foreach (var symbol in (PairsToTrade ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
			{
				var trimmed = symbol.Trim();
				if (trimmed.Length == 0)
					continue;

				var security = this.GetSecurity(trimmed);
				if (security != null)
					securities.Add(security);
			}
		}

		if (Security != null)
			securities.Add(Security);

		foreach (var security in securities)
		{
			var pipSize = CalculatePipSize(security);
			var pointSize = CalculatePointSize(security);

			var context = new SymbolContext(security, pipSize, pointSize);

			_contexts.Add(security, context);
			_configuredSecurities.Add(security);

			var tradeSubscription = SubscribeCandles(TradeCandleType, security);
			tradeSubscription
				.Bind(candle => ProcessTradeCandle(context, candle))
				.Start();

			var signalSubscription = SubscribeCandles(SignalCandleType, security);
			signalSubscription
				.Bind(candle => ProcessSignalCandle(context, candle))
				.Start();
		}

		StartProtection();
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security is not { } security)
			return;

		if (!_contexts.TryGetValue(security, out var context))
			return;

		if (trade.Trade == null)
			return;

		RegisterFill(context, trade.Order.Side, trade.Trade.Volume, trade.Trade.Price);
	}

	private void ProcessSignalCandle(SymbolContext context, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previous = context.PreviousSignalCandle;
		context.PreviousSignalCandle = candle;

		if (previous == null)
			return;

		var buySignal = candle.ClosePrice < previous.OpenPrice && previous.ClosePrice > previous.OpenPrice;
		var sellSignal = candle.ClosePrice > previous.OpenPrice && previous.ClosePrice < previous.OpenPrice;

		if (buySignal)
			TryOpenPosition(context, true, candle.ClosePrice);
		else if (sellSignal)
			TryOpenPosition(context, false, candle.ClosePrice);
	}

	private void ProcessTradeCandle(SymbolContext context, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!context.TradeStreamInitialized)
		{
			context.TradeStreamInitialized = true;
			context.LastClosePrice = candle.ClosePrice;
			return;
		}

		context.HasNewBar = true;
		context.LastClosePrice = candle.ClosePrice;

		if (EnableMartingale)
			UpdateMartingale(context, candle.ClosePrice);

		UpdateTrailing(context, candle.ClosePrice);
		CheckRiskTargets(context, candle.ClosePrice);
	}

	private void TryOpenPosition(SymbolContext context, bool isLong, decimal referencePrice)
	{
		if (!CanTradeOnCurrentBar(context))
			return;

		if (isLong)
		{
			if (context.LongLegs.Count > 0 || context.PendingFills.Any(p => p.Side == Sides.Buy))
				return;

			var volume = NormalizeVolume(context, Lots);
			if (volume <= 0m)
				return;

			BuyMarket(volume, context.Security);
			context.PendingFills.Add(new PendingFill(Sides.Buy, volume));

			context.LongStopPrice = StopLossPips > 0m && context.PipSize > 0m
				? referencePrice - StopLossPips * context.PipSize
				: null;

			context.LongTakePrice = TakeProfitPips > 0m && context.PipSize > 0m
				? referencePrice + TakeProfitPips * context.PipSize
				: null;
		}
		else
		{
			if (context.ShortLegs.Count > 0 || context.PendingFills.Any(p => p.Side == Sides.Sell))
				return;

			var volume = NormalizeVolume(context, Lots);
			if (volume <= 0m)
				return;

			SellMarket(volume, context.Security);
			context.PendingFills.Add(new PendingFill(Sides.Sell, volume));

			context.ShortStopPrice = StopLossPips > 0m && context.PipSize > 0m
				? referencePrice + StopLossPips * context.PipSize
				: null;

			context.ShortTakePrice = TakeProfitPips > 0m && context.PipSize > 0m
				? referencePrice - TakeProfitPips * context.PipSize
				: null;
		}
	}

	private void UpdateMartingale(SymbolContext context, decimal price)
	{
		var point = context.PointSize;
		if (point <= 0m)
			return;

		var distance = StepPoints * point;
		if (distance <= 0m)
			return;

		if (context.LongLegs.Count > 0 && !context.ClosingLong)
		{
			var minPrice = context.LongLegs.Min(l => l.Price);
			if (minPrice - price >= distance)
			{
				var volume = NormalizeVolume(context, Lots * Pow(NextLotMultiplier, context.LongLegs.Count));
				if (volume > 0m)
				{
					BuyMarket(volume, context.Security);
					context.PendingFills.Add(new PendingFill(Sides.Buy, volume));
				}
			}
		}

		if (context.ShortLegs.Count > 0 && !context.ClosingShort)
		{
			var maxPrice = context.ShortLegs.Max(l => l.Price);
			if (price - maxPrice >= distance)
			{
				var volume = NormalizeVolume(context, Lots * Pow(NextLotMultiplier, context.ShortLegs.Count));
				if (volume > 0m)
				{
					SellMarket(volume, context.Security);
					context.PendingFills.Add(new PendingFill(Sides.Sell, volume));
				}
			}
		}
	}

	private void UpdateTrailing(SymbolContext context, decimal price)
	{
		var point = context.PointSize;
		if (point <= 0m)
			return;

		var trailing = TrailingStopPoints * point;
		var step = TrailingStepPoints * point;
		if (trailing <= 0m)
			return;

		if (context.LongLegs.Count == 1)
		{
			var entry = context.LongLegs[0].Price;
			var currentStop = context.LongStopPrice;

			if ((!currentStop.HasValue || currentStop.Value < entry) && price - entry > trailing + step)
			{
				context.LongStopPrice = entry + trailing;
			}
			else if (currentStop.HasValue && currentStop.Value > entry && price - currentStop.Value > trailing + step)
			{
				context.LongStopPrice = price - trailing;
			}
		}

		if (context.ShortLegs.Count == 1)
		{
			var entry = context.ShortLegs[0].Price;
			var currentStop = context.ShortStopPrice;

			if ((!currentStop.HasValue || currentStop.Value > entry) && entry - price > trailing + step)
			{
				context.ShortStopPrice = entry - trailing;
			}
			else if (currentStop.HasValue && currentStop.Value < entry && currentStop.Value - price > trailing + step)
			{
				context.ShortStopPrice = price + trailing;
			}
		}
	}

	private void CheckRiskTargets(SymbolContext context, decimal price)
	{
		if (context.LongLegs.Count > 0 && !context.ClosingLong)
		{
			if (context.LongTakePrice is decimal take && price >= take)
			{
				ClosePositions(context, true);
				return;
			}

			if (context.LongStopPrice is decimal stop && price <= stop)
			{
				ClosePositions(context, true);
				return;
			}
		}

		if (context.ShortLegs.Count > 0 && !context.ClosingShort)
		{
			if (context.ShortTakePrice is decimal take && price <= take)
			{
				ClosePositions(context, false);
				return;
			}

			if (context.ShortStopPrice is decimal stop && price >= stop)
				ClosePositions(context, false);
		}
	}

	private void ClosePositions(SymbolContext context, bool isLong)
	{
		var legs = isLong ? context.LongLegs : context.ShortLegs;
		if (legs.Count == 0)
			return;

		var volume = NormalizeVolume(context, legs.Sum(l => l.Volume));
		if (volume <= 0m)
			return;

		if (isLong)
		{
			context.ClosingLong = true;
			SellMarket(volume, context.Security);
			context.PendingFills.Add(new PendingFill(Sides.Sell, volume));
		}
		else
		{
			context.ClosingShort = true;
			BuyMarket(volume, context.Security);
			context.PendingFills.Add(new PendingFill(Sides.Buy, volume));
		}
	}

	private void RegisterFill(SymbolContext context, Sides side, decimal volume, decimal price)
	{
		var pending = context.PendingFills.FirstOrDefault(p => p.Side == side);
		if (pending != null)
		{
			pending.Volume -= volume;
			if (pending.Volume <= 0m)
				context.PendingFills.Remove(pending);
		}

		if (side == Sides.Buy)
		{
			var remaining = volume;
			while (remaining > 0m && context.ShortLegs.Count > 0)
			{
				var leg = context.ShortLegs[0];
				if (remaining >= leg.Volume)
				{
					remaining -= leg.Volume;
					context.ShortLegs.RemoveAt(0);
				}
				else
				{
					leg.Volume -= remaining;
					remaining = 0m;
				}
			}

			if (remaining > 0m)
				context.LongLegs.Add(new PositionLeg(remaining, price));
		}
		else if (side == Sides.Sell)
		{
			var remaining = volume;
			while (remaining > 0m && context.LongLegs.Count > 0)
			{
				var leg = context.LongLegs[0];
				if (remaining >= leg.Volume)
				{
					remaining -= leg.Volume;
					context.LongLegs.RemoveAt(0);
				}
				else
				{
					leg.Volume -= remaining;
					remaining = 0m;
				}
			}

			if (remaining > 0m)
				context.ShortLegs.Add(new PositionLeg(remaining, price));
		}

		if (context.LongLegs.Count == 0)
		{
			context.LongStopPrice = null;
			context.LongTakePrice = null;
			context.ClosingLong = false;
		}

		if (context.ShortLegs.Count == 0)
		{
			context.ShortStopPrice = null;
			context.ShortTakePrice = null;
			context.ClosingShort = false;
		}

		RecalculateTargets(context);
	}

	private void RecalculateTargets(SymbolContext context)
	{
		var pip = context.PipSize;
		var point = context.PointSize;

		if (context.LongLegs.Count > 0)
		{
			var totalVolume = context.LongLegs.Sum(l => l.Volume);
			if (totalVolume > 0m)
			{
				var average = context.LongLegs.Sum(l => l.Volume * l.Price) / totalVolume;
				var offset = TakeProfitOffsetPoints * point;

				if (EnableTakeProfitAverage && context.LongLegs.Count >= 2)
					context.LongTakePrice = average + offset;
				else if (TakeProfitPips > 0m && pip > 0m)
					context.LongTakePrice = context.LongLegs.Min(l => l.Price) + TakeProfitPips * pip;
				else
					context.LongTakePrice = null;

				context.LongStopPrice = StopLossPips > 0m && pip > 0m
					? context.LongLegs.Min(l => l.Price) - StopLossPips * pip
					: null;
			}
		}
		else
		{
			context.LongTakePrice = null;
			context.LongStopPrice = null;
		}

		if (context.ShortLegs.Count > 0)
		{
			var totalVolume = context.ShortLegs.Sum(l => l.Volume);
			if (totalVolume > 0m)
			{
				var average = context.ShortLegs.Sum(l => l.Volume * l.Price) / totalVolume;
				var offset = TakeProfitOffsetPoints * point;

				if (EnableTakeProfitAverage && context.ShortLegs.Count >= 2)
					context.ShortTakePrice = average - offset;
				else if (TakeProfitPips > 0m && pip > 0m)
					context.ShortTakePrice = context.ShortLegs.Max(l => l.Price) - TakeProfitPips * pip;
				else
					context.ShortTakePrice = null;

				context.ShortStopPrice = StopLossPips > 0m && pip > 0m
					? context.ShortLegs.Max(l => l.Price) + StopLossPips * pip
					: null;
			}
		}
		else
		{
			context.ShortTakePrice = null;
			context.ShortStopPrice = null;
		}
	}

	private bool CanTradeOnCurrentBar(SymbolContext context)
	{
		if (!NewBarTrade)
			return true;

		if (!context.HasNewBar)
			return false;

		context.HasNewBar = false;
		return true;
	}

	private static decimal Pow(decimal value, int exponent)
	{
		if (exponent <= 0)
			return 1m;

		return (decimal)Math.Pow((double)value, exponent);
	}

	private static decimal CalculatePipSize(Security security)
	{
		var step = security?.PriceStep ?? 0m;
		var decimals = security?.Decimals ?? 0;

		if (step <= 0m)
			step = (decimal)Math.Pow(10, -(decimals > 0 ? decimals : 4));

		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step > 0m ? step : 0.0001m;
	}

	private static decimal CalculatePointSize(Security security)
	{
		var step = security?.PriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	private static decimal NormalizeVolume(SymbolContext context, decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = context.Security;
		var step = security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = decimal.Truncate(volume / step);
			volume = steps * step;
		}

		var min = security?.VolumeMin ?? 0m;
		if (min > 0m && volume < min)
			volume = min;

		var max = security?.VolumeMax ?? 0m;
		if (max > 0m && volume > max)
			volume = max;

		return volume > 0m ? volume : 0m;
	}

	private sealed class SymbolContext
	{
		public SymbolContext(Security security, decimal pipSize, decimal pointSize)
		{
			Security = security;
			PipSize = pipSize;
			PointSize = pointSize;
		}

		public Security Security { get; }
		public decimal PipSize { get; }
		public decimal PointSize { get; }
		public bool HasNewBar { get; set; }
		public bool TradeStreamInitialized { get; set; }
		public ICandleMessage PreviousSignalCandle { get; set; }
		public decimal? LastClosePrice { get; set; }
		public decimal? LongStopPrice { get; set; }
		public decimal? ShortStopPrice { get; set; }
		public decimal? LongTakePrice { get; set; }
		public decimal? ShortTakePrice { get; set; }
		public bool ClosingLong { get; set; }
		public bool ClosingShort { get; set; }
		public List<PositionLeg> LongLegs { get; } = new();
		public List<PositionLeg> ShortLegs { get; } = new();
		public List<PendingFill> PendingFills { get; } = new();
	}

	private sealed class PositionLeg
	{
		public PositionLeg(decimal volume, decimal price)
		{
			Volume = volume;
			Price = price;
		}

		public decimal Volume { get; set; }
		public decimal Price { get; }
	}

	private sealed class PendingFill
	{
		public PendingFill(Sides side, decimal volume)
		{
			Side = side;
			Volume = volume;
		}

		public Sides Side { get; }
		public decimal Volume { get; set; }
	}
}
