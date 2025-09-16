using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-currency breakout strategy converted from the original CH2010 structure expert.
/// Watches daily candles to define trend bias and 30-minute candles for entries and exits.
/// </summary>
public class Ch2010StructureStrategy : Strategy
{
	private readonly StrategyParam<Security> _usdChf;
	private readonly StrategyParam<Security> _gbpUsd;
	private readonly StrategyParam<Security> _audUsd;
	private readonly StrategyParam<Security> _usdJpy;
	private readonly StrategyParam<Security> _eurGbp;
	
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _minTradeVolume;
	private readonly StrategyParam<decimal> _maxTradeVolume;
	private readonly StrategyParam<decimal> _maxAggregateVolume;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _breakoutBufferPercent;
	private readonly StrategyParam<DataType> _dailyCandleType;
	private readonly StrategyParam<DataType> _intradayCandleType;
	
	private readonly List<InstrumentContext> _contexts;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="Ch2010StructureStrategy"/> class.
	/// </summary>
	public Ch2010StructureStrategy()
	{
		_usdChf = Param<Security>(nameof(UsdChfSecurity), null);
		_usdChf.SetDisplay("USD/CHF", "USDCHF symbol to trade", "Instruments");
		_gbpUsd = Param<Security>(nameof(GbpUsdSecurity), null);
		_gbpUsd.SetDisplay("GBP/USD", "GBPUSD symbol to trade", "Instruments");
		_audUsd = Param<Security>(nameof(AudUsdSecurity), null);
		_audUsd.SetDisplay("AUD/USD", "AUDUSD symbol to trade", "Instruments");
		_usdJpy = Param<Security>(nameof(UsdJpySecurity), null);
		_usdJpy.SetDisplay("USD/JPY", "USDJPY symbol to trade", "Instruments");
		_eurGbp = Param<Security>(nameof(EurGbpSecurity), null);
		_eurGbp.SetDisplay("EUR/GBP", "EURGBP symbol to trade", "Instruments");
		
		_tradeVolume = Param(nameof(TradeVolume), 1m);
		_tradeVolume.SetGreaterThanZero();
		_tradeVolume.SetDisplay("Trade Volume", "Nominal volume used for entries", "Risk");
		_minTradeVolume = Param(nameof(MinTradeVolume), 0.1m);
		_minTradeVolume.SetGreaterThanZero();
		_minTradeVolume.SetDisplay("Minimum Volume", "Lower bound that mirrors the MQL expert", "Risk");
		_maxTradeVolume = Param(nameof(MaxTradeVolume), 5m);
		_maxTradeVolume.SetGreaterThanZero();
		_maxTradeVolume.SetDisplay("Maximum Volume", "Upper bound for a single position", "Risk");
		_maxAggregateVolume = Param(nameof(MaxAggregateVolume), 15m);
		_maxAggregateVolume.SetGreaterThanZero();
		_maxAggregateVolume.SetDisplay("Aggregate Volume", "Cap across all instruments", "Risk");
		_stopLossPercent = Param(nameof(StopLossPercent), 1.5m);
		_stopLossPercent.SetGreaterThanZero();
		_stopLossPercent.SetDisplay("Stop Loss %", "Protective stop percentage", "Risk");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 3m);
		_takeProfitPercent.SetGreaterThanZero();
		_takeProfitPercent.SetDisplay("Take Profit %", "Profit target percentage", "Risk");
		_breakoutBufferPercent = Param(nameof(BreakoutBufferPercent), 10m);
		_breakoutBufferPercent.SetGreaterThanZero();
		_breakoutBufferPercent.SetDisplay("Buffer %", "Percentage of daily range added above/below breakout", "Logic");
		
		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame());
		_dailyCandleType.SetDisplay("Daily Candle", "Time frame used for the daily bias", "Data");
		_intradayCandleType = Param(nameof(IntradayCandleType), TimeSpan.FromMinutes(30).TimeFrame());
		_intradayCandleType.SetDisplay("Intraday Candle", "Time frame used for intraday execution", "Data");
		
		_contexts = new List<InstrumentContext>
		{
			new InstrumentContext("USDCHF", () => UsdChfSecurity),
			new InstrumentContext("GBPUSD", () => GbpUsdSecurity),
			new InstrumentContext("AUDUSD", () => AudUsdSecurity),
			new InstrumentContext("USDJPY", () => UsdJpySecurity),
			new InstrumentContext("EURGBP", () => EurGbpSecurity)
		};
	}
	
	/// <summary>
	/// USDCHF security parameter.
	/// </summary>
	public Security UsdChfSecurity
	{
		get => _usdChf.Value;
		set => _usdChf.Value = value;
	}
	
	/// <summary>
	/// GBPUSD security parameter.
	/// </summary>
	public Security GbpUsdSecurity
	{
		get => _gbpUsd.Value;
		set => _gbpUsd.Value = value;
	}
	
	/// <summary>
	/// AUDUSD security parameter.
	/// </summary>
	public Security AudUsdSecurity
	{
		get => _audUsd.Value;
		set => _audUsd.Value = value;
	}
	
	/// <summary>
	/// USDJPY security parameter.
	/// </summary>
	public Security UsdJpySecurity
	{
		get => _usdJpy.Value;
		set => _usdJpy.Value = value;
	}
	
	/// <summary>
	/// EURGBP security parameter.
	/// </summary>
	public Security EurGbpSecurity
	{
		get => _eurGbp.Value;
		set => _eurGbp.Value = value;
	}
	
	/// <summary>
	/// Nominal trade volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}
	
	/// <summary>
	/// Minimum allowed volume.
	/// </summary>
	public decimal MinTradeVolume
	{
		get => _minTradeVolume.Value;
		set => _minTradeVolume.Value = value;
	}
	
	/// <summary>
	/// Maximum allowed volume for a single position.
	/// </summary>
	public decimal MaxTradeVolume
	{
		get => _maxTradeVolume.Value;
		set => _maxTradeVolume.Value = value;
	}
	
	/// <summary>
	/// Maximum combined exposure across all instruments.
	/// </summary>
	public decimal MaxAggregateVolume
	{
		get => _maxAggregateVolume.Value;
		set => _maxAggregateVolume.Value = value;
	}
	
	/// <summary>
	/// Stop-loss percentage applied to entries.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	
	/// <summary>
	/// Take-profit percentage applied to entries.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}
	
	/// <summary>
	/// Buffer in percent of the daily range used to trigger breakouts.
	/// </summary>
	public decimal BreakoutBufferPercent
	{
		get => _breakoutBufferPercent.Value;
		set => _breakoutBufferPercent.Value = value;
	}
	
	/// <summary>
	/// Daily candle type.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}
	
	/// <summary>
	/// Intraday candle type.
	/// </summary>
	public DataType IntradayCandleType
	{
		get => _intradayCandleType.Value;
		set => _intradayCandleType.Value = value;
	}
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var context in _contexts)
		{
			var security = context.Security;
			
			if (security == null)
			{
				continue;
			}
			
			yield return (security, DailyCandleType);
			yield return (security, IntradayCandleType);
		}
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		foreach (var context in _contexts)
		{
			context.Reset();
		}
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var hasSecurity = false;
		
		foreach (var context in _contexts)
		{
			var security = context.Security;
			
			if (security == null)
			{
				continue;
			}
			
			hasSecurity = true;
			
			var localContext = context;
			var dailySubscription = SubscribeCandles(DailyCandleType, true, security);
			dailySubscription.Bind(candle => ProcessDailyCandle(localContext, candle));
			dailySubscription.Start();
			
			var intradaySubscription = SubscribeCandles(IntradayCandleType, true, security);
			intradaySubscription.Bind(candle => ProcessIntradayCandle(localContext, candle));
			intradaySubscription.Start();
		}
		
		if (!hasSecurity)
		{
			throw new InvalidOperationException("At least one security must be configured.");
		}
	}
	
	private void ProcessDailyCandle(InstrumentContext context, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}
		
		context.DailyDate = candle.OpenTime.Date;
		context.DailyHigh = candle.HighPrice;
		context.DailyLow = candle.LowPrice;
		context.DailyClose = candle.ClosePrice;
		context.HasLevels = true;
		context.LongTriggered = false;
		context.ShortTriggered = false;
		
		if (candle.ClosePrice > candle.OpenPrice)
		{
			context.Bias = BiasDirection.Long;
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			context.Bias = BiasDirection.Short;
		}
		else
		{
			context.Bias = BiasDirection.Neutral;
		}
		
		LogInfo($"[{context.Alias}] Daily candle captured. High={candle.HighPrice} Low={candle.LowPrice} Close={candle.ClosePrice}");
	}
	
	private void ProcessIntradayCandle(InstrumentContext context, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}
		
		if (!context.HasLevels)
		{
			return;
		}
		
		if (context.DailyDate != candle.OpenTime.Date)
		{
			return;
		}
		
		var security = context.Security;
		
		if (security == null)
		{
			return;
		}
		
		var position = GetPositionValue(security, Portfolio) ?? 0m;
		
		UpdatePositionSnapshot(context, candle.ClosePrice, position);
		
		if (position != 0)
		{
			ManageOpenPosition(context, position, candle.ClosePrice);
			return;
		}
		
		var range = context.DailyHigh - context.DailyLow;
		
		if (range <= 0)
		{
			return;
		}
		
		var buffer = range * (BreakoutBufferPercent / 100m);
		var longTrigger = context.DailyHigh + buffer;
		var shortTrigger = context.DailyLow - buffer;
		
		if (!context.LongTriggered && context.Bias != BiasDirection.Short)
		{
			if (candle.ClosePrice > longTrigger)
			{
				TryEnterPosition(context, Sides.Buy, candle.ClosePrice, "Daily breakout long");
				context.LongTriggered = true;
			}
		}
		
		if (!context.ShortTriggered && context.Bias != BiasDirection.Long)
		{
			if (candle.ClosePrice < shortTrigger)
			{
				TryEnterPosition(context, Sides.Sell, candle.ClosePrice, "Daily breakout short");
				context.ShortTriggered = true;
			}
		}
	}
	private void TryEnterPosition(InstrumentContext context, Sides side, decimal price, string reason)
	{
		if (context.ExitInProgress)
		{
			return;
		}
		
		var security = context.Security;
		
		if (security == null)
		{
			return;
		}
		
		var volume = AdjustVolumeForLimits(TradeVolume);
		
		if (volume <= 0)
		{
			return;
		}
		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = side,
			Volume = volume,
			Type = OrderTypes.Market,
			Comment = $"{context.Alias}:{reason}"
		});
		
		context.EntrySide = side;
		context.EntryPrice = price;
		context.StopPrice = null;
		context.TakeProfitPrice = null;
		context.ExitInProgress = false;
		
		LogInfo($"[{context.Alias}] Enter {side} at {price} vol={volume}. Reason={reason}");
	}
	private void ManageOpenPosition(InstrumentContext context, decimal position, decimal closePrice)
	{
		if (context.EntrySide == null)
		{
			return;
		}
		
		var isLong = position > 0;
		
		if (context.StopPrice == null || context.TakeProfitPrice == null)
		{
			var entryPrice = context.EntryPrice ?? closePrice;
			var stopOffset = entryPrice * (StopLossPercent / 100m);
			var takeOffset = entryPrice * (TakeProfitPercent / 100m);
			
			if (isLong)
			{
				context.StopPrice = entryPrice - stopOffset;
				context.TakeProfitPrice = entryPrice + takeOffset;
			}
			else
			{
				context.StopPrice = entryPrice + stopOffset;
				context.TakeProfitPrice = entryPrice - takeOffset;
			}
		}
		if (context.ExitInProgress)
		{
			return;
		}
		
		if (isLong)
		{
			if (context.StopPrice != null && closePrice <= context.StopPrice.Value)
			{
				ExitPosition(context, position, Sides.Sell, $"StopLoss at {context.StopPrice.Value}");
				return;
			}
			
			if (context.TakeProfitPrice != null && closePrice >= context.TakeProfitPrice.Value)
			{
				ExitPosition(context, position, Sides.Sell, $"TakeProfit at {context.TakeProfitPrice.Value}");
			}
		}
		else
		{
			var volume = Math.Abs(position);
			
			if (context.StopPrice != null && closePrice >= context.StopPrice.Value)
			{
				ExitPosition(context, volume, Sides.Buy, $"StopLoss at {context.StopPrice.Value}");
				return;
			}
			
			if (context.TakeProfitPrice != null && closePrice <= context.TakeProfitPrice.Value)
			{
				ExitPosition(context, volume, Sides.Buy, $"TakeProfit at {context.TakeProfitPrice.Value}");
			}
		}
	}
	private void ExitPosition(InstrumentContext context, decimal volume, Sides side, string reason)
	{
		if (volume <= 0)
		{
			return;
		}
		
		var security = context.Security;
		
		if (security == null)
		{
			return;
		}
		
		context.ExitInProgress = true;
		
		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = side,
			Volume = volume,
			Type = OrderTypes.Market,
			Comment = $"{context.Alias}:{reason}"
		});
		
		LogInfo($"[{context.Alias}] Exit {side} vol={volume}. Reason={reason}");
	}
	private decimal AdjustVolumeForLimits(decimal desired)
	{
		if (desired <= 0)
		{
			return 0m;
		}
		
		var volume = Math.Min(desired, MaxTradeVolume);
		
		if (volume < MinTradeVolume)
		{
			return 0m;
		}
		
		var totalExposure = 0m;
		
		foreach (var context in _contexts)
		{
			var security = context.Security;
			
			if (security == null)
			{
				continue;
			}
			
			var pos = GetPositionValue(security, Portfolio) ?? 0m;
			totalExposure += Math.Abs(pos);
		}
		
		var remaining = MaxAggregateVolume - totalExposure;
		
		if (remaining <= 0)
		{
			return 0m;
		}
		
		return Math.Min(volume, remaining);
	}
	private void UpdatePositionSnapshot(InstrumentContext context, decimal price, decimal position)
	{
		if (position == context.LastKnownPosition)
		{
			return;
		}
		
		if (position == 0)
		{
			context.ResetPosition();
			return;
		}
		
		context.LastKnownPosition = position;
		context.EntrySide = position > 0 ? Sides.Buy : Sides.Sell;
		context.EntryPrice = price;
		context.ExitInProgress = false;
		
		var stopOffset = price * (StopLossPercent / 100m);
		var takeOffset = price * (TakeProfitPercent / 100m);
		
		if (position > 0)
		{
			context.StopPrice = price - stopOffset;
			context.TakeProfitPrice = price + takeOffset;
		}
		else
		{
			context.StopPrice = price + stopOffset;
			context.TakeProfitPrice = price - takeOffset;
		}
	}
	private enum BiasDirection
	{
		Neutral,
		Long,
		Short
	}
	
	private sealed class InstrumentContext
	{
		private readonly Func<Security> _securityProvider;
		
		public InstrumentContext(string alias, Func<Security> securityProvider)
		{
			Alias = alias;
			_securityProvider = securityProvider;
			Reset();
		}
		
		public string Alias { get; }
		
		public Security Security => _securityProvider();
		
		public DateTime? DailyDate { get; set; }
		
		public decimal DailyHigh { get; set; }
		
		public decimal DailyLow { get; set; }
		
		public decimal DailyClose { get; set; }
		
		public BiasDirection Bias { get; set; }
		
		public bool HasLevels { get; set; }
		
		public bool LongTriggered { get; set; }
		
		public bool ShortTriggered { get; set; }
		
		public decimal LastKnownPosition { get; set; }
		
		public Sides? EntrySide { get; set; }
		
		public decimal? EntryPrice { get; set; }
		
		public decimal? StopPrice { get; set; }
		
		public decimal? TakeProfitPrice { get; set; }
		
		public bool ExitInProgress { get; set; }
		
		public void Reset()
		{
			DailyDate = null;
			DailyHigh = 0m;
			DailyLow = 0m;
			DailyClose = 0m;
			Bias = BiasDirection.Neutral;
			HasLevels = false;
			LongTriggered = false;
			ShortTriggered = false;
			ResetPosition();
		}
		
		public void ResetPosition()
		{
			LastKnownPosition = 0m;
			EntrySide = null;
			EntryPrice = null;
			StopPrice = null;
			TakeProfitPrice = null;
			ExitInProgress = false;
		}
	}
}
