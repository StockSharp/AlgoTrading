using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using timer to enter after condition lasts for specified seconds.
/// </summary>
public class TimeStrategy : Strategy
{
	private readonly StrategyParam<int> _ticksFromOpen;
	private readonly StrategyParam<int> _secondsCondition;
	private readonly StrategyParam<bool> _resetOnNewBar;
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset? _timeBegin;
	private bool _lastCond;

	/// <summary>
	/// Minimum ticks from open for condition.
	/// </summary>
	public int TicksFromOpen
	{
		get => _ticksFromOpen.Value;
		set => _ticksFromOpen.Value = value;
	}

	/// <summary>
	/// Seconds that condition must hold.
	/// </summary>
	public int SecondsCondition
	{
		get => _secondsCondition.Value;
		set => _secondsCondition.Value = value;
	}

	/// <summary>
	/// Reset timer on new bar.
	/// </summary>
	public bool ResetOnNewBar
	{
		get => _resetOnNewBar.Value;
		set => _resetOnNewBar.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public TimeStrategy()
	{
		_ticksFromOpen = Param(nameof(TicksFromOpen), 0)
			.SetDisplay("Ticks From Open", "Minimal ticks from open", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 10, 1);
		_secondsCondition = Param(nameof(SecondsCondition), 20)
			.SetDisplay("Seconds Condition", "Seconds condition must hold", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);
		_resetOnNewBar = Param(nameof(ResetOnNewBar), true)
			.SetDisplay("Reset On New Bar", "Reset timer on new bar", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_timeBegin = null;
		_lastCond = false;
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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var tickSize = Security.PriceStep ?? 1m;
		var cond = candle.HighPrice - candle.OpenPrice > tickSize * TicksFromOpen;

		var seconds = SecondsSince(cond, ResetOnNewBar, candle.CloseTime);

		if (seconds > SecondsCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!cond && Position > 0)
		{
			SellMarket(Position);
		}
	}

	private int SecondsSince(bool cond, bool resetCond, DateTimeOffset currentTime)
	{
		if (resetCond)
		{
			_timeBegin = cond ? currentTime : null;
		}
		else if (cond)
		{
			if (!_lastCond)
				_timeBegin = currentTime;
		}
		else
		{
			_timeBegin = null;
		}

		_lastCond = cond;

		return _timeBegin.HasValue ? (int)(currentTime - _timeBegin.Value).TotalSeconds : 0;
	}
}