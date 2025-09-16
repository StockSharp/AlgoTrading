using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle shadow percent strategy converted from MetaTrader.
/// Trades when a candle shows an extended wick compared to its body.
/// Position size is derived from risk percentage and stop distance.
/// </summary>
public class CandleShadowPercentStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _minBodyPips;
	private readonly StrategyParam<bool> _enableTopShadow;
	private readonly StrategyParam<decimal> _topShadowPercent;
	private readonly StrategyParam<bool> _topShadowIsMinimum;
	private readonly StrategyParam<bool> _enableLowerShadow;
	private readonly StrategyParam<decimal> _lowerShadowPercent;
	private readonly StrategyParam<bool> _lowerShadowIsMinimum;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal? _entryPrice;
	
	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}
	
	/// <summary>
	/// Take profit in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}
	
	/// <summary>
	/// Risk percentage per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}
	
	/// <summary>
	/// Minimum body size in pips to evaluate shadows.
	/// </summary>
	public int MinBodyPips
	{
		get => _minBodyPips.Value;
		set => _minBodyPips.Value = value;
	}
	
	/// <summary>
	/// Enables signals based on the top shadow.
	/// </summary>
	public bool EnableTopShadow
	{
		get => _enableTopShadow.Value;
		set => _enableTopShadow.Value = value;
	}
	
	/// <summary>
	/// Threshold for the top shadow as a percentage of the body.
	/// </summary>
	public decimal TopShadowPercent
	{
		get => _topShadowPercent.Value;
		set => _topShadowPercent.Value = value;
	}
	
	/// <summary>
	/// If true the top shadow percentage acts as a minimum threshold.
	/// </summary>
	public bool TopShadowIsMinimum
	{
		get => _topShadowIsMinimum.Value;
		set => _topShadowIsMinimum.Value = value;
	}
	
	/// <summary>
	/// Enables signals based on the lower shadow.
	/// </summary>
	public bool EnableLowerShadow
	{
		get => _enableLowerShadow.Value;
		set => _enableLowerShadow.Value = value;
	}
	
	/// <summary>
	/// Threshold for the lower shadow as a percentage of the body.
	/// </summary>
	public decimal LowerShadowPercent
	{
		get => _lowerShadowPercent.Value;
		set => _lowerShadowPercent.Value = value;
	}
	
	/// <summary>
	/// If true the lower shadow percentage acts as a minimum threshold.
	/// </summary>
	public bool LowerShadowIsMinimum
	{
		get => _lowerShadowIsMinimum.Value;
		set => _lowerShadowIsMinimum.Value = value;
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
	/// Initializes a new instance of the <see cref="CandleShadowPercentStrategy"/>.
	/// </summary>
	public CandleShadowPercentStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk")
			.SetCanOptimize(true);
		
		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit", "Take profit distance in pips", "Risk")
			.SetCanOptimize(true);
		
		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Risk percentage per trade", "Risk")
			.SetCanOptimize(true);
		
		_minBodyPips = Param(nameof(MinBodyPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Body", "Minimum candle body size in pips", "Pattern")
			.SetCanOptimize(true);
		
		_enableTopShadow = Param(nameof(EnableTopShadow), true)
			.SetDisplay("Use Top Shadow", "Enable sell signals from upper wicks", "Pattern");
		
		_topShadowPercent = Param(nameof(TopShadowPercent), 30m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Top Shadow %", "Upper wick percentage threshold", "Pattern")
			.SetCanOptimize(true);
		
		_topShadowIsMinimum = Param(nameof(TopShadowIsMinimum), true)
			.SetDisplay("Top Shadow Uses Min", "If true the threshold is treated as a minimum", "Pattern");
		
		_enableLowerShadow = Param(nameof(EnableLowerShadow), true)
			.SetDisplay("Use Lower Shadow", "Enable buy signals from lower wicks", "Pattern");
		
		_lowerShadowPercent = Param(nameof(LowerShadowPercent), 80m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Lower Shadow %", "Lower wick percentage threshold", "Pattern")
			.SetCanOptimize(true);
		
		_lowerShadowIsMinimum = Param(nameof(LowerShadowIsMinimum), true)
			.SetDisplay("Lower Shadow Uses Min", "If true the threshold is treated as a minimum", "Pattern");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for pattern detection", "Data");
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
		
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_entryPrice = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
		
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
		
		ManageOpenPosition(candle);
		
		var pipSize = GetPipSize();
		var minBody = MinBodyPips * pipSize;
		
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		if (body < minBody || body <= 0m)
			return;
		
		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		
		var topRatio = body > 0m ? upperShadow / body * 100m : 0m;
		var lowerRatio = body > 0m ? lowerShadow / body * 100m : 0m;
		
		var topSignal = EnableTopShadow && upperShadow > 0m && CheckThreshold(topRatio, TopShadowPercent, TopShadowIsMinimum);
		var lowerSignal = EnableLowerShadow && lowerShadow > 0m && CheckThreshold(lowerRatio, LowerShadowPercent, LowerShadowIsMinimum);
		
		if (topSignal && lowerSignal)
		{
			if (topRatio > lowerRatio)
				lowerSignal = false;
			else
				topSignal = false;
		}
		
		if (topSignal && Position <= 0)
		{
			EnterShort(candle, pipSize);
		}
		else if (lowerSignal && Position >= 0)
		{
			EnterLong(candle, pipSize);
		}
	}
	
	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var stopHit = _longStop.HasValue && candle.LowPrice <= _longStop.Value;
			var takeHit = _longTake.HasValue && candle.HighPrice >= _longTake.Value;
			
			if (stopHit || takeHit)
			{
				SellMarket(Position);
				LogInfo($"Closing long at {candle.ClosePrice}. Stop hit: {stopHit}, Take hit: {takeHit}");
				_longStop = null;
				_longTake = null;
				_entryPrice = null;
			}
		}
		else if (Position < 0)
		{
			var stopHit = _shortStop.HasValue && candle.HighPrice >= _shortStop.Value;
			var takeHit = _shortTake.HasValue && candle.LowPrice <= _shortTake.Value;
			
			if (stopHit || takeHit)
			{
				BuyMarket(-Position);
				LogInfo($"Closing short at {candle.ClosePrice}. Stop hit: {stopHit}, Take hit: {takeHit}");
				_shortStop = null;
				_shortTake = null;
				_entryPrice = null;
			}
		}
	}
	
	private void EnterLong(ICandleMessage candle, decimal pipSize)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		var stopDistance = StopLossPips * pipSize;
		if (stopDistance <= 0m)
			return;
		
		var takeDistance = TakeProfitPips * pipSize;
		var volume = CalculatePositionSize(stopDistance);
		if (volume <= 0m)
			return;
		
		var quantity = volume;
		if (Position < 0)
			quantity += Math.Abs(Position);
		
		var entryPrice = candle.ClosePrice;
		var stopPrice = entryPrice - stopDistance;
		var takePrice = takeDistance > 0m ? entryPrice + takeDistance : (decimal?)null;
		
		BuyMarket(quantity);
		
		_longStop = stopPrice;
		_longTake = takePrice;
		_shortStop = null;
		_shortTake = null;
		_entryPrice = entryPrice;
		
		LogInfo($"Entered long at {entryPrice} with quantity {quantity}. Stop {stopPrice}, Take {(takePrice.HasValue ? takePrice.Value.ToString() : "n/a")}");
	}
	
	private void EnterShort(ICandleMessage candle, decimal pipSize)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		var stopDistance = StopLossPips * pipSize;
		if (stopDistance <= 0m)
			return;
		
		var takeDistance = TakeProfitPips * pipSize;
		var volume = CalculatePositionSize(stopDistance);
		if (volume <= 0m)
			return;
		
		var quantity = volume;
		if (Position > 0)
			quantity += Math.Abs(Position);
		
		var entryPrice = candle.ClosePrice;
		var stopPrice = entryPrice + stopDistance;
		var takePrice = takeDistance > 0m ? entryPrice - takeDistance : (decimal?)null;
		
		SellMarket(quantity);
		
		_shortStop = stopPrice;
		_shortTake = takePrice;
		_longStop = null;
		_longTake = null;
		_entryPrice = entryPrice;
		
		LogInfo($"Entered short at {entryPrice} with quantity {quantity}. Stop {stopPrice}, Take {(takePrice.HasValue ? takePrice.Value.ToString() : "n/a")}");
	}
	
	private decimal CalculatePositionSize(decimal stopDistance)
	{
		var defaultVolume = Volume > 0m ? Volume : 1m;
		
		if (Portfolio == null || Portfolio.CurrentValue <= 0m)
			return defaultVolume;
		
		var riskAmount = Portfolio.CurrentValue * (RiskPercent / 100m);
		if (riskAmount <= 0m || stopDistance <= 0m)
			return defaultVolume;
		
		var size = riskAmount / stopDistance;
		return size > 0m ? size : defaultVolume;
	}
	
	private static bool CheckThreshold(decimal ratio, decimal threshold, bool isMinimum)
	{
		return isMinimum ? ratio >= threshold : ratio <= threshold;
	}
	
	private decimal GetPipSize()
	{
		return Security?.PriceStep ?? 1m;
	}
}