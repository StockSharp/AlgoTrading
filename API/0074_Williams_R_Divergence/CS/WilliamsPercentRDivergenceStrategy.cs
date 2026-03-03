using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R Divergence strategy.
/// Detects divergences between price and Williams %R for reversal signals.
/// Bullish: price falling but Williams %R rising (oversold zone).
/// Bearish: price rising but Williams %R falling (overbought zone).
/// </summary>
public class WilliamsPercentRDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevPrice;
	private decimal _prevWR;
	private int _cooldown;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
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
	public WilliamsPercentRDivergenceStrategy()
	{
		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators");

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
		_prevPrice = default;
		_prevWR = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPrice = 0;
		_prevWR = 0;
		_cooldown = 0;

		var williamsR = new WilliamsR { Length = WilliamsRPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(williamsR, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, williamsR);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevPrice == 0)
		{
			_prevPrice = candle.ClosePrice;
			_prevWR = wrValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrice = candle.ClosePrice;
			_prevWR = wrValue;
			return;
		}

		// Bullish divergence: price lower but WR higher (in oversold zone)
		var bullishDiv = candle.ClosePrice < _prevPrice && wrValue > _prevWR;
		// Bearish divergence: price higher but WR lower (in overbought zone)
		var bearishDiv = candle.ClosePrice > _prevPrice && wrValue < _prevWR;

		if (Position == 0 && bullishDiv && wrValue < -80)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && bearishDiv && wrValue > -20)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && wrValue > -20)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && wrValue < -80)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevPrice = candle.ClosePrice;
		_prevWR = wrValue;
	}
}
