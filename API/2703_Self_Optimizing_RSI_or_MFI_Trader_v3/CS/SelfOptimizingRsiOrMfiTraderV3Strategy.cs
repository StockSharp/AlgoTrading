using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that dynamically optimizes RSI or MFI threshold levels over a rolling history window.
/// Chooses the most profitable overbought/oversold levels and executes trades with ATR or point based risk control.
/// </summary>
public class SelfOptimizingRsiOrMfiTraderV3Strategy : Strategy
{
	private readonly StrategyParam<int> _optimizingPeriods;
	private readonly StrategyParam<bool> _useAggressiveEntries;
	private readonly StrategyParam<bool> _tradeReverse;
	private readonly StrategyParam<bool> _oneOrderAtATime;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<bool> _useDynamicVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<IndicatorSource> _indicatorChoice;
	private readonly StrategyParam<int> _indicatorTopValue;
	private readonly StrategyParam<int> _indicatorBottomValue;
	private readonly StrategyParam<int> _indicatorPeriod;
	private readonly StrategyParam<bool> _useDynamicTargets;
	private readonly StrategyParam<int> _staticStopLossPoints;
	private readonly StrategyParam<int> _staticTakeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<int> _breakEvenTriggerPoints;
	private readonly StrategyParam<int> _breakEvenPaddingPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal indicator, decimal close)> _history = new();

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	private IIndicator _indicator;
	private AverageTrueRange _atr;

	/// <summary>
	/// Indicator source used for optimization.
	/// </summary>
	public enum IndicatorSource
	{
		/// <summary>
		/// Use Relative Strength Index values.
		/// </summary>
		RelativeStrengthIndex,

		/// <summary>
		/// Use Money Flow Index values.
		/// </summary>
		MoneyFlowIndex,
	}

	/// <summary>
	/// Number of bars evaluated when searching for best thresholds.
	/// </summary>
	public int OptimizingPeriods
	{
		get => _optimizingPeriods.Value;
		set => _optimizingPeriods.Value = value;
	}

	/// <summary>
	/// Allow entries without waiting for indicator crosses.
	/// </summary>
	public bool UseAggressiveEntries
	{
		get => _useAggressiveEntries.Value;
		set => _useAggressiveEntries.Value = value;
	}

	/// <summary>
	/// Invert profitability preference to trade opposite direction.
	/// </summary>
	public bool TradeReverse
	{
		get => _tradeReverse.Value;
		set => _tradeReverse.Value = value;
	}

	/// <summary>
	/// Restrict strategy to a single open position at a time.
	/// </summary>
	public bool OneOrderAtATime
	{
		get => _oneOrderAtATime.Value;
		set => _oneOrderAtATime.Value = value;
	}

	/// <summary>
	/// Static volume used when dynamic sizing is disabled.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Enable risk based position sizing.
	/// </summary>
	public bool UseDynamicVolume
	{
		get => _useDynamicVolume.Value;
		set => _useDynamicVolume.Value = value;
	}

	/// <summary>
	/// Percentage of portfolio risked per trade when sizing dynamically.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Oscillator used for optimization.
	/// </summary>
	public IndicatorSource IndicatorChoice
	{
		get => _indicatorChoice.Value;
		set => _indicatorChoice.Value = value;
	}

	/// <summary>
	/// Highest threshold tested when searching for overbought levels.
	/// </summary>
	public int IndicatorTopValue
	{
		get => _indicatorTopValue.Value;
		set => _indicatorTopValue.Value = value;
	}

	/// <summary>
	/// Lowest threshold tested when searching for oversold levels.
	/// </summary>
	public int IndicatorBottomValue
	{
		get => _indicatorBottomValue.Value;
		set => _indicatorBottomValue.Value = value;
	}

	/// <summary>
	/// Period used for the selected indicator.
	/// </summary>
	public int IndicatorPeriod
	{
		get => _indicatorPeriod.Value;
		set => _indicatorPeriod.Value = value;
	}

	/// <summary>
	/// Enable ATR based stop-loss and take-profit levels.
	/// </summary>
	public bool UseDynamicTargets
	{
		get => _useDynamicTargets.Value;
		set => _useDynamicTargets.Value = value;
	}

	/// <summary>
	/// Static stop-loss distance expressed in points when dynamic targets are disabled.
	/// </summary>
	public int StaticStopLossPoints
	{
		get => _staticStopLossPoints.Value;
		set => _staticStopLossPoints.Value = value;
	}

	/// <summary>
	/// Static take-profit distance expressed in points when dynamic targets are disabled.
	/// </summary>
	public int StaticTakeProfitPoints
	{
		get => _staticTakeProfitPoints.Value;
		set => _staticTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// ATR multiplier applied to stop-loss when dynamic targets are enabled.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier applied to take-profit when dynamic targets are enabled.
	/// </summary>
	public decimal TakeProfitAtrMultiplier
	{
		get => _takeProfitAtrMultiplier.Value;
		set => _takeProfitAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Enable stop adjustment to breakeven once profit target is reached.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit threshold in points required to arm the breakeven stop.
	/// </summary>
	public int BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Additional padding in points applied once breakeven triggers.
	/// </summary>
	public int BreakEvenPaddingPoints
	{
		get => _breakEvenPaddingPoints.Value;
		set => _breakEvenPaddingPoints.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="SelfOptimizingRsiOrMfiTraderV3Strategy"/>.
	/// </summary>
	public SelfOptimizingRsiOrMfiTraderV3Strategy()
	{
		_optimizingPeriods = Param(nameof(OptimizingPeriods), 144)
			.SetGreaterThanZero()
			.SetDisplay("Optimization Bars", "Number of bars used for optimization", "General")
			.SetCanOptimize(true)
			.SetOptimize(60, 240, 30);

		_useAggressiveEntries = Param(nameof(UseAggressiveEntries), false)
			.SetDisplay("Aggressive Entries", "Allow entries without indicator crosses", "Trading");

		_tradeReverse = Param(nameof(TradeReverse), false)
			.SetDisplay("Reverse Trading", "Swap profitability preference for opposite trades", "Trading");

		_oneOrderAtATime = Param(nameof(OneOrderAtATime), true)
			.SetDisplay("One Position", "Permit only one open position", "Trading");

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Static order volume when sizing manually", "Risk");

		_useDynamicVolume = Param(nameof(UseDynamicVolume), true)
			.SetDisplay("Dynamic Volume", "Use risk percentage for position sizing", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 2m)
			.SetRange(0.1m, 10m)
			.SetDisplay("Risk %", "Percent of capital risked per trade", "Risk");

		_indicatorChoice = Param(nameof(IndicatorChoice), IndicatorSource.RelativeStrengthIndex)
			.SetDisplay("Indicator", "Oscillator optimized by the strategy", "Indicator");

		_indicatorTopValue = Param(nameof(IndicatorTopValue), 100)
			.SetDisplay("Top Level", "Upper bound for level search", "Indicator");

		_indicatorBottomValue = Param(nameof(IndicatorBottomValue), 0)
			.SetDisplay("Bottom Level", "Lower bound for level search", "Indicator");

		_indicatorPeriod = Param(nameof(IndicatorPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Indicator Period", "Averaging period for RSI or MFI", "Indicator");

		_useDynamicTargets = Param(nameof(UseDynamicTargets), true)
			.SetDisplay("Dynamic Targets", "Use ATR based stop-loss and take-profit", "Risk");

		_staticStopLossPoints = Param(nameof(StaticStopLossPoints), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Static Stop", "Stop-loss in points when dynamic targets disabled", "Risk");

		_staticTakeProfitPoints = Param(nameof(StaticTakeProfitPoints), 2000)
			.SetGreaterThanZero()
			.SetDisplay("Static Take", "Take-profit in points when dynamic targets disabled", "Risk");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Mult", "Stop-loss multiplier applied to ATR", "Risk");

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 7m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Take Mult", "Take-profit multiplier applied to ATR", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use Breakeven", "Move stop to breakeven after trigger", "Risk");

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 200)
			.SetGreaterThanZero()
			.SetDisplay("Breakeven Trigger", "Profit in points required to arm breakeven", "Risk");

		_breakEvenPaddingPoints = Param(nameof(BreakEvenPaddingPoints), 100)
			.SetGreaterThanZero()
			.SetDisplay("Breakeven Padding", "Padding in points applied after trigger", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for analysis", "General");

		Volume = 1m;
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

		_history.Clear();
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_indicator = null;
		_atr = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = IndicatorChoice switch
		{
			IndicatorSource.MoneyFlowIndex => new MoneyFlowIndex { Length = IndicatorPeriod },
			_ => new RelativeStrengthIndex { Length = IndicatorPeriod }
		};

		_atr = new AverageTrueRange { Length = IndicatorPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_indicator, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal indicatorValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_history.Add((indicatorValue, candle.ClosePrice));

		var maxNeeded = Math.Max(OptimizingPeriods + 1, 3);
		while (_history.Count > maxNeeded)
		{
			_history.RemoveAt(0);
		}

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var stepPrice = Security?.StepPrice ?? priceStep;
		if (stepPrice <= 0m)
			stepPrice = priceStep;

		var triggerDiff = UseBreakEven ? BreakEvenTriggerPoints * priceStep : 0m;
		var paddingPoints = BreakEvenPaddingPoints > BreakEvenTriggerPoints ? 0 : BreakEvenPaddingPoints;
		var paddingDiff = UseBreakEven ? paddingPoints * priceStep : 0m;

		ManageOpenPosition(candle, triggerDiff, paddingDiff);

		if (_history.Count < maxNeeded)
			return;

		var indicatorValues = new decimal[_history.Count];
		var closeValues = new decimal[_history.Count];
		for (var i = 0; i < _history.Count; i++)
		{
			var source = _history[_history.Count - 1 - i];
			indicatorValues[i] = source.indicator;
			closeValues[i] = source.close;
		}

		decimal stopLossDiff;
		decimal takeProfitDiff;

		if (UseDynamicTargets)
		{
			if (atrValue <= 0m)
				return;

			stopLossDiff = atrValue * StopLossAtrMultiplier;
			takeProfitDiff = atrValue * TakeProfitAtrMultiplier;
		}
		else
		{
			stopLossDiff = StaticStopLossPoints * priceStep;
			takeProfitDiff = StaticTakeProfitPoints * priceStep;
		}

		if (stopLossDiff <= 0m || takeProfitDiff <= 0m)
			return;

		var volume = CalculateVolume(stopLossDiff);
		if (volume <= 0m)
			return;

		var stepMultiplier = priceStep > 0m ? stepPrice / priceStep : 1m;

		var (sellLevel, sellProfit) = CalculateBestSellLevel(indicatorValues, closeValues, stopLossDiff, takeProfitDiff, volume, stepMultiplier);
		var (buyLevel, buyProfit) = CalculateBestBuyLevel(indicatorValues, closeValues, stopLossDiff, takeProfitDiff, volume, stepMultiplier);

		var adjustedSellProfit = sellProfit;
		var adjustedBuyProfit = buyProfit;
		if (TradeReverse)
		{
			adjustedSellProfit = buyProfit;
			adjustedBuyProfit = sellProfit;
		}

		var canEnter = !OneOrderAtATime || Position == 0m;
		var currentIndicator = indicatorValues[0];
		var previousIndicator = indicatorValues[1];

		if (adjustedSellProfit > adjustedBuyProfit)
		{
			if (canEnter && ((currentIndicator < sellLevel && previousIndicator > sellLevel) || UseAggressiveEntries))
			{
				EnterShort(candle, volume, stopLossDiff, takeProfitDiff);
			}
		}
		else if (adjustedSellProfit < adjustedBuyProfit)
		{
			if (canEnter && ((currentIndicator > buyLevel && previousIndicator < buyLevel) || UseAggressiveEntries))
			{
				EnterLong(candle, volume, stopLossDiff, takeProfitDiff);
			}
		}
	}

	private (int level, decimal profit) CalculateBestSellLevel(decimal[] indicatorValues, decimal[] closeValues, decimal stopLossDiff, decimal takeProfitDiff, decimal volume, decimal stepMultiplier)
	{
		var bottom = Math.Min(IndicatorBottomValue, IndicatorTopValue);
		var top = Math.Max(IndicatorBottomValue, IndicatorTopValue);
		var bestProfit = 0m;
		var bestLevel = bottom;
		var updated = false;

		for (var level = bottom; level <= top; level++)
		{
			var profit = EvaluateSellLevel(indicatorValues, closeValues, level, stopLossDiff, takeProfitDiff, volume, stepMultiplier);
			if (profit > bestProfit)
			{
				bestProfit = profit;
				bestLevel = level;
				updated = true;
			}
		}

		return (bestLevel, updated ? bestProfit : 0m);
	}

	private (int level, decimal profit) CalculateBestBuyLevel(decimal[] indicatorValues, decimal[] closeValues, decimal stopLossDiff, decimal takeProfitDiff, decimal volume, decimal stepMultiplier)
	{
		var bottom = Math.Min(IndicatorBottomValue, IndicatorTopValue);
		var top = Math.Max(IndicatorBottomValue, IndicatorTopValue);
		var bestProfit = 0m;
		var bestLevel = top;
		var updated = false;

		for (var level = top; level >= bottom; level--)
		{
			var profit = EvaluateBuyLevel(indicatorValues, closeValues, level, stopLossDiff, takeProfitDiff, volume, stepMultiplier);
			if (profit > bestProfit)
			{
				bestProfit = profit;
				bestLevel = level;
				updated = true;
			}
		}

		return (bestLevel, updated ? bestProfit : 0m);
	}

	private decimal EvaluateSellLevel(decimal[] indicatorValues, decimal[] closeValues, int level, decimal stopLossDiff, decimal takeProfitDiff, decimal volume, decimal stepMultiplier)
	{
		var totalProfit = 0m;
		if (indicatorValues.Length < 3)
			return 0m;

		var threshold = (decimal)level;
		for (var i = indicatorValues.Length - 2; i >= 2; i--)
		{
			if (indicatorValues[i] < threshold && indicatorValues[i + 1] > threshold)
			{
				var entryPrice = closeValues[i];
				for (var j = i - 1; j >= 1; j--)
				{
					var price = closeValues[j];
					if (price >= entryPrice + stopLossDiff)
					{
						var loss = (price - entryPrice) * stepMultiplier * volume;
						totalProfit -= loss;
						i = j;
						break;
					}

					if (price <= entryPrice - takeProfitDiff)
					{
						var gain = (entryPrice - price) * stepMultiplier * volume;
						totalProfit += gain;
						i = j;
						break;
					}
				}
			}
		}

		return totalProfit;
	}

	private decimal EvaluateBuyLevel(decimal[] indicatorValues, decimal[] closeValues, int level, decimal stopLossDiff, decimal takeProfitDiff, decimal volume, decimal stepMultiplier)
	{
		var totalProfit = 0m;
		if (indicatorValues.Length < 3)
			return 0m;

		var threshold = (decimal)level;
		for (var i = indicatorValues.Length - 2; i >= 2; i--)
		{
			if (indicatorValues[i] > threshold && indicatorValues[i + 1] < threshold)
			{
				var entryPrice = closeValues[i];
				for (var j = i - 1; j >= 1; j--)
				{
					var price = closeValues[j];
					if (price <= entryPrice - stopLossDiff)
					{
						var loss = (entryPrice - price) * stepMultiplier * volume;
						totalProfit -= loss;
						i = j;
						break;
					}

					if (price >= entryPrice + takeProfitDiff)
					{
						var gain = (price - entryPrice) * stepMultiplier * volume;
						totalProfit += gain;
						i = j;
						break;
					}
				}
			}
		}

		return totalProfit;
	}

	private decimal CalculateVolume(decimal stopLossDiff)
	{
		var volume = BaseVolume;

		if (UseDynamicVolume && stopLossDiff > 0m && Security != null)
		{
			var priceStep = Security.PriceStep ?? 0m;
			var stepPrice = Security.StepPrice ?? 0m;
			if (priceStep > 0m && stepPrice > 0m)
			{
				var stopPoints = stopLossDiff / priceStep;
				var riskPerUnit = stopPoints * stepPrice;
				var capital = Portfolio?.CurrentValue ?? 0m;
				var riskBudget = capital * (RiskPercent / 100m);
				if (riskPerUnit > 0m && riskBudget > 0m)
				{
					var rawVolume = riskBudget / riskPerUnit;
					if (rawVolume > 0m)
						volume = rawVolume;
				}
			}
		}

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security == null)
			return Math.Max(volume, 0.01m);

		var step = Security.VolumeStep ?? 0m;
		var min = Security.MinVolume ?? 0m;
		var max = Security.MaxVolume ?? decimal.MaxValue;

		if (step <= 0m)
			step = 1m;

		if (min <= 0m)
			min = step;

		if (volume < min)
			volume = min;

		if (volume > max)
			volume = max;

		volume = Math.Floor(volume / step) * step;
		if (volume <= 0m)
			volume = min;

		return volume;
	}

	private void EnterLong(ICandleMessage candle, decimal volume, decimal stopLossDiff, decimal takeProfitDiff)
	{
		var orderVolume = volume;
		if (Position < 0m)
			orderVolume += Math.Abs(Position);

		BuyMarket(orderVolume);

		_entryPrice = candle.ClosePrice;
		_stopPrice = _entryPrice - stopLossDiff;
		_takeProfitPrice = _entryPrice + takeProfitDiff;
	}

	private void EnterShort(ICandleMessage candle, decimal volume, decimal stopLossDiff, decimal takeProfitDiff)
	{
		var orderVolume = volume;
		if (Position > 0m)
			orderVolume += Position;

		SellMarket(orderVolume);

		_entryPrice = candle.ClosePrice;
		_stopPrice = _entryPrice + stopLossDiff;
		_takeProfitPrice = _entryPrice - takeProfitDiff;
	}

	private void ManageOpenPosition(ICandleMessage candle, decimal triggerDiff, decimal paddingDiff)
	{
		if (Position > 0m)
		{
			if (UseBreakEven && _entryPrice is decimal entry && _stopPrice is decimal currentStop)
			{
				var triggerPrice = entry + triggerDiff;
				var targetStop = entry + paddingDiff;
				if (triggerDiff > 0m && candle.HighPrice >= triggerPrice && currentStop < targetStop)
					_stopPrice = targetStop;
			}

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_takeProfitPrice is decimal target && candle.HighPrice >= target)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}
		}
		else if (Position < 0m)
		{
			if (UseBreakEven && _entryPrice is decimal entry && _stopPrice is decimal currentStop)
			{
				var triggerPrice = entry - triggerDiff;
				var targetStop = entry - paddingDiff;
				if (triggerDiff > 0m && candle.LowPrice <= triggerPrice && currentStop > targetStop)
					_stopPrice = targetStop;
			}

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_takeProfitPrice is decimal target && candle.LowPrice <= target)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}
}
