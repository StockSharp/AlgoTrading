using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that trades in the direction of a rising or falling adaptive moving average when price extends beyond an ATR buffer.
/// </summary>
public class AdaptiveEmaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _fast;
	private readonly StrategyParam<int> _slow;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _breakoutAtrMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private KaufmanAdaptiveMovingAverage _adaptiveEma;
	private AverageTrueRange _atr;
	private decimal _previousAdaptiveEmaValue;
	private bool _isInitialized;
	private int _cooldown;

	/// <summary>
	/// Fast period for KAMA smoothing.
	/// </summary>
	public int Fast
	{
		get => _fast.Value;
		set => _fast.Value = value;
	}

	/// <summary>
	/// Slow period for KAMA smoothing.
	/// </summary>
	public int Slow
	{
		get => _slow.Value;
		set => _slow.Value = value;
	}

	/// <summary>
	/// Main lookback period for KAMA.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Minimum ATR multiple required above or below KAMA for entry.
	/// </summary>
	public decimal BreakoutAtrMultiplier
	{
		get => _breakoutAtrMultiplier.Value;
		set => _breakoutAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public AdaptiveEmaBreakoutStrategy()
	{
		_fast = Param(nameof(Fast), 2)
			.SetRange(1, 20)
			.SetDisplay("Fast Period", "Fast period for KAMA smoothing", "KAMA");

		_slow = Param(nameof(Slow), 30)
			.SetRange(5, 100)
			.SetDisplay("Slow Period", "Slow period for KAMA smoothing", "KAMA");

		_lookback = Param(nameof(Lookback), 10)
			.SetRange(2, 100)
			.SetDisplay("Lookback", "Main lookback period for KAMA", "KAMA");

		_breakoutAtrMultiplier = Param(nameof(BreakoutAtrMultiplier), 0.75m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Breakout ATR", "ATR multiple required for entry", "Signals");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 72)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for the strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_adaptiveEma = null;
		_atr = null;
		_previousAdaptiveEmaValue = 0m;
		_isInitialized = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_adaptiveEma = new KaufmanAdaptiveMovingAverage
		{
			Length = Lookback,
			FastSCPeriod = Fast,
			SlowSCPeriod = Slow,
		};
		_atr = new AverageTrueRange { Length = 14 };
		_cooldown = 0;
		_isInitialized = false;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_adaptiveEma, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adaptiveEma);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal adaptiveEmaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_adaptiveEma.IsFormed || !_atr.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (!_isInitialized)
		{
			_previousAdaptiveEmaValue = adaptiveEmaValue;
			_isInitialized = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_previousAdaptiveEmaValue = adaptiveEmaValue;
			return;
		}

		var isTrendUp = adaptiveEmaValue > _previousAdaptiveEmaValue;
		var isTrendDown = adaptiveEmaValue < _previousAdaptiveEmaValue;
		var breakoutDistance = candle.ClosePrice - adaptiveEmaValue;
		var requiredDistance = atrValue * BreakoutAtrMultiplier;

		if (Position == 0)
		{
			if (isTrendUp && breakoutDistance >= requiredDistance)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (isTrendDown && breakoutDistance <= -requiredDistance)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (candle.ClosePrice <= adaptiveEmaValue || isTrendDown)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice >= adaptiveEmaValue || isTrendUp)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}

		_previousAdaptiveEmaValue = adaptiveEmaValue;
	}
}
