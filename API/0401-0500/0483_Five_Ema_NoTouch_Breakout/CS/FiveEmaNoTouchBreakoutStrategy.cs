using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 5 EMA No-Touch Breakout Strategy - waits for a candle completely above or below the EMA and enters on breakout.
/// </summary>
public class FiveEmaNoTouchBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _rewardRisk;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;

	private decimal? _pendingLongHigh;
	private decimal? _pendingLongLow;
	private decimal? _pendingShortLow;
	private decimal? _pendingShortHigh;
	private bool _longReady;
	private bool _shortReady;

	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;
	private int _cooldownRemaining;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Reward to risk ratio.
	/// </summary>
	public decimal RewardRisk
	{
		get => _rewardRisk.Value;
		set => _rewardRisk.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FiveEmaNoTouchBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of EMA", "EMA")
			
			.SetOptimize(3, 10, 1);

		_rewardRisk = Param(nameof(RewardRisk), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Reward : Risk", "Reward to risk ratio", "Risk Management")
			
			.SetOptimize(1.0m, 5.0m, 0.5m);

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
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

		_pendingLongHigh = null;
		_pendingLongLow = null;
		_pendingShortLow = null;
		_pendingShortHigh = null;
		_longReady = false;
		_shortReady = false;
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || !_ema.IsFormed)
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		if (high < emaValue)
		{
			_pendingLongHigh = high;
			_pendingLongLow = low;
			_longReady = true;
			_shortReady = false;
		}
		else if (low > emaValue)
		{
			_pendingShortLow = low;
			_pendingShortHigh = high;
			_shortReady = true;
			_longReady = false;
		}

		// Check stop/target exits (always, regardless of cooldown)
		if (Position > 0 && _longStop is decimal ls && _longTarget is decimal lt)
		{
			if (low <= ls || high >= lt)
			{
				SellMarket(Math.Abs(Position));
				_longStop = null;
				_longTarget = null;
				_cooldownRemaining = CooldownBars;
				return;
			}
		}
		else if (Position < 0 && _shortStop is decimal ss && _shortTarget is decimal st)
		{
			if (high >= ss || low <= st)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = null;
				_shortTarget = null;
				_cooldownRemaining = CooldownBars;
				return;
			}
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		if (_longReady && _pendingLongHigh is decimal longHigh && high > longHigh)
		{
			if (_pendingLongLow is decimal longLow)
			{
				_longStop = longLow;
				_longTarget = close + (close - longLow) * RewardRisk;
				BuyMarket(Volume + Math.Abs(Position));
				_longReady = false;
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (_shortReady && _pendingShortLow is decimal shortLow && low < shortLow)
		{
			if (_pendingShortHigh is decimal shortHigh)
			{
				_shortStop = shortHigh;
				_shortTarget = close - (shortHigh - close) * RewardRisk;
				SellMarket(Volume + Math.Abs(Position));
				_shortReady = false;
				_cooldownRemaining = CooldownBars;
			}
		}
	}
}
