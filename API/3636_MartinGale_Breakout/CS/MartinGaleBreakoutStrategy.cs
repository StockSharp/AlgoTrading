using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy with martingale-style recovery converted from the MT4 expert advisor "MartinGaleBreakout".
/// </summary>
public class MartinGaleBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _requiredHistory;

	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _balancePercentageAvailable;
	private readonly StrategyParam<decimal> _takeProfitBalancePercent;
	private readonly StrategyParam<decimal> _stopLossBalancePercent;
	private readonly StrategyParam<decimal> _startRecoveryFactor;
	private readonly StrategyParam<decimal> _takeProfitPointsMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _history = new();

	private decimal _takeProfit;
	private decimal _stopLoss;
	private bool _recovering;
	private decimal _pointSize;
	private decimal _stepPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="MartinGaleBreakoutStrategy"/> class.
	/// </summary>
	public MartinGaleBreakoutStrategy()
	{
		_requiredHistory = Param(nameof(RequiredHistory), 11)
			.SetDisplay("Required History", "Number of finished candles kept for breakout evaluation", "General")
			.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetDisplay("Take Profit Points", "Base take-profit distance in price points", "Risk")
			.SetGreaterThanZero();

		_balancePercentageAvailable = Param(nameof(BalancePercentageAvailable), 50m)
			.SetDisplay("Balance Allocation (%)", "Maximum share of balance available for new positions", "Risk")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_takeProfitBalancePercent = Param(nameof(TakeProfitBalancePercent), 0.1m)
			.SetDisplay("TP Balance (%)", "Target profit as a percentage of balance", "Risk")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_stopLossBalancePercent = Param(nameof(StopLossBalancePercent), 10m)
			.SetDisplay("SL Balance (%)", "Maximum drawdown per recovery cycle", "Risk")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_startRecoveryFactor = Param(nameof(StartRecoveryFactor), 0.2m)
			.SetDisplay("Recovery Factor", "Portion of the stop-loss used to start recovery", "Risk")
			.SetRange(0m, 1m)
			.SetCanOptimize(true);

		_takeProfitPointsMultiplier = Param(nameof(TakeProfitPointsMultiplier), 1m)
			.SetDisplay("Recovery TP Multiplier", "Increase in take-profit distance while recovering", "Risk")
			.SetRange(0m, 10m)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Working candle series", "General");
	}

	/// <summary>
	/// Gets or sets the base take-profit distance in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the maximum share of balance available for opening new positions.
	/// </summary>
	public decimal BalancePercentageAvailable
	{
		get => _balancePercentageAvailable.Value;
		set => _balancePercentageAvailable.Value = value;
	}

	/// <summary>
	/// Gets or sets the desired profit target as a percentage of balance.
	/// </summary>
	public decimal TakeProfitBalancePercent
	{
		get => _takeProfitBalancePercent.Value;
		set => _takeProfitBalancePercent.Value = value;
	}

	/// <summary>
	/// Gets or sets the stop-loss limit as a percentage of balance.
	/// </summary>
	public decimal StopLossBalancePercent
	{
		get => _stopLossBalancePercent.Value;
		set => _stopLossBalancePercent.Value = value;
	}

	/// <summary>
	/// Gets or sets the coefficient that reduces the initial stop-loss during recovery.
	/// </summary>
	public decimal StartRecoveryFactor
	{
		get => _startRecoveryFactor.Value;
		set => _startRecoveryFactor.Value = value;
	}

	/// <summary>
	/// Gets or sets the multiplier applied to the take-profit distance while recovering losses.
	/// </summary>
	public decimal TakeProfitPointsMultiplier
	{
		get => _takeProfitPointsMultiplier.Value;
		set => _takeProfitPointsMultiplier.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle series used for breakout detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Gets or sets the number of finished candles stored for breakout evaluation.
	/// </summary>
	public int RequiredHistory
	{
		get => _requiredHistory.Value;
		set => _requiredHistory.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_history.Clear();

		_pointSize = Security?.PriceStep ?? 0m;
		if (_pointSize <= 0m)
			_pointSize = 1m;

		_stepPrice = Security?.StepPrice ?? 0m;
		if (_stepPrice <= 0m)
			_stepPrice = _pointSize;

		InitializeTargets();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void InitializeTargets()
	{
		var balance = GetPortfolioValue();
		_takeProfit = balance * TakeProfitBalancePercent / 100m;
		_stopLoss = balance * StopLossBalancePercent / 100m * StartRecoveryFactor;
		_recovering = false;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateHistory(candle);

		var currentProfit = GetUnrealizedPnL(candle.ClosePrice);
		var balance = GetPortfolioValue();

		if (balance <= 0m)
			return;

		if (currentProfit <= -_stopLoss || currentProfit >= _takeProfit)
		{
			var maxStopLoss = balance * StopLossBalancePercent / 100m;

			if (currentProfit <= -_stopLoss && _stopLoss < maxStopLoss)
			{
				CloseAllPositions();
				_takeProfit -= currentProfit;
				_stopLoss = maxStopLoss;
				_recovering = true;
				return;
			}

			CloseAllPositions();
			_takeProfit = balance * TakeProfitBalancePercent / 100m;
			_stopLoss = balance * StopLossBalancePercent / 100m * StartRecoveryFactor;
			_recovering = false;
			return;
		}

		if (Position != 0m)
			return;

		if (_history.Count < RequiredHistory)
			return;

		var current = _history[^1];
		var baseOffset = TakeProfitPoints * _pointSize;
		var offsetMultiplier = _recovering ? TakeProfitPointsMultiplier : 1m;
		var targetOffset = baseOffset * offsetMultiplier;

		if (IsBullBreakout())
		{
			var entryPrice = current.ClosePrice;
			var volume = CalculateVolumeForTarget(_takeProfit, entryPrice, entryPrice + targetOffset);

			if (volume <= 0m)
				return;

			var exposureLimit = balance * BalancePercentageAvailable / 100m;
			var requiredCapital = EstimateRequiredCapital(entryPrice, volume);

			if (requiredCapital > exposureLimit)
				return;

			BuyMarket(volume);
		}
		else if (IsBearBreakout())
		{
			var entryPrice = current.ClosePrice;
			var volume = CalculateVolumeForTarget(_takeProfit, entryPrice, entryPrice - targetOffset);

			if (volume <= 0m)
				return;

			var exposureLimit = balance * BalancePercentageAvailable / 100m;
			var requiredCapital = EstimateRequiredCapital(entryPrice, volume);

			if (requiredCapital > exposureLimit)
				return;

			SellMarket(volume);
		}
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_history.Add(candle);

		if (_history.Count > RequiredHistory)
			_history.RemoveAt(0);
	}

	private bool IsBullBreakout()
	{
		var candle = _history[^1];
		var body = candle.ClosePrice - candle.OpenPrice;
		var range = candle.HighPrice - candle.LowPrice;

		if (body <= 0m)
			return false;

		return IsAbnormalCandle(range) && body > 0.5m * range;
	}

	private bool IsBearBreakout()
	{
		var candle = _history[^1];
		var body = candle.OpenPrice - candle.ClosePrice;
		var range = candle.HighPrice - candle.LowPrice;

		if (body <= 0m)
			return false;

		return IsAbnormalCandle(range) && body > 0.5m * range;
	}

	private bool IsAbnormalCandle(decimal currentRange)
	{
		decimal sum = 0m;

		for (var i = 0; i < _history.Count - 1; i++)
		{
			var previous = _history[i];
			sum += previous.HighPrice - previous.LowPrice;
		}

		var count = _history.Count - 1;
		if (count <= 0)
			return false;

		var averageRange = sum / count;
		if (averageRange <= 0m)
			return false;

		return currentRange > averageRange * 3m;
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}
	}

	private decimal CalculateVolumeForTarget(decimal targetProfit, decimal startPrice, decimal endPrice)
	{
		if (targetProfit <= 0m)
			return 0m;

		var distance = Math.Abs(endPrice - startPrice);
		if (distance <= 0m)
			return 0m;

		decimal profitPerUnit;

		if (_pointSize > 0m && _stepPrice > 0m)
		{
			profitPerUnit = distance / _pointSize * _stepPrice;
		}
		else
		{
			profitPerUnit = distance;
		}

		if (profitPerUnit <= 0m)
			return 0m;

		var rawVolume = targetProfit / profitPerUnit;
		return NormalizeVolume(rawVolume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security is null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Ceiling((double)(volume / step));
			volume = (decimal)steps * step;
		}

		var min = security.VolumeMin ?? 0m;
		if (min > 0m && volume < min)
			volume = min;

		var max = security.VolumeMax ?? 0m;
		if (max > 0m && volume > max)
			volume = max;

		return volume;
	}

	private decimal EstimateRequiredCapital(decimal price, decimal volume)
	{
		if (price <= 0m || volume <= 0m)
			return 0m;

		var multiplier = Security?.Multiplier ?? 1m;
		return price * volume * multiplier;
	}

	private decimal GetUnrealizedPnL(decimal price)
	{
		if (Position == 0m)
			return 0m;

		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
			return 0m;

		var volume = Position;
		var diff = price - entryPrice;

		if (_pointSize > 0m && _stepPrice > 0m)
		{
			var steps = diff / _pointSize;
			return steps * _stepPrice * volume;
		}

		return diff * volume;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue > 0m)
			return portfolio.CurrentValue;

		if (portfolio?.BeginValue > 0m)
			return portfolio.BeginValue;

		return 0m;
	}
}
