using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Three-Bar Reversal Down strategy.
/// Pattern: 1st bar bullish, 2nd bar bullish with higher high, 3rd bar bearish closing below 2nd low.
/// Uses SMA for exit.
/// </summary>
public class ThreeBarReversalDownStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private ICandleMessage _bar1;
	private ICandleMessage _bar2;
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ThreeBarReversalDownStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators");

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
		_bar1 = null;
		_bar2 = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bar1 = null;
		_bar2 = null;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			_bar1 = _bar2;
			_bar2 = candle;
			return;
		}

		if (_bar1 != null && _bar2 != null)
		{
			// Three-bar reversal down
			var bar1Bullish = _bar1.ClosePrice > _bar1.OpenPrice;
			var bar2Bullish = _bar2.ClosePrice > _bar2.OpenPrice;
			var bar2HigherHigh = _bar2.HighPrice > _bar1.HighPrice;
			var bar3Bearish = candle.ClosePrice < candle.OpenPrice;
			var bar3BelowBar2Low = candle.ClosePrice < _bar2.LowPrice;

			var threeBarDown = bar1Bullish && bar2Bullish && bar2HigherHigh && bar3Bearish && bar3BelowBar2Low;

			// Three-bar reversal up
			var bar1Bearish = _bar1.ClosePrice < _bar1.OpenPrice;
			var bar2Bearish = _bar2.ClosePrice < _bar2.OpenPrice;
			var bar2LowerLow = _bar2.LowPrice < _bar1.LowPrice;
			var bar3Bullish = candle.ClosePrice > candle.OpenPrice;
			var bar3AboveBar2High = candle.ClosePrice > _bar2.HighPrice;

			var threeBarUp = bar1Bearish && bar2Bearish && bar2LowerLow && bar3Bullish && bar3AboveBar2High;

			if (Position == 0 && threeBarDown)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (Position == 0 && threeBarUp)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (Position > 0 && candle.ClosePrice < smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (Position < 0 && candle.ClosePrice > smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		_bar1 = _bar2;
		_bar2 = candle;
	}
}
