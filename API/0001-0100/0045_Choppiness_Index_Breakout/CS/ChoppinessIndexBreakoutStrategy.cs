using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Choppiness Index Breakout strategy.
/// Enters when market transitions from choppy to trending state.
/// </summary>
public class ChoppinessIndexBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _choppinessPeriod;
	private readonly StrategyParam<decimal> _choppinessThreshold;
	private readonly StrategyParam<decimal> _highChoppinessThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevChoppiness;
	private int _cooldown;

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Choppiness Index Period.
	/// </summary>
	public int ChoppinessPeriod
	{
		get => _choppinessPeriod.Value;
		set => _choppinessPeriod.Value = value;
	}

	/// <summary>
	/// Choppiness Threshold (low = trending).
	/// </summary>
	public decimal ChoppinessThreshold
	{
		get => _choppinessThreshold.Value;
		set => _choppinessThreshold.Value = value;
	}

	/// <summary>
	/// High Choppiness Threshold (for exit).
	/// </summary>
	public decimal HighChoppinessThreshold
	{
		get => _highChoppinessThreshold.Value;
		set => _highChoppinessThreshold.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize the Choppiness Index Breakout strategy.
	/// </summary>
	public ChoppinessIndexBreakoutStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 10);

		_choppinessPeriod = Param(nameof(ChoppinessPeriod), 14)
			.SetDisplay("Choppiness Period", "Period for Choppiness Index calculation", "Indicators")
			.SetOptimize(10, 30, 5);

		_choppinessThreshold = Param(nameof(ChoppinessThreshold), 99m)
			.SetDisplay("Choppiness Threshold", "Threshold below which market is trending", "Entry")
			.SetOptimize(90m, 100m, 1m);

		_highChoppinessThreshold = Param(nameof(HighChoppinessThreshold), 99.5m)
			.SetDisplay("High Choppiness", "Threshold above which to exit positions", "Exit")
			.SetOptimize(95m, 100m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevChoppiness = 100m;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevChoppiness = 100m;
		_cooldown = 0;

		var ma = new SimpleMovingAverage { Length = MAPeriod };
		var choppinessIndex = new ChoppinessIndex { Length = ChoppinessPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, choppinessIndex, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal choppinessValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevChoppiness = choppinessValue;
			return;
		}

		var isTrending = choppinessValue < ChoppinessThreshold;
		var isChoppy = choppinessValue > HighChoppinessThreshold;

		if (Position == 0 && isTrending)
		{
			if (candle.ClosePrice > maValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (candle.ClosePrice < maValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && isChoppy)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && isChoppy)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevChoppiness = choppinessValue;
	}
}
