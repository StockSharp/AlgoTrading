using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Accumulation/Distribution (A/D) Strategy.
/// Long entry: A/D rising and price above MA.
/// Short entry: A/D falling and price below MA.
/// </summary>
public class ADStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _previousADValue;
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
	/// Initialize <see cref="ADStrategy"/>.
	/// </summary>
	public ADStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average", "Indicators")
			.SetOptimize(10, 50, 10);

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
		_previousADValue = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousADValue = 0;
		_cooldown = 0;

		var ma = new SimpleMovingAverage { Length = MAPeriod };
		var ad = new AccumulationDistributionLine();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ad, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal adValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_previousADValue == 0)
		{
			_previousADValue = adValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_previousADValue = adValue;
			return;
		}

		var adRising = adValue > _previousADValue;

		if (Position == 0)
		{
			if (adRising && candle.ClosePrice > maValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (!adRising && candle.ClosePrice < maValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && !adRising)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && adRising)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_previousADValue = adValue;
	}
}
