using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the Money Fixed Risk expert advisor using StockSharp's high level API.
/// The strategy periodically evaluates trade size based on a fixed risk percentage
/// and opens a long position with symmetric stop-loss and take-profit levels.
/// </summary>
public class MoneyFixedRiskStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _ticksInterval;

	private decimal _pipSize;
	private int _tickCounter;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Percentage of equity risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Number of ticks between position size evaluations.
	/// </summary>
	public int TicksInterval
	{
		get => _ticksInterval.Value;
		set => _ticksInterval.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MoneyFixedRiskStrategy"/>.
	/// </summary>
	public MoneyFixedRiskStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 25)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
		.SetCanOptimize(true);

		_riskPercent = Param(nameof(RiskPercent), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Risk %", "Percent of equity risked per trade", "Risk")
		.SetCanOptimize(true);

		_ticksInterval = Param(nameof(TicksInterval), 980)
		.SetGreaterThanZero()
		.SetDisplay("Ticks Interval", "Ticks between position size checks", "General")
		.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_tickCounter = 0;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceStep = Security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		throw new InvalidOperationException("Security price step is not specified.");

		var decimals = Security.Decimals;
		var pipMultiplier = decimals is 3 or 5 ? 10m : 1m;

		_pipSize = priceStep * pipMultiplier;

		var trades = SubscribeTrades();
		trades
		.Bind(ProcessTrade)
		.Start();

		StartProtection();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;
		if (price is null || price.Value <= 0m)
		return;

		// Manage existing long position and exit on stop-loss or take-profit.
		if (Position > 0 && _stopPrice > 0m)
		{
			if (price.Value <= _stopPrice || price.Value >= _takeProfitPrice)
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}

		_tickCounter++;

		if (_tickCounter < TicksInterval)
		return;

		_tickCounter = 0;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var stopDistance = GetStopDistance();
		if (stopDistance <= 0m)
		return;

		var volume = CalculateVolume(stopDistance);
		if (volume <= 0m)
		return;

		// Close short exposure if any and open the long trade sized by risk.
		BuyMarket(volume + Math.Max(0m, -Position));

		_stopPrice = price.Value - stopDistance;
		_takeProfitPrice = price.Value + stopDistance;
	}

	private decimal GetStopDistance()
	{
		return StopLossPips * _pipSize;
	}

	private decimal CalculateVolume(decimal stopDistance)
	{
		if (Portfolio == null)
		return 0m;

		var equity = Portfolio.CurrentValue;
		if (equity <= 0m)
		equity = Portfolio.CurrentBalance;
		if (equity <= 0m)
		equity = Portfolio.BeginValue;

		if (equity <= 0m)
		return 0m;

		var priceStep = Security.PriceStep ?? 0m;
		var stepPrice = Security.StepPrice ?? priceStep;

		if (priceStep <= 0m || stepPrice <= 0m)
		return 0m;

		var steps = stopDistance / priceStep;
		if (steps <= 0m)
		return 0m;

		var riskAmount = equity * RiskPercent / 100m;
		var riskPerContract = steps * stepPrice;
		if (riskPerContract <= 0m)
		return 0m;

		var rawVolume = riskAmount / riskPerContract;
		if (rawVolume <= 0m)
		return 0m;

		return NormalizeVolume(rawVolume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var volumeStep = Security.VolumeStep ?? 1m;
		var minVolume = Security.MinVolume ?? volumeStep;
		var maxVolume = Security.MaxVolume;

		if (volumeStep <= 0m)
		volumeStep = 1m;

		if (volume < minVolume)
		return 0m;

		var steps = Math.Floor(volume / volumeStep);
		var normalized = steps * volumeStep;

		if (normalized < minVolume)
		normalized = minVolume;

		if (maxVolume != null && normalized > maxVolume.Value)
		normalized = maxVolume.Value;

		return normalized;
	}
}
