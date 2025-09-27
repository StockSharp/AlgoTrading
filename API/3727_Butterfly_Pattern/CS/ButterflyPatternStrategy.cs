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

/// <summary>
/// Detects bullish and bearish butterfly harmonic patterns on a configurable timeframe.
/// Distributes positions across three take-profit levels and supports optional break-even
/// and trailing-stop management.
/// </summary>
public class ButterflyPatternStrategy : Strategy
{
	private sealed class Pivot
	{
		public Pivot(DateTimeOffset time, decimal price, bool isHigh)
		{
			Time = time;
			Price = price;
			IsHigh = isHigh;
		}

		public DateTimeOffset Time { get; }

		public decimal Price { get; }

		public bool IsHigh { get; }
	}

	private sealed class PatternState
	{
		private readonly List<ICandleMessage> _candles = new();
		private readonly List<Pivot> _pivots = new();

		public Sides? Side { get; set; }

		public decimal RemainingVolume { get; set; }

		public decimal Lot1 { get; set; }

		public decimal Lot2 { get; set; }

		public decimal Lot3 { get; set; }

		public bool Tp1Filled { get; set; }

		public bool Tp2Filled { get; set; }

		public bool Tp3Filled { get; set; }

		public decimal? EntryPrice { get; set; }

		public decimal? StopPrice { get; set; }

		public decimal Tp1Price { get; set; }

		public decimal Tp2Price { get; set; }

		public decimal Tp3Price { get; set; }

		public bool BreakEvenApplied { get; set; }

		public bool TrailingActivated { get; set; }

		public DateTimeOffset? LastPatternTime { get; set; }

		public void ResetPosition()
		{
			Side = null;
			RemainingVolume = 0m;
			Lot1 = 0m;
			Lot2 = 0m;
			Lot3 = 0m;
			Tp1Filled = false;
			Tp2Filled = false;
			Tp3Filled = false;
			EntryPrice = null;
			StopPrice = null;
			Tp1Price = 0m;
			Tp2Price = 0m;
			Tp3Price = 0m;
			BreakEvenApplied = false;
			TrailingActivated = false;
		}

		public void ResetSeries()
		{
			ResetPosition();
			_candles.Clear();
			_pivots.Clear();
			LastPatternTime = null;
		}

		public void AddCandle(ICandleMessage candle)
		{
			_candles.Add(candle);
		}

		public bool TryExtractPivot(int left, int right, out Pivot pivot)
		{
			pivot = default;

			var required = left + right + 1;
			if (_candles.Count < required)
				return false;

			var index = _candles.Count - 1 - right;
			if (index < left)
				return false;

			var middle = _candles[index];
			var isHigh = true;
			var isLow = true;
			var from = index - left;
			var to = index + right;

			for (var i = from; i <= to; i++)
			{
				if (i < 0 || i >= _candles.Count)
					continue;

				if (i == index)
					continue;

				var c = _candles[i];
				if (c.HighPrice > middle.HighPrice)
					isHigh = false;

				if (c.LowPrice < middle.LowPrice)
					isLow = false;
			}

			if (!isHigh && !isLow)
				return false;

			pivot = new Pivot(middle.OpenTime, isHigh ? middle.HighPrice : middle.LowPrice, isHigh);

			if (_candles.Count > required)
				_candles.RemoveAt(0);

			return true;
		}

		public void AddPivot(Pivot pivot)
		{
			_pivots.Add(pivot);
			if (_pivots.Count > 5)
				_pivots.RemoveAt(0);
		}

		public bool TryGetPattern(out Pivot x, out Pivot a, out Pivot b, out Pivot c, out Pivot d)
		{
			x = default;
			a = default;
			b = default;
			c = default;
			d = default;

			if (_pivots.Count < 5)
				return false;

			x = _pivots[^5];
			a = _pivots[^4];
			b = _pivots[^3];
			c = _pivots[^2];
			d = _pivots[^1];
			return true;
		}
	}

	private readonly PatternState _state = new();

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _pivotLeft;
	private readonly StrategyParam<int> _pivotRight;
	private readonly StrategyParam<decimal> _tolerance;
	private readonly StrategyParam<bool> _allowTrading;
	private readonly StrategyParam<bool> _useFixedVolume;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _adjustLots;
	private readonly StrategyParam<decimal> _tp1Percent;
	private readonly StrategyParam<decimal> _tp2Percent;
	private readonly StrategyParam<decimal> _tp3Percent;
	private readonly StrategyParam<decimal> _minPatternQuality;
	private readonly StrategyParam<bool> _useSessionFilter;
	private readonly StrategyParam<int> _sessionStartHour;
	private readonly StrategyParam<int> _sessionEndHour;
	private readonly StrategyParam<bool> _revalidatePattern;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<int> _breakEvenAfterTp;
	private readonly StrategyParam<decimal> _breakEvenTrigger;
	private readonly StrategyParam<decimal> _breakEvenProfit;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<int> _trailAfterTp;
	private readonly StrategyParam<decimal> _trailStart;
	private readonly StrategyParam<decimal> _trailStep;

	public ButterflyPatternStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(2).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for pattern detection", "General");

		_pivotLeft = Param(nameof(PivotLeft), 1)
		.SetGreaterOrEqual(1)
		.SetDisplay("Pivot Left", "Bars to the left when validating a pivot", "Pattern");

		_pivotRight = Param(nameof(PivotRight), 1)
		.SetGreaterOrEqual(1)
		.SetDisplay("Pivot Right", "Bars to the right when validating a pivot", "Pattern");

		_tolerance = Param(nameof(Tolerance), 0.10m)
		.SetGreaterThanZero()
		.SetDisplay("Ratio Tolerance", "Maximum deviation allowed for Fibonacci ratios", "Pattern");

		_allowTrading = Param(nameof(AllowTrading), true)
		.SetDisplay("Allow Trading", "Enable order generation when patterns are confirmed", "Trading");

		_useFixedVolume = Param(nameof(UseFixedVolume), true)
		.SetDisplay("Use Fixed Volume", "Use fixed trade volume instead of risk-based sizing", "Risk");

		_fixedVolume = Param(nameof(FixedVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Fixed Volume", "Volume to trade when fixed sizing is active", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Percent", "Risk per trade as a percentage of portfolio value", "Risk");

		_adjustLots = Param(nameof(AdjustLotsForTakeProfits), true)
		.SetDisplay("Adjust Lots", "Normalize take-profit allocations to match total volume", "Risk");

		_tp1Percent = Param(nameof(Tp1Percent), 50m)
		.SetNotNegative()
		.SetDisplay("TP1 %", "Share of volume closed at the first take-profit", "Targets");

		_tp2Percent = Param(nameof(Tp2Percent), 30m)
		.SetNotNegative()
		.SetDisplay("TP2 %", "Share of volume closed at the second take-profit", "Targets");

		_tp3Percent = Param(nameof(Tp3Percent), 20m)
		.SetNotNegative()
		.SetDisplay("TP3 %", "Share of volume closed at the third take-profit", "Targets");

		_minPatternQuality = Param(nameof(MinPatternQuality), 0.1m)
		.SetDisplay("Minimum Quality", "Minimum harmonic score required to trade", "Pattern");

		_useSessionFilter = Param(nameof(UseSessionFilter), false)
		.SetDisplay("Use Session Filter", "Only trade within configured session hours", "Trading");

		_sessionStartHour = Param(nameof(SessionStartHour), 8)
		.SetDisplay("Session Start", "Session start hour in exchange time", "Trading");

		_sessionEndHour = Param(nameof(SessionEndHour), 16)
		.SetDisplay("Session End", "Session end hour in exchange time", "Trading");

		_revalidatePattern = Param(nameof(RevalidatePattern), false)
		.SetDisplay("Revalidate Pattern", "Confirm that price has not invalidated the setup", "Pattern");

		_useBreakEven = Param(nameof(UseBreakEven), false)
		.SetDisplay("Use Break-Even", "Enable break-even management", "Risk");

		_breakEvenAfterTp = Param(nameof(BreakEvenAfterTp), 1)
		.SetGreaterOrEqual(1)
		.SetDisplay("Break-Even After TP", "Activate break-even after the specified take-profit", "Risk");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 30m)
		.SetDisplay("Break-Even Trigger", "Points required to lock break-even", "Risk");

		_breakEvenProfit = Param(nameof(BreakEvenProfit), 5m)
		.SetDisplay("Break-Even Profit", "Profit offset applied to break-even", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
		.SetDisplay("Use Trailing", "Enable trailing stop management", "Risk");

		_trailAfterTp = Param(nameof(TrailAfterTp), 2)
		.SetGreaterOrEqual(1)
		.SetDisplay("Trail After TP", "Activate trailing after the specified take-profit", "Risk");

		_trailStart = Param(nameof(TrailStart), 20m)
		.SetDisplay("Trail Start", "Points required before trailing", "Risk");

		_trailStep = Param(nameof(TrailStep), 5m)
		.SetDisplay("Trail Step", "Trailing step in price points", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int PivotLeft
	{
		get => _pivotLeft.Value;
		set => _pivotLeft.Value = value;
	}

	public int PivotRight
	{
		get => _pivotRight.Value;
		set => _pivotRight.Value = value;
	}

	public decimal Tolerance
	{
		get => _tolerance.Value;
		set => _tolerance.Value = value;
	}

	public bool AllowTrading
	{
		get => _allowTrading.Value;
		set => _allowTrading.Value = value;
	}

	public bool UseFixedVolume
	{
		get => _useFixedVolume.Value;
		set => _useFixedVolume.Value = value;
	}

	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public bool AdjustLotsForTakeProfits
	{
		get => _adjustLots.Value;
		set => _adjustLots.Value = value;
	}

	public decimal Tp1Percent
	{
		get => _tp1Percent.Value;
		set => _tp1Percent.Value = value;
	}

	public decimal Tp2Percent
	{
		get => _tp2Percent.Value;
		set => _tp2Percent.Value = value;
	}

	public decimal Tp3Percent
	{
		get => _tp3Percent.Value;
		set => _tp3Percent.Value = value;
	}

	public decimal MinPatternQuality
	{
		get => _minPatternQuality.Value;
		set => _minPatternQuality.Value = value;
	}

	public bool UseSessionFilter
	{
		get => _useSessionFilter.Value;
		set => _useSessionFilter.Value = value;
	}

	public int SessionStartHour
	{
		get => _sessionStartHour.Value;
		set => _sessionStartHour.Value = value;
	}

	public int SessionEndHour
	{
		get => _sessionEndHour.Value;
		set => _sessionEndHour.Value = value;
	}

	public bool RevalidatePattern
	{
		get => _revalidatePattern.Value;
		set => _revalidatePattern.Value = value;
	}

	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	public int BreakEvenAfterTp
	{
		get => _breakEvenAfterTp.Value;
		set => _breakEvenAfterTp.Value = value;
	}

	public decimal BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	public decimal BreakEvenProfit
	{
		get => _breakEvenProfit.Value;
		set => _breakEvenProfit.Value = value;
	}

	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	public int TrailAfterTp
	{
		get => _trailAfterTp.Value;
		set => _trailAfterTp.Value = value;
	}

	public decimal TrailStart
	{
		get => _trailStart.Value;
		set => _trailStart.Value = value;
	}

	public decimal TrailStep
	{
		get => _trailStep.Value;
		set => _trailStep.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_state.ResetSeries();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_state.ResetSeries();

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

		UpdateRiskManagement(candle);

		if (!IsWithinSession(candle.OpenTime))
		return;

		_state.AddCandle(candle);

		if (_state.TryExtractPivot(PivotLeft, PivotRight, out var pivot))
		{
		_state.AddPivot(pivot);
		TryDetectPattern(candle);
		}
	}

	private bool IsWithinSession(DateTimeOffset time)
	{
		if (!UseSessionFilter)
		return true;

		var hour = time.Hour;
		if (SessionStartHour < SessionEndHour)
		return hour >= SessionStartHour && hour < SessionEndHour;

		return hour >= SessionStartHour || hour < SessionEndHour;
	}

	private void TryDetectPattern(ICandleMessage candle)
	{
		if (!_state.TryGetPattern(out var x, out var a, out var b, out var c, out var d))
		return;

		if (_state.LastPatternTime is DateTimeOffset last && last == d.Time)
		return;

		var side = DetectPatternType(x, a, b, c, d);
		if (side == null)
		return;

		var quality = AssessPatternQuality(x, a, b, c, d, side.Value);
		if (quality < MinPatternQuality)
		{
		LogInfo($"Pattern discarded: quality {quality:F3} below threshold {MinPatternQuality:F3}.");
		return;
		}

		if (RevalidatePattern && !RevalidateBeforeTrading(candle.ClosePrice, c.Price, a.Price, x.Price, side.Value))
		{
		LogInfo("Pattern invalidated by price action.");
		return;
		}

		_state.LastPatternTime = d.Time;

		if (!AllowTrading)
		{
		LogInfo("Trading disabled. Pattern ignored.");
		return;
		}

		if (_state.Side != null && _state.RemainingVolume > 0m)
		{
		LogInfo("Active position detected. New signal skipped.");
		return;
		}

		ExecutePattern(candle, side.Value, a, c);
	}

	private Sides? DetectPatternType(Pivot x, Pivot a, Pivot b, Pivot c, Pivot d)
	{
		var diffBear = x.Price - a.Price;
		if (x.IsHigh && !a.IsHigh && b.IsHigh && !c.IsHigh && d.IsHigh && diffBear > 0m)
		{
		var idealB = a.Price + 0.786m * diffBear;
		if (Math.Abs(b.Price - idealB) <= Tolerance * diffBear)
		{
		var bc = b.Price - c.Price;
		if (bc >= 0.382m * diffBear && bc <= 0.886m * diffBear)
		{
		var cd = d.Price - c.Price;
		if (cd >= 1.27m * diffBear && cd <= 1.618m * diffBear && d.Price > x.Price)
		return Sides.Sell;
		}
		}
		}

		var diffBull = a.Price - x.Price;
		if (!x.IsHigh && a.IsHigh && !b.IsHigh && c.IsHigh && !d.IsHigh && diffBull > 0m)
		{
		var idealB = a.Price - 0.786m * diffBull;
		if (Math.Abs(b.Price - idealB) <= Tolerance * diffBull)
		{
		var bc = c.Price - b.Price;
		if (bc >= 0.382m * diffBull && bc <= 0.886m * diffBull)
		{
		var cd = c.Price - d.Price;
		if (cd >= 1.27m * diffBull && cd <= 1.618m * diffBull && d.Price < x.Price)
		return Sides.Buy;
		}
		}
		}

		return null;
	}

	private decimal AssessPatternQuality(Pivot x, Pivot a, Pivot b, Pivot c, Pivot d, Sides side)
	{
		var diff = side == Sides.Buy ? a.Price - x.Price : x.Price - a.Price;
		if (diff == 0m)
		return 0m;

		var score = 1m;
		var idealB = side == Sides.Buy ? a.Price - 0.786m * diff : a.Price + 0.786m * diff;
		var bDeviation = Math.Abs(b.Price - idealB) / diff;
		score -= bDeviation * 0.2m;

		var idealC = side == Sides.Buy
		? b.Price + 0.618m * (a.Price - b.Price)
		: b.Price - 0.618m * (b.Price - a.Price);
		var cDeviation = Math.Abs(c.Price - idealC) / diff;
		score -= cDeviation * 0.2m;

		var idealD = side == Sides.Buy
		? c.Price - 1.414m * (c.Price - b.Price)
		: c.Price + 1.414m * (b.Price - c.Price);
		var dDeviation = Math.Abs(d.Price - idealD) / diff;
		score -= dDeviation * 0.2m;

		var abDuration = (b.Time - a.Time).TotalSeconds;
		var cdDuration = (d.Time - c.Time).TotalSeconds;
		if (abDuration > 0 && cdDuration > 0)
		score -= (decimal)Math.Abs(1.0 - abDuration / cdDuration) * 0.1m;

		var xaDuration = (a.Time - x.Time).TotalSeconds;
		var bcDuration = (c.Time - b.Time).TotalSeconds;
		if (xaDuration > 0 && bcDuration > 0)
		score -= (decimal)Math.Abs(1.0 - xaDuration / bcDuration) * 0.1m;

		return Math.Max(0m, Math.Min(1m, score));
	}

	private bool RevalidateBeforeTrading(decimal currentPrice, decimal dPrice, decimal aPrice, decimal xPrice, Sides side)
	{
		var direction = side == Sides.Buy ? 1m : -1m;
		var diff = side == Sides.Buy ? aPrice - xPrice : xPrice - aPrice;
		if (diff <= 0m)
		return false;

		var priceMovement = (currentPrice - dPrice) * direction;
		if (priceMovement < 0m)
		return false;

		return Math.Abs(priceMovement) <= 0.3m * diff;
	}

	private void ExecutePattern(ICandleMessage candle, Sides side, Pivot a, Pivot c)
	{
		var entryPrice = candle.ClosePrice;
		var tp3 = c.Price;
		var diff = side == Sides.Buy ? tp3 - entryPrice : entryPrice - tp3;
		if (diff <= 0m)
		{
		LogInfo("Pattern skipped: invalid take-profit distance.");
		return;
		}

		var tp1 = side == Sides.Buy ? entryPrice + diff / 3m : entryPrice - diff / 3m;
		var tp2 = side == Sides.Buy ? entryPrice + diff * 2m / 3m : entryPrice - diff * 2m / 3m;
		var stop = side == Sides.Buy ? entryPrice - (tp2 - entryPrice) * 3m : entryPrice + (entryPrice - tp2) * 3m;

		var step = Security.PriceStep ?? 0.0001m;
		var minDistance = step;
		if (Math.Abs(entryPrice - stop) < minDistance || Math.Abs(tp1 - entryPrice) < minDistance || Math.Abs(tp2 - entryPrice) < minDistance || Math.Abs(tp3 - entryPrice) < minDistance)
		{
		LogInfo("Pattern skipped: protective distances below minimal step.");
		return;
		}

		var volume = CalculatePositionVolume(entryPrice, stop);
		if (volume <= 0m)
		{
		LogInfo("Pattern skipped: volume calculation returned zero.");
		return;
		}

		SplitVolumes(volume, out var lot1, out var lot2, out var lot3);
		var total = lot1 + lot2 + lot3;
		if (total <= 0m)
		{
		LogInfo("Pattern skipped: no tradable volume.");
		return;
		}

		var order = side == Sides.Buy ? BuyMarket(total) : SellMarket(total);
		if (order == null)
		{
		LogInfo("Failed to place entry order.");
		return;
		}

		_state.Side = side;
		_state.EntryPrice = entryPrice;
		_state.StopPrice = stop;
		_state.Lot1 = lot1;
		_state.Lot2 = lot2;
		_state.Lot3 = lot3;
		_state.RemainingVolume = total;
		_state.Tp1Filled = lot1 <= 0m;
		_state.Tp2Filled = lot2 <= 0m;
		_state.Tp3Filled = lot3 <= 0m;
		_state.Tp1Price = tp1;
		_state.Tp2Price = tp2;
		_state.Tp3Price = tp3;
		_state.BreakEvenApplied = false;
		_state.TrailingActivated = false;

		LogInfo($"{side} entry at {entryPrice:F5}, stop {stop:F5}, TP1 {tp1:F5}, TP2 {tp2:F5}, TP3 {tp3:F5}, volume {total:F2}.");
	}

	private decimal CalculatePositionVolume(decimal entryPrice, decimal stopPrice)
	{
		var minVolume = Security.MinVolume ?? 0.01m;
		var maxVolume = Security.MaxVolume ?? 0m;
		var step = Security.VolumeStep ?? 0.01m;

		decimal volume;
		if (UseFixedVolume)
		{
		volume = FixedVolume;
		}
		else
		{
		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		var riskAmount = portfolioValue * RiskPercent / 100m;
		var stepPrice = Security.StepPrice ?? 1m;
		var priceStep = Security.PriceStep ?? 1m;
		var distance = Math.Abs(entryPrice - stopPrice);
		if (distance <= 0m || priceStep <= 0m || stepPrice <= 0m)
		return 0m;

		var riskPerUnit = distance / priceStep * stepPrice;
		if (riskPerUnit <= 0m)
		return 0m;

		volume = riskAmount / riskPerUnit;
		}

		if (step > 0m)
		volume = Math.Floor(volume / step) * step;

		if (maxVolume > 0m)
		volume = Math.Min(volume, maxVolume);

		return Math.Max(volume, minVolume);
	}

	private void SplitVolumes(decimal total, out decimal lot1, out decimal lot2, out decimal lot3)
	{
		var percents = Tp1Percent + Tp2Percent + Tp3Percent;
		if (percents <= 0m)
		{
		lot1 = total / 3m;
		lot2 = total / 3m;
		lot3 = total - lot1 - lot2;
		}
		else
		{
		lot1 = total * Tp1Percent / percents;
		lot2 = total * Tp2Percent / percents;
		lot3 = total - lot1 - lot2;
		}

		if (AdjustLotsForTakeProfits)
		{
		var sum = lot1 + lot2 + lot3;
		if (sum != 0m)
		{
		var scale = total / sum;
		lot1 *= scale;
		lot2 *= scale;
		lot3 = total - lot1 - lot2;
		}
		}

		var step = Security.VolumeStep ?? 0.01m;
		if (step > 0m)
		{
		lot1 = Math.Round(lot1 / step) * step;
		lot2 = Math.Round(lot2 / step) * step;
		lot3 = Math.Round(lot3 / step) * step;
		}

		var minVolume = Security.MinVolume ?? 0.01m;
		if (lot1 > 0m && lot1 < minVolume)
		lot1 = minVolume;
		if (lot2 > 0m && lot2 < minVolume)
		lot2 = minVolume;
		if (lot3 > 0m && lot3 < minVolume)
		lot3 = minVolume;

		var sumAfter = lot1 + lot2 + lot3;
		if (sumAfter > total && sumAfter > 0m)
		{
		var scale = total / sumAfter;
		lot1 *= scale;
		lot2 *= scale;
		lot3 = total - lot1 - lot2;
		}

		lot1 = Math.Max(0m, lot1);
		lot2 = Math.Max(0m, lot2);
		lot3 = Math.Max(0m, lot3);
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		if (_state.Side == null || _state.RemainingVolume <= 0m || _state.EntryPrice is not decimal entry)
		return;

		var side = _state.Side.Value;
		var direction = side == Sides.Buy ? 1m : -1m;
		var step = Security.PriceStep ?? 1m;

		if (_state.StopPrice is decimal stop)
		{
		var hit = side == Sides.Buy ? candle.LowPrice <= stop : candle.HighPrice >= stop;
		if (hit)
		{
		ExitAll();
		LogInfo($"Stop-loss hit at {stop:F5}.");
		return;
		}
		}

		if (!_state.Tp1Filled && _state.Lot1 > 0m)
		{
		var reached = side == Sides.Buy ? candle.HighPrice >= _state.Tp1Price : candle.LowPrice <= _state.Tp1Price;
		if (reached)
		ExitPartial(_state.Lot1, _state.Tp1Price, 1);
		}

		if (!_state.Tp2Filled && _state.Lot2 > 0m)
		{
		var reached = side == Sides.Buy ? candle.HighPrice >= _state.Tp2Price : candle.LowPrice <= _state.Tp2Price;
		if (reached)
		ExitPartial(_state.Lot2, _state.Tp2Price, 2);
		}

		if (!_state.Tp3Filled && _state.Lot3 > 0m)
		{
		var reached = side == Sides.Buy ? candle.HighPrice >= _state.Tp3Price : candle.LowPrice <= _state.Tp3Price;
		if (reached)
		ExitPartial(_state.Lot3, _state.Tp3Price, 3);
		}

		if (_state.RemainingVolume <= 0m)
		{
		_state.ResetPosition();
		return;
		}

		ApplyBreakEven(candle, entry, direction, step);
		ApplyTrailing(candle, entry, direction, step);
	}

	private void ExitPartial(decimal volume, decimal price, int tpIndex)
	{
		if (volume <= 0m)
		return;

		Order order = _state.Side == Sides.Buy ? SellMarket(volume) : BuyMarket(volume);
		if (order == null)
		{
		LogInfo($"Failed to exit partial position for TP{tpIndex}.");
		return;
		}

		_state.RemainingVolume = Math.Max(0m, _state.RemainingVolume - volume);

		switch (tpIndex)
		{
		case 1:
		_state.Tp1Filled = true;
		break;
		case 2:
		_state.Tp2Filled = true;
		break;
		case 3:
		_state.Tp3Filled = true;
		break;
		}

		LogInfo($"TP{tpIndex} executed at {price:F5}.");
	}

	private void ExitAll()
	{
		if (_state.Side == null || _state.RemainingVolume <= 0m)
		{
		_state.ResetPosition();
		return;
		}

		var volume = _state.RemainingVolume;
		Order order = _state.Side == Sides.Buy ? SellMarket(volume) : BuyMarket(volume);
		if (order == null)
		{
		LogInfo("Failed to close position at stop.");
		return;
		}

		_state.ResetPosition();
	}

	private void ApplyBreakEven(ICandleMessage candle, decimal entry, decimal direction, decimal step)
	{
		if (!UseBreakEven || _state.BreakEvenApplied || _state.StopPrice is not decimal currentStop)
		return;

		if (!IsGatePassed(BreakEvenAfterTp, _state.Tp1Filled, _state.Tp2Filled))
		return;

		if (BreakEvenTrigger <= 0m)
		return;

		var movement = (candle.ClosePrice - entry) * direction;
		if (movement < BreakEvenTrigger * step)
		return;

		var newStop = entry + direction * BreakEvenProfit * step;
		if (direction > 0m)
		{
		if (newStop <= currentStop)
		return;
		}
		else if (newStop >= currentStop)
		{
		return;
		}

		_state.StopPrice = newStop;
		_state.BreakEvenApplied = true;
		LogInfo($"Break-even adjusted to {newStop:F5}.");
	}

	private void ApplyTrailing(ICandleMessage candle, decimal entry, decimal direction, decimal step)
	{
		if (!UseTrailingStop || _state.StopPrice is not decimal currentStop)
		return;

		if (!IsGatePassed(TrailAfterTp, _state.Tp1Filled, _state.Tp2Filled))
		return;

		if (TrailStart <= 0m || TrailStep <= 0m)
		return;

		var movement = (candle.ClosePrice - entry) * direction;
		if (movement < TrailStart * step)
		return;

		var newStop = candle.ClosePrice - direction * TrailStep * step;
		if (direction > 0m)
		{
		if (newStop <= currentStop)
		return;
		}
		else if (newStop >= currentStop)
		{
		return;
		}

		_state.StopPrice = newStop;
		_state.TrailingActivated = true;
		LogInfo($"Trailing stop updated to {newStop:F5}.");
	}

	private static bool IsGatePassed(int gate, bool tp1Filled, bool tp2Filled)
	{
		var normalized = gate < 1 ? 1 : gate > 2 ? 2 : gate;
		return normalized switch
		{
		1 => tp1Filled,
		2 => tp2Filled,
		_ => false,
		};
	}
}

