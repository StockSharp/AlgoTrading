using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dealers Trade MACD strategy converted from MQL5 implementation.
/// </summary>
public class DealersTradeMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _intervalPoints;
	private readonly StrategyParam<decimal> _secureProfit;
	private readonly StrategyParam<bool> _accountProtection;
	private readonly StrategyParam<int> _positionsForProtection;
	private readonly StrategyParam<bool> _reverseCondition;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;

	private MovingAverageConvergenceDivergence _macd = null!;
	private decimal? _previousMacd;
	private decimal _lastEntryPrice;
	private readonly List<PositionState> _longPositions = new();
	private readonly List<PositionState> _shortPositions = new();

	/// <summary>
	/// Initializes a new instance of <see cref="DealersTradeMacdStrategy"/>.
	/// </summary>
	public DealersTradeMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetDisplay("Fixed Volume", "Lot size used when above zero", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetDisplay("Risk %", "Risk percent when fixed volume is zero", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 90m)
			.SetDisplay("Stop Loss pts", "Stop loss distance in price steps", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 30m)
			.SetDisplay("Take Profit pts", "Take profit distance in price steps", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 15m)
			.SetDisplay("Trailing Stop pts", "Trailing stop distance in price steps", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m)
			.SetDisplay("Trailing Step pts", "Additional distance before trailing updates", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 5)
			.SetDisplay("Max Positions", "Maximum concurrent entries", "Money Management");

		_intervalPoints = Param(nameof(IntervalPoints), 15m)
			.SetDisplay("Interval pts", "Minimum distance between new entries", "Money Management");

		_secureProfit = Param(nameof(SecureProfit), 50m)
			.SetDisplay("Secure Profit", "Profit threshold that triggers protection", "Money Management");

		_accountProtection = Param(nameof(AccountProtection), true)
			.SetDisplay("Account Protection", "Close best trade after reaching secure profit", "Money Management");

		_positionsForProtection = Param(nameof(PositionsForProtection), 3)
			.SetDisplay("Protect From", "Minimum positions before triggering protection", "Money Management");

		_reverseCondition = Param(nameof(ReverseCondition), false)
			.SetDisplay("Reverse Signal", "Invert MACD slope direction", "Trading");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 14)
			.SetDisplay("MACD Fast", "Fast EMA period", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "Slow EMA period", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 1)
			.SetDisplay("MACD Signal", "Signal EMA period", "Indicators");

		_maxVolume = Param(nameof(MaxVolume), 5m)
			.SetDisplay("Max Volume", "Absolute cap for trade volume", "Risk")
			.SetGreaterThanZero();

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.6m)
			.SetDisplay("Volume Multiplier", "Multiplier for additional positions", "Money Management")
			.SetGreaterThanZero();
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fixed lot size. When zero risk based sizing is used.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Percent of equity risked when sizing dynamically.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Extra distance required before the trailing stop moves.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Maximum number of open entries.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Minimum price distance between sequential entries.
	/// </summary>
	public decimal IntervalPoints
	{
		get => _intervalPoints.Value;
		set => _intervalPoints.Value = value;
	}

	/// <summary>
	/// Profit target for account protection logic.
	/// </summary>
	public decimal SecureProfit
	{
		get => _secureProfit.Value;
		set => _secureProfit.Value = value;
	}

	/// <summary>
	/// Enables profit locking when enough trades are open.
	/// </summary>
	public bool AccountProtection
	{
		get => _accountProtection.Value;
		set => _accountProtection.Value = value;
	}

	/// <summary>
	/// Minimum number of positions before account protection activates.
	/// </summary>
	public int PositionsForProtection
	{
		get => _positionsForProtection.Value;
		set => _positionsForProtection.Value = value;
	}

	/// <summary>
	/// Inverts the MACD slope direction.
	/// </summary>
	public bool ReverseCondition
	{
		get => _reverseCondition.Value;
		set => _reverseCondition.Value = value;
	}

	/// <summary>
	/// MACD fast EMA period.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal EMA period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Maximum allowed total volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the base volume for each additional entry.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
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
		_macd?.Reset();
		_previousMacd = null;
		_lastEntryPrice = 0m;
		_longPositions.Clear();
		_shortPositions.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = MacdFastPeriod,
			Slow = MacdSlowPeriod,
			Signal = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal _, decimal __)
	{
		if (candle.State != CandleStates.Finished)
			return;

		HandleTrailingAndExits(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousMacd = macdValue;
			return;
		}

		var openPositions = _longPositions.Count + _shortPositions.Count;
		var continueOpening = openPositions < MaxPositions;

		var direction = 0;

		if (_previousMacd is null)
		{
			_previousMacd = macdValue;
			return;
		}

		if (macdValue > _previousMacd)
			direction = 1;
		else if (macdValue < _previousMacd)
			direction = -1;

		if (ReverseCondition)
			direction = -direction;

		if (AccountProtection && openPositions > PositionsForProtection)
		{
			var totalProfit = CalculateTotalProfit(candle.ClosePrice);
			if (totalProfit >= SecureProfit)
			{
				CloseMostProfitablePosition(candle.ClosePrice);
				_previousMacd = macdValue;
				return;
			}
		}

		if (continueOpening && direction > 0 && _shortPositions.Count == 0)
			TryOpenLong(candle);
		else if (continueOpening && direction < 0 && _longPositions.Count == 0)
			TryOpenShort(candle);

		_previousMacd = macdValue;
	}

	private void HandleTrailingAndExits(ICandleMessage candle)
	{
		var step = GetPriceStep();
		var trailingDistance = TrailingStopPoints * step;
		var trailingActivation = (TrailingStopPoints + TrailingStepPoints) * step;

		for (var i = _longPositions.Count - 1; i >= 0; i--)
		{
			var state = _longPositions[i];

			if (state.TakeProfitPrice > 0 && candle.HighPrice >= state.TakeProfitPrice)
			{
				SellMarket(state.Volume);
				_longPositions.RemoveAt(i);
				_lastEntryPrice = 0m;
				continue;
			}

			if (state.StopPrice > 0 && candle.LowPrice <= state.StopPrice)
			{
				SellMarket(state.Volume);
				_longPositions.RemoveAt(i);
				_lastEntryPrice = 0m;
				continue;
			}

			if (TrailingStopPoints > 0 && candle.ClosePrice - state.EntryPrice > trailingActivation)
			{
				var candidateStop = candle.ClosePrice - trailingDistance;
				if (state.StopPrice == 0m || state.StopPrice < candle.ClosePrice - trailingActivation)
					state.StopPrice = candidateStop;
			}
		}

		for (var i = _shortPositions.Count - 1; i >= 0; i--)
		{
			var state = _shortPositions[i];

			if (state.TakeProfitPrice > 0 && candle.LowPrice <= state.TakeProfitPrice)
			{
				BuyMarket(state.Volume);
				_shortPositions.RemoveAt(i);
				_lastEntryPrice = 0m;
				continue;
			}

			if (state.StopPrice > 0 && candle.HighPrice >= state.StopPrice)
			{
				BuyMarket(state.Volume);
				_shortPositions.RemoveAt(i);
				_lastEntryPrice = 0m;
				continue;
			}

			if (TrailingStopPoints > 0 && state.EntryPrice - candle.ClosePrice > trailingActivation)
			{
				var candidateStop = candle.ClosePrice + trailingDistance;
				if (state.StopPrice == 0m || state.StopPrice > candle.ClosePrice + trailingActivation)
					state.StopPrice = candidateStop;
			}
		}
	}

	private void TryOpenLong(ICandleMessage candle)
	{
		var step = GetPriceStep();
		var interval = IntervalPoints * step;

		if (_lastEntryPrice != 0m && Math.Abs(_lastEntryPrice - candle.ClosePrice) < interval)
			return;

		var baseVolume = FixedVolume > 0 ? FixedVolume : CalculateRiskVolume(step);
		if (baseVolume <= 0)
			return;

		var openPositions = _longPositions.Count + _shortPositions.Count;
		var lotCoefficient = openPositions == 0 ? 1m : Pow(VolumeMultiplier, openPositions + 1);
		var volume = NormalizeVolume(baseVolume * lotCoefficient);
		if (volume <= 0 || volume > MaxVolume)
			return;

		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		BuyMarket(volume);

		_longPositions.Add(new PositionState
		{
			EntryPrice = candle.ClosePrice,
			Volume = volume,
			StopPrice = stopDistance > 0 ? candle.ClosePrice - stopDistance : 0m,
			TakeProfitPrice = takeDistance > 0 ? candle.ClosePrice + takeDistance : 0m
		});

		_lastEntryPrice = candle.ClosePrice;
	}

	private void TryOpenShort(ICandleMessage candle)
	{
		var step = GetPriceStep();
		var interval = IntervalPoints * step;

		if (_lastEntryPrice != 0m && Math.Abs(_lastEntryPrice - candle.ClosePrice) < interval)
			return;

		var baseVolume = FixedVolume > 0 ? FixedVolume : CalculateRiskVolume(step);
		if (baseVolume <= 0)
			return;

		var openPositions = _longPositions.Count + _shortPositions.Count;
		var lotCoefficient = openPositions == 0 ? 1m : Pow(VolumeMultiplier, openPositions + 1);
		var volume = NormalizeVolume(baseVolume * lotCoefficient);
		if (volume <= 0 || volume > MaxVolume)
			return;

		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		SellMarket(volume);

		_shortPositions.Add(new PositionState
		{
			EntryPrice = candle.ClosePrice,
			Volume = volume,
			StopPrice = stopDistance > 0 ? candle.ClosePrice + stopDistance : 0m,
			TakeProfitPrice = takeDistance > 0 ? candle.ClosePrice - takeDistance : 0m
		});

		_lastEntryPrice = candle.ClosePrice;
	}

	private decimal CalculateRiskVolume(decimal priceStep)
	{
		if (StopLossPoints <= 0)
			return 0m;

		var stopDistance = StopLossPoints * priceStep;
		if (stopDistance <= 0)
			return 0m;

		if (Portfolio is null)
			return 0m;

		var equity = Portfolio.CurrentValue;
		if (equity <= 0)
			return 0m;

		var riskAmount = equity * (RiskPercent / 100m);
		return riskAmount / stopDistance;
	}

	private decimal CalculateTotalProfit(decimal currentPrice)
	{
		decimal profit = 0m;

		foreach (var pos in _longPositions)
			profit += (currentPrice - pos.EntryPrice) * pos.Volume;

		foreach (var pos in _shortPositions)
			profit += (pos.EntryPrice - currentPrice) * pos.Volume;

		return profit;
	}

	private void CloseMostProfitablePosition(decimal currentPrice)
	{
		PositionState? best = null;
		var bestIsLong = false;
		decimal bestProfit = 0m;

		foreach (var pos in _longPositions)
		{
			var profit = (currentPrice - pos.EntryPrice) * pos.Volume;
			if (profit > bestProfit)
			{
				bestProfit = profit;
				best = pos;
				bestIsLong = true;
			}
		}

		foreach (var pos in _shortPositions)
		{
			var profit = (pos.EntryPrice - currentPrice) * pos.Volume;
			if (profit > bestProfit)
			{
				bestProfit = profit;
				best = pos;
				bestIsLong = false;
			}
		}

		if (best is null || bestProfit <= 0m)
			return;

		if (bestIsLong)
		{
			SellMarket(best.Volume);
			_longPositions.Remove(best);
		}
		else
		{
			BuyMarket(best.Volume);
			_shortPositions.Remove(best);
		}

		_lastEntryPrice = 0m;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		return volume;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step > 0)
			return step;

		var decimals = Security?.Decimals ?? 0;
		if (decimals > 0)
			return (decimal)Math.Pow(10, -decimals);

		return 0.0001m;
	}

	private static decimal Pow(decimal value, int power)
	{
		if (power <= 0)
			return 1m;

		return (decimal)Math.Pow((double)value, power);
	}

	private sealed class PositionState
	{
		public decimal EntryPrice { get; set; }
		public decimal Volume { get; set; }
		public decimal StopPrice { get; set; }
		public decimal TakeProfitPrice { get; set; }
	}
}
