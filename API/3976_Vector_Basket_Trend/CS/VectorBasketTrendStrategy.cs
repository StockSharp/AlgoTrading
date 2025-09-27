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
/// Port of the MetaTrader 4 expert advisor "Vector".
/// The strategy trades up to four correlated forex pairs using smoothed moving averages.
/// </summary>
public class VectorBasketTrendStrategy : Strategy
{
	private readonly StrategyParam<Security> _secondSecurity;
	private readonly StrategyParam<Security> _thirdSecurity;
	private readonly StrategyParam<Security> _fourthSecurity;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _rangeCandleType;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _maxDrawdownPercent;

	private readonly Dictionary<Security, InstrumentContext> _contexts = new();
	private readonly Dictionary<Security, decimal> _lastPositions = new();

	private decimal _initialBalance;
	private bool _profitTargetTriggered;
	private bool _drawdownTriggered;

	/// <summary>
	/// Secondary instrument representing GBPUSD in the original script.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurity.Value;
		set => _secondSecurity.Value = value;
	}

	/// <summary>
	/// Third instrument representing USDCHF in the original script.
	/// </summary>
	public Security ThirdSecurity
	{
		get => _thirdSecurity.Value;
		set => _thirdSecurity.Value = value;
	}

	/// <summary>
	/// Fourth instrument representing USDJPY in the original script.
	/// </summary>
	public Security FourthSecurity
	{
		get => _fourthSecurity.Value;
		set => _fourthSecurity.Value = value;
	}

	/// <summary>
	/// Trading candle type used to compute the smoothed moving averages.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type that defines the profit target distance.
	/// </summary>
	public DataType RangeCandleType
	{
		get => _rangeCandleType.Value;
		set => _rangeCandleType.Value = value;
	}

	/// <summary>
	/// Base volume requested for each trade.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Percentage profit target that closes every open position.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Maximum tolerated equity drawdown expressed in percent.
	/// </summary>
	public decimal MaxDrawdownPercent
	{
		get => _maxDrawdownPercent.Value;
		set => _maxDrawdownPercent.Value = value;
	}

	/// <summary>
/// Initializes a new instance of the <see cref="VectorBasketTrendStrategy"/> class.
/// </summary>
public VectorBasketTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for MA calculation", "General");

		_rangeCandleType = Param(nameof(RangeCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Range Candle Type", "Higher timeframe for pip target", "General");

		_secondSecurity = Param<Security>(nameof(SecondSecurity))
			.SetDisplay("Second Security", "Optional correlated instrument", "General");

		_thirdSecurity = Param<Security>(nameof(ThirdSecurity))
			.SetDisplay("Third Security", "Optional correlated instrument", "General");

		_fourthSecurity = Param<Security>(nameof(FourthSecurity))
			.SetDisplay("Fourth Security", "Optional correlated instrument", "General");

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetDisplay("Base Volume", "Requested volume for each trade", "Trading");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.5m)
			.SetDisplay("Account Take Profit %", "Equity gain that forces a global exit", "Risk");

		_maxDrawdownPercent = Param(nameof(MaxDrawdownPercent), 30m)
			.SetDisplay("Max Drawdown %", "Equity loss that forces a global exit", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var security in EnumerateSecurities())
		{
			yield return (security, CandleType);
			yield return (security, RangeCandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_contexts.Clear();
		_lastPositions.Clear();
		_initialBalance = 0m;
		_profitTargetTriggered = false;
		_drawdownTriggered = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		_initialBalance = Portfolio?.CurrentValue ?? 0m;
		CreateContext(Security);
		CreateContext(SecondSecurity);
		CreateContext(ThirdSecurity);
		CreateContext(FourthSecurity);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var security = trade.Order.Security;
		if (security == null)
			return;

		if (!_contexts.TryGetValue(security, out var context))
			return;

		var position = GetPositionVolume(security);
		_lastPositions.TryGetValue(security, out var previousPosition);
		var tradeVolume = trade.Trade.Volume ?? trade.Order.Volume ?? 0m;

		if (previousPosition == 0m && position != 0m)
		{
			context.EntryPrice = trade.Trade.Price;
		}
		else if (position == 0m)
		{
			context.EntryPrice = null;
		}
		else if (Math.Sign((double)previousPosition) != Math.Sign((double)position))
		{
			context.EntryPrice = trade.Trade.Price;
		}
		else if (position > 0m && trade.Order.Side == Sides.Buy)
		{
			UpdateAverageEntry(context, previousPosition, position, tradeVolume, trade.Trade.Price);
		}
		else if (position < 0m && trade.Order.Side == Sides.Sell)
		{
			UpdateAverageEntry(context, Math.Abs(previousPosition), Math.Abs(position), tradeVolume, trade.Trade.Price);
		}

		_lastPositions[security] = position;
	}

	private void CreateContext(Security security)
	{
		if (security == null)
			return;

		if (_contexts.ContainsKey(security))
			return;

		var fast = new SmoothedMovingAverage { Length = 3, CandlePrice = CandlePrice.Median };
		var slow = new SmoothedMovingAverage { Length = 7, CandlePrice = CandlePrice.Median };
		var context = new InstrumentContext(security, fast, slow, GetPipSize(security));
		_contexts.Add(security, context);
		_lastPositions[security] = GetPositionVolume(security);

		var subscription = SubscribeCandles(CandleType, security: security);
		subscription
			.Bind(fast, slow, (candle, fastValue, slowValue) => ProcessInstrumentCandle(context, candle, fastValue, slowValue))
			.Start();

		var rangeSubscription = SubscribeCandles(RangeCandleType, security: security);
		rangeSubscription
			.Bind(candle => ProcessRangeCandle(context, candle))
			.Start();
	}

	private void ProcessInstrumentCandle(InstrumentContext context, ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		context.FastValue = fastValue;
		context.SlowValue = slowValue;
		context.LastClose = candle.ClosePrice;

		EvaluateExits(context);
		EvaluateEntries();
		CheckGlobalRisk();
	}

	private void ProcessRangeCandle(InstrumentContext context, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0m || context.PipSize <= 0m)
		{
			context.TargetDistance = 0m;
			return;
		}

		var pipRange = range / context.PipSize;
		var targetPips = Math.Min(13m, pipRange / 5m);
		context.TargetDistance = targetPips * context.PipSize;
	}

	private void EvaluateEntries()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var totalTrend = 0m;
		var readyPairs = 0;

		foreach (var context in _contexts.Values)
		{
			if (context.FastValue is decimal fast && context.SlowValue is decimal slow)
			{
				totalTrend += fast - slow;
				readyPairs++;
			}
		}

		if (readyPairs == 0 || totalTrend == 0m)
			return;

		var bullish = totalTrend > 0m;

		foreach (var context in _contexts.Values)
		{
			if (context.FastValue is not decimal fast || context.SlowValue is not decimal slow)
				continue;

			var position = GetPositionVolume(context.Security);
			if (bullish)
			{
				if (fast > slow && position <= 0m)
					TryEnter(context, Sides.Buy, Math.Abs(position));
			}
			else
			{
				if (fast < slow && position >= 0m)
					TryEnter(context, Sides.Sell, Math.Abs(position));
			}
		}
	}

	private void EvaluateExits(InstrumentContext context)
	{
		if (context.EntryPrice is null || context.TargetDistance <= 0m || context.LastClose is null)
			return;

		var position = GetPositionVolume(context.Security);
		if (position == 0m)
			return;

		var entryPrice = context.EntryPrice.Value;
		var lastClose = context.LastClose.Value;
		var profit = lastClose - entryPrice;

		if (position > 0m && profit >= context.TargetDistance)
		{
			SellMarket(position, context.Security);
		}
		else if (position < 0m && -profit >= context.TargetDistance)
		{
			BuyMarket(Math.Abs(position), context.Security);
		}
	}

	private void TryEnter(InstrumentContext context, Sides side, decimal currentPosition)
	{
		var volume = NormalizeVolume(context.Security, BaseVolume);
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
		{
			if (currentPosition > 0m)
			{
				SellMarket(currentPosition, context.Security);
			}
			BuyMarket(volume, context.Security);
		}
		else
		{
			if (currentPosition > 0m)
			{
				BuyMarket(currentPosition, context.Security);
			}
			SellMarket(volume, context.Security);
		}
	}

	private decimal NormalizeVolume(Security security, decimal requestedVolume)
	{
		var step = security.VolumeStep ?? 0m;
		var min = security.MinVolume ?? 0m;
		var max = security.MaxVolume;
		var volume = requestedVolume;

		if (step > 0m)
			volume = Math.Round(volume / step) * step;

		if (min > 0m && volume < min)
			volume = min;

		if (max != null && volume > max.Value)
			volume = max.Value;

		return volume;
	}

	private void CheckGlobalRisk()
	{
		if (Portfolio?.CurrentValue is not decimal currentValue || _initialBalance <= 0m)
			return;

		var profit = currentValue - _initialBalance;
		var profitThreshold = _initialBalance * (TakeProfitPercent / 100m);
		var lossThreshold = _initialBalance * (MaxDrawdownPercent / 100m);

		if (!_profitTargetTriggered && profitThreshold > 0m && profit >= profitThreshold)
		{
			_profitTargetTriggered = true;
			CloseAllPositions("Global profit target reached");
		}

		if (!_drawdownTriggered && lossThreshold > 0m && -profit >= lossThreshold)
		{
			_drawdownTriggered = true;
			CloseAllPositions("Global drawdown limit reached");
		}
	}

	private void CloseAllPositions(string reason)
	{
		foreach (var context in _contexts.Values)
		{
			var position = GetPositionVolume(context.Security);
			if (position > 0m)
			{
				SellMarket(position, context.Security);
			}
			else if (position < 0m)
			{
				BuyMarket(Math.Abs(position), context.Security);
			}
		}

		LogInfo(reason);
	}

	private void UpdateAverageEntry(InstrumentContext context, decimal previousPosition, decimal currentPosition, decimal tradeVolume, decimal tradePrice)
	{
		if (tradeVolume <= 0m || previousPosition <= 0m || currentPosition <= 0m)
		{
			context.EntryPrice ??= tradePrice;
			return;
		}

		if (context.EntryPrice is decimal existing)
		{
			context.EntryPrice = ((existing * previousPosition) + (tradePrice * tradeVolume)) / currentPosition;
		}
		else
		{
			context.EntryPrice = tradePrice;
		}
	}

	private IEnumerable<Security> EnumerateSecurities()
	{
		if (Security != null)
			yield return Security;
		if (SecondSecurity != null)
			yield return SecondSecurity;
		if (ThirdSecurity != null)
			yield return ThirdSecurity;
		if (FourthSecurity != null)
			yield return FourthSecurity;
	}

	private static decimal GetPipSize(Security security)
	{
		var step = security.PriceStep;
		if (step == null || step == 0m)
			return 0.0001m;

		return step.Value;
	}

	private sealed class InstrumentContext
	{
		public InstrumentContext(Security security, SmoothedMovingAverage fast, SmoothedMovingAverage slow, decimal pipSize)
		{
			Security = security;
			Fast = fast;
			Slow = slow;
			PipSize = pipSize;
		}

		public Security Security { get; }
		public SmoothedMovingAverage Fast { get; }
		public SmoothedMovingAverage Slow { get; }
		public decimal PipSize { get; }
		public decimal? FastValue { get; set; }
		public decimal? SlowValue { get; set; }
		public decimal TargetDistance { get; set; }
		public decimal? EntryPrice { get; set; }
		public decimal? LastClose { get; set; }
	}
}

