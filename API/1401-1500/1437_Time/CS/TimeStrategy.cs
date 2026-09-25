using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Timed intrabar high-above-open strategy.
/// </summary>
public class TimeStrategy : Strategy
{
	private readonly StrategyParam<int> _ticksFromOpen;
	private readonly StrategyParam<int> _secondsCondition;
	private readonly StrategyParam<bool> _resetOnNewBar;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _barStart;
	private decimal _barOpen;
	private decimal _barHigh;
	private DateTime? _conditionSince;

	public int TicksFromOpen { get => _ticksFromOpen.Value; set => _ticksFromOpen.Value = value; }
	public int SecondsCondition { get => _secondsCondition.Value; set => _secondsCondition.Value = value; }
	public bool ResetOnNewBar { get => _resetOnNewBar.Value; set => _resetOnNewBar.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TimeStrategy()
	{
		_ticksFromOpen = Param(nameof(TicksFromOpen), 0).SetNotNegative();
		_secondsCondition = Param(nameof(SecondsCondition), 20).SetNotNegative();
		_resetOnNewBar = Param(nameof(ResetOnNewBar), true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_barStart = null;
		_barOpen = 0m;
		_barHigh = 0m;
		_conditionSince = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeLevel1().Bind(ProcessLevel1).Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		var price =
			message.TryGetDecimal(Level1Fields.LastTradePrice) ??
			message.TryGetDecimal(Level1Fields.BestAskPrice) ??
			message.TryGetDecimal(Level1Fields.BestBidPrice);

		if (price is not decimal currentPrice || currentPrice <= 0m)
			return;

		var timeFrame = CandleType.Arg is TimeSpan tf && tf > TimeSpan.Zero
			? tf
			: TimeSpan.FromMinutes(1);

		var currentBarStart = Align(message.ServerTime, timeFrame);
		if (_barStart != currentBarStart)
		{
			_barStart = currentBarStart;
			_barOpen = currentPrice;
			_barHigh = currentPrice;

			if (ResetOnNewBar)
				_conditionSince = null;
		}
		else
			_barHigh = Math.Max(_barHigh, currentPrice);

		var point = Security?.PriceStep ?? 0m;
		if (point <= 0m)
			point = 0.0001m;

		var condition = IsPriceConditionMet(_barOpen, _barHigh, point, TicksFromOpen);

		if (!condition)
		{
			_conditionSince = null;

			if (Position > 0m)
				SellMarket(Math.Abs(Position));

			return;
		}

		_conditionSince ??= message.ServerTime;

		if (Position == 0m &&
			message.ServerTime - _conditionSince.Value >= TimeSpan.FromSeconds(SecondsCondition))
			BuyMarket();
	}

	internal static bool IsPriceConditionMet(
		decimal open,
		decimal high,
		decimal priceStep,
		int ticksFromOpen)
		=> high - open >= ticksFromOpen * priceStep;

	private static DateTime Align(DateTime time, TimeSpan frame)
	{
		var ticks = time.TimeOfDay.Ticks / frame.Ticks * frame.Ticks;
		return time.Date + TimeSpan.FromTicks(ticks);
	}
}
