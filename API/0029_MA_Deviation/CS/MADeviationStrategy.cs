using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades when price deviates significantly from its moving average.
/// Opens positions when price deviates by a specified percentage from MA
/// and closes when price returns to MA.
/// </summary>
public class MADeviationStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _deviationPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;

	/// <summary>
	/// Period for Moving Average calculation.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Deviation percentage from MA required for entry.
	/// </summary>
	public decimal DeviationPercent
	{
		get => _deviationPercent.Value;
		set => _deviationPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
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
	/// Initialize the MA Deviation strategy.
	/// </summary>
	public MADeviationStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 5);

		_deviationPercent = Param(nameof(DeviationPercent), 2m)
			.SetDisplay("Deviation %", "Deviation percentage from MA required for entry", "Entry")
			.SetOptimize(1m, 10m, 1m);

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
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (maValue == 0)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var deviation = (candle.ClosePrice - maValue) / maValue * 100;

		if (Position == 0)
		{
			// Price far below MA -> buy (expect reversion up)
			if (deviation < -DeviationPercent)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			// Price far above MA -> sell (expect reversion down)
			else if (deviation > DeviationPercent)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			// Exit long when price returns to or above MA
			if (candle.ClosePrice >= maValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			// Exit short when price returns to or below MA
			if (candle.ClosePrice <= maValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
	}
}
