using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XAUUSD strategy with fixed profit and loss targets.
/// </summary>
public class XauusdSimple20Profit100LossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _lossLimit;
	private readonly StrategyParam<TimeSpan> _tradeCooldown;
	private readonly StrategyParam<TimeSpan> _entryCooldown;
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset? _lastLossTime;
	private DateTimeOffset? _lastProfitTime;
	private decimal _entryPrice;

	public XauusdSimple20Profit100LossStrategy()
	{
		_profitTarget = Param<decimal>(nameof(ProfitTarget), 20m)
			.SetDisplay("Profit Target")
			.SetCanOptimize(true);
		_lossLimit = Param<decimal>(nameof(LossLimit), 100m)
			.SetDisplay("Loss Limit")
			.SetCanOptimize(true);
		_tradeCooldown = Param(nameof(TradeCooldown), TimeSpan.FromHours(12))
			.SetDisplay("Trade Cooldown");
		_entryCooldown = Param(nameof(EntryCooldown), TimeSpan.FromMinutes(15))
			.SetDisplay("Entry Cooldown");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type");
	}

	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }
	public decimal LossLimit { get => _lossLimit.Value; set => _lossLimit.Value = value; }
	public TimeSpan TradeCooldown { get => _tradeCooldown.Value; set => _tradeCooldown.Value = value; }
	public TimeSpan EntryCooldown { get => _entryCooldown.Value; set => _entryCooldown.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var timeNow = candle.CloseTime;

		var canEnter = Position == 0
			&& (!_lastLossTime.HasValue || timeNow - _lastLossTime >= TradeCooldown)
			&& (!_lastProfitTime.HasValue || timeNow - _lastProfitTime >= EntryCooldown);

		if (canEnter)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			return;
		}

		if (Position <= 0)
			return;

		var pnl = (candle.ClosePrice - _entryPrice) * Position;

		if (pnl >= ProfitTarget)
		{
			SellMarket(Position);
			_lastProfitTime = timeNow;
		}
		else if (pnl <= -LossLimit)
		{
			SellMarket(Position);
			_lastLossTime = timeNow;
		}
	}
}
